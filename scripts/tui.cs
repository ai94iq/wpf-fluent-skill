#:property PublishAot=false
// Keyboard menu for the skill's tasks without an agent: scaffold a project, add
// icons, run a project's build/test/run/package/logs/clean scripts, page through
// the reference guides, and check the skill repository.
// Run through tui.bat (Windows) or tui.sh (macOS/Linux/Git Bash), or directly:
// dotnet run scripts/tui.cs -- <repoRoot> [--help]
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

var repo = FindRepoRoot(args.Length > 0 ? args[0] : null);
var help = args.Contains("--help") || args.Contains("-h");
if (repo is null)
{
    if (help)
    {
        PrintCommands(Environment.CurrentDirectory, noTerminal: false);
        return 0;
    }
    Console.Error.WriteLine("[tui] FAILED: could not find the skill repository (SKILL.md and assets/template).");
    Console.Error.WriteLine("      Run through scripts\\tui.bat or scripts/tui.sh, or pass the repository path.");
    return 1;
}
if (help)
{
    PrintCommands(repo, noTerminal: false);
    return 0;
}
if (!Ui.CanInteract)
{
    PrintCommands(repo, noTerminal: true);
    return 0;
}

try
{
    Console.OutputEncoding = Encoding.UTF8;
}
catch (Exception)
{
}

return new App(repo).Run();

static string? FindRepoRoot(string? start)
{
    foreach (var candidate in new[] { start, Environment.CurrentDirectory })
    {
        if (string.IsNullOrWhiteSpace(candidate)) continue;
        string? dir;
        try { dir = Path.GetFullPath(candidate); }
        catch (Exception) { continue; }
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "SKILL.md")) && Directory.Exists(Path.Combine(dir, "assets", "template")))
                return dir;
            dir = Path.GetDirectoryName(dir);
        }
    }
    return null;
}

static void PrintCommands(string repo, bool noTerminal)
{
    void Item(string name, string command) => Console.WriteLine($"  {name.PadRight(14)} {command}");

    Console.WriteLine("win-desktop-fluent — keyboard menu");
    Console.WriteLine();
    if (noTerminal)
        Console.WriteLine("No interactive terminal was detected. Run tui.bat or tui.sh from a terminal, or use these commands:");
    else
        Console.WriteLine("Usage: scripts\\tui.bat | scripts/tui.sh  (or: dotnet run scripts/tui.cs -- <repoRoot>)");
    Console.WriteLine();
    Console.WriteLine($"Repository: {repo}");
    Console.WriteLine();
    if (OperatingSystem.IsWindows())
    {
        Item("New project", @"scripts\new-project.bat MyApp ""My Company"" C:\Projects\MyApp [--ui wpf|winui] [--no-build]");
        Item("Add icons", @"scripts\add-icons.bat C:\Projects\MyApp person_add filter");
        Item("Project tools", "run build.bat, test.bat, run.bat, package.bat, logs.bat or clean.bat in the project folder");
        Item("Guides", @"references\*.md");
        Item("Check skill", @"scripts\check-skill.bat");
    }
    else
    {
        Item("New project", @"dotnet run scripts/new-project.cs -- assets/template MyApp ""My Company"" ~/Projects/MyApp [--ui wpf|winui] [--no-build]");
        Item("Add icons", "dotnet run scripts/add-icons.cs -- ~/Projects/MyApp person_add filter");
        Item("Project tools", "the generated project's .bat scripts need Windows");
        Item("Guides", "references/*.md");
        Item("Check skill", "dotnet run scripts/check-skill.cs -- .");
    }
    Console.WriteLine();
}

enum Style
{
    Normal,
    Muted,
    Title,
    Selected,
    Error,
    Success,
    Heading,
}

readonly record struct Segment(string Text, Style Kind);

sealed class Line
{
    readonly List<Segment> _segments = [];
    Style? _fill;

    public static Line Blank => new();

    public Line Add(string text, Style style = Style.Normal)
    {
        if (text.Length > 0) _segments.Add(new Segment(text, style));
        return this;
    }

    public Line Fill(Style style)
    {
        _fill = style;
        return this;
    }

    public void Write(int width)
    {
        var remaining = width;
        foreach (var segment in _segments)
        {
            if (remaining <= 0) return;
            var text = segment.Text.Length <= remaining ? segment.Text : segment.Text[..remaining];
            Ui.Apply(segment.Kind);
            Console.Write(text);
            remaining -= text.Length;
        }
        if (remaining <= 0) return;
        Ui.Apply(_fill ?? Style.Normal);
        Console.Write(new string(' ', remaining));
    }
}

static class Ui
{
    static int _width = -1;
    static int _height = -1;
    static bool _dirty;

    public static bool CanInteract => !Console.IsInputRedirected && !Console.IsOutputRedirected;

    public static bool Colors => string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR"));

    public static void Init()
    {
        try { Console.Title = "win-desktop-fluent"; } catch (Exception) { }
        try { Console.CursorVisible = false; } catch (Exception) { }
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            Shutdown();
            Environment.Exit(0);
        };
    }

    public static void Shutdown()
    {
        try { Console.ResetColor(); } catch (Exception) { }
        try { Console.CursorVisible = true; } catch (Exception) { }
        try { Console.Clear(); } catch (Exception) { }
        Console.WriteLine("win-desktop-fluent — menu closed.");
    }

    public static void Apply(Style style)
    {
        if (!Colors) return;
        var (foreground, background) = style switch
        {
            Style.Muted => (ConsoleColor.DarkGray, ConsoleColor.Black),
            Style.Title => (ConsoleColor.Cyan, ConsoleColor.Black),
            Style.Selected => (ConsoleColor.Black, ConsoleColor.Cyan),
            Style.Error => (ConsoleColor.Red, ConsoleColor.Black),
            Style.Success => (ConsoleColor.Green, ConsoleColor.Black),
            Style.Heading => (ConsoleColor.Cyan, ConsoleColor.Black),
            _ => (ConsoleColor.Gray, ConsoleColor.Black),
        };
        Console.ForegroundColor = foreground;
        Console.BackgroundColor = background;
    }

    public static void Draw(string title, string? subtitle, IReadOnlyList<Line> body, string footer)
    {
        var width = Width();
        var height = Height();
        if (width != _width || height != _height)
        {
            _width = width;
            _height = height;
            ClearScreen();
        }
        if (_dirty)
        {
            _dirty = false;
            ClearScreen();
        }

        var lines = new List<Line>(height);
        lines.Add(new Line().Add(" " + title, Style.Title));
        if (!string.IsNullOrEmpty(subtitle)) lines.Add(new Line().Add(" " + subtitle, Style.Muted));
        lines.Add(Line.Blank);
        lines.AddRange(body);

        var footerRow = Math.Max(0, height - 1);
        if (lines.Count > footerRow) lines.RemoveRange(footerRow, lines.Count - footerRow);
        while (lines.Count < footerRow) lines.Add(Line.Blank);
        lines.Add(new Line().Add(" " + footer, Style.Muted));

        for (var row = 0; row < lines.Count && row < height; row++)
        {
            try { Console.SetCursorPosition(0, row); }
            catch (Exception) { _dirty = true; return; }
            lines[row].Write(width);
        }
        if (Colors) Console.ResetColor();
    }

    public static int Menu(string title, string? subtitle, IReadOnlyList<MenuItem> items, string footer, int initial = 0, IReadOnlyList<Line>? details = null)
    {
        var index = Math.Clamp(initial, 0, items.Count - 1);
        var labelWidth = items.Max(item => item.Label.Length);
        while (true)
        {
            var body = new List<Line>();
            if (details is not null) body.AddRange(details);
            for (var i = 0; i < items.Count; i++)
            {
                var selected = i == index;
                var line = new Line();
                line.Add(selected ? "  > " : "    ", selected ? Style.Selected : Style.Normal);
                line.Add(i < 9 ? $"{i + 1}. " : "    ", selected ? Style.Selected : Style.Muted);
                line.Add(items[i].Label.PadRight(labelWidth), selected ? Style.Selected : Style.Normal);
                if (items[i].Description.Length > 0)
                    line.Add("   " + items[i].Description, selected ? Style.Selected : Style.Muted);
                line.Fill(selected ? Style.Selected : Style.Normal);
                body.Add(line);
            }
            Draw(title, subtitle, body, footer);
            var key = Console.ReadKey(true);
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    index = (index + items.Count - 1) % items.Count;
                    break;
                case ConsoleKey.DownArrow:
                    index = (index + 1) % items.Count;
                    break;
                case ConsoleKey.Home:
                    index = 0;
                    break;
                case ConsoleKey.End:
                    index = items.Count - 1;
                    break;
                case ConsoleKey.Enter:
                    return index;
                case ConsoleKey.Escape:
                case ConsoleKey.Q:
                    return -1;
                default:
                    if (key.KeyChar is >= '1' and <= '9' && key.KeyChar - '1' < items.Count)
                        return key.KeyChar - '1';
                    break;
            }
        }
    }

    public static string? Prompt(string title, string? subtitle, string label, string initial, string hint, Func<string, string?> validate)
    {
        var value = initial ?? string.Empty;
        var cursor = value.Length;
        string? error = null;
        while (true)
        {
            var body = new List<Line>
            {
                new Line().Add("  " + label, Style.Normal),
                InputLine(value, cursor),
                Line.Blank,
                error is null ? new Line().Add("  " + hint, Style.Muted) : new Line().Add("  " + error, Style.Error),
            };
            Draw(title, subtitle, body, "Enter confirm · Esc cancel · ←/→ Home/End edit · Backspace delete");
            var key = Console.ReadKey(true);
            switch (key.Key)
            {
                case ConsoleKey.Enter:
                {
                    var clean = value.Trim().Trim('"');
                    error = validate(clean);
                    if (error is null) return clean;
                    break;
                }
                case ConsoleKey.Escape:
                    return null;
                case ConsoleKey.Backspace:
                    if (cursor > 0)
                    {
                        value = value.Remove(cursor - 1, 1);
                        cursor--;
                    }
                    error = null;
                    break;
                case ConsoleKey.Delete:
                    if (cursor < value.Length) value = value.Remove(cursor, 1);
                    error = null;
                    break;
                case ConsoleKey.LeftArrow:
                    if (cursor > 0) cursor--;
                    break;
                case ConsoleKey.RightArrow:
                    if (cursor < value.Length) cursor++;
                    break;
                case ConsoleKey.Home:
                    cursor = 0;
                    break;
                case ConsoleKey.End:
                    cursor = value.Length;
                    break;
                default:
                    if (!char.IsControl(key.KeyChar))
                    {
                        value = value.Insert(cursor, key.KeyChar.ToString());
                        cursor++;
                        error = null;
                    }
                    break;
            }
        }
    }

    static Line InputLine(string value, int cursor)
    {
        var capacity = Math.Max(8, Width() - 6);
        var start = Math.Clamp(Math.Max(0, cursor - capacity), 0, Math.Max(0, value.Length - capacity));
        var visible = value.Substring(start, Math.Min(capacity, value.Length - start));
        var caret = cursor - start;
        var text = visible[..caret] + "▏" + visible[caret..];
        return new Line().Add("  ", Style.Normal).Add(text, value.Length == 0 ? Style.Muted : Style.Normal);
    }

    public static void Message(string title, string? subtitle, string text, Style style = Style.Normal)
    {
        Draw(title, subtitle, [new Line().Add("  " + text, style)], "Press Enter to continue");
        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.Key is ConsoleKey.Enter or ConsoleKey.Escape or ConsoleKey.Spacebar) return;
        }
    }

    public static void View(string title, string subtitle, IReadOnlyList<string> lines)
    {
        if (lines.Count == 0)
        {
            Message(title, subtitle, "This file is empty.", Style.Muted);
            return;
        }
        var offset = 0;
        while (true)
        {
            var page = Math.Max(1, Height() - 4);
            offset = Math.Clamp(offset, 0, Math.Max(0, lines.Count - 1));
            var last = Math.Min(lines.Count, offset + page);
            var body = new List<Line>();
            for (var i = offset; i < last; i++) body.Add(GuideLine(lines[i]));
            Draw(title, $"{subtitle}  ({offset + 1}-{last} of {lines.Count})", body, "↑/↓ line · PgUp/PgDn page · Home/End · Esc back");
            var key = Console.ReadKey(true);
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    offset--;
                    break;
                case ConsoleKey.DownArrow:
                    offset++;
                    break;
                case ConsoleKey.PageUp:
                    offset -= page;
                    break;
                case ConsoleKey.PageDown:
                    offset += page;
                    break;
                case ConsoleKey.Home:
                    offset = 0;
                    break;
                case ConsoleKey.End:
                    offset = lines.Count - 1;
                    break;
                case ConsoleKey.Escape:
                case ConsoleKey.Q:
                    return;
            }
        }
    }

    static Line GuideLine(string text)
    {
        if (text.StartsWith('#')) return new Line().Add(text, Style.Heading);
        if (text.StartsWith("```")) return new Line().Add(text, Style.Muted);
        return new Line().Add(text, Style.Normal);
    }

    public static int RunExternal(string what, string file, IReadOnlyList<string> arguments, string workDirectory)
    {
        try { Console.ResetColor(); } catch (Exception) { }
        ClearScreen();
        Console.WriteLine($"» {what}");
        Console.WriteLine($"  {file} {string.Join(' ', arguments)}");
        Console.WriteLine();

        var exit = 1;
        try
        {
            var info = new ProcessStartInfo(file) { WorkingDirectory = workDirectory, UseShellExecute = false };
            foreach (var argument in arguments) info.ArgumentList.Add(argument);
            using var process = Process.Start(info) ?? throw new InvalidOperationException($"Could not start {file}.");
            process.WaitForExit();
            exit = process.ExitCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"[tui] FAILED: {ex.Message}");
        }

        Console.WriteLine();
        Console.WriteLine(exit == 0 ? "[tui] OK" : $"[tui] FAILED (exit code {exit}) — the details are above.");
        Pause("Press Enter to return to the menu.");
        _dirty = true;
        return exit;
    }

    static void Pause(string message)
    {
        Console.WriteLine();
        Console.Write($"[tui] {message}");
        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.Key is ConsoleKey.Enter or ConsoleKey.Escape or ConsoleKey.Spacebar)
            {
                Console.WriteLine();
                return;
            }
        }
    }

    static int Width()
    {
        try { return Math.Clamp(Console.WindowWidth - 1, 20, 500); }
        catch (Exception) { return 99; }
    }

    static int Height()
    {
        try { return Math.Clamp(Console.WindowHeight, 8, 200); }
        catch (Exception) { return 24; }
    }

    static void ClearScreen()
    {
        try { Console.Clear(); } catch (Exception) { }
        if (OperatingSystem.IsWindows())
        {
            try { Console.SetWindowPosition(0, 0); } catch (Exception) { }
        }
    }
}

sealed record MenuItem(string Label, string Description);

sealed class Settings
{
    public string? Company { get; set; }
    public string? ProjectsRoot { get; set; }
    public string? LastProject { get; set; }
    public string? LastUi { get; set; }

    static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "win-desktop-fluent", "tui.json");

    public static Settings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath)) ?? new Settings();
        }
        catch (Exception)
        {
        }
        return new Settings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception)
        {
        }
    }
}

sealed class App
{
    const string ProductPattern = "^[A-Z][A-Za-z0-9]{1,40}$";
    const string CompanyPattern = @"^[\p{L}\p{N} .\-]{1,60}$";

    readonly string _repo;
    readonly Settings _settings;

    public App(string repo)
    {
        _repo = repo;
        _settings = Settings.Load();
    }

    public int Run()
    {
        Ui.Init();
        try
        {
            while (true)
            {
                var choice = Ui.Menu(
                    "win-desktop-fluent",
                    "Do the skill's tasks from a keyboard — no agent needed.",
                    [
                        new MenuItem("New project", "Scaffold a WPF or WinUI 3 solution from the template"),
                        new MenuItem("Add icons", "Download Fluent outline icons for an existing project"),
                        new MenuItem("Project tools", "Build, test, run, package, inspect logs, clean"),
                        new MenuItem("Read the guides", "The same references an agent reads"),
                        new MenuItem("Check the skill", "Formatting and size budgets of this repository"),
                        new MenuItem("Quit", "Close the menu"),
                    ],
                    "↑/↓ move · Enter select · 1-6 jump · Esc or Q quit");
                if (choice is -1 or 5) return 0;
                switch (choice)
                {
                    case 0: NewProject(); break;
                    case 1: AddIcons(); break;
                    case 2: ProjectTools(); break;
                    case 3: Guides(); break;
                    case 4: CheckSkill(); break;
                }
            }
        }
        finally
        {
            Ui.Shutdown();
        }
    }

    void NewProject()
    {
        const string Title = "New project";
        const string Subtitle = "Scaffold a complete project from the skill template.";

        var product = Ui.Prompt(Title, Subtitle, "App name (PascalCase, e.g. Tasks)", "",
            "The name of the solution, the projects and the app folders.",
            value => Regex.IsMatch(value, ProductPattern)
                ? null
                : "Use a capital letter followed by letters and digits (2-41 characters), e.g. MyApp.");
        if (product is null) return;

        var company = Ui.Prompt(Title, Subtitle, "Company name", _settings.Company ?? "",
            "Goes into the app metadata and the installer.",
            value => Regex.IsMatch(value, CompanyPattern)
                ? null
                : "Letters, digits, spaces, dots and hyphens only (1-60 characters).");
        if (company is null) return;

        var target = Ui.Prompt(Title, Subtitle, "Folder for the new project", DefaultTarget(product),
            "The folder is created and must be empty; parent folders are made too.", ValidateTarget);
        if (target is null) return;

        var uiChoice = Ui.Menu(Title, Subtitle + " Choose the UI technology.",
            [
                new MenuItem("WPF + WPF-UI", "The default, works on Windows 10 and 11"),
                new MenuItem("WinUI 3", "Windows App SDK 1.x, unpackaged"),
            ],
            "↑/↓ choose · Enter select · Esc cancel",
            _settings.LastUi == "winui" ? 1 : 0);
        if (uiChoice < 0) return;
        var ui = uiChoice == 1 ? "winui" : "wpf";
        var fullTarget = Path.GetFullPath(target);

        var details = new List<Line>
        {
            new Line().Add("  App       ", Style.Muted).Add(product, Style.Normal),
            new Line().Add("  Company   ", Style.Muted).Add(company, Style.Normal),
            new Line().Add("  Folder    ", Style.Muted).Add(fullTarget, Style.Normal),
            new Line().Add("  UI        ", Style.Muted).Add(ui == "wpf" ? "WPF + WPF-UI 4.x" : "WinUI 3", Style.Normal),
            Line.Blank,
        };

        bool build;
        if (OperatingSystem.IsWindows())
        {
            var finish = Ui.Menu(Title + " — confirm", null,
                [
                    new MenuItem("Create, build and test", "Runs the tests once, then makes the first commit (a few minutes)"),
                    new MenuItem("Create only", "Skip the first build; run test.bat later"),
                    new MenuItem("Back", "Change something"),
                ],
                "↑/↓ choose · Enter select · Esc cancel", 0, details);
            if (finish is 2 or -1) return;
            build = finish == 0;
        }
        else
        {
            var finish = Ui.Menu(Title + " — confirm", "This is not Windows; the first build must wait for a Windows machine.",
                [
                    new MenuItem("Create the project", "Scaffold without building"),
                    new MenuItem("Back", "Change something"),
                ],
                "↑/↓ choose · Enter select · Esc cancel", 0, details);
            if (finish is 1 or -1) return;
            build = false;
        }

        var arguments = new List<string> { Path.Combine(_repo, "assets", "template"), product, company, fullTarget, "--ui", ui };
        if (!build) arguments.Add("--no-build");
        var exit = Ui.RunExternal("Scaffolding the project (the first restore takes a minute)", "dotnet",
            ["run", Path.Combine(_repo, "scripts", "new-project.cs"), "--", .. arguments], _repo);
        if (exit != 0) return;

        _settings.Company = company;
        _settings.ProjectsRoot = Path.GetDirectoryName(fullTarget);
        _settings.LastProject = fullTarget;
        _settings.LastUi = ui;
        _settings.Save();
    }

    void AddIcons()
    {
        const string Title = "Add icons";
        const string Subtitle = @"Downloads Fluent UI System Icons (24px outline, MIT) and regenerates Resources\Icons.g.cs.";

        var root = Ui.Prompt(Title, Subtitle, "Project folder", _settings.LastProject ?? "",
            @"The folder created by New project (it contains src\<App>.App).", ValidateProjectRoot);
        if (root is null) return;

        var current = ReadIconList(root);
        var hint = current.Count > 0
            ? $"Already added: {string.Join(", ", current)}. Leave empty to rebuild from this list."
            : "Find names at github.com/microsoft/fluentui-system-icons — the 'Person Add' folder is person_add.";
        var names = Ui.Prompt(Title, Subtitle, "New icon names (spaces or commas)", "", hint, ValidateIconNames);
        if (names is null) return;

        var added = SplitNames(names);
        if (added.Count == 0 && current.Count == 0)
        {
            Ui.Message(Title, null, "Enter at least one icon name, e.g. person_add filter.", Style.Error);
            return;
        }

        var arguments = new List<string> { root };
        arguments.AddRange(added);
        var exit = Ui.RunExternal("Adding icons", "dotnet",
            ["run", Path.Combine(_repo, "scripts", "add-icons.cs"), "--", .. arguments], _repo);
        if (exit == 0)
        {
            _settings.LastProject = root;
            _settings.Save();
        }
    }

    void ProjectTools()
    {
        const string Title = "Project tools";
        var root = Ui.Prompt(Title, "Run the repo scripts of a project created by this skill.",
            "Project folder", _settings.LastProject ?? "", @"The folder that contains <App>.slnx.", ValidateExistingProject);
        if (root is null) return;
        root = Path.GetFullPath(root);
        _settings.LastProject = root;
        _settings.Save();

        while (true)
        {
            var name = Path.GetFileName(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            List<Line>? details = OperatingSystem.IsWindows()
                ? null
                : [new Line().Add("  These project scripts are Windows .bat files; they do not run on this OS.", Style.Error), Line.Blank];
            var choice = Ui.Menu($"Project tools — {name}", null,
                [
                    new MenuItem("Build", "Self-contained win-x64 publish (build.bat)"),
                    new MenuItem("Test", "Run every test (test.bat)"),
                    new MenuItem("Run", "Open the app; close its window to come back (run.bat)"),
                    new MenuItem("Package", "Test, build and create the MSI (package.bat)"),
                    new MenuItem("Logs", "Read the last build, test or package output"),
                    new MenuItem("Clean", "Delete bin, obj and artifacts (clean.bat)"),
                    new MenuItem("Back", "Choose another project"),
                ],
                "↑/↓ move · Enter select · Esc back", 0, details);
            switch (choice)
            {
                case -1 or 6: return;
                case 0: RunBatch(root, "build.bat", "Build", "--no-pause"); break;
                case 1: RunBatch(root, "test.bat", "Test", "--no-pause"); break;
                case 2: RunBatch(root, "run.bat", "Run", "--no-pause"); break;
                case 3: RunBatch(root, "package.bat", "Package", "--no-pause"); break;
                case 4: Logs(root); break;
                case 5: RunBatch(root, "clean.bat", "Clean", "--no-pause"); break;
            }
        }
    }

    void Logs(string root)
    {
        var directory = Path.Combine(root, "artifacts", "logs");
        var logs = new List<FileInfo>();
        try
        {
            if (Directory.Exists(directory))
                logs.AddRange(new DirectoryInfo(directory).GetFiles("*.log").OrderByDescending(file => file.LastWriteTimeUtc));
        }
        catch (Exception)
        {
        }

        if (logs.Count == 0)
        {
            Ui.Message("Logs", root, "No logs yet. Build, test or package first.", Style.Muted);
            return;
        }

        var choice = Ui.Menu("Logs", root,
            logs.Select(file => new MenuItem(file.Name, $"{file.Length / 1024} KB · {file.LastWriteTime:yyyy-MM-dd HH:mm}")).ToList(),
            "↑/↓ move · Enter open · Esc back");
        if (choice < 0) return;

        var kind = Ui.Menu($"Logs — {logs[choice].Name}", null,
            [
                new MenuItem("Errors and warnings", "One line per problem, plus the log path"),
                new MenuItem("Everything", "The whole file"),
            ],
            "↑/↓ choose · Enter select · Esc back");
        if (kind < 0) return;

        RunBatch(root, "logs.bat", $"Log: {logs[choice].Name}", Path.GetFileNameWithoutExtension(logs[choice].Name), kind == 0 ? "error" : "all", "--no-pause");
    }

    void Guides()
    {
        var items = new (string Path, string Label, string Description)[]
        {
            (Path.Combine("references", "project-setup.md"), "Project setup", "New project, git, scripts and docs"),
            (Path.Combine("references", "code-patterns.md"), "Code patterns", "Startup, data, caching, testing, threading"),
            (Path.Combine("references", "localization.md"), "Localization and RTL", "Strings, dates, numbers, Arabic"),
            (Path.Combine("references", "ui-ux.md"), "UI and UX", "Screens, forms, inputs, styling, icons"),
            (Path.Combine("references", "ui-frameworks.md"), "WPF vs WinUI 3", "Which stack, WinUI-specific rules"),
            (Path.Combine("references", "installer.md"), "Installer and releases", "WiX v6 MSI"),
            ("SKILL.md", "Skill overview", "Rules, locked stack and review checklist"),
            ("README.md", "Readme", "What this repository is"),
        };
        while (true)
        {
            var choice = Ui.Menu("Read the guides", "Plain text in the terminal; the links and screenshots stay in the files.",
                items.Select(item => new MenuItem(item.Label, item.Description)).ToList(),
                "↑/↓ move · Enter open · Esc back");
            if (choice < 0) return;
            var file = Path.Combine(_repo, items[choice].Path);
            if (!File.Exists(file))
            {
                Ui.Message("Read the guides", items[choice].Label, "File not found: " + file, Style.Error);
                continue;
            }
            Ui.View(items[choice].Label, items[choice].Path.Replace('\\', '/'), File.ReadAllLines(file));
        }
    }

    void CheckSkill()
    {
        Ui.RunExternal("Checking the skill repository (line budgets and formatting)", "dotnet",
            ["run", Path.Combine(_repo, "scripts", "check-skill.cs"), "--", _repo], _repo);
    }

    void RunBatch(string root, string script, string what, params string[] extra)
    {
        if (!File.Exists(Path.Combine(root, script)))
        {
            Ui.Message(what, root, $"{script} was not found in this folder.", Style.Error);
            return;
        }
        if (!OperatingSystem.IsWindows())
        {
            Ui.Message(what, root, "This project script is a Windows .bat file. Run it on Windows.", Style.Error);
            return;
        }
        var command = string.Join(' ', new[] { script }.Concat(extra));
        Ui.RunExternal(what, Environment.GetEnvironmentVariable("COMSPEC") ?? "cmd.exe", ["/d", "/c", command], root);
    }

    string DefaultTarget(string product)
    {
        var root = string.IsNullOrWhiteSpace(_settings.ProjectsRoot)
            ? System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "source", "repos")
            : _settings.ProjectsRoot;
        return System.IO.Path.Combine(root, product);
    }

    static List<string> ReadIconList(string root)
    {
        try
        {
            var app = Directory.EnumerateDirectories(System.IO.Path.Combine(root, "src"), "*.App").FirstOrDefault();
            if (app is null) return [];
            var list = System.IO.Path.Combine(app, "Resources", "icons.txt");
            return File.Exists(list)
                ? File.ReadAllLines(list).Select(line => line.Trim()).Where(line => line.Length > 0).ToList()
                : [];
        }
        catch (Exception)
        {
            return [];
        }
    }

    static List<string> SplitNames(string value) => value
        .Split([' ', ',', '\t', ';'], StringSplitOptions.RemoveEmptyEntries)
        .Select(name => name.Trim().ToLowerInvariant())
        .Where(name => name.Length > 0)
        .Distinct(StringComparer.Ordinal)
        .Order(StringComparer.Ordinal)
        .ToList();

    static string? ValidateTarget(string value)
    {
        if (value.Length == 0) return "Enter a folder path.";
        string full;
        try { full = System.IO.Path.GetFullPath(value); }
        catch (Exception) { return "That path is not valid."; }
        try
        {
            if (Directory.Exists(full) && Directory.EnumerateFileSystemEntries(full).Any())
                return "That folder is not empty. Pick an empty or a new folder.";
        }
        catch (Exception)
        {
            return "That folder cannot be read.";
        }
        return null;
    }

    static string? ValidateProjectRoot(string value)
    {
        if (!Directory.Exists(value)) return "That folder does not exist.";
        try
        {
            if (!Directory.EnumerateDirectories(System.IO.Path.Combine(value, "src"), "*.App").Any())
                return @"No src\*.App project found in that folder.";
        }
        catch (Exception)
        {
            return "That folder does not contain src (is it a project folder?).";
        }
        return null;
    }

    static string? ValidateExistingProject(string value)
    {
        if (!Directory.Exists(value)) return "That folder does not exist.";
        try
        {
            if (!Directory.EnumerateFiles(value, "*.slnx").Any())
                return "No .slnx file found — is this a project folder created by New project?";
        }
        catch (Exception)
        {
            return "That folder cannot be read.";
        }
        return null;
    }

    static string? ValidateIconNames(string value)
    {
        foreach (var name in SplitNames(value))
        {
            if (!Regex.IsMatch(name, "^[a-z0-9_]+$"))
                return $"'{name}' is not a valid name. Use lowercase snake_case, e.g. person_add.";
        }
        return null;
    }
}
