#:property PublishAot=false
// Scaffolds a new project from the skill template. Run through new-project.bat, not directly.
// Args: <templateDir> <Product> <Company> <targetDir> [--ui wpf|winui] [--no-build]
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

const string NuGet = "https://api.nuget.org/v3-flatcontainer";
const string Fonts = "https://raw.githubusercontent.com/notofonts/notofonts.github.io/main/fonts/NotoSansArabic/hinted/ttf";
const string FontLicense = "https://raw.githubusercontent.com/notofonts/arabic/main/OFL.txt";
string[] textExtensions = [".cs", ".xaml", ".csproj", ".props", ".slnx", ".json", ".md", ".bat", ".resx", ".sql",
    ".wxs", ".wxl", ".wixproj", ".manifest", ".txt", ".rtf", ".editorconfig", ".gitattributes", ".gitignore"];

try
{
    if (args.Length < 4) return Fail("Usage: new-project.bat <Product> <Company> <TargetFolder> [--ui wpf|winui] [--no-build]");
    var templateRoot = Path.GetFullPath(args[0]);
    var (product, company, targetDir) = (args[1], args[2], Path.GetFullPath(args[3]));
    var build = !args.Contains("--no-build");
    var uiIndex = Array.IndexOf(args, "--ui");
    var ui = uiIndex > 0 && uiIndex + 1 < args.Length ? args[uiIndex + 1].ToLowerInvariant() : "wpf";
    if (ui is not ("wpf" or "winui")) return Fail("--ui must be wpf or winui.");
    string[] layers = [Path.Combine(templateRoot, "common"), Path.Combine(templateRoot, ui)];

    if (!Regex.IsMatch(product, "^[A-Z][A-Za-z0-9]{1,40}$"))
        return Fail("Product must be PascalCase letters and digits, e.g. MyApp.");
    if (!Regex.IsMatch(company, @"^[\p{L}\p{N} .\-]{1,60}$"))
        return Fail("Company may contain letters, digits, spaces, dots and hyphens only.");
    if (!layers.All(Directory.Exists)) return Fail($"Template not found under {templateRoot}");
    if (Directory.Exists(targetDir) && Directory.EnumerateFileSystemEntries(targetDir).Any())
        return Fail($"Target folder is not empty: {targetDir}");

    var sdk = Run(Environment.CurrentDirectory, capture: true, "dotnet", "--version").Trim();
    if (!sdk.StartsWith("10.", StringComparison.Ordinal)) return Fail($".NET 10 SDK required, found '{sdk}'.");

    Step(1, "Resolving latest stable package versions");
    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    var tokens = await ResolveVersionTokens(http, layers);
    tokens["__Product__"] = product;
    tokens["__Company__"] = company;
    tokens["__UpgradeCode__"] = Guid.NewGuid().ToString("D").ToUpperInvariant();
    tokens["__Date__"] = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    tokens["__Year__"] = DateTime.Now.Year.ToString(CultureInfo.InvariantCulture);
    tokens["__SdkVersion__"] = sdk;
    tokens["__UiStack__"] = ui == "wpf" ? "WPF + WPF-UI 4.x" : "WinUI 3 (Windows App SDK 1.x, unpackaged)";

    Step(2, $"Copying template ({ui})");
    foreach (var layer in layers) CopyTemplate(layer, targetDir, tokens);

    Step(3, "Downloading Noto Sans Arabic (OFL)");
    var fontsOk = await DownloadFonts(http, Path.Combine(targetDir, "src", $"{product}.App", "Assets", "Fonts"));

    Step(4, "Creating solution and git repository");
    Run(targetDir, capture: false, "git", "init", "-q", "-b", "main");
    Run(targetDir, capture: false, "git", "config", "core.hooksPath", ".githooks");   // pre-commit runs all tests
    Run(targetDir, capture: false, "dotnet", "new", "sln", "-n", product, "--format", "slnx");
    Run(targetDir, capture: false, "dotnet", "sln", $"{product}.slnx", "add",
        $"src/{product}.App", $"src/{product}.Core", $"src/{product}.Data",
        $"tests/{product}.Core.Tests", $"tests/{product}.Data.Tests", $"tests/{product}.App.Tests");

    if (build)
    {
        Step(5, "Building and testing (first restore takes a minute)");
        Run(targetDir, capture: false, "dotnet", "test", "--solution", $"{product}.slnx", "-c", "Release");
    }
    else
    {
        Step(5, "Build skipped (--no-build)");
    }

    Step(6, "First commit");
    var committed = HasGitIdentity(targetDir);
    if (committed)
    {
        Run(targetDir, capture: false, "git", "add", "-A");
        // Tests already ran in step 5, so the pre-commit hook is skipped for this one commit only.
        string[] verify = build ? ["--no-verify"] : [];
        Run(targetDir, capture: false, "git", ["commit", "-q", .. verify,
            "-m", "repo: chore: scaffold solution",
            "-m", $"Create {product}.slnx ({tokens["__UiStack__"]}) with App, Core and Data projects, three test projects and the WiX installer from the skill template. Package versions are the latest stable at scaffold time."]);
    }

    Console.WriteLine();
    Console.WriteLine($"[new-project] OK: {targetDir}");
    if (!fontsOk) Console.WriteLine($"[new-project] TODO: add NotoSansArabic-Regular/Medium/Bold.ttf and OFL.txt to src\\{product}.App\\Assets\\Fonts");
    if (!committed) Console.WriteLine("[new-project] TODO: git identity is not set. Ask the user for name and email, set them with 'git config', then commit.");
    if (!build) Console.WriteLine("[new-project] TODO: run test.bat --no-pause");
    return 0;
}
catch (Exception ex)
{
    return Fail(ex.Message);
}

static int Fail(string message)
{
    Console.Error.WriteLine($"[new-project] FAILED: {message}");
    return 1;
}

static void Step(int number, string text) => Console.WriteLine($"[{number}/6] {text}");

static string Run(string workDir, bool capture, string file, params string[] arguments)
{
    var info = new ProcessStartInfo(file) { WorkingDirectory = workDir, RedirectStandardOutput = capture };
    foreach (var argument in arguments) info.ArgumentList.Add(argument);
    using var process = Process.Start(info) ?? throw new InvalidOperationException($"Could not start {file}.");
    var output = capture ? process.StandardOutput.ReadToEnd() : string.Empty;
    process.WaitForExit();
    if (process.ExitCode != 0)
        throw new InvalidOperationException($"'{file} {string.Join(' ', arguments)}' exited with code {process.ExitCode}.");
    return output;
}

static bool HasGitIdentity(string workDir)
{
    try
    {
        return Run(workDir, capture: true, "git", "config", "user.name").Trim().Length > 0
            && Run(workDir, capture: true, "git", "config", "user.email").Trim().Length > 0;
    }
    catch (InvalidOperationException)
    {
        return false;                                   // git config exits 1 when the key is unset
    }
}

async Task<Dictionary<string, string>> ResolveVersionTokens(HttpClient http, string[] roots)
{
    // Tokens look like __V:PackageId__ or __V:PackageId@MaxMajor__.
    var pattern = new Regex(@"__V:(?<id>[A-Za-z0-9.\-]+)(?:@(?<major>\d+))?__");
    var result = new Dictionary<string, string>(StringComparer.Ordinal);
    foreach (var file in roots.SelectMany(r => Directory.EnumerateFiles(r, "*", SearchOption.AllDirectories)).Where(IsText))
    foreach (Match match in pattern.Matches(File.ReadAllText(file)))
    {
        if (result.ContainsKey(match.Value)) continue;
        int? maxMajor = match.Groups["major"].Success
            ? int.Parse(match.Groups["major"].Value, CultureInfo.InvariantCulture)
            : null;
        result[match.Value] = await LatestStable(http, match.Groups["id"].Value, maxMajor);
        Console.WriteLine($"      {match.Groups["id"].Value} {result[match.Value]}");
    }
    return result;
}

static async Task<string> LatestStable(HttpClient http, string id, int? maxMajor)
{
    var index = await http.GetFromJsonAsync<NuGetIndex>($"{NuGet}/{id.ToLowerInvariant()}/index.json")
        ?? throw new InvalidOperationException($"No version list for {id}.");
    var best = index.Versions
        .Where(v => !v.Contains('-'))
        .Select(v => (Text: v, Parsed: Version.TryParse(v.Split('+')[0], out var parsed) ? parsed : null))
        .Where(v => v.Parsed is not null && (maxMajor is null || v.Parsed.Major <= maxMajor))
        .OrderByDescending(v => v.Parsed)
        .Select(v => v.Text)
        .FirstOrDefault();
    return best ?? throw new InvalidOperationException($"No stable version of {id} found (max major {maxMajor}).");
}

void CopyTemplate(string from, string to, Dictionary<string, string> tokens)
{
    foreach (var source in Directory.EnumerateFiles(from, "*", SearchOption.AllDirectories))
    {
        var relative = Path.GetRelativePath(from, source).Replace("__Product__", tokens["__Product__"]);
        var destination = Path.Combine(to, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        if (!IsText(source))
        {
            File.Copy(source, destination, overwrite: true);
            continue;
        }

        var text = File.ReadAllText(source);
        foreach (var (key, value) in tokens) text = text.Replace(key, value, StringComparison.Ordinal);
        if (text.Contains("__", StringComparison.Ordinal) && Regex.IsMatch(text, @"__[A-Z][A-Za-z]*(:[^_]+)?__"))
            throw new InvalidOperationException($"Unreplaced token in {relative}.");
        File.WriteAllText(destination, text.ReplaceLineEndings("\r\n"), new UTF8Encoding(false));
    }
}

static async Task<bool> DownloadFonts(HttpClient http, string fontsDir)
{
    try
    {
        foreach (var weight in new[] { "Regular", "Medium", "Bold" })
        {
            var bytes = await http.GetByteArrayAsync($"{Fonts}/NotoSansArabic-{weight}.ttf");
            await File.WriteAllBytesAsync(Path.Combine(fontsDir, $"NotoSansArabic-{weight}.ttf"), bytes);
        }
        await File.WriteAllTextAsync(Path.Combine(fontsDir, "OFL.txt"), await http.GetStringAsync(FontLicense));
        File.Delete(Path.Combine(fontsDir, ".gitkeep"));
        return true;
    }
    catch (HttpRequestException)
    {
        return false;
    }
}

bool IsText(string path)
{
    var name = Path.GetFileName(path);
    var extension = name.StartsWith('.') ? name : Path.GetExtension(name);
    return textExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
}

sealed record NuGetIndex(string[] Versions);
