# WPF + WPF-UI vs WinUI 3

Contents
1. Decision rule
2. Plain-language summary for the user
3. Comparison
4. What changes in code with WinUI 3
5. Control equivalents
6. Tray icon (optional)

## 1. Decision rule

The UI framework is chosen once, when the project is created (`new-project.bat ... --ui wpf|winui`), and never changed afterwards. Switching later means rewriting every screen.

- Default to **WPF + WPF-UI**.
- Use **WinUI 3** only if the user explicitly wants it after hearing the summary below, or needs something only WinUI does well: touch and pen as the main input, or the exact Windows 11 look and motion.
- For apps that draw heavily (thousands of shapes), keep WPF: its custom-drawing path (`DrawingVisual`) handles that volume, and WinUI would need an extra library (Win2D) that isn't in the stack.

If the user hasn't chosen, ask once, using the summary below, and recommend WPF-UI.

## 2. Plain-language summary for the user

Relay this in Arabic, in your own words, in three or four short lines:

- **WPF-UI** (recommended): mature and fast to build with. It looks modern like Windows 11 and works the same on Windows 10. Smaller download. Supports Arabic-Indic digits. Best for apps with many forms, lists and large drawings.
- **WinUI 3**: Microsoft's newest technology, with the most native Windows 11 look, smoother animations and better touch support. The installer is bigger (about 40–60 MB more), development is slower, there are fewer ready-made add-ons, and some features (Arabic-Indic digits, very large custom drawings) need extra work.

## 3. Comparison

| Topic | WPF + WPF-UI 4.x | WinUI 3 (Windows App SDK 1.x) |
|---|---|---|
| Maturity | WPF since 2006, very stable; WPF-UI is a community library | Microsoft's current native UI; still evolving |
| Look | Fluent styling recreated by WPF-UI | Native Fluent controls and motion |
| Windows 10 support | 10 1809+ and 11; Mica on 11 only | 10 1809+ and 11; Mica on 11 only |
| Distribution | Self-contained exe, WiX MSI | Unpackaged + self-contained Windows App SDK, WiX MSI; larger output |
| Startup, memory | Fast with ReadyToRun; lower memory | Slightly heavier at startup |
| XAML | `{Binding}`, DataTriggers, implicit DataTemplates, `StringFormat` | `{x:Bind}` (compiled, typed), VisualStateManager, no DataTriggers |
| Localization markup | `{l:Tr Key}` | `{l:Tr Key=Key}` (named arguments only) |
| RTL | `FlowDirection` on the root, complete | `FlowDirection` on the root, complete |
| Arabic-Indic digits | Yes (`NumberSubstitution`) | No built-in way; setting hidden |
| Theme / accent | Live switching for both | Theme live; accent applied at startup (restart) |
| Dialogs | Native message box with RTL flags | `ContentDialog` (async, needs `XamlRoot`) |
| Custom drawing at scale | `DrawingVisual`, fine for 3,000+ items | Shapes and Canvas up to ~3,000; beyond that needs Win2D (not allowed) |
| Designer | Visual Studio XAML designer | No designer; hot reload only |
| Third-party controls | Large ecosystem | Smaller ecosystem |

## 4. What changes in code with WinUI 3

Everything in Core, Data, the tests, the docs, scripts and installer is identical. The scaffold puts the differences in `assets/template/winui/`.

- **Project:** `net10.0-windows10.0.19041.0`, `UseWinUI`, `WindowsPackageType=None`, `WindowsAppSDKSelfContained=true`, `RuntimeIdentifier=win-x64`. Assets such as fonts are `Content` and are referenced with `ms-appx:///`.
- **Partial classes:** every class that derives from or implements a WinUI type (`Window`, `UserControl`, `MarkupExtension`, `IValueConverter`) must be `partial`. This is a platform requirement, not a way around the file-size limits.
- **Bindings:** use `{x:Bind ViewModel.Prop, Mode=OneWay}`. Give each page a typed `public XViewModel ViewModel { get; }` property set in its constructor. Use `{Binding}` only where `x:Bind` can't reach.
- **Windows:** `Window` isn't a `FrameworkElement`. Set `DataContext`, `FlowDirection`, `RequestedTheme` and `Language` on the root element (`ApplyCultureDirection` and `ThemeService` already do this).
- **Startup:** there's no `OnExit`. Clean-up runs on `MainWindow.Closed`. A second instance exits silently.
- **Dialogs:** `IDialogService` is async, and the WinUI version uses `ContentDialog` with the window's `XamlRoot`. Never create a `ContentDialog` elsewhere.
- **UI thread:** use `DispatcherQueue`. `DataChangeNotifier` is created on the UI thread at startup.
- **Accent:** `ThemeService.ApplyAccentResources` sets the `SystemAccentColor*` resources in the `App` constructor. Changing the accent requires a restart.
- **Backdrop:** use `MicaBackdrop` when `MicaController.IsSupported()`; otherwise apply the theme's page background brush.
- **Navigation:** the built-in `NavigationView` with a `Frame`. Pages are resolved from DI.
- **Triggers:** WinUI has no DataTriggers. Use converters (`LoadStateVisible`) or VisualStateManager.

## 5. Control equivalents

| Need | WPF + WPF-UI | WinUI 3 |
|---|---|---|
| Window | `ui:FluentWindow` + `ui:TitleBar` | `Window` + `ExtendsContentIntoTitleBar` + `SetTitleBar` |
| Text input | `ui:TextBox` (`PlaceholderText`) | `TextBox` (`Header`, `PlaceholderText`) |
| Number | `ui:NumberBox` | `NumberBox` |
| Password | `ui:PasswordBox` | `PasswordBox` (`PasswordRevealMode`) |
| Drop-down | `ComboBox` | `ComboBox` (`Header`) |
| Search with suggestions | `ui:AutoSuggestBox` | `AutoSuggestBox` |
| Date | `DatePicker` | `CalendarDatePicker` |
| On/off setting | `ui:ToggleSwitch` | `ToggleSwitch` |
| Single choice | `RadioButton` group | `RadioButtons` |
| Primary button | `ui:Button Appearance="Primary"` | `Button Style="{StaticResource AccentButtonStyle}"` |
| Inline message | `ui:InfoBar` | `InfoBar` |
| Progress | `ui:ProgressRing` | `ProgressRing` |
| Text styles | `ui:TextBlock FontTypography="Title"` | `Style="{StaticResource TitleTextBlockStyle}"` |
| Navigation | `ui:NavigationView` (per the WPF-UI 4.x sample) | `NavigationView` |

For WPF-UI, check member names against the installed 4.x version (search its package XAML) before using a property for the first time. Never guess from 3.x examples.

## 6. Tray icon (optional)

Neither stack has a tray icon built in. Add one only when the product asks, and record it as an ADR first (agent rule 4).

- **Library:** `H.NotifyIcon` 2.x — `H.NotifyIcon.Wpf` for WPF, `H.NotifyIcon.WinUI` for WinUI. Nothing else in the stack provides a tray icon.
- **Icons:** ship four `.ico` files under `Assets/` — filled and outline, each in a light and a dark variant (`tray-light.ico`, `tray-dark.ico`, `tray-outline-light.ico`, `tray-outline-dark.ico`). The notification area follows the **taskbar** theme, which can differ from the app theme, so pick the file from `HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize` → `SystemUsesLightTheme` (not `AppsUseLightTheme`), never from `RequestedTheme`.
- **The taskbar theme can change while the app runs.** `ElementTheme.Default` follows Windows by itself, but the tray icon and any custom frame color do not. Watch for the change and reapply:
  - `WM_SETTINGCHANGE` with `lParam == "ImmersiveColorSet"` — Windows broadcasts it to top-level windows only, so a message-only window never sees it;
  - or a registry notification (`RegNotifyChangeKeyValue`) on the Personalize key, which fires immediately instead of waiting in the window message queue.
  Reapply on the UI thread (`DispatcherQueue` / `Dispatcher`), and only when the resolved icon file actually changed.
- Menu: Show/Hide, Settings, Exit; left-click does the default action. Register the icon in DI as a singleton and dispose it with the host — a collected instance loses the icon silently.
