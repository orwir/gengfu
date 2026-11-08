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

| Token           | Light   | Dark    | Used for                          |
| --------------- | ------- | ------- | --------------------------------- |
| surface.base    | #F3F3F3 | #202020 | window (Mica)                     |
| surface.card    | #FBFBFB | #2B2B2B | cards                             |
| stroke.card     | #E5E5E5 | #1D1D1D | card border                       |
| text.primary    | #1B1B1B | #FFFFFF | text                              |
| text.secondary  | #5F5F5F | #CFCFCF | captions, past calls              |
| text.disabled   | #A0A0A0 | #7A7A7A | disabled controls                 |
| accent.fill     | #005FB8 | #60CDFF | primary button, next call, links  |
| accent.on       | #FFFFFF | #000000 | text on accent                    |
| status.success  | #0F7B0F | #6CCB5F | connected                         |
| status.caution  | #9D5D00 | #FCE100 | warnings                          |
| status.critical | #C42B1C | #FF99A4 | errors, not connected             |

- **Type:** Segoe UI Variable only — Display for the clock, Text for everything else.
  Clock 64/72 Semibold with tabular figures · Subtitle 20/28 Semibold · Body strong
  14/20 Semibold · Body 14/20 · Caption 12/16.
- **Spacing:** 4 px grid — 4, 8, 12, 16, 24, 32. **Radius:** 4 controls, 8 cards and window.
- **Elevation:** cards get a 1 px stroke and no shadow; the window gets the system shadow.
- **Motion:** 167 ms fast, 250 ms normal, decelerate (0, 0, 0, 1).
- Text pairs pass 4.5:1 in both themes; controls are the Windows-standard 32 px tall.

## Components

- **Window:** 380 px wide; title bar = icon, name, minimize, close (no maximize).
  Settings replaces the main view in the same window, with a back arrow, and scrolls.
- **Buttons:** accent (Start), standard (Stop), subtle (Test call, the Settings icon);
  Fluent rest / hover / pressed / disabled. Start and Stop are one button that flips, so
  keyboard focus stays on it.
- **Clock digits:** minutes and seconds are fields that look like plain digits. Hover →
  subtle fill; focus → field fill, 2 px accent underline, whole value selected (accent fill,
  text on accent), the running clock shown until the first keystroke. Arrows above and
  below each nudge ±1 min / ±1 s; Tab skips them, since ↑ ↓ in a field do the same. Enter
  and Esc hand focus to the clock as a whole, so the digits read as plain text again.
  Disabled until Start.
- **Calls list:** one row per call — called (secondary, check), calling (accent, speaker,
  tinted), next (accent, "in m:ss", tinted), later (primary).
- **Status line:** dot + text — connected (success), connecting (hollow dot), not
  connected (critical, with a Settings link).
- **Update notice:** info bar above the footer — applies on next start, optional Restart.
- **Settings:** grouped cards; short controls (drop-downs, toggles) on the right, text
  fields stacked; radios for who hears the calls (server group or chosen channels).

## Patterns

- **Sync:** Start sets 35:00. Correct with the arrows or by typing; every future call
  moves with the clock. Typed minutes keep the running seconds; typed seconds land on the
  nearest minute and are timed from the first keystroke (from Enter after a pause, for
  typing ahead). Enter applies, and so does leaving the field (timed from leaving after a
  pause of over 3 s); Esc cancels, ↑ ↓ nudge. Verify the rounding against the real game
  timer — the sync offsets depend on it.
- **No sync claims:** Gengfu can't see the game timer, so it shows no sync status and never
  says it is "in sync" — the clock itself is the feedback. Out-of-range typed values are
  ignored. No dialogs, no toasts.
- **Copy:** short and plain; units always written ("30 s" / "30 с"); in Russian, decimal
  comma and «» quotes. Event names and call text are user data, not copy.

## Screens

| Screen      | Design file (canvas)                          | Source                                                                                                              |
| ----------- | --------------------------------------------- | ------------------------------------------------------------------------------------------------------------------- |
| Main window | Main.dc.html (views: MainDark.dc.html)        | [MainWindow.xaml](../../src/Gengfu.App/MainWindow.xaml), [ClockDigits.xaml](../../src/Gengfu.App/Controls/ClockDigits.xaml) |
| Settings    | Settings.dc.html (views: SettingsRu.dc.html)  | —                                                                                                                   |
