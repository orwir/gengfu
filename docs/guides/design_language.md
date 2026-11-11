# Gengfu design language

The *what* of Gengfu's interface: tokens, components, patterns. The *how* is
[design_workflow.md](design_workflow.md). The visual source of truth is the Claude
Design canvas (https://claude.ai/artifact/5VwWCavteqkpQQadJMEeBR — private until shared). It
has a page per surface (Main window, Settings, Foundations), each with its state boards grouped
by kind, one notes card, and the options a fork turned down, labelled NOT TAKEN.

## Principles

- **Native Windows 11.** WPF Fluent look; no custom chrome beyond the title bar.
- **Compact.** One 380 px window. The clock is the only large thing; the next call stands out in
  the list.
- **The clock mirrors the game.** Same format, counts down 35:00 → 00:00, rounds up
  (35:00 holds for the first second, 00:00 shows only at the end). Digits never animate.
- **Theme.** Light and dark follow Windows' app mode unless Settings › General picks one; the
  accent follows the Windows accent in either. Windows' high contrast wins over the pick, and
  Windows' own file picker keeps Windows' theme.
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

- **In code:** always the Fluent brush, by DynamicResource, so a switch of theme, Windows' or the
  one Settings picks, repaints it; the hex values are the default accent's. Hints use the Fluent
  InfoBar backgrounds: InfoBarInformationalSeverityBackgroundBrush and
  InfoBarWarningSeverityBackgroundBrush; how a privilege key went, InfoBarSuccessSeverityBackgroundBrush
  and InfoBarErrorSeverityBackgroundBrush.
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
- **Buttons:** accent (Start), standard (Stop, Connect, Change, Add event), subtle (Test call,
  the Settings icon, an event's bin), link (accent text: the status line's Settings, "Use a sound
  file instead", Back to default); Fluent rest / hover / pressed / disabled. Start and Stop are
  one button that flips, so keyboard focus stays on it. Test call is off unless Gengfu is
  connected with someone to hear it and there is an event to call. While a Test call plays, the
  button calling reads "Calling…" and stays on, so keyboard focus stays on it too, but a press does
  nothing until the call ends; the others are off meanwhile. The button calling is the one
  pressed, or, for a call pressed in the other view, the main window's Test call or the Calls
  card's in Settings. An icon and its label share
  the centre of what holds them, the label's capitals rather than its line: the IconLabel
  style lifts a 14 px label 2.2 px beside its 16 px icon, and both hang from the top of a
  panel that centres the pair, so rounding to device pixels moves them together.
- **Clock digits:** minutes and seconds are fields that look like plain digits. Hover →
  subtle fill; focus → field fill, 2 px accent underline, whole value selected (accent fill,
  text on accent), the running clock shown until the first keystroke. Arrows above and
  below each nudge ±1 min / ±1 s; Tab skips them, since ↑ ↓ in a field do the same. Enter
  and Esc hand focus to the clock as a whole, so the digits read as plain text again; Tab from
  there goes on from the field left, minutes to seconds to Stop, as from the field itself.
  Disabled until Start.
- **Line under the clock:** centred secondary body text: "Press Start when the match begins"
  before Start; "Match the in-game timer" while a call is next, since the list shows the call;
  "Calling now:" and what listeners hear (the text, the file's name or the gong; for a call of
  several events at one spawn time, each sound once, in turn, apart by " · ") in accent Semibold
  while one plays; "No more calls this match" after the last; "Match over" at 00:00.
- **Calls list:** one row per call: its time, its event's name and spawn, and how it stands —
  called (secondary, check), not sent (secondary, critical ✕ "Not sent"), calling (accent,
  speaker, tinted), next (accent, "in m:ss", tinted), later (primary). A call is not sent when
  it comes due with Gengfu off the server or nobody chosen to hear it, when the server says it
  reached nobody (once it has played: a call longer than 3 s shows its check until then), or when a correction takes the clock past its spawn
  first. When the calls don't fit, the card stops above the buttons and its rows scroll,
  keeping the call calling or next second from the top; it moves only when that call changes,
  so a scroll by wheel stays meanwhile. With no calls, secondary body text says why, no events or
  no spawn times, with a Settings link under it.
- **Status line:** dot + text, caption size — connected, naming who hears (success);
  connecting, or raising the bot's identity for the server (hollow dot: not yet whispering, and
  nothing to fix); trying again, saying why (hollow caution dot, caution text) and counting
  down to the next attempt in m:ss, as the calls list counts to the next call, since a moving count shows Gengfu at work and when it tries, then "trying
  again…" while the attempt runs; connected with nobody chosen (caution); connected, but the
  server said the last call reached nobody (caution, until a whisper gets through, the
  connection is new or other listeners are chosen); not connected, saying why (critical). A
  Settings link follows only a line that waits for the player in Settings: nobody chosen, or not
  connected. A line that tries again, or says nobody heard the last call, has none, though a
  typo or the chosen group can be the cause: it mostly clears by itself, and nothing should push
  the player to change a setup that works; Settings is a button away, and on a retry line a link
  would push out the countdown. Every state's copy is in the strings and on the canvas's
  Foundations.
- **Update notice:** a bar above the footer, in the card's colours with an accent "i" beside its
  first line, once a downloaded version is ready: it applies on next start, and Restart applies it
  now. It shows only while the clock is stopped, so a match is never interrupted. Restart closes
  Gengfu the usual way, off the server and saved, and Velopack opens it again.
- **App icon:** the title bar's gong mark (hanger, rim and boss) in Windows blue, #0078D4, a fixed
  colour that reads on light and dark taskbars, since an icon can't follow the accent. Up to 32 px
  it has a simpler drawing of its own, so its strokes stay crisp. `scripts/make-icon.cs` draws
  `Gengfu.ico` at 16, 20, 24, 32, 40, 48, 64 and 256 px, for the exe, its shortcuts, Settings ›
  Apps and the Setup file; the title bar keeps the mark in the accent colour.
- **Settings:** grouped cards; short controls (drop-downs, toggles) on the right in a fixed
  column, text fields stacked. The column is 152 px beside the radios and 168 elsewhere:
  the Fluent drop-down keeps about 50 px for its chevron. Empty fields and drop-downs show a
  placeholder. One button connects with what the fields hold, or disconnects, and Enter in
  a field connects too; it offers Connect again as soon as a field changes. Beside it a line says
  how the connection stands, in shorter words than the main window's: Connected, whoever hears
  and whatever the last call reached; the main window's retry lines, wrapped, countdown included;
  or why Gengfu isn't connected. Radios for who hears the calls: a server group,
  from the server's list, or chosen channels, ticked in a flyout in the server's tree order
  that stays open while ticking (Esc, Enter or a click outside closes it). Until Gengfu is
  connected the drop-downs are off; the server's lists are read once it is, and again whenever
  a list opens, and until they are in the group drop-down holds the group already chosen.
  Connecting to another server clears the choice, and the status line asks who hears the calls.
  Privilege keys are rare, so under Connect a link, Use a privilege key, opens the key's field in
  its place, with focus in it; Use beside the field is off until Gengfu is connected and a key is
  typed, and Enter in the field does the same. A key the server took empties the field and keeps focus in it; one it
  didn't stays, so a typo can be fixed. The field stays open, and opens again when Settings is
  made anew while a key is typed or an answer stands.
  General starts with Language and Theme, each offering Same as Windows first, then its fixed
  choices. It ends with the version, in a row as tall as theirs. Under the version, across the
  card, a caption says where updates stand: checking, up to date, downloading, installed, or
  couldn't check; a copy Setup didn't install shows none.
- **Calls and Events:** Calls holds what every call shares: Call ahead, Voice, and a Test call
  that plays the first event's call. Events, under it, lists the events whose spawns get calls,
  one card each, as Windows 11's own settings expanders: closed, the event's name and, under it in
  caption size, its spawn times and own file, or in caution with a 12 px "!", what is wrong with
  it; the header is the one tab stop, Space or Enter opens it, and a chevron says which way. Open,
  under a divider: Name with the bin beside it (an empty name is the jungle, which the placeholder
  says), Spawn times, Say or the event's sound file, then its own Test call and the switch between
  words and file. Tab takes the bin last, after the switch, so typing that runs on from the name
  lands in Spawn times, where a space can't press it. Spawn times are one field, read on Enter or
  leaving it: game times or whole minutes, written back sorted as 25:00; one it can't read changes
  nothing, and the calls keep the times from before. Say wraps, so a long Russian call shows
  whole. Under the
  events, Add event, and Back to default while the events differ from the built-in ones, the
  jungle alone at its five times; how the jungle sounds doesn't count, and Back to default keeps
  it. With no events, a card says no calls go out.
- **Sound file:** in an event, in place of Say, the file's name with a note icon and Change;
  "Use text instead" goes back to the words.
- **Hints:** an info bar with an accent "i" (the no-voice hint, under Voice), or a caution bar
  with a "!" under an event's field: times it can't read, nothing to say or words Windows failed
  to speak (the gong plays), or a file that can't play, missing on this PC or not a sound Windows
  plays (the words play instead, quoted, or the gong when it is the voice or the words can't be
  spoken either). Under the Test call
  pressed last, a caution bar says the server said the last call reached nobody, and why that may
  be, for the group or the channels: Settings hides the main window's status line. Under the
  privilege key, a success bar with a check names the group the server put the bot in, or an
  error bar with a cross
  says why the server didn't take the key; it stays until another key is typed or Connect is
  pressed, through the connection's own reconnects.
- **Settings file bar:** when settings.json has an error, a caution bar sits under the title
  bar, above Settings' scroll, so it shows however far Settings is scrolled. It names the line
  and the setting, or the line alone for an error outside any setting, such as a missing comma,
  or says Gengfu can't read the file when Windows won't let it. It says Gengfu works with the
  settings shown and saves nothing until then, and links to Open settings file, which opens the
  file in the app Windows uses for .json files, or in Notepad. Settings opens the file nowhere
  else: it edits everything the file holds. The main window doesn't change for it.
- **Voice list:** "Gong (no voice)" first, then every installed voice as "name — language"
  (the language in its own tongue), the app language's first. Any voice may be picked; a new
  language chosen in General drops one that can't speak it, for the language's own.
- **No-voice hint:** when no voice speaks the app's language, Voice shows the gong and an
  info bar under it says so: accent info icon, caption text naming the way in (Language &
  region › the language › Language options), and a link that opens Language & region.
  Not Speech › Add voices, which can fail silently.

## Patterns

- **Sync:** Start sets 35:00. Correct with the arrows or by typing; every future call
  moves with the clock. Typed minutes keep the running seconds; typed seconds land on the
  nearest minute and are timed from the first keystroke (from Enter after a pause, for
  typing ahead). Enter applies, and so does leaving the field, never read as typing ahead;
  Esc cancels, ↑ ↓ nudge. Verify the rounding against the real game timer — the sync
  offsets depend on it.
- **No sync claims:** Gengfu can't see the game timer, so it shows no sync status and never
  says it is "in sync" — the clock itself is the feedback. Out-of-range typed values are
  ignored. No dialogs, no toasts; Windows' own file picker is the one exception.
- **Settings apply at once:** no Save or OK. A new language remakes the views; Settings stays
  where it was, with focus on Language. A new theme does the same, with focus on Theme. An event's
  default words follow its name, the lead and the language until the user edits them.
- **Undo, not a dialog:** what takes the user's work away, an event's bin or Back to default, acts
  at once and leaves a bar in the update notice's look, an accent "i", its words and Undo, with
  focus on Undo. It stays until the next change of the same things or Settings closes. Add event
  puts the new one at the end, open, its name selected so typing replaces it.
- **Marks beside words:** an icon or a dot centres on the capitals of the words beside it, those
  of the first line where they wrap, as in buttons. Caption words, 12 px on 16 px lines, have
  their capitals' centre 0.75 px under the line's middle: in a bar the words lift to the icon
  (BarText), so a one-line bar stays centred; in a line of words the mark comes down to them
  (StatusDot, CallMark), so they stay in line with the words around them. A mark hangs from its
  words' top, so rounding to device pixels moves the two together.
- **Copy:** short and plain; units always written ("30 s" / "30 с"); in Russian, decimal
  comma and «» quotes. Event names and call text are user data, not copy; the file's own words,
  such as settings.json and a setting's key, stay as the file spells them in both languages.

## Screens

| Screen      | Design file (canvas)                          | Source                                                                                                              |
| ----------- | --------------------------------------------- | ------------------------------------------------------------------------------------------------------------------- |
| Main window | Main.dc.html, its states on the canvas's Main window page | [MainWindow.xaml](../../src/Gengfu.App/MainWindow.xaml), [MainView.xaml](../../src/Gengfu.App/Views/MainView.xaml), [ClockDigits.xaml](../../src/Gengfu.App/Controls/ClockDigits.xaml) |
| Settings    | Settings.dc.html, its states on the canvas's Settings page | [MainWindow.xaml](../../src/Gengfu.App/MainWindow.xaml), [SettingsView.xaml](../../src/Gengfu.App/Views/SettingsView.xaml), [EventRow.xaml](../../src/Gengfu.App/Views/EventRow.xaml) |
| App icon    | AppIcon.dc.html, on the canvas's Foundations page | [make-icon.cs](../../scripts/make-icon.cs), which draws `src/Gengfu.App/Gengfu.ico` |
