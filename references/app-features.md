# Optional features

Patterns for features many products need but the base template does not include. Add one only when the product asks, and record the decision as an ADR (`docs/decisions/`, agent rule 4).

Contents
1. Update check (GitHub releases)
2. First-run welcome
3. Encrypted database

## 1. Update check (GitHub releases)

A setting with a "Check now" button that compares the running build with the newest GitHub release. Never download or install anything automatically.

```csharp
// App/Services/IUpdateChecker.cs
public interface IUpdateChecker
{
    // The newer version when one exists; null when up to date. Throws when the check itself fails.
    Task<string?> NewerVersionAsync(CancellationToken ct);
}
```

```csharp
// App/Services/UpdateChecker.cs
// The running build's version, e.g. "0.2.0".
public static string CurrentVersion { get; } =
    typeof(UpdateChecker).Assembly.GetName().Version is { } version
        ? $"{version.Major}.{version.Minor}.{version.Build}"
        : "0.0.0";

public async Task<string?> NewerVersionAsync(CancellationToken ct)
{
    using var request = new HttpRequestMessage(
        HttpMethod.Get, "https://api.github.com/repos/MyOwner/MyApp/releases/latest");
    request.Headers.UserAgent.ParseAdd("MyApp");     // GitHub rejects requests without one
    using var response = await Http.SendAsync(request, ct);
    response.EnsureSuccessStatusCode();

    using var document = await JsonDocument.ParseAsync(
        await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
    var tag = document.RootElement.TryGetProperty("tag_name", out var value) ? value.GetString() : null;
    return IsNewer(tag, CurrentVersion) ? tag!.TrimStart('v', 'V') : null;
}

public static bool IsNewer(string? tag, string current) =>
    tag is not null
    && Version.TryParse(tag.TrimStart('v', 'V'), out var latest)
    && Version.TryParse(current, out var mine)
    && latest > mine;
```

- One `HttpClient` with a short timeout (about 15 s) as a static field.
- Keep `IsNewer` static and testable; cover newer, older, equal, a bad tag and a `v` prefix with `[Theory]` cases.
- On a newer version show the version and a link to the release page; on failure show a one-line "couldn't check" with Retry. Log the outcome, never personal data.
- Persist an opt-out in `AppSettings`; run the check on demand, at most once per launch.

## 2. First-run welcome

A small window, shown once, that explains what the app does and how to get back to it.

- Add `WelcomeShown` (bool) to `AppSettings`. Set it immediately after showing the window so a crash cannot show it twice.
- Show it after the host is up and after migrations, before the user can interact with the main content (WinUI: activate a second `Window` and attach the theme service; WPF: `ShowDialog()`).
- Content: one or two sentences, the hotkey if the app has one, one primary button. No feature tour, no blocking work.
- Never gate startup on it: if it fails to show, the app must still start.

## 3. Encrypted database

For products that store personal data and should protect it at rest; SQLCipher encrypts the whole file.

- Packages: use `Microsoft.Data.Sqlite.Core` instead of `Microsoft.Data.Sqlite`, plus `SQLitePCLRaw.bundle_e_sqlcipher`; call `SQLitePCL.Batteries_V2.Init()` before the first connection.
- Key: 32 random bytes as hex, protected with `ProtectedData.Protect(..., DataProtectionScope.CurrentUser)` (DPAPI), stored beside the database (`key.bin`). Deleting the file makes the data unreadable, so back it up with the database. Never log it, never put it in the settings file.
- `DataOptions` gains `string? EncryptionKey = null` (tests keep it null).
- The connection factory:

```csharp
var builder = new SqliteConnectionStringBuilder
{
    DataSource = options.DatabasePath,
    ForeignKeys = true,
    Pooling = true,
};
if (options.EncryptionKey is not null) builder.Password = options.EncryptionKey;
```

Microsoft.Data.Sqlite sends `PRAGMA key` before anything else and only once per physical connection, so pooling stays correct. After opening, keep plaintext off the disk:

```sql
PRAGMA temp_store = MEMORY;              -- no plaintext temp files
PRAGMA cipher_memory_security = ON;      -- scrub freed pages and keys
```

- Encrypting an existing plaintext database (the upgrade path): a SQLCipher file has a random header, so a file starting with `SQLite format 3\0` is plaintext. If so: open it normally, `ATTACH DATABASE '<new file>' AS encrypted KEY '<key>';`, `SELECT sqlcipher_export('encrypted');`, copy `PRAGMA user_version` over manually (the export does not), `DETACH`, close all connections, replace the file, then delete its `-wal`/`-shm` sidecars and any plaintext backups.
