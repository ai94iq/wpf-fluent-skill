---
name: wpf-fluent-rtl
description: Prescriptive rules and scaffolding scripts for native Windows 10/11 desktop apps in C# with WPF + WPF-UI (default) or WinUI 3, CommunityToolkit.Mvvm, Generic Host, Serilog, SQLite with Dapper, IMemoryCache, resx localization and a WiX MSI, with modern outline-icon styling, light/dark/accent theming, test-first automation and DRY/KISS. Any UI language works; Arabic and other right-to-left languages get first-class support (RTL flow, Arabic plurals, Hijri dates, Arabic text search). Covers project scaffolding, git commit format, bat scripts, forms and inputs, loading states, lazy loading, caching and token-efficient agent behavior. Use whenever the user creates, scaffolds, writes, reviews, styles, tests, builds, packages, commits or debugs a Windows desktop app, XAML, ViewModels, forms, SQLite, localization or installers, or mentions WPF, WinUI, Arabic, RTL, Hijri, WiX, MSI, icons, themes, dark mode. Agents using this skill reply in the language the user writes in.
---

# Windows desktop apps — Windows 10 & 11

This skill is prescriptive. Every choice in it is final for projects that use it. Goals, in priority order: the UI thread never blocks, nothing loads before it's needed, text, dates and numbers are correct in every language the app ships (Arabic/RTL included), the UI looks clean and modern, and the app behaves the same on Windows 10 and 11.

## Agent rules (read first)

1. **The user is not very technical.** Use plain words. When a technical term can't be avoided, add a few plain words explaining it the first time. Never ask the user to choose between technical options this skill already decides: decide, then say what you did. Ask only about the product (what it should do, wording, names, which preset color). When the user must do something, give exact steps ("double-click `run.bat`").
2. **Reply in the language the user writes in.** If that language is Arabic, use Modern Standard Arabic and keep in English any term whose Arabic translation would sound odd or be inaccurate, wrapped in backticks so it doesn't scramble the sentence direction: `build`, `commit`, `ViewModel`, `XAML`, `binding`, `cache`, `migration`, `repository`, `installer`, `MSI`, `script`, `resx`, `NuGet`, `debug`. Use Arabic for everyday words: ملف، مجلد، نافذة، صفحة، زر، إعدادات، قاعدة البيانات، اختبار، خطأ. Code, comments, commit messages and project docs stay in English.
3. **Keep replies short.** By default, 5 lines or fewer:
   - what changed, in terms the user sees;
   - the result ("الاختبارات نجحت" when replying in Arabic);
   - the next step, or one question.

   Don't paste code, file lists or plans into the chat, and don't restate the request. Go longer only when the user asks for more detail.
4. **Use only what this skill specifies.** Don't add, replace or "try" any library, framework, tool, file format or pattern that isn't listed, even one you consider equivalent. If a need truly isn't covered, stop, propose one option in one plain sentence, and wait. Record every approved addition as an ADR (section 7).
5. **Read only the reference you need:**
   - New project, git, scripts, docs → `references/project-setup.md`
   - Writing code (startup, data, caching, lazy loading, localization, dates, loading states, tests) → `references/code-patterns.md`
   - Screens, forms, inputs, styling, icons, themes → `references/ui-ux.md`
   - WPF-UI vs WinUI 3, and WinUI-specific rules → `references/ui-frameworks.md`
   - Installer and releases → `references/installer.md`
6. **Definition of done** for every feature, in this order:
   1. Write the tests first (section 4).
   2. Implement until they pass.
   3. Update the affected docs (section 7).
   4. Commit in the required format (section 5). The pre-commit hook runs every test and blocks the commit on failure. Never use `--no-verify`, and never skip or delete a test to make it pass.
7. **Scripts:** always pass `--no-pause`. Never run `run.bat` yourself; it opens the app and waits until it's closed.
8. **Git safety:** never push, force-push, rebase, amend or rewrite history unless the user asks. If `git config user.name` is empty, ask for name and email; never invent an identity.
9. **Existing projects:** read `Directory.Packages.props` and the `.csproj` files first. Keep installed major versions and don't upgrade without asking. Bring in template pieces by copying the matching file from `assets/template/` (section 1), never by rewriting it from memory.

## Token budget

Input:
- Find before you read. Search with `grep`/`rg` for the symbol or key, then view only the line range you need.
- Never re-read a file you already have unless it changed.
- Never open `bin/`, `obj/`, `Icons.g.cs`, fonts or whole `.resx` files. Search resx for the keys you need.
- Build output: never read whole logs. `test.bat`, `build.bat` and `package.bat` already capture their full output to `artifacts\logs\<script>.log` and print only errors; see the "Build output is never read in full" rule below the locked stack.
- Load one reference at a time, only when the task needs it.
- Generate, don't write: use `new-project.bat` for a new project and `add-icons.bat` for icons, never hand-written boilerplate or SVG paths.

Output:
- Edit with small targeted replacements. Never rewrite a whole file to change a few lines.
- Don't echo back code you wrote, and don't summarize diffs in chat.
- Batch related edits into one step. Keep commit bodies to the point (1–6 lines).

## Locked stack

| Concern | Use | Not allowed |
|---|---|---|
| Runtime | .NET 10 (LTS), C# `latest` | .NET Framework, older .NET |
| UI framework | **WPF + WPF-UI 4.x (default)** or **WinUI 3** (Windows App SDK 1.x, unpackaged), chosen once at scaffold. See `ui-frameworks.md` | Avalonia, WinForms, MAUI, Uno, MahApps, MaterialDesignInXaml, HandyControl, ModernWpf, WPF `ThemeMode` |
| MVVM | CommunityToolkit.Mvvm 8.4+ with `PageViewModel` | Prism, ReactiveUI, Caliburn.Micro, hand-written `INotifyPropertyChanged` |
| DI and host | `Microsoft.Extensions.Hosting` | Other containers, service locator |
| Database | SQLite via `Microsoft.Data.Sqlite` + Dapper | EF Core, System.Data.SQLite, sqlite-net, other ORMs |
| Migrations | Numbered `.sql` files + `PRAGMA user_version` | EF migrations, FluentMigrator, DbUp |
| Caching | `IMemoryCache` + `GetOrLoadAsync` | HybridCache, Redis, ad-hoc static dictionaries (identity maps excepted) |
| Logging | Serilog: File + Async sinks | NLog, log4net, `Console`/`Debug.WriteLine` |
| JSON | `System.Text.Json` | Newtonsoft.Json |
| Localization | `.resx` + `Tr`; a neutral language plus satellite assemblies (template ships Arabic neutral, English satellite) | Hard-coded UI text, generated `Designer.cs`, `.resw`/`x:Uid`, JSON localization |
| Icons | Fluent UI System Icons, 24px Regular (outline), via `add-icons.bat` and `AppIcon` | Icon fonts (Segoe Fluent Icons is Windows 11-only), emoji, PNG icons, other icon packs |
| Fonts | Noto Sans Arabic with Segoe UI fallback for Arabic UIs; otherwise the system UI font | Any other bundled font |
| Tests | xUnit v3 + NSubstitute; pre-commit hook | MSTest, NUnit, Moq, FluentAssertions, UI automation (FlaUI, WinAppDriver, Appium) |
| Installer | WiX Toolset v6 MSI | Inno Setup, NSIS, MSIX, ClickOnce, Velopack, Squirrel |
| Packages | Central Package Management | Versions in `.csproj` files |
| Solution file | `.slnx` | `.sln` |
| Scripts | `.bat` in the repo root; skill scripts are C# file-based apps | PowerShell, Makefiles, Cake, Nuke |
| Architecture | Feature folders in App; Core and Data projects | Views/ViewModels type-folders, extra layers, MediatR, CQRS |

Reasons for the non-obvious choices:
- **Dapper:** explicit SQL for recursive queries, full-text search and paging, with no hidden queries.
- **xUnit v3 + NSubstitute:** FluentAssertions 8 needs a paid license, and Moq shipped telemetry in 2023.
- **`IMemoryCache`:** HybridCache only adds serialization in a single-process app.
- **Fluent outline icons:** the same design language as Windows 11, they work on Windows 10, and they're MIT-licensed.
- **No UI automation:** behavior is tested through ViewModels, which is faster and more reliable.

UI languages: **the app chooses one neutral resx language plus satellites. The template ships Arabic (ar-SA) neutral and English satellite; change the pair at scaffold if the product needs a different one.**

## Build output is never read in full

Every repo script writes its full output into `artifacts\logs\<script>.log` and prints one line — `OK`, or `FAILED` plus the error lines from `logs.bat`. Agents must not `type` or re-run these scripts to see the whole log:
- After a failure, read `logs.bat <script> error` output (already shown) or open only the matching `artifacts\logs\<script>.log` range with `grep`/`rg` first.
- Never paste a full build or test log into the chat, even on failure.
- `dotnet test`, `dotnet publish` and the WiX build are never run directly by an agent: always through `test.bat`, `build.bat` and `package.bat`, so the log capture stays in one place.

## 1. Solution layout

Create projects with `scripts\new-project.bat` (section 6). Replace `MyApp` with the product name.

```
MyApp/
├── MyApp.slnx, global.json, Directory.Build.props, Directory.Packages.props
├── .editorconfig, .gitignore, .gitattributes, .githooks/pre-commit
├── README.md, CHANGELOG.md
├── run.bat  build.bat  test.bat  package.bat  logs.bat  clean.bat
├── docs/            architecture, database, localization, release, known-issues, decisions/ (ADRs)
├── src/
│   ├── MyApp.App/   UI (WPF + WPF-UI, or WinUI 3)
│   │   ├── Assets/          app.ico, Fonts/
│   │   ├── Common/          PageViewModel, LoadState, LoadStateOverlay, AppIcon, converters, shared controls
│   │   ├── Features/<Name>/ <Name>Page.xaml(.cs), <Name>ViewModel.cs, feature-only controls
│   │   ├── Hosting/         startup pieces: AppPaths, AppSettings, SettingsStore, Culture, AppLogging, ServiceRegistration
│   │   ├── Localization/    Tr, TrExtension
│   │   ├── Resources/       Strings.resx (ar), Strings.en.resx, Styles.xaml, Icons.g.cs, icons.txt
│   │   ├── Services/        DialogService, DateFormatter, ThemeService, DataChangeNotifier
│   │   └── Shell/           MainWindow, MainWindowViewModel
│   ├── MyApp.Core/  pure C#: Models, Abstractions, Caching, Text, Theming
│   └── MyApp.Data/  SQLite + Dapper: Migrations/, Repositories/, connection factory, initializer
├── tests/           MyApp.Core.Tests, MyApp.Data.Tests, MyApp.App.Tests
├── installer/       WiX v6 project
└── artifacts/       build output (git-ignored)
```

The template lives in the skill folder: `assets/template/common/` (shared) plus `assets/template/wpf/` or `assets/template/winui/` (UI-specific files).

Dependency rules:
- **Core** (`net10.0`) references nothing.
- **Data** (`net10.0`) references Core.
- **App** references both.

The `net10.0` targets make it impossible for UI types to leak into Core or Data. New screens go in `Features/<Name>/`. Code used by two or more features goes in `Common/` (UI) or Core (logic).

## 2. DRY and KISS

**DRY: one place per concern.** Before writing a helper, search for an existing one.

| Concern | The one place |
|---|---|
| User-visible text | `Tr` + `Strings.resx` / `Strings.en.resx` |
| Dates on screen | `IDateFormatter` |
| Dialogs | `IDialogService` |
| Theme and accent | `IThemeService` + `AccentPresets` |
| Loading, empty, error, retry | `PageViewModel` + `LoadStateOverlay` |
| Icons | `AppIcon` + `Icons.g.cs` |
| Spacing, corner radius | tokens in `Styles.xaml` |
| Form label + error | `FormField` (`ui-ux.md`) |
| File locations | `AppPaths` |
| Settings | `AppSettings` + `SettingsStore` |
| Database connection | `SqliteConnectionFactory` |
| Arabic search text | `ArabicText.NormalizeForSearch` |
| Caching | `GetOrLoadAsync` |
| Change notifications | `IDataChangeNotifier` |
| Logging setup | `AppLogging` |

- Extract shared code on the second copy of any logic of three lines or more. Repeating a one-liner is fine.
- Docs describe structure and decisions. Code is the source of truth for details, so don't copy code into docs.

**KISS: the simplest thing that works.**
- No speculative abstractions. Interfaces exist only for things a test swaps out: services and repositories used by ViewModels. Helpers stay static or concrete.
- Inheritance only as `PageViewModel` → page ViewModel (one level). No generic repositories, no reflection, no events where a method call works.
- Use early returns and keep nesting to 3 levels or fewer.
- Use built-in controls first. Write a custom control only when it's used in two or more places.

## 3. Code conventions and file size

| File | Target | Hard max |
|---|---|---|
| `.cs` class | 250 lines | 400 |
| ViewModel | 250 | 350 |
| `.xaml` view | 200 | 300 |
| `.xaml.cs` code-behind | 80 | 150 |
| Method | 30 | 50 |
| `.bat` script | 40 | 60 |

`.resx`, `.sql` migrations and generated files (`*.g.cs`) are exempt. Past a target, split by responsibility in the same change: extract a service, a sub-ViewModel, a control or a resource dictionary. Never use `partial`, `#region` or crammed lines to dodge a limit. WinUI classes are `partial` because the platform requires it, not to split files.

- One public type per file. The file name matches the type; the file-scoped namespace matches the folder.
- `_camelCase` private fields; `PascalCase` constants and static readonly fields.
- Async methods end in `Async` and take `CancellationToken ct` last.
- Classes are `sealed` unless designed for inheritance. Immutable data uses `record`.
- Comments say why, not what.
- No user-visible strings in C# or XAML; they all go through resx. Log and exception messages stay as English literals.
- Data-layer row records use only `long`, `double`, `string`, `byte[]` (nullable allowed). Convert to `DateOnly`, enums and other domain types in the repository.

## 4. Testing: test-first for every feature

For each feature, before implementing it:
1. **Write tests that describe the behavior.**
   - ViewModels: each command, `CanExecute`, validation, and the Loading/Empty/Error states.
   - Repositories: each method against a real temp-file SQLite database (`TempDatabase`).
   - Core logic: every public function.
2. **Run them and see them fail** (`test.bat --no-pause`).
3. **Implement until they pass.**

Rules:
- Tests live in `tests/<Project>.Tests/<Feature>/<Class>Tests.cs`. Name each one as a sentence: `Saving_without_a_name_shows_required_error`. One behavior per test, arranged as Arrange / Act / Assert.
- Use NSubstitute for interfaces (`IDialogService`, repositories in ViewModel tests). No real network, no `Thread.Sleep`, no dependence on test order.
- Automatic checks already exist; keep them passing:
  - resx keys match and every key used exists (`LocalizationTests`);
  - every `AppIcon Kind` used exists (`IconTests`);
  - migrations apply and backups are written (`DatabaseInitializerTests`).
- XAML layout and visuals aren't unit-tested. After a UI change, tell the user in one line what to look at: the screen in Arabic and English, light and dark.
- `[Fact(Skip = ...)]` and deleting failing tests need the user's approval.

## 5. Git and commits

`new-project.bat` creates the repository, sets `core.hooksPath` to `.githooks`, and makes the first commit. Work on `main`.

```
<project>: <type>: <short title>

<body: what changed and why, 1–6 lines, wrapped at 72 characters>
```

- `project`: `app`, `core`, `data`, `tests`, `installer`, `docs`, or `repo` (scripts, props files, solution).
- `type`: `feat`, `fix`, `perf`, `refactor`, `test`, `docs`, `build`, `chore`, `i18n` or `style`.
- Title line: 60 characters or fewer, imperative, lowercase after the colons, no trailing period, English.
- Body: required except for trivial `docs`/`chore` commits. Say why, mention migration numbers, and start with `BREAKING:` when data or settings become incompatible.
- One logical change per commit. A feature's tests and implementation go in the same commit.
- Commit with `git commit -m "<title>" -m "<body>"`. The hook runs the tests (about a minute); if it fails, fix the cause and commit again.
- Never commit `bin/`, `obj/`, `artifacts/`, databases, logs, `*.user`, certificates or secrets.
- Releases get an annotated tag `vX.Y.Z` (`docs/release.md`).

Example:

```
data: perf: use keyset paging for people list

Replace OFFSET paging with (NameSearch, Id) keyset paging so deep
scrolling stays fast. Adds index IX_Person_NameSearch_Id in 0004.
```

## 6. Scripts

**Skill scripts** live in this skill's `scripts/` folder and run once per task:

| Script | What it does |
|---|---|
| `new-project.bat Product "Company" TargetFolder [--ui wpf\|winui]` | Builds the whole project from the template: latest package versions, fonts, solution, git with the hook, tests, first commit |
| `add-icons.bat ProjectRoot name ...` | Adds Fluent outline icons by snake_case name (`person_add`) and regenerates `Icons.g.cs` |

**Repo scripts** live in the project root:

| Script | What it does |
|---|---|
| `run.bat` | Run the app in Debug (for the user, not agents) |
| `build.bat` | Publish the Release, self-contained, ReadyToRun exe to `artifacts\publish\win-x64` |
| `test.bat` | Run all tests (output to `artifacts\logs\test.log`) |
| `package.bat` | Tests, build, then the MSI to `artifacts\installer` |
| `logs.bat` | Show error (or all) lines from a script's saved log: `logs.bat test error` |
| `clean.bat` | Delete every `bin`, `obj` and `artifacts` folder |

Repo scripts are ASCII-only with CRLF line endings, and start with `cd /d "%~dp0"`. They print errors only, return nonzero on failure, and pause unless given `--no-pause`.

## 7. Documentation

| File | Update when |
|---|---|
| `README.md` | Setup, scripts, requirements or structure change |
| `CHANGELOG.md` (`## [Unreleased]`) | Every user-visible `feat`, `fix`, `perf`, `i18n` |
| `docs/architecture.md` | A project, component, flow or feature folder is added |
| `docs/decisions/NNNN-title.md` | Any approved addition or reversal (copy `TEMPLATE.md`) |
| `docs/database.md` | Every migration |
| `docs/localization.md` | New terminology (glossary first, then resx) |
| `docs/release.md` | The release process changes |
| `docs/known-issues.md` | An issue is found or fixed |

Docs are updated in the same commit as the change.

## 8. Startup

The template's `App.xaml.cs` runs, in order:
1. Load settings.
2. Culture and RTL.
3. Logging and exception handlers.
4. Single-instance check.
5. Host and DI.
6. Backup and migrate off the UI thread.
7. Show the main window.

Pages load their own data when navigated to, never in constructors. DI lifetimes: shell, repositories, factories and services are Singleton; pages and their ViewModels are Transient.

## 9. MVVM and loading states

- Every page ViewModel derives from `PageViewModel` and implements `LoadCoreAsync(ct)`, returning `false` when there's nothing to show.
  - The page's navigated-to hook calls `EnsureLoadedAsync()`, which loads once.
  - Data that other screens can change uses `ReloadOnChange("area")`.
- The page XAML shows content only in states `Idle`, `Loading` and `Loaded`, with `LoadStateOverlay` on top. The overlay handles the spinner, empty text, and the error with a Retry button. Usage is in `code-patterns.md`.
- `[ObservableProperty]` goes on partial properties, never fields. Set defaults in the constructor.
- ViewModels never reference UI types (`MessageBox`, `Brush`, `Visibility`, `Dispatcher`, `Window`). Use services.
- Buttons that run async work bind to the command. Show progress with `XCommand.IsRunning`; re-entry is already blocked.
- An initial bulk load assigns a new collection. Paged lists append pages through `IIncrementalSource`.
- Validation lives in the ViewModel as `XError` string properties (null when valid), with `CanSave` derived from them (`ui-ux.md`).
- Code-behind only for view concerns (focus, scroll, `PasswordBox`). Cross-ViewModel messages use `WeakReferenceMessenger`.

## 10. Threading

- The UI thread does UI work only. CPU-heavy work runs in `Task.Run`.
- Repositories run Dapper's synchronous API inside `Task.Run`, because `Microsoft.Data.Sqlite` async calls are synchronous anyway.
- ViewModels never use `ConfigureAwait(false)`. Core and Data always do.
- Never call `.Result` or `.Wait()`.

## 11. SQLite with Dapper

- One connection per unit of work via `SqliteConnectionFactory.Open()`. `DatabaseInitializer` sets WAL once and runs migrations.
- SQL is written as raw string literals with anonymous-object parameters. Never concatenate values into SQL.
- Batch writes run in one transaction.
- Dates are ISO-8601 text in invariant culture. Money is integer minor units.
- Index every filter and sort column. Check big queries with `EXPLAIN QUERY PLAN`.
- **Arabic search:** fill a `NameSearch` shadow column with `ArabicText.NormalizeForSearch` and normalize queries the same way. Use prefix `LIKE` under 10,000 rows and FTS5 above.
- **Migrations:** `Migrations/NNNN_snake_case.sql`, never edited once applied. Each one adds a line to `docs/database.md`.
- **Backups:** automatic before migrations and daily; the newest 14 are kept.

## 12. Lazy loading and caching

| Thing | Rule |
|---|---|
| Pages | Created on navigation; data loaded by `EnsureLoadedAsync` |
| Rarely used services | Inject `Lazy<T>` |
| Lists | Keyset paging, 50 per page, load-more on scroll. Never `OFFSET` |
| Wide rows, notes, photos | Separate tables; list queries select list columns only |
| Images | Thumbnails decoded at display size when visible, then frozen |
| Search | 300 ms debounce; cancel the previous query |

Caching layers, cheapest first:
1. ViewModel state.
2. Identity map (graphs).
3. `IMemoryCache` through `GetOrLoadAsync`: caches the task, never caches failures, every entry has a size and expiry.
4. Disk cache in `%LOCALAPPDATA%\MyApp\cache`, safe to delete.
5. SQLite page cache.

Keys are `"{area}:{id}"`, plus `":{culture}"` for localized values. Cache immutable records only. After a commit, the repository evicts its keys and calls `IDataChangeNotifier.Notify(...)`.

## 13. Localization, Arabic and RTL

Localization rules apply to every app: no user-visible string outside resx, dates and numbers only through services. The Arabic and RTL rules below apply whenever the app ships a right-to-left UI — the template supports it out of the box, it isn't required.

- **Culture:** `ar-SA` by default, English as a setting. Changing the language restarts the app. `Culture.Configure` forces the Gregorian calendar (ar-SA defaults to Umm al-Qura).
- **Dates:** only through `IDateFormatter`, which handles Gregorian or Hijri, date precision, and the Umm al-Qura range fallback. Never format dates in XAML.
- **Digits:** Western by default. Arabic-Indic is a WPF-only setting, done by number substitution.
- **Direction:** every window calls `this.ApplyCultureDirection()`. Never set RTL on individual controls. Exceptions:
  - images and logos → `FlowDirection="LeftToRight"`;
  - LTR-content inputs (email, URL, phone, path, code, password, username) → `FlowDirection="LeftToRight"`;
  - directional icons → `IsDirectional="True"`;
  - dialogs → `IDialogService`.
- **Mixed text:** put LTR fragments in their own `Run` with `FlowDirection="LeftToRight"`, or follow them with RLM (`\u200F`).
- **Fonts:** Noto Sans Arabic with Segoe UI as fallback, applied once on the window root. Base size 15. Never italic.
- **Strings:**
  - `Strings.resx` (Arabic) and `Strings.en.resx` hold identical, sorted keys. There's no Designer file; edit the XML directly.
  - Keys follow `Feature_Element_Purpose`.
  - XAML: `{l:Tr Key}` in WPF, `{l:Tr Key=Key}` in WinUI. C#: `Tr.Get`, `Tr.Format`, `Tr.Plural(base, n)`, using the six Arabic plural keys `_zero/_one/_two/_few/_many/_other`.
  - New terminology goes into the glossary in `docs/localization.md` first.

## 14. UI, styling, icons and theming

Follow `references/ui-ux.md` for every screen. The non-negotiables:
- **Clean, modern, calm.** Theme brushes only, one accent color used sparingly, an 8 px corner radius on cards, spacing from tokens on a 4 px grid.
- **No AI-slop styling:** no gradients, glows, colored card backgrounds, heavy borders, custom drop shadows, emoji or decorative illustrations.
- **Icons:** outline only, through `AppIcon`, at 16, 20 or 24 px, in the text color. Icon-only buttons need a tooltip and an accessible name.
- **Forms:** single column, label above the field, inline error below, one primary button.
- **Theming:** System, Light or Dark, plus an accent from `AccentPresets` or the Windows accent, all through `IThemeService`. High Contrast always wins. Never hard-code colors; the only exception is the accent presets.
- **Accessibility:** full keyboard use, visible focus, at least 4.5:1 contrast, and meaning never carried by color alone.

## 15. Logging

- Use message templates, never interpolation: `log.LogInformation("Loaded {Count} rows", count)`. Inject `ILogger<T>`; static `Log` appears only in `App.xaml.cs`.
- Logs go to `%LOCALAPPDATA%\MyApp\logs`, daily, keeping 14 files. Never log names, dates of birth, passwords or photos; log IDs instead.

## 16. Performance

- Lists stay virtualized. Never put a list inside a `StackPanel` or outer `ScrollViewer`.
- Images get a decode size and are frozen. No per-item effects or shadows.
- Zero binding errors. Flat panel trees. Heavy converter logic moves into ViewModels.
- Measure with the Visual Studio profiler before optimizing.

## 17. Publishing and installer

- `build.bat` publishes `win-x64`, self-contained, ReadyToRun. No Native AOT, trimming, single-file or ARM64.
- `package.bat` builds the WiX v6 MSI (`references/installer.md`). The version lives only in `Directory.Build.props`, and every shipped MSI gets a higher one.
- Before each release, test a fresh install and an upgrade on Windows 10 22H2 and Windows 11, as a standard user.

## 18. Review checklist

1. Anything outside the locked stack or this skill's patterns. Duplicated logic that already has a home (section 2).
2. Missing tests for new behavior, skipped tests, stale docs, a malformed commit message, files over their limits.
3. Blocking calls on the UI thread; UI types in ViewModels; data loaded in constructors.
4. SQL built by concatenation, culture-formatted stored dates, `int` in Dapper rows, edited migrations, `OFFSET` paging, a query per row.
5. Caches without size, expiry or eviction; cached failures; shared mutable cached objects.
6. Hard-coded strings or colors; resx drift; number + word instead of plural keys.
7. RTL problems: mirrored images, unflagged directional icons, LTR inputs typed RTL, dates formatted in XAML.
8. Styling slop: gradients, shadows, colored boxes, mixed icon styles, literal margins instead of tokens.
9. Windows 11-only visuals without a Windows 10 fallback; lost list virtualization.
10. Interpolated log messages; personal data in logs.
