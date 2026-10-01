# Localization, Arabic and RTL

Read this when adding strings, dates, numbers or any right-to-left UI. The rules apply to every app: no user-visible string outside resx, dates and numbers only through services. The Arabic and RTL rules apply whenever the app ships a right-to-left UI — the template supports it out of the box, it isn't required.

## Strings

- `Strings.resx` (neutral language, Arabic in the template) and `Strings.en.resx` (satellite) hold identical, sorted keys. There's no Designer file; edit the XML directly.
- Keys follow `Feature_Element_Purpose`.
- XAML: `{l:Tr Key}` in WPF, `{l:Tr Key=Key}` in WinUI. C#: `Tr.Get`, `Tr.Format`, `Tr.Plural(base, n)`.
- Plurals use the six Arabic plural keys `_zero/_one/_two/_few/_many/_other` through `Tr.Plural`; never concatenate a number with a word.
- New terminology goes into the glossary in `docs/localization.md` first, then resx.

## Culture

- The `Language` setting is empty by default: the app follows the system locale for dates, numbers, calendar and direction. Setting it to a specific culture (e.g. `ar-SA`, `en-US`) overrides the system; changing it restarts the app.
- The system's calendar is respected as-is (Saudi Arabia defaults to Umm al-Qura, most locales to Gregorian). Never force or convert calendars in app code.

## Dates and digits

- Dates only through `IDateFormatter`, which uses the current culture's standard patterns and so follows the system locale automatically. Never format dates in XAML.
- Digits: Western by default. Arabic-Indic is a WPF-only setting, done by number substitution.

## Direction (RTL)

- Every window calls `this.ApplyCultureDirection()`. Never set RTL on individual controls. Exceptions:
  - images and logos → `FlowDirection="LeftToRight"`;
  - LTR-content inputs (email, URL, phone, path, code, password, username) → `FlowDirection="LeftToRight"`;
  - directional icons → `IsDirectional="True"`;
  - dialogs → `IDialogService`.
- Mixed text: put LTR fragments in their own `Run` with `FlowDirection="LeftToRight"`, or follow them with RLM (`\u200F`).

## Fonts

- Arabic UIs: Noto Sans Arabic with Segoe UI as fallback, applied once on the window root. Other languages: the system UI font.
- Base size 15. Never italic.
