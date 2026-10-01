# Project setup

Contents
1. Create a new project (one command)
2. What the generator does
3. When it fails
4. Adding to an existing project
5. Solution scripts
6. Git and commits
7. Documentation
8. Keeping docs current

## 1. Create a new project (one command)

1. Ask the user only what you can't decide yourself:
   - the product name (an English PascalCase identifier, e.g. `MyApp`; the display name in the app language goes into `App_Name` in resx later);
   - the publisher name;
   - the target folder;
   - WPF-UI or WinUI 3 (`ui-frameworks.md`; recommend WPF-UI);
   - the neutral resx language if not Arabic (the template ships Arabic neutral + English satellite; another pair means swapping the resx files and culture defaults after scaffold).
2. Check `dotnet --version` is 10.x and `git --version` works. If not, tell the user exactly what to install (.NET 10 SDK, Git for Windows).
3. Run, from any folder:

```bat
"<skill folder>\scripts\new-project.bat" MyApp "My Company" C:\Projects\MyApp --ui wpf
```

The first run takes a few minutes (package restore and tests). It prints six numbered steps and then `OK` or `FAILED: <reason>`. Report the result to the user in one or two lines.

## 2. What the generator does

1. Resolves the latest stable NuGet version for every package, capped at the tested major version, and writes them into `Directory.Packages.props` and the WiX project.
2. Copies `assets/template/common` plus the chosen UI layer, filling in the product, company, a new installer `UpgradeCode`, dates and the SDK version. Text files get CRLF endings; `.githooks/pre-commit` keeps LF.
3. Downloads Noto Sans Arabic (Regular, Medium, Bold) and its OFL license into `Assets/Fonts`.
4. Runs `git init -b main`, sets `core.hooksPath=.githooks`, creates `<Product>.slnx` and adds the six projects. The installer isn't in the solution; `package.bat` builds it after publishing.
5. Runs every test (skipped with `--no-build`).
6. Makes the first commit `repo: chore: scaffold solution` if a git identity is set. Otherwise it prints a TODO: ask the user for name and email, run `git config user.name "..."` and `git config user.email "..."` in the repo, then commit.

Then fill in the README's TODO lines (one-sentence description, license) and replace `installer/License.rtf` text with the user's license, edited in WordPad.

## 3. When it fails

| Message | Fix |
|---|---|
| `.NET 10 SDK required` | The user installs the .NET 10 SDK, then you rerun |
| `Target folder is not empty` | Choose a new folder; never delete the user's files |
| NuGet or HTTP errors | Check the internet connection, then rerun |
| Font download failed | The project still works (falls back to Segoe UI); add the fonts later as printed |
| Test failure in step 5 | Read only the failing test's output, fix the template file in the project, run `test.bat --no-pause`, then commit |

Never scaffold by hand as a fallback. Fix the cause and rerun.

## 4. Adding to an existing project

For a project not created by the generator (such as the user's current WPF app):
1. Read its `.csproj` files and folder layout first.
2. Adopt the template piece by piece, in this order, one commit each:
   1. Central Package Management (`Directory.Packages.props`) and `Directory.Build.props`.
   2. `.editorconfig`, `.gitattributes`, `.gitignore`, `.githooks/pre-commit` + `git config core.hooksPath .githooks`.
   3. Repo scripts (`*.bat`).
   4. Test projects with `TempDatabase`, `TestPaths`, `LocalizationTests`, `IconTests`.
   5. `Hosting/`, `Localization/`, `Services/`, `Common/` pieces as features need them.
   6. Icons via `add-icons.bat`.
3. Copy each file from the skill's `assets/template/` (common, then the matching UI layer) and replace `__Product__`, `__Company__` and the other `__Token__` placeholders. Never retype template files from memory.
4. Don't reorganize the whole project at once. Move a feature into `Features/<Name>/` when you next work on it.

## 5. Solution scripts

**Skill scripts** live in this skill's `scripts/` folder and run once per task:

| Script | What it does |
|---|---|
| `new-project.bat Product "Company" TargetFolder [--ui wpf\|winui]` | Builds the whole project from the template: latest package versions, fonts, solution, git with the hook, tests, first commit |
| `add-icons.bat ProjectRoot name ...` | Adds Fluent outline icons by snake_case name (`person_add`) and regenerates `Icons.g.cs` |
| `check-skill.bat` | Checks the skill repository itself: line-count budgets and text formatting; run by CI on every push |

**Repo scripts** live in the project root:

| Script | What it does |
|---|---|
| `run.bat` | Run the app in Debug (for the user, not agents) |
| `build.bat` | Publish the Release, self-contained, ReadyToRun exe to `artifacts\publish\win-x64` |
| `test.bat` | Run all tests |
| `package.bat` | Tests, build, then the MSI to `artifacts\installer` |
| `logs.bat` | Show error (or all) lines from a script's saved log: `logs.bat test error` |
| `clean.bat` | Delete every `bin`, `obj` and `artifacts` folder |

Repo scripts are ASCII-only with CRLF line endings, and start with `cd /d "%~dp0"`. Every dotnet-running script captures its full output in `artifacts\logs\<script>.log` and prints one line, or the error lines on failure. They return nonzero on failure and pause unless given `--no-pause`.

## 6. Git and commits

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
- Identity is per repo (`git config` without `--global`); ask the user, never invent it. Never push, force-push, rebase, amend or rewrite history unless the user asks.

Example:

```
data: perf: use keyset paging for people list

Replace OFFSET paging with (NameSearch, Id) keyset paging so deep
scrolling stays fast. Adds index IX_Person_NameSearch_Id in 0004.
```

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

## 8. Keeping docs current

The templates live in the generated `docs/` folder. Update them in the same commit as the change:
- **`CHANGELOG.md`:** one line per user-visible change under `## [Unreleased]`, in English, written for the user ("Add search by name"), not for developers.
- **`docs/database.md`:** one row per migration, plus any new table or index.
- **`docs/localization.md`:** add terminology to the glossary before using it in resx.
- **`docs/architecture.md`:** add each new feature folder to the feature map.
- **`docs/decisions/`:** copy `TEMPLATE.md` to the next number for each approved exception to the locked stack.
