# Installer: WiX Toolset v6 MSI

Contents
1. Fixed decisions
2. Files
3. WinUI 3 projects
4. Arabic installer text
5. Versioning and upgrades
6. Signing
7. Install testing
8. Rules
9. Publishing to winget

## 1. Fixed decisions

| Topic | Decision |
|---|---|
| Toolset | WiX v6, SDK-style `.wixproj` built with `dotnet build`. Pin the latest 6.x stable for both the SDK and `WixToolset.UI.wixext`, at the same version |
| Package | One per-machine MSI, x64, installed to `Program Files\MyApp` |
| Payload | The self-contained `build.bat` output in `artifacts\publish\win-x64`. No prerequisites, no bootstrapper bundle |
| UI | `WixUI_InstallDir` (welcome, license, folder, install) |
| Language | `ar-SA` installer (codepage 1256) |
| Shortcut | One Start Menu shortcut, advertised from the exe's component. No desktop shortcut |
| Upgrades | `MajorUpgrade` with a fixed `UpgradeCode` and `AllowSameVersionUpgrades` |
| User data | Never touched: `%LOCALAPPDATA%\MyApp` survives uninstall |

Licensing: WiX v6 adopted the Open Source Maintenance Fee for organizations that earn revenue from it. Confirm the current terms on wixtoolset.org before a commercial release and tell the user if they apply.

## 2. Files

The generator creates these from the template. Edit them in place; never rewrite them from memory.

| File | Contains |
|---|---|
| `installer/MyApp.Installer.wixproj` | WiX SDK and UI extension pinned to the same 6.x version, `ar-SA` culture, x64, version from `Directory.Build.props` |
| `installer/Package.wxs` | Per-machine package, the fixed `UpgradeCode` (generated once, never changed), `MajorUpgrade`, all publish files via `<Files>`, an advertised Start Menu shortcut in the exe's component, `WixUI_InstallDir` |
| `installer/Package.ar-SA.wxl` | Our strings with codepage 1256 (MSI databases aren't Unicode) |
| `installer/License.rtf` | License text. Edit in WordPad or Word, never by hand |

The installer isn't part of the solution. `package.bat` runs it after `build.bat`, because it packages the publish folder.

## 3. WinUI 3 projects

Nothing changes in the installer. The self-contained, unpackaged publish output already includes the Windows App SDK runtime, so the MSI is larger (about 40–60 MB more) and needs no prerequisites.

## 4. Arabic installer text

If the build reports missing ar-SA WixUI strings, add them to `Package.ar-SA.wxl` rather than switching culture.

## 5. Versioning and upgrades

- `Version` in `Directory.Build.props` is `MAJOR.MINOR.PATCH`, and nothing else sets a version.
- Windows Installer compares only the first three fields, with limits of 255, 255 and 65,535. Every MSI handed to anyone must have a higher version than the last one, or `MajorUpgrade` won't replace it.
- `UpgradeCode` never changes. The product code is generated per build automatically; don't set it.
- `AllowSameVersionUpgrades` lets a test build install over the same version; the ICE61 warning it triggers is expected. Upgrades from older versions are unaffected.
- Removing or renaming files is safe under `MajorUpgrade`, because the old version is removed first.

## 6. Signing

- Sign `MyApp.exe` after `build.bat` and before the MSI build, then sign the MSI. Use `signtool sign /fd SHA256 /tr <timestamp-url> /td SHA256 ...` with the certificate the user provides.
- Certificates and passwords never enter the repo. Add a signing step to `package.bat` only when the user supplies a certificate, and document it in `docs/release.md`.
- Unsigned builds work but trigger SmartScreen warnings. Say so whenever you hand over an unsigned MSI.

## 7. Install testing

Before each release, on a Windows 10 22H2 VM and on Windows 11, as a standard user (the MSI elevates itself):
1. Fresh install. Launch from the Start Menu. Check the Arabic installer text and the app's RTL layout.
2. Upgrade over the previous MSI. Data and settings must remain.
3. Uninstall. `Program Files\MyApp` is gone and `%LOCALAPPDATA%\MyApp` is untouched.
4. Install the older MSI over the newer one. It must show the downgrade message.

## 8. Rules

- No custom actions, services, drivers, firewall rules or extra registry writes, unless the user approves (agent rule 4).
- Never delete or modify user data (`%LOCALAPPDATA%\MyApp`) during install, upgrade or uninstall.
- No desktop shortcut, no auto-start entry, no "launch after install" checkbox unless the user asks.
- Keep `Package.wxs` under 150 lines. If it grows, move fragments into separate `.wxs` files in `installer/`.

## 9. Publishing to winget

winget is how most users install a desktop app. Keep the manifests in `packaging/winget/` (schema 1.6) and let CI submit them from the MSI it built:

- On a version tag, the release pipeline builds the MSI, publishes the GitHub release, then rewrites `PackageVersion`, `InstallerUrl`, `InstallerSha256`, `ProductCode` and the release-notes URL in the three manifest files.
- The SHA256 and the product code must come from the CI-built MSI, never from a local `package.bat` output: MSIs are not byte-reproducible, so the hash and product code differ per build.
- Submission runs `wingetcreate submit` with a classic GitHub token that has the `public_repo` scope, stored as the repository secret `WINGET_TOKEN`. Without the secret, skip the submission and leave the refreshed manifests for a manual run.
- **Never reference `secrets.*` in a step `if:`** — the workflow becomes invalid and fails at startup with zero jobs. Put the secret in a job-level `env` and test `env.TOKEN != ''` in the step instead.
- Manual fallback: download the MSI attached to the release, compute its SHA256, read its product code, update the three files, then run `wingetcreate submit --prtitle "New version: Publisher.MyApp version X.Y.Z" --token <token> packaging\winget`.
- winget's validation runs on the pull request; once it is merged, `winget install Publisher.MyApp` works.
