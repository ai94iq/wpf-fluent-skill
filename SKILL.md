---
name: wpf-fluent-rtl
description: Prescriptive rules and scaffolding scripts for native Windows 10/11 desktop apps in C# with WPF + WPF-UI (default) or WinUI 3, CommunityToolkit.Mvvm, Generic Host, Serilog, SQLite with Dapper, IMemoryCache, resx localization and a WiX MSI, with modern outline-icon styling, light/dark/accent theming, test-first automation and DRY/KISS. Any UI language works; Arabic and other right-to-left languages get first-class support (RTL flow, Arabic plurals, Hijri dates, Arabic text search). Covers project scaffolding, git commit format, bat scripts, forms and inputs, loading states, lazy loading, caching and token-efficient agent behavior. Use whenever the user creates, scaffolds, writes, reviews, styles, tests, builds, packages, commits or debugs a Windows desktop app, XAML, ViewModels, forms, SQLite, localization or installers, or mentions WPF, WinUI, Arabic, RTL, Hijri, WiX, MSI, icons, themes or dark mode. Agents using this skill reply in the language the user writes in.
---

# Windows desktop apps — Windows 10 & 11

This skill is prescriptive. Every choice in it is final for projects that use it. Goals, in priority order: the UI thread never blocks, nothing loads before it's needed, text, dates and numbers are correct in every language the app ships (Arabic/RTL included), the UI looks clean and modern, and the app behaves the same on Windows 10 and 11.

This file holds the identity, the agent rules and the locked stack. The how-to lives in `references/` — read only what the task needs.

## Agent rules (read first)

1. **The user is not very technical.** Use plain words. When a technical term can't be avoided, add a few plain words explaining it the first time. Never ask the user to choose between technical options this skill already decides: decide, then say what you did. Ask only about the product (what it should do, wording, names, which preset color). When the user must do something, give exact steps ("double-click `run.bat`").
2. **Reply in the language the user writes in.** If that language is Arabic, use Modern Standard Arabic and keep in English any term whose Arabic translation would sound odd or be inaccurate, wrapped in backticks so it doesn't scramble the sentence direction: `build`, `commit`, `ViewModel`, `XAML`, `binding`, `cache`, `migration`, `repository`, `installer`, `MSI`, `script`, `resx`, `NuGet`, `debug`. Use Arabic for everyday words: ملف، مجلد، نافذة، صفحة، زر، إعدادات، قاعدة البيانات، اختبار، خطأ. Code, comments, commit messages and project docs stay in English.
3. **Keep replies short.** By default, 5 lines or fewer: what changed in terms the user sees, the result, then the next step or one question. Don't paste code, file lists or plans into the chat, and don't restate the request. Go longer only when the user asks for more detail.
4. **Use only what this skill specifies.** Don't add, replace or "try" any library, framework, tool, file format or pattern that isn't listed, even one you consider equivalent. If a need truly isn't covered, stop, propose one option in one plain sentence, and wait. Record every approved addition as an ADR (`docs/decisions/`).
5. **Definition of done** for every feature, in this order:
   1. Write the tests first (`code-patterns.md` §5).
   2. Implement until they pass (`test.bat --no-pause`).
   3. Update the affected docs (`project-setup.md` §7).
   4. Commit in the required format (`project-setup.md` §6). The pre-commit hook runs every test and blocks the commit on failure. Never use `--no-verify`, and never skip or delete a test to make it pass.
6. **Scripts:** always pass `--no-pause`. Never run `run.bat` yourself; it opens the app and waits until it's closed.
7. **Git safety:** never push, force-push, rebase, amend or rewrite history unless the user asks. If `git config user.name` is empty, ask for name and email; never invent an identity.
8. **Existing projects:** read `Directory.Packages.props` and the `.csproj` files first. Keep installed major versions and don't upgrade without asking. Bring in template pieces by copying the matching file from `assets/template/`, never by rewriting it from memory (`project-setup.md` §4).

## Token budget

Input:
- Find before you read. Search with `grep`/`rg` for the symbol or key, then view only the line range you need.
- Never re-read a file you already have unless it changed.
- Never open `bin/`, `obj/`, `Icons.g.cs`, fonts or whole `.resx` files. Search resx for the keys you need.
- Build output: never read whole logs. `test.bat`, `build.bat` and `package.bat` capture their full output to `artifacts\logs\<script>.log` and print only the error lines.
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

Every dotnet-running repo script writes its full output into `artifacts\logs\<script>.log` and prints one line — `OK`, or `FAILED` plus the error lines. Agents:
- run `dotnet test`, `dotnet publish` and the WiX build only through `test.bat`, `build.bat` and `package.bat`, so the log capture stays in one place;
- never `type` or re-run a script to see the whole log; use `logs.bat <script> error`, or `grep` the log file for the specific error first;
- never paste a full build or test log into the chat, even on failure.

## References — read only what you need

| When | Read |
|---|---|
| New project, git, scripts, docs | `project-setup.md` |
| Writing code (startup, data, caching, conventions, testing, threading, logging) | `code-patterns.md` |
| Strings, dates, digits, Arabic, RTL | `localization.md` |
| Screens, forms, inputs, styling, icons, themes | `ui-ux.md` |
| WPF-UI vs WinUI 3, WinUI-specific rules | `ui-frameworks.md` |
| Installer and releases | `installer.md` |

## Review checklist

1. Anything outside the locked stack or the reference patterns. Duplicated logic that already has a home (`code-patterns.md` §3).
2. Missing tests for new behavior, skipped tests, stale docs, a malformed commit message, files over their limits.
3. Blocking calls on the UI thread; UI types in ViewModels; data loaded in constructors.
4. SQL built by concatenation, culture-formatted stored dates, `int` in Dapper rows, edited migrations, `OFFSET` paging, a query per row.
5. Caches without size, expiry or eviction; cached failures; shared mutable cached objects.
6. Hard-coded strings or colors; resx drift; number + word instead of plural keys.
7. RTL problems: mirrored images, unflagged directional icons, LTR inputs typed RTL, dates formatted in XAML.
8. Styling slop: gradients, shadows, colored boxes, mixed icon styles, literal margins instead of tokens.
9. Windows 11-only visuals without a Windows 10 fallback; lost list virtualization.
10. Interpolated log messages; personal data in logs.
