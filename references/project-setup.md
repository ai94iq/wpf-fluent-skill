# Project setup

Contents
1. Create a new project (one command)
2. What the generator does
3. When it fails
4. Adding to an existing project
5. Git details
6. Keeping docs current

## 1. Create a new project (one command)

1. Ask the user only what you can't decide yourself:
   - the product name (an English PascalCase identifier, e.g. `MyApp`; the Arabic display name goes into `App_Name` in resx later);
   - the publisher name;
   - the target folder;
   - WPF-UI or WinUI 3 (`ui-frameworks.md`; recommend WPF-UI).
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

## 5. Git details

- Identity is per repo (`git config` without `--global`). Ask the user; never invent it.
- The hook (`.githooks/pre-commit`) runs `dotnet test -c Release -v q` before every commit. If it blocks a commit, fix the code; never bypass it.
- Work on `main`. Branches only if the user asks.
- Release tags: `git tag -a v1.2.0 -m "v1.2.0"` after the release commit (`docs/release.md`).

## 6. Keeping docs current

The templates live in the generated `docs/` folder. Update them in the same commit as the change:
- **`CHANGELOG.md`:** one line per user-visible change under `## [Unreleased]`, in English, written for the user ("Add search by name"), not for developers.
- **`docs/database.md`:** one row per migration, plus any new table or index.
- **`docs/localization.md`:** add terminology to the glossary before using it in resx.
- **`docs/architecture.md`:** add each new feature folder to the feature map.
- **`docs/decisions/`:** copy `TEMPLATE.md` to the next number for each approved exception to the locked stack.
