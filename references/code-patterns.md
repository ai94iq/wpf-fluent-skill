# Code patterns

The template is the source of truth for infrastructure code. This file indexes it and holds the coding rules every feature reuses. Replace `MyApp` with the product name.

Contents
1. Template index
2. Solution layout
3. DRY and KISS
4. Code conventions and file size
5. Testing: test-first for every feature
6. Threading
7. Page with loading states
8. Paged list
9. Search with debounce
10. Repository: SQL, cache, change notification
11. SQLite with Dapper
12. Lazy loading and caching
13. Logging
14. Tests: ViewModel, repository, Core
15. resx entries and plurals

## 1. Template index

New projects get all of this from `new-project.bat`. In an existing project, copy the file you need from the skill's `assets/template/common/` or `assets/template/<wpf|winui>/` and replace `__Product__`. Never retype these from memory.

| File (under `src/MyApp.App/` unless noted) | Purpose |
|---|---|
| `App.xaml.cs` | Startup order, single instance, exception handlers |
| `Hosting/ServiceRegistration.cs` | All DI registrations; add new services here |
| `Hosting/AppPaths.cs`, `AppSettings.cs`, `SettingsStore.cs` | Data folders, settings record, atomic JSON save |
| `Hosting/Culture.cs`, `WindowExtensions.cs` | Culture, Gregorian calendar, RTL, fonts, digits |
| `Hosting/AppLogging.cs`, `LazyService.cs` | Serilog setup; `Lazy<T>` injection |
| `Localization/Tr.cs`, `TrExtension.cs` | `Tr.Get/Format/Plural`; `{l:Tr ...}` |
| `Services/DialogService.cs`, `DateFormatter.cs`, `ThemeService.cs`, `DataChangeNotifier.cs` | Dialogs, dates, theme/accent, UI-thread change messages |
| `Services/IIncrementalSource.cs` | Contract for paged lists |
| `Common/PageViewModel.cs`, `LoadState.cs`, `LoadStateOverlay.xaml`, `LoadStateToVisibilityConverter.cs` | Loading, empty, error, retry |
| `Common/AppIcon.cs`, `Resources/Icons.g.cs`, `Resources/icons.txt` | Outline icons (regenerate with `add-icons.bat`) |
| `Resources/Styles.xaml` | Converter, spacing tokens, (WinUI) font |
| `src/MyApp.Core/Text/ArabicText.cs`, `ArabicPlural.cs` | Arabic search normalization, plural forms |
| `src/MyApp.Core/Theming/*` | Accent presets and shades |
| `src/MyApp.Core/Caching/MemoryCacheExtensions.cs` | `GetOrLoadAsync` |
| `src/MyApp.Data/SqliteConnectionFactory.cs`, `DatabaseInitializer.cs` | Connections, WAL, migrations, backups |
| `tests/MyApp.Data.Tests/TempDatabase.cs`, `tests/MyApp.App.Tests/TestPaths.cs` | Test fixtures |

## 2. Solution layout

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
│   │   ├── Resources/       Strings.resx (neutral), Strings.en.resx, Styles.xaml, Icons.g.cs, icons.txt
│   │   ├── Services/        DialogService, DateFormatter, ThemeService, DataChangeNotifier
│   │   └── Shell/           MainWindow, MainWindowViewModel
│   ├── MyApp.Core/  pure C#: Models, Abstractions, Caching, Text, Theming
│   └── MyApp.Data/  SQLite + Dapper: Migrations/, Repositories/, connection factory, initializer
├── tests/           MyApp.Core.Tests, MyApp.Data.Tests, MyApp.App.Tests
├── installer/       WiX v6 project
└── artifacts/       build output (git-ignored)
```

Dependency rules: **Core** (`net10.0`) references nothing; **Data** references Core; **App** references both. The `net10.0` targets make it impossible for UI types to leak into Core or Data. New screens go in `Features/<Name>/`. Code used by two or more features goes in `Common/` (UI) or Core (logic).

The template lives in the skill folder: `assets/template/common/` (shared) plus `assets/template/wpf/` or `assets/template/winui/` (UI-specific files).

## 3. DRY and KISS

**DRY: one place per concern.** Before writing a helper, search for an existing one.

| Concern | The one place |
|---|---|
| User-visible text | `Tr` + resx |
| Dates on screen | `IDateFormatter` |
| Dialogs | `IDialogService` |
| Theme and accent | `IThemeService` + `AccentPresets` |
| Loading, empty, error, retry | `PageViewModel` + `LoadStateOverlay` |
| Icons | `AppIcon` + `Icons.g.cs` |
| Spacing, corner radius | tokens in `Styles.xaml` |
| Form label + error | `FormField` (section 7 of `ui-ux.md`) |
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

## 4. Code conventions and file size

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

## 5. Testing: test-first for every feature

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
- XAML layout and visuals aren't unit-tested. After a UI change, tell the user in one line what to look at: the screen in each shipped language, light and dark.
- `[Fact(Skip = ...)]` and deleting failing tests need the user's approval.

## 6. Threading

- The UI thread does UI work only. CPU-heavy work runs in `Task.Run`.
- Repositories run Dapper's synchronous API inside `Task.Run`, because `Microsoft.Data.Sqlite` async calls are synchronous anyway.
- ViewModels never use `ConfigureAwait(false)`. Core and Data always do.
- Never call `.Result` or `.Wait()`.

## 7. Page with loading states

```csharp
// Features/People/PeopleViewModel.cs
public sealed partial class PeopleViewModel : PageViewModel
{
    private readonly IPersonRepository _repo;

    public PeopleViewModel(IPersonRepository repo, ILogger<PeopleViewModel> log) : base(log)
    {
        _repo = repo;
        ReloadOnChange("person");
    }

    public ObservableCollection<PersonListItem> Items { get; } = [];

    protected override async Task<bool> LoadCoreAsync(CancellationToken ct)
    {
        var page = await _repo.GetPageAsync(null, 50, ct);
        Items.Clear();
        foreach (var item in page) Items.Add(item);
        return Items.Count > 0;
    }
}
```

WPF view:

```xml
<Grid Margin="{StaticResource PagePadding}">
  <ListView ItemsSource="{Binding Items}"
            Visibility="{Binding State, Converter={StaticResource LoadStateVisible}, ConverterParameter='Idle,Loading,Loaded'}" />
  <common:LoadStateOverlay State="{Binding State}"
                           ErrorMessage="{Binding ErrorMessage}"
                           EmptyText="{l:Tr People_Empty}"
                           RetryCommand="{Binding ReloadCommand}" />
</Grid>
```

WinUI view: the same structure with `x:Bind`, for example `State="{x:Bind ViewModel.State, Mode=OneWay}"`, and `{l:Tr Key=People_Empty}`. The page constructor sets `public PeopleViewModel ViewModel { get; }`.

Call `await ViewModel.EnsureLoadedAsync()` from the page's navigated-to hook (WPF-UI: check the navigation-aware interface's member names in the installed 4.x version; WinUI: `OnNavigatedTo`).

MVVM rules:
- Every page ViewModel derives from `PageViewModel` and implements `LoadCoreAsync(ct)`, returning `false` when there's nothing to show. `EnsureLoadedAsync()` loads once; `ReloadOnChange("area")` reloads when other screens change the data.
- The page XAML shows content only in states `Idle`, `Loading` and `Loaded`, with `LoadStateOverlay` on top (spinner, empty text, error with Retry).
- `[ObservableProperty]` goes on partial properties, never fields. Set defaults in the constructor.
- ViewModels never reference UI types (`MessageBox`, `Brush`, `Visibility`, `Dispatcher`, `Window`). Use services.
- Buttons that run async work bind to the command. Show progress with `XCommand.IsRunning`; re-entry is already blocked.
- An initial bulk load assigns a new collection. Paged lists append pages through `IIncrementalSource`.
- Validation lives in the ViewModel as `XError` string properties (null when valid), with `CanSave` derived from them (`ui-ux.md`).
- Code-behind only for view concerns (focus, scroll, `PasswordBox`). Cross-ViewModel messages use `WeakReferenceMessenger`.

Startup: the template's `App.xaml.cs` runs, in order: settings → culture and RTL → logging and exception handlers → single-instance check → host and DI → backup and migrate off the UI thread → show the main window. Pages load their own data when navigated to, never in constructors. DI lifetimes: shell, repositories, factories and services are Singleton; pages and their ViewModels are Transient.

## 8. Paged list

Repository: keyset paging (never `OFFSET`), with an index on `(NameSearch, Id)`:

```csharp
public Task<IReadOnlyList<PersonListItem>> GetPageAsync(PageCursor? after, int pageSize, CancellationToken ct) =>
    Task.Run<IReadOnlyList<PersonListItem>>(() =>
    {
        using var connection = factory.Open();
        return connection.Query<PersonListRow>(
                """
                SELECT Id, GivenName, FamilyName, NameSearch
                FROM Person
                WHERE (NameSearch, Id) > (@key, @id)
                ORDER BY NameSearch, Id
                LIMIT @pageSize
                """,
                new { key = after?.Key ?? string.Empty, id = after?.Id ?? 0L, pageSize })
            .Select(r => new PersonListItem(r.Id, r.GivenName, r.FamilyName, new PageCursor(r.NameSearch, r.Id)))
            .ToList();
    }, ct);

// Data row: long/double/string/byte[] only, so Dapper's constructor mapping matches SQLite types.
internal sealed record PersonListRow(long Id, string GivenName, string? FamilyName, string NameSearch);
```

ViewModel: also implement `IIncrementalSource` and add:

```csharp
private PageCursor? _cursor;

[ObservableProperty]
public partial bool HasMore { get; set; }       // set true in LoadCoreAsync when a full page arrives

[RelayCommand]
private async Task LoadMoreAsync(CancellationToken ct)
{
    var page = await _repo.GetPageAsync(_cursor, 50, ct);
    foreach (var item in page) Items.Add(item);
    if (page.Count > 0) _cursor = page[^1].Cursor;
    HasMore = page.Count == 50;
}
```

View (WPF code-behind; in WinUI handle the inner `ScrollViewer.ViewChanged`):

```csharp
private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
{
    if (e.VerticalOffset + e.ViewportHeight < e.ExtentHeight - 200) return;
    if (DataContext is IIncrementalSource { HasMore: true } source && source.LoadMoreCommand.CanExecute(null))
        source.LoadMoreCommand.Execute(null);
}
```

## 9. Search with debounce

The repository normalizes the query with `ArabicText.NormalizeForSearch`.

```csharp
private CancellationTokenSource? _searchCts;

partial void OnQueryChanged(string value) => _ = SearchDebouncedAsync(value);

private async Task SearchDebouncedAsync(string query)
{
    _searchCts?.Cancel();
    var cts = _searchCts = new CancellationTokenSource();
    try
    {
        await Task.Delay(300, cts.Token);
        Results = await _repo.SearchAsync(query, 50, cts.Token);
    }
    catch (OperationCanceledException)
    {
        // superseded by a newer query
    }
}
```

## 10. Repository: SQL, cache, change notification

```csharp
public sealed class PersonRepository(
    SqliteConnectionFactory factory, IMemoryCache cache, IDataChangeNotifier changes) : IPersonRepository
{
    public Task<PersonDetails?> GetAsync(long id, CancellationToken ct) =>
        cache.GetOrLoadAsync($"person:{id}", () => Task.Run(() => Load(id)), TimeSpan.FromMinutes(10))
             .WaitAsync(ct);

    public Task UpdateNameAsync(long id, string givenName, string? familyName, CancellationToken ct) =>
        Task.Run(() =>
        {
            using var connection = factory.Open();
            connection.Execute(
                """
                UPDATE Person SET GivenName = @givenName, FamilyName = @familyName, NameSearch = @nameSearch
                WHERE Id = @id
                """,
                new { id, givenName, familyName, nameSearch = ArabicText.NormalizeForSearch($"{givenName} {familyName}") });
            cache.Remove($"person:{id}");
            changes.Notify(new DataChanged("person", id));
        }, ct);

    private PersonDetails? Load(long id) { /* Dapper query → row → domain record */ }
}
```

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

## 13. Logging

- Use message templates, never interpolation: `log.LogInformation("Loaded {Count} rows", count)`. Inject `ILogger<T>`; static `Log` appears only in `App.xaml.cs`.
- Logs go to `%LOCALAPPDATA%\MyApp\logs`, daily, keeping 14 files. Never log names, dates of birth, passwords or photos; log IDs instead.

## 14. Tests: ViewModel, repository, Core

ViewModel (substitute the interfaces, assert observable behavior):

```csharp
public sealed class PeopleViewModelTests
{
    [Fact]
    public async Task No_people_gives_empty_state()
    {
        var repo = Substitute.For<IPersonRepository>();
        repo.GetPageAsync(null, 50, Arg.Any<CancellationToken>()).Returns([]);
        var vm = new PeopleViewModel(repo, NullLogger<PeopleViewModel>.Instance);

        await vm.EnsureLoadedAsync();

        Assert.Equal(LoadState.Empty, vm.State);
    }
}
```

Repository (a real database file with all migrations applied):

```csharp
public sealed class PersonRepositoryTests
{
    [Fact]
    public async Task Search_finds_names_regardless_of_tashkeel()
    {
        using var db = new TempDatabase();
        var repo = new PersonRepository(db.Factory, new MemoryCache(new MemoryCacheOptions()), Substitute.For<IDataChangeNotifier>());
        await repo.AddAsync(new NewPerson("مُحَمَّد"), TestContext.Current.CancellationToken);

        var results = await repo.SearchAsync("محمد", 10, TestContext.Current.CancellationToken);

        Assert.Single(results);
    }
}
```

In xUnit v3, pass `TestContext.Current.CancellationToken` to async calls (its analyzer warns otherwise, and warnings are errors).

Core: plain `[Theory]` + `[InlineData]` tables, as in `ArabicTextTests` and `ArabicPluralTests`.

## 15. resx entries and plurals

Add every new key to both files, keeping them sorted. `LocalizationTests` fails otherwise.

```xml
<!-- Strings.resx (Arabic) -->
<data name="People_Count_zero" xml:space="preserve"><value>لا يوجد أشخاص</value></data>
<data name="People_Count_one" xml:space="preserve"><value>شخص واحد</value></data>
<data name="People_Count_two" xml:space="preserve"><value>شخصان</value></data>
<data name="People_Count_few" xml:space="preserve"><value>{0} أشخاص</value></data>
<data name="People_Count_many" xml:space="preserve"><value>{0} شخصًا</value></data>
<data name="People_Count_other" xml:space="preserve"><value>{0} شخص</value></data>
```

English fills the same six keys (`zero`: "No people", `one`: "1 person", the others: "{0} people"). Use them with `Tr.Plural("People_Count", count)`.
