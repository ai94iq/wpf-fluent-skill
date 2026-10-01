# Code patterns

The template is the source of truth for infrastructure code. This file indexes it and adds the patterns every feature reuses. Replace `MyApp` with the product name.

Contents
1. Template index
2. Page with loading states
3. Paged list
4. Search with debounce
5. Lazy tree node
6. Repository: SQL, cache, change notification
7. Tests: ViewModel, repository, Core
8. resx entries and plurals

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

## 2. Page with loading states

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

## 3. Paged list

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

## 4. Search with debounce

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

## 5. Lazy tree node

A placeholder child shows the expander; children load on first expand. Compute `hasChildren` with `EXISTS (...)` in the query that loads the node. WPF binds `IsExpanded` in the `TreeView` item container style; WinUI uses `TreeView` with `HasUnrealizedChildren` and the `Expanding` event.

```csharp
public sealed partial class NodeViewModel : ObservableObject
{
    private readonly Func<CancellationToken, Task<IReadOnlyList<NodeViewModel>>>? _loadChildren;
    private readonly ILogger _log;
    private bool _loaded;

    public NodeViewModel(string title, bool hasChildren,
        Func<CancellationToken, Task<IReadOnlyList<NodeViewModel>>>? loadChildren, ILogger log)
    {
        Title = title;
        _loadChildren = loadChildren;
        _log = log;
        if (hasChildren) Children.Add(new NodeViewModel("…", false, null, log));
    }

    public string Title { get; }

    public ObservableCollection<NodeViewModel> Children { get; } = [];

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    async partial void OnIsExpandedChanged(bool value)
    {
        if (!value || _loaded || _loadChildren is null) return;
        _loaded = true;
        try
        {
            var items = await _loadChildren(CancellationToken.None);
            Children.Clear();
            foreach (var child in items) Children.Add(child);
        }
        catch (Exception ex)
        {
            _loaded = false;
            _log.LogError(ex, "Loading children failed for node {Title}", Title);
        }
    }
}
```

## 6. Repository: SQL, cache, change notification

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

## 7. Tests: ViewModel, repository, Core

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

## 8. resx entries and plurals

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
