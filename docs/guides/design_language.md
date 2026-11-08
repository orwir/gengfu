# Gengfu design language

The *what* of Gengfu's interface: tokens, components, patterns. The *how* is
[design_workflow.md](design_workflow.md). The visual source of truth is the Claude
Design canvas (https://claude.ai/artifact/5VwWCavteqkpQQadJMEeBR — private until shared).

## Principles

- **Native Windows 11.** WPF Fluent look; no custom chrome beyond the title bar.
- **Compact.** One 380 px window. The clock and the next call are the only large things.
- **The clock mirrors the game.** Same format, counts down 35:00 → 00:00, rounds up
  (35:00 holds for the first second, 00:00 shows only at the end). Digits never animate.
- **System theme.** Light and dark follow Windows; the accent follows the Windows accent.
- **Two languages.** Every string ships in English and Russian; leave room for Russian,
  which runs about a quarter longer.

## Tokens

| Token           | Light   | Dark    | Used for                          | Fluent brush                          |
| --------------- | ------- | ------- | --------------------------------- | ------------------------------------- |
| surface.base    | #F3F3F3 | #202020 | window                            | SolidBackgroundFillColorBaseBrush     |
| surface.card    | #FBFBFB | #2B2B2B | cards                             | CardBackgroundFillColorDefaultBrush   |
| stroke.card     | #E5E5E5 | #1D1D1D | card border                       | CardStrokeColorDefaultBrush           |
| text.primary    | #1B1B1B | #FFFFFF | text                              | TextFillColorPrimaryBrush             |
| text.secondary  | #5F5F5F | #CFCFCF | captions, past calls              | TextFillColorSecondaryBrush           |
| text.disabled   | #A0A0A0 | #7A7A7A | disabled controls                 | TextFillColorDisabledBrush            |
| accent.fill     | #005FB8 | #60CDFF | primary button, next call, links  | AccentFillColorDefaultBrush; text: AccentTextFillColorPrimaryBrush |
| accent.on       | #FFFFFF | #000000 | text on accent                    | TextOnAccentFillColorPrimaryBrush     |
| status.success  | #0F7B0F | #6CCB5F | connected                         | SystemFillColorSuccessBrush           |
| status.caution  | #9D5D00 | #FCE100 | warnings                          | SystemFillColorCautionBrush           |
| status.critical | #C42B1C | #FF99A4 | errors, not connected             | SystemFillColorCriticalBrush          |

- **In code:** always the Fluent brush, by DynamicResource, so a switch of the Windows theme
  repaints it; the hex values are the default accent's. Hints use the Fluent InfoBar
  backgrounds: InfoBarInformationalSeverityBackgroundBrush and InfoBarWarningSeverityBackgroundBrush.
  The one exception is the title bar's close button: its red (#C42B1C on hover) and white
  glyph are Windows' own in both themes.

- **Type:** Segoe UI Variable only — Display for the clock, Text for everything else.
  Clock 64/72 Semibold with tabular figures · Subtitle 20/28 Semibold · Body strong
  14/20 Semibold · Body 14/20 · Caption 12/16.
- **Spacing:** 4 px grid — 4, 8, 12, 16, 24, 32. **Radius:** 4 controls, 8 cards and window.
- **Elevation:** cards get a 1 px stroke and no shadow; the window gets the system shadow.
- **Motion:** 167 ms fast, 250 ms normal, decelerate (0, 0, 0, 1).
- Text pairs pass 4.5:1 in both themes; controls are the Windows-standard 32 px tall.

## Components

- **Window:** 380 × 616 px; title bar = icon, name, minimize, close (no maximize).
  Settings replaces the main view in the same window, with a back arrow in the title bar,
  and scrolls; Esc goes back too. Gengfu draws the title bar on the flat surface.base, with
  no Mica: under a Mica frame Windows draws its own caption buttons as well. Minimize and
  close keep out of the Tab order, as Windows' own do; the back arrow takes focus, its
  focus ring drawn inside it.
- **Buttons:** accent (Start), standard (Stop, Connect, Change), subtle (Test call, the
  Settings icon), link (accent text: the status line's Settings, "Use a sound file
  instead"); Fluent rest / hover / pressed / disabled. Start and Stop are one button that
  flips, so keyboard focus stays on it. Test call is off unless Gengfu is connected with
  someone to hear it, and reads "Calling…" while its call plays. An icon and its label share
  the centre of what holds them, the label's capitals rather than its line: the IconLabel
  style lifts a 14 px label 1 px off its line's centre.
- **Clock digits:** minutes and seconds are fields that look like plain digits. Hover →
  subtle fill; focus → field fill, 2 px accent underline, whole value selected (accent fill,
  text on accent), the running clock shown until the first keystroke. Arrows above and
  below each nudge ±1 min / ±1 s; Tab skips them, since ↑ ↓ in a field do the same. Enter
  and Esc hand focus to the clock as a whole, so the digits read as plain text again.
  Disabled until Start.
- **Calls list:** one row per call: its time, its event's name and spawn, and how it stands —
  called (secondary, check), not sent (secondary, critical ✕ "Not sent"), calling (accent,
  speaker, tinted), next (accent, "in m:ss", tinted), later (primary). A call is not sent when
  it comes due with Gengfu off the server or nobody chosen to hear it, or when a correction
  takes the clock past its spawn first. When the calls don't fit, the card stops above the
  buttons and its rows scroll, keeping the call calling or next second from the top; it moves
  only when that call changes, so a scroll by wheel stays meanwhile.
- **Status line:** dot + text, caption size — connected, naming who hears (success);
  connecting (hollow dot); trying again, saying why (hollow caution dot, caution text);
  connected with nobody chosen (caution); not connected, saying why (critical). A Settings
  link follows only when the user has to act. Every state's copy is in the strings and on
  the canvas's Foundations.
- **Update notice:** a bar above the footer, in the card's colours with an accent "i", once a
  downloaded version is ready: it applies on next start, and Restart applies it now. It shows only
  while the clock is stopped, so a match is never interrupted. Restart closes Gengfu the usual way,
  off the server and saved, and Velopack opens it again.
- **App icon:** the title bar's gong mark in Windows blue, #0078D4, a fixed colour that reads on
  light and dark taskbars, since an icon can't follow the accent. Up to 32 px it has a simpler
  drawing of its own. `scripts/make-icon.cs` draws `Gengfu.ico`, 16–256 px; the title bar keeps
  the mark in the accent colour.
- **Settings:** grouped cards; short controls (drop-downs, toggles) on the right in a fixed
  column, text fields stacked. The column is 152 px beside the radios and 168 elsewhere:
  the Fluent drop-down keeps about 50 px for its chevron. Empty fields and drop-downs show a
  placeholder. One button connects with what the fields hold, or disconnects; it offers
  Connect again as soon as a field changes. Radios for who hears the calls: a server group,
  from the server's list, or chosen channels, ticked in a flyout in the server's tree order
  that stays open while ticking (Esc, Enter or a click outside closes it). Until Gengfu is
  connected the drop-downs are off; the server's lists are read once it is, and again whenever
  a list opens, and until they are in the group drop-down holds the group already chosen.
  Connecting to another server clears the choice, and the status line asks who hears the calls.
  General ends with the version and an Open settings file link, which opens the file in the app
  Windows uses for .json files, or in Notepad. Under the version, across the card, a caption says
  where updates stand: checking, up to date, downloading, installed, or couldn't check; a copy
  Setup didn't install shows none.
- **Sound file:** in place of Say and Voice, the file's name with a note icon and Change;
  "Use text instead" goes back to the text.
- **Hints:** an info bar with an accent "i" (the no-voice hint), or a caution bar with a
  "!" when a call plays the gong for another reason: no text, Windows failed to speak it,
  or the file can't be played.
- **Settings file bar:** when settings.json has an error, a caution bar sits under the title
  bar, above Settings' scroll, so it shows however far Settings is scrolled. It names the line
  and the setting, or the line alone for an error outside any setting, such as a missing comma,
  or says Gengfu can't read the file when Windows won't let it. It says Gengfu works with the
  settings shown and saves nothing until then, and links to Open settings file. The main window
  doesn't change for it.
- **Voice list:** "Gong (no voice)" first, then every installed voice as "name — language"
  (the language in its own tongue), the app language's first. Any voice may be picked.
- **No-voice hint:** when no voice speaks the app's language, Voice shows the gong and an
  info bar under it says so: accent info icon, caption text naming the way in (Language &
  region › the language › Language options), and a link that opens Language & region.
  Not Speech › Add voices, which can fail silently.

## Patterns

- **Sync:** Start sets 35:00. Correct with the arrows or by typing; every future call
  moves with the clock. Typed minutes keep the running seconds; typed seconds land on the
  nearest minute and are timed from the first keystroke (from Enter after a pause, for
  typing ahead). Enter applies, and so does leaving the field (timed from leaving after a
  pause of over 3 s); Esc cancels, ↑ ↓ nudge. Verify the rounding against the real game
  timer — the sync offsets depend on it.
- **No sync claims:** Gengfu can't see the game timer, so it shows no sync status and never
  says it is "in sync" — the clock itself is the feedback. Out-of-range typed values are
  ignored. No dialogs, no toasts; Windows' own file picker is the one exception.
- **Settings apply at once:** no Save or OK. A new language remakes the views; Settings stays
  where it was, with focus on Language. A call's default text follows the lead and the
  language until the user edits it.
- **Copy:** short and plain; units always written ("30 s" / "30 с"); in Russian, decimal
  comma and «» quotes. Event names and call text are user data, not copy; the file's own words,
  such as settings.json and a setting's key, stay as the file spells them in both languages.

## Screens

| Screen      | Design file (canvas)                          | Source                                                                                                              |
| ----------- | --------------------------------------------- | ------------------------------------------------------------------------------------------------------------------- |
| Main window | Main.dc.html (views: MainDark.dc.html, MainRetrying.dc.html, MainPreset.dc.html, MainPresetRu.dc.html, MainUpdate.dc.html, MainUpdateRu.dc.html) | [MainWindow.xaml](../../src/Gengfu.App/MainWindow.xaml), [MainView.xaml](../../src/Gengfu.App/Views/MainView.xaml), [ClockDigits.xaml](../../src/Gengfu.App/Controls/ClockDigits.xaml) |
| Settings    | Settings.dc.html (views: SettingsRu.dc.html, SettingsNoVoice.dc.html, SettingsVoiceList.dc.html, SettingsFirstRun.dc.html, SettingsDark.dc.html, SettingsSoundFile.dc.html, SettingsFileError.dc.html, SettingsFileErrorRu.dc.html, SettingsUpdate.dc.html, SettingsUpdateRu.dc.html) | [MainWindow.xaml](../../src/Gengfu.App/MainWindow.xaml), [SettingsView.xaml](../../src/Gengfu.App/Views/SettingsView.xaml) |
| App icon    | AppIcon.dc.html | [make-icon.cs](../../scripts/make-icon.cs), which draws `src/Gengfu.App/Gengfu.ico` |
