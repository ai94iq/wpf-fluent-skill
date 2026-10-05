# UI and UX

XAML examples use WPF + WPF-UI (`ui:` = `http://schemas.lepo.co/wpfui/2022/xaml`). WinUI 3 differences are noted inline and mapped in `ui-frameworks.md`. `l:` is the localization namespace, `common:` is `MyApp.App.Common`.

Contents
1. Principles
2. Layout and spacing
3. Typography
4. Styling: modern, no slop
5. Icons
6. Theming and accent
7. Forms and validation
8. Input controls
9. Buttons and actions
10. Lists and tables
11. Feedback, confirmations, empty states
12. Accessibility and keyboard
13. RTL screen checklist

## 1. Principles

- **One job per screen, one primary action per view.** Secondary actions are visibly quieter.
- **Show, don't make users remember.** Defaults are preselected, labels are always visible, and recent values are kept.
- **Forgiving:** destructive actions are confirmed by name; everything else saves without ceremony.
- **Immediate feedback:** every click responds within 100 ms (pressed state, progress, or result). Anything over a second shows progress. Never freeze.
- **Errors say what to do,** next to the cause, in plain words.
- **Progressive disclosure:** advanced options go behind an "خيارات إضافية" expander, not on the main form.

## 2. Layout and spacing

- **4 px grid.** Use the tokens in `Styles.xaml`, never literal margins:

  | Token | Value |
  |---|---|
  | `PagePadding` | 24 |
  | `SectionSpacing` | 32 below each section |
  | `FieldSpacing` | 16 below each field |
  | `LabelSpacing` | 4 between a label and its field |
  | `CardRadius` | 8 |

- **Forms are a single column,** at most 640 px wide, starting at the reading start (the right edge in Arabic). Labels go above fields, never beside them; Arabic labels are long and side labels break in RTL.
- **Lists and content** stretch to the window, with a minimum window size of 800×500.
- **Page structure:** title (Title style), then an optional one-line description (Body, secondary text color), then sections, each with a Subtitle header.
- **Alignment:** everything aligns to the start. Center only empty states and loading indicators.
- Write layout in start/end terms. In RTL, WPF and WinUI mirror `HorizontalAlignment`, margins and column order automatically, so never hand-flip values.

## 3. Typography

Use the Fluent type ramp only:

| Use | WPF-UI | WinUI 3 |
|---|---|---|
| Page title | `ui:TextBlock FontTypography="Title"` | `TitleTextBlockStyle` |
| Section header | `FontTypography="Subtitle"` | `SubtitleTextBlockStyle` |
| Emphasis in body | `FontTypography="BodyStrong"` | `BodyStrongTextBlockStyle` |
| Body, labels | default (15 for Arabic) | `BodyTextBlockStyle` |
| Helper text, metadata | `FontTypography="Caption"` + secondary text brush | `CaptionTextBlockStyle` |

- Use at most two weights per screen (regular and semibold). No italics (Arabic has none), no ALL CAPS, and no custom font sizes outside the ramp.
- Long text wraps (`TextWrapping="Wrap"`). Truncate with an ellipsis only in lists, with the full text in a tooltip.
- Don't give Arabic text fixed heights; diacritics need vertical room.

## 4. Styling: modern, no slop

**Do:**
- Use the window backdrop (Mica on Windows 11, solid on Windows 10) with content directly on it.
- Group related content on a **card** only when grouping helps: card background brush, 1 px card stroke brush, `CardRadius`. Nothing else.
- Build hierarchy from typography and spacing, not color or boxes.
- Use **one accent color**, only for the primary button, selection, focus, links and toggles. Status colors (success, caution, critical) appear only in status messages.
- Use theme brushes for everything (`TextFillColorPrimaryBrush`, `TextFillColorSecondaryBrush`, `CardBackgroundFillColorDefaultBrush`, `CardStrokeColorDefaultBrush` and the other design-token brushes). Before the first use of a key in WPF-UI, confirm it exists in the installed theme dictionaries.
- Leave elevation to the system: flyouts, menus, dialogs and tooltips bring their own shadows.

**Never:**
- Gradients (backgrounds, buttons, text, borders), glows, neon, or glass effects beyond the window backdrop.
- Colored or tinted card backgrounds, "badges" everywhere, or rainbow category colors.
- `DropShadowEffect`, custom shadows, double or heavy borders, or thick dividers.
- Colored, filled or multi-color icons, emoji as icons, or mixed icon styles.
- Decorative illustrations, stock imagery or AI-generated art in the UI.
- Center-aligned forms or body text, or random sizes and weights.
- Bouncy, looping or attention-seeking animation. Rely on the controls' built-in transitions.

## 5. Icons

- **Only** `AppIcon` with Fluent UI System Icons, 24px Regular (outline): `<common:AppIcon Kind="Search" />`. Add new icons with `add-icons.bat ProjectRoot icon_name`; never paste path data by hand.
- **Sizes:**
  - 16 inside dense lists;
  - 20 default (buttons, navigation);
  - 24 for page-level or empty-state icons.

  Set `Width` and `Height` together.
- **Color** comes from the surrounding text automatically. Don't set `Foreground` except to use the secondary text brush.
- **RTL:** icons aren't mirrored. Set `IsDirectional="True"` only for icons that point a direction: arrows, chevrons, back/forward, send, undo/redo, sign out.
- **Primary actions** show icon + text. Icon-only buttons are allowed for well-known actions (search, close, more, settings) and need both a tooltip and an accessible name:

```xml
<ui:Button Appearance="Transparent"
           ToolTip="{l:Tr Common_Settings}"
           AutomationProperties.Name="{l:Tr Common_Settings}"
           Command="{Binding OpenSettingsCommand}">
  <common:AppIcon Kind="Settings" />
</ui:Button>
```

## 6. Theming and accent

- **Settings:**
  - Theme: System (default), Light or Dark.
  - Accent: the Windows accent (default, `Accent = null`) or one of `AccentPresets` (Blue, Teal, Green, Purple, Orange, Rose, Graphite). Every preset passes 4.5:1 with white text.

  Both are stored in `AppSettings`.
- **Applying:** `IThemeService.Attach(window)` runs once in `MainWindow`. A settings page calls `SettingsStore.Save(next)`, then `theme.Apply(next)`.
  - WPF applies theme and accent live.
  - WinUI applies the theme live; the accent needs a restart.
- **High Contrast:** Windows High Contrast always overrides the user's theme and accent. Never fight it.
- **Never hard-code colors.** The only exception is the accent swatches on the settings page.
- Test every screen in Light and Dark before committing UI work, and tell the user what to check.

**Settings section (WPF):**

```xml
<StackPanel Margin="{StaticResource SectionSpacing}">
  <ui:TextBlock FontTypography="Subtitle" Text="{l:Tr Settings_Appearance}" Margin="{StaticResource FieldSpacing}" />

  <TextBlock Text="{l:Tr Settings_Theme}" Margin="{StaticResource LabelSpacing}" />
  <ComboBox ItemsSource="{Binding ThemeOptions}" DisplayMemberPath="Label" SelectedValuePath="Value"
            SelectedValue="{Binding Theme}" Width="280" HorizontalAlignment="Left" Margin="{StaticResource FieldSpacing}" />

  <TextBlock Text="{l:Tr Settings_Accent}" Margin="{StaticResource LabelSpacing}" />
  <ItemsControl ItemsSource="{Binding AccentOptions}">
    <ItemsControl.ItemsPanel>
      <ItemsPanelTemplate><WrapPanel /></ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
    <ItemsControl.ItemTemplate>
      <DataTemplate>
        <!-- A swatch is the one place a literal color is allowed -->
        <RadioButton GroupName="Accent" IsChecked="{Binding IsSelected}" Command="{Binding SelectCommand}"
                     ToolTip="{Binding Label}" AutomationProperties.Name="{Binding Label}" Margin="0,0,8,8">
          <Ellipse Width="20" Height="20" Fill="{Binding Swatch}" />
        </RadioButton>
      </DataTemplate>
    </ItemsControl.ItemTemplate>
  </ItemsControl>
</StackPanel>
```

The ViewModel builds `AccentOptions` from `AccentPresets.All`, using `Tr.Get($"Accent_{preset.Key}")` for labels and a "Windows accent" option with `Hex = null`. `Swatch` is a hex string; WPF converts it to a brush in binding.

## 7. Forms and validation

**Field pattern (WPF): `FormField`** is a shared control in `Common/` with a label above, the input, and an error below. Add it when the first form is built.

```csharp
// Common/FormField.cs
public sealed class FormField : HeaderedContentControl
{
    public static readonly DependencyProperty ErrorProperty = DependencyProperty.Register(
        nameof(Error), typeof(string), typeof(FormField), new PropertyMetadata(null));

    public string? Error
    {
        get => (string?)GetValue(ErrorProperty);
        set => SetValue(ErrorProperty, value);
    }
}
```

```xml
<!-- Styles.xaml. Confirm the critical brush key in the installed WPF-UI theme before use. -->
<Style TargetType="common:FormField">
  <Setter Property="Focusable" Value="False" />
  <Setter Property="Margin" Value="{StaticResource FieldSpacing}" />
  <Setter Property="Template">
    <Setter.Value>
      <ControlTemplate TargetType="common:FormField">
        <StackPanel>
          <ContentPresenter ContentSource="Header" Margin="{StaticResource LabelSpacing}" />
          <ContentPresenter />
          <TextBlock x:Name="ErrorText" Text="{TemplateBinding Error}" TextWrapping="Wrap" Margin="0,4,0,0"
                     Foreground="{DynamicResource SystemFillColorCriticalBrush}" />
        </StackPanel>
        <ControlTemplate.Triggers>
          <Trigger Property="Error" Value="{x:Null}">
            <Setter TargetName="ErrorText" Property="Visibility" Value="Collapsed" />
          </Trigger>
        </ControlTemplate.Triggers>
      </ControlTemplate>
    </Setter.Value>
  </Setter>
</Style>
```

WinUI: use the control's built-in `Header` for the label and a `TextBlock` below it with `Style="{StaticResource ErrorTextStyle}"` (defined in `Styles.xaml`), made visible through an `x:Bind` function when the error isn't null.

**Validation lives in the ViewModel.** Errors stay hidden until the first save attempt, then update live. The Save button stays enabled; clicking it shows what's missing, which is clearer than a greyed-out button. Shared rules go in `Common/Validate.cs`, using resx keys such as `Validation_Required`:

```csharp
public static class Validate
{
    public static string? Required(string? value) =>
        string.IsNullOrWhiteSpace(value) ? Tr.Get("Validation_Required") : null;

    public static string? MaxLength(string? value, int max) =>
        value is { Length: var n } && n > max ? Tr.Format("Validation_MaxLength", max) : null;
}
```

```csharp
private bool _submitted;

[ObservableProperty]
[NotifyPropertyChangedFor(nameof(NameError))]
public partial string Name { get; set; }

public string? NameError => _submitted ? Validate.Required(Name) ?? Validate.MaxLength(Name, 100) : null;

[RelayCommand]
private async Task SaveAsync(CancellationToken ct)
{
    _submitted = true;
    OnPropertyChanged(nameof(NameError));
    if (NameError is not null) return;              // the view moves focus to the first error
    await _repo.SaveAsync(ToModel(), ct);
}
```

Form rules:
- Mark optional fields as "(اختياري)" rather than starring required ones, since most fields are required.
- Set `MaxLength` from the database column. Trim text on save.
- Enter submits single-line forms (`IsDefault="True"` on the primary button), and Esc cancels dialogs.
- Keep entered values after an error; never clear the form.

## 8. Input controls

**Text box:** the label sits above (`FormField`); the placeholder is an example, never the label.

```xml
<common:FormField Header="{l:Tr Person_GivenName}" Error="{Binding NameError}">
  <ui:TextBox Text="{Binding Name, UpdateSourceTrigger=PropertyChanged}"
              PlaceholderText="{l:Tr Person_GivenName_Example}" MaxLength="100" />
</common:FormField>
```

- LTR content (email, phone, URL, username, codes, IBAN, paths) uses `FlowDirection="LeftToRight"` plus an `InputScope` (`EmailSmtpAddress`, `TelephoneNumber`, `Url`, `Number`) for touch keyboards.
- Use `UpdateSourceTrigger=PropertyChanged` only when the ViewModel reacts while typing (search, live validation).

**Text area (multi-line):**

```xml
<ui:TextBox Text="{Binding Notes}" AcceptsReturn="True" TextWrapping="Wrap"
            MinHeight="96" MaxHeight="240" VerticalScrollBarVisibility="Auto" MaxLength="2000" />
```

Show a counter (`Tr.Format("Common_CharCount", n, max)`) only when the limit is likely to be reached.

**Number:** `ui:NumberBox` (WinUI `NumberBox`) with `Minimum`/`Maximum`. Never a free text box for numbers.

**Drop-down (ComboBox):** for one choice among 5 or more options. Options are a list of `Option<T>(T Value, string Label)` records in `Common/` with labels from `Tr`.

```xml
<ComboBox ItemsSource="{Binding CountryOptions}" DisplayMemberPath="Label"
          SelectedValuePath="Value" SelectedValue="{Binding Country}" />
```

- Preselect a sensible default.
- Order options logically (frequency or natural order); use alphabetical order only for long lists.
- Above about 15 options, use an `AutoSuggestBox` filtered with `ArabicText.NormalizeForSearch`.

**Single choice (2–4 options):** radio buttons, stacked vertically, all visible, one preselected. In WinUI use the `RadioButtons` control with `Header`.

**Multiple choice:**
- Up to about 7 options: a list of check boxes, each bound to a `Selectable<T>(T Value, string Label) { IsSelected }` item.
- More than 7: a `ListView` with `SelectionMode="Multiple"`, with the container's `IsSelected` bound to the item.

**Check box vs toggle switch:**
- `CheckBox` for a choice that's confirmed by Save, or for agreeing to something.
- `ui:ToggleSwitch` for a setting that applies immediately.
- Phrase labels positively ("إظهار التفاصيل", not "عدم إخفاء…"). The box sits on the reading-start side automatically in RTL.

**Date picker:** `DatePicker` in WPF, `CalendarDatePicker` in WinUI. Bind to `DateTime?` and convert to `DateOnly` in the ViewModel.

- Its calendar follows the app culture, which follows the system locale by default.
- Show the chosen date elsewhere through `IDateFormatter`.
- Partial dates (unknown month or day): a small composite input — a precision `ComboBox` (unknown, year, month, day), a year `NumberBox`, and month/day `ComboBox`es shown according to the precision. Store the precision alongside the date.

**Search:** `ui:AutoSuggestBox` or a text box with a leading `AppIcon Kind="Search"`. Search runs as the user types (300 ms debounce, Arabic-normalized), and Esc clears it.

**Login (only when the user asks for accounts or a password):**

```xml
<common:FormField Header="{l:Tr Login_Username}">
  <ui:TextBox Text="{Binding Username}" FlowDirection="LeftToRight" />
</common:FormField>
<common:FormField Header="{l:Tr Login_Password}" Error="{Binding LoginError}">
  <ui:PasswordBox x:Name="PasswordInput" PasswordChanged="OnPasswordChanged" FlowDirection="LeftToRight" />
</common:FormField>
<ui:Button Appearance="Primary" IsDefault="True" Content="{l:Tr Login_SignIn}" Command="{Binding SignInCommand}" />
```

```csharp
// Code-behind: passwords aren't bindable on purpose; hand the value to the ViewModel.
private void OnPasswordChanged(object sender, RoutedEventArgs e) =>
    ((LoginViewModel)DataContext).Password = PasswordInput.Password;
```

- Store only a PBKDF2 hash, never the password:

```csharp
public static class PasswordHasher   // Core/Security
{
    private const int Iterations = 600_000;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return string.Create(CultureInfo.InvariantCulture,
            $"pbkdf2-sha256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}");
    }

    public static bool Verify(string password, string stored)
    {
        var parts = stored.Split('$');
        if (parts.Length != 4 || parts[0] != "pbkdf2-sha256") return false;
        var expected = Convert.FromBase64String(parts[3]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromBase64String(parts[2]),
            int.Parse(parts[1], CultureInfo.InvariantCulture), HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
```

- Show one generic error ("اسم المستخدم أو كلمة المرور غير صحيحة") that never reveals which part was wrong.
- After 5 failed attempts, wait 30 seconds before allowing more.
- Keep the username after a failure; clear only the password.
- Use the built-in reveal button. Never log usernames or passwords.

## 9. Buttons and actions

- **One primary** button per view (`Appearance="Primary"`; WinUI `AccentButtonStyle`). Everything else uses the default appearance; toolbars and rows use `Transparent`/subtle.
- **Labels are verbs** naming the result: "حفظ", "إضافة شخص", "تصدير". Not "موافق" for actions.
- **Placement:** form buttons sit at the end of the form in a horizontal stack aligned to the end, primary first in reading order ([حفظ] [إلغاء]). RTL mirroring places them correctly.
- **Busy state:** while a command runs, the button is disabled automatically. Show a small `ProgressRing` bound to `XCommand.IsRunning` for anything over a second.
- **Destructive actions** (delete) are never primary and always go through `ConfirmAsync`, which names the item.

## 10. Lists and tables

- `ListView` with virtualization (WPF: `VirtualizingPanel.VirtualizationMode="Recycling"`). Each item shows an icon or avatar, a primary line and a secondary line in Caption style, with comfortable padding (12 vertical, 16 horizontal).
- Selection, hover and focus visuals come from the theme. Don't restyle them.
- Use a `DataGrid` (WPF only) only for spreadsheet-style editing. WinUI has no built-in grid, so use a `ListView` with column-aligned item templates.
- Numbers and dates in columns align to the end. Column order mirrors in RTL automatically.
- Paging and search follow `code-patterns.md` (keyset, debounce).

## 11. Feedback, confirmations, empty states

- **Loading, empty, error:** always `LoadStateOverlay` driven by `PageViewModel`.
  - Empty text says what's missing and what to do ("لا يوجد أشخاص بعد. أضف أول شخص."). Show it with one primary action, plus an optional 24 px outline icon.
  - Errors offer Retry.
- **Success:** a short `InfoBar` (severity Success) at the top of the content that closes itself after about 4 seconds. Don't show one for trivial saves.
- **Page-level warnings and errors:** an `InfoBar` that stays until dismissed or fixed.
- **Inline field and save errors** are red in every theme: the WPF `FormField` error line, or WinUI `TextBlock Style="{StaticResource ErrorTextStyle}"`. Never the default text color.
- **Confirm by name:** "حذف «أحمد بن علي»؟ لا يمكن التراجع عن ذلك." with buttons [حذف] [إلغاء], defaulting to Cancel.
- Never use a dialog for something an inline message can say.

## 12. Accessibility and keyboard

- Everything works with the keyboard. Tab order follows reading order (mirrored in RTL automatically, so never hard-code `TabIndex`). Esc closes, Enter confirms.
- Focus visuals stay visible; never set `FocusVisualStyle="{x:Null}"`.
- Every icon-only control and image has `AutomationProperties.Name`. Fields are labeled with `AutomationProperties.LabeledBy` or a `Header`.
- Text contrast is at least 4.5:1, which theme brushes already meet. Never carry meaning by color alone; pair it with an icon or text.
- Click targets are at least 32×32 px (40 for touch-first WinUI screens).

## 13. RTL screen checklist

Check every new screen in Arabic and English:
1. The layout reads from the right in Arabic, with nothing hand-mirrored.
2. Logos and photos are not mirrored; directional icons are.
3. LTR fields (email, phone, codes) type left-to-right.
4. Mixed Arabic/English text keeps punctuation in place.
5. Dates come from `IDateFormatter`; digits follow the setting.
6. Nothing is clipped (Arabic marks, longer English labels).
7. Light and Dark both look right; High Contrast is usable.
