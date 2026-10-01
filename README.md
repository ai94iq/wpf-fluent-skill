# win-desktop-fluent

An agent skill for building modern Windows 10/11 desktop apps in C#: a
prescriptive technology stack, reference guides, and scripts that scaffold a
complete, tested project in one command — with first-class Arabic and
right-to-left support.

## What it gives you

- **One-command scaffold** — `new-project.bat` creates a WPF + WPF-UI (default)
  or WinUI 3 app with Core/Data/App projects, three xUnit v3 test projects,
  repo scripts, docs and a WiX v6 MSI, then runs the tests and makes the first
  commit.
- **A locked stack** so agents don't improvise: .NET 10, CommunityToolkit.Mvvm,
  Microsoft.Extensions.Hosting, Serilog, SQLite via Microsoft.Data.Sqlite +
  Dapper (numbered SQL migrations), IMemoryCache, System.Text.Json, resx
  localization, xUnit v3 + NSubstitute, Central Package Management, `.slnx`,
  `.bat` scripts.
- **Localization that holds up** — dates and numbers follow the system locale,
  Arabic plurals via CLDR categories, Arabic text normalization for search,
  RTL layout with correct handling of directional icons and LTR fields.
- **Rules for agent work** — test-first definition of done, a pre-commit hook
  that runs all tests, a fixed commit format, and token rules so build output
  is logged instead of read in full.

## Requirements

- Windows 10 22H2 or Windows 11, x64
- .NET 10 SDK
- Git for Windows

## Use

Copy or clone this repository into your agent's skills directory (for example
`%USERPROFILE%\.claude\skills\win-desktop-fluent`), then ask your agent to
create a Windows desktop app. To scaffold directly:

```bat
scripts\new-project.bat MyApp "My Company" C:\Projects\MyApp --ui wpf
```

Use `--ui winui` for the WinUI 3 variant. The first run takes a few minutes
(package restore, fonts and tests).

## Layout

| Path | Contents |
|---|---|
| `SKILL.md` | Identity, agent rules, locked stack, review checklist |
| `references/` | Project setup, code patterns, localization/RTL, UI/UX, UI frameworks, installer |
| `scripts/` | `new-project.bat` (full scaffold), `add-icons.bat` (icons), `check-skill.bat` (size and formatting checks) |
| `assets/template/` | Template files: `common/` plus the `wpf/` or `winui/` layer |

## Status

Scaffold, tests and Release build are validated on .NET SDK 10.0.401 for both
UI variants.
