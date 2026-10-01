# Handover — wpf-fluent-rtl (win-desktop-fluent)

State as of 2026-10-01. `SKILL.md` stays the source of truth for the agent
rules and the locked stack; this file records the repository state, the fixes
applied on this date, and the review against the local .NET skills.

## What this skill is

A prescriptive scaffold and rule set for Windows 10/11 desktop apps: WPF +
WPF-UI (default) or WinUI 3, CommunityToolkit.Mvvm, Generic Host, Serilog,
SQLite + Dapper, IMemoryCache, resx localization (Arabic neutral), xUnit v3 +
NSubstitute, WiX v6 MSI. The how-to lives in `references/`; the template lives
in `assets/template/{common,wpf,winui}`; `scripts/new-project.bat` builds a
complete project and `scripts/add-icons.bat` regenerates `Icons.g.cs`.

## Repository state

- `SKILL.md` frontmatter is renamed to `win-desktop-fluent`; the folder name
  is unchanged, so the skill id stays `wpf-fluent-rtl`.
- Three template fixes are committed on `main`:
  - `5254310` repo: fix: cap xunit.v3 at 3.x for the VSTest runner
  - `c617f35` repo: fix: add System.IO to the WPF test project usings
  - `39a4aec` repo: fix: alias Path and Shape in the WinUI AppIcon
- Validation on .NET SDK 10.0.401: `new-project.bat` ran end-to-end for both
  `--ui wpf` and `--ui winui` (28 tests pass, first commit made); `test.bat`,
  the hook-style `dotnet test`, and `build.bat` pass on the WPF scaffold.

## Why the fixes (evidence)

1. **xunit.v3 cap.** `__V:xunit.v3__` resolved 4.0.1; `xunit.v3` 4.x is
   MTP-first ("Installing this package installs xunit.v3.mtp-v2"). On .NET 10,
   the SDK rejects VSTest for MTP apps: "Testing with VSTest target is no
   longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later".
   `new-project.bat` failed at step 5 for every new project. Capping at the
   tested 3.x major (3.2.2) keeps the VSTest setup working with
   `xunit.runner.visualstudio@3` (3.1.5).
2. **System.IO.** `UseWPF=true` removes `System.IO` (and `System.Net.Http`)
   from implicit usings. The shared test files (`TestPaths`, `IconTests`,
   `LocalizationTests`) failed with CS0103 in the WPF App.Tests project.
3. **WinUI Path.** `Microsoft.UI.Xaml.Shapes.Path` collided with the implicit
   `System.IO.Path` (CS0104); the XAML compiler then cascaded WMC0001 errors
   for local types. Type aliases resolve it.

## Standards review vs the local .NET skills

Aligned (no change needed):
- MSBuild: Directory.Build.props layout, Central Package Management, minimal
  csproj files — matches directory-build-organization, convert-to-cpm,
  msbuild-antipatterns (no AP-01..AP-15 hits), property-patterns,
  item-management.
- xUnit v3 conventions: `OutputType=Exe`, `TestContext.Current.CancellationToken`
  — matches migrate-xunit-to-xunit-v3 and code-testing-agent.
- Nullable enabled, warnings as errors, minimal justified `!` — matches
  migrate-nullable-references.
- No sync-over-async, `ConfigureAwait(false)` in Core/Data, message-template
  logging, `NullLogger` instead of mocked `ILogger`, NSubstitute for interfaces
  — matches analyzing-dotnet-performance and exp-mock-usage-analysis.
- Dapper + `Task.Run`, keyset paging, cache size/expiry/failure eviction — as
  prescribed by the skill itself and the performance skill.

Intentional deviations (documented, not changed):
- Static helpers (`AppPaths`, `SettingsStore`, `Culture`, `Tr`, `AppLogging`)
  and ambient time/IO in `DatabaseInitializer` would be flagged by
  detect-static-dependencies. The skill keeps helpers static by design (KISS)
  and tests use real temp files. If a test ever needs a fixed clock, add a
  `TimeProvider` seam through testability-obstacle rather than rewriting the
  helpers.

Open follow-ups (owner decisions):
1. **MTP migration (recommended next).** xUnit 4.x is MTP-only; the local
   skills (platform-detection, migrate-vstest-to-mtp) and the dotnet docs
   recommend the .NET 10 native MTP mode. Verified recipe: add
   `"test": { "runner": "Microsoft.Testing.Platform" }` to `global.json`; drop
   `Microsoft.NET.Test.Sdk` and `xunit.runner.visualstudio`; use xunit.v3 4.x;
   keep `OutputType=Exe`; remove `--nologo` from `test.bat`, the pre-commit
   hook and `new-project.cs` (in MTP mode it is forwarded to the app and fails
   the run); prefer `--project`/`--solution` in commands. Never mix VSTest and
   MTP projects in one solution.
2. `docs/architecture.md` describes Core as holding "layout logic"; it holds
   domain, text, caching and theming logic. Reword to "domain logic".
3. The folder is still `wpf-fluent-rtl` while the frontmatter name is
   `win-desktop-fluent`; decide whether to rename the folder to match.

## How to re-verify

- WPF: `scripts\new-project.bat Demo "Demo Co" %TEMP%\Demo --ui wpf`
- WinUI: same with `--ui winui`
- Inside the scaffold: `test.bat --no-pause`, `build.bat --no-pause`,
  `package.bat --no-pause`.
