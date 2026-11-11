# Gengfu — UX review, 2026-11

The first UX review of Gengfu, run for #42 before 0.4.0 opens Gengfu to players who have never
heard of it. This file is its plan and its record: the baseline, a walk of every journey, what
the walks found, and what was decided. It follows [ux_review.md](../guides/ux_review.md), and the
next review starts from its open items and its decision register.

**Goal.** Walk Gengfu's journeys as a newcomer from the promo video would, and fix or file what
gets in the way before 0.4.0 ships.

**Scope.** The installed app (main window and Settings), its install and update, and the README as
a newcomer's only guide. Out: the front page, guide and teaser that #46 and #47 write, and the
signing #41 adds; Phase 8 walks them once they land.

**Phases.** 0, 1 and 8, the minimum viable review. Skipped deliberately: 2–7. Phase 7 works from
usage data Gengfu doesn't collect; what the walks saw of first runs, errors, copy and the keyboard
is recorded under Phase 1.

**Status.** Phases 0 and 1 ran on 2026-11-11 against 0.3.0. Phase 8 runs once #41, #46 and #47
land, before the 0.4.0 release.

**How to use this file**

- Findings are numbered per phase (P1 #3); action items carry a slug that is unique in this file.
- Severity uses the seven labels of [codebase_review.md](../guides/codebase_review.md) § Severity
  vocabulary.
- A letter-shaped item waits for the maintainer's pick; its register row says when it was decided.
- Journeys are named as `cuj-set` (A) names them; 0.1 maps them to the eight they replace.
- This file stays in `docs/reviews/` for good.

## Phase 0 — Baseline and inventory

### 0.1 Journeys

[vision.md](../product/vision.md) § Critical user journeys held eight journeys as of 0.3.0: First
setup, Match night, Mid-match fix, Early end, Update, Share a preset, Strict server and The guild's
calls. None was a Recovery journey, which the template requires, so the walks added one: a call
nobody heard. `cuj-set` (A) folds the nine into six, and this file names them so: 1 First contact
(First setup, from the front page), 2 Match night (with Mid-match fix and Early end), 3 A call
nobody heard (with Strict server), 4 The guild's calls, 5 Update, 6 Share a preset.

### 0.2 What players have said

One message reached the maintainer, from a guildmate who set up 0.3 alone: "The interface is
simpler than in the previous version; I figured it out even without you." The previous version is
an earlier, more complex tool for the same job, not an earlier Gengfu. No questions or complaints
came in, and nobody outside the guild has filed an issue, starred or forked the repo.

### 0.3 Surface

| Metric | Baseline (2026-11-11, 0.3.0) |
| --- | --- |
| First contact steps, cold | 13, of which 3 in the browser, not walked |
| First contact decisions, cold | 4, of which 1 in the browser, not walked |
| First contact time, Run anyway → test call heard | 1 min 36 s, at the walk's pace |
| What a player waits through in First contact | Setup 1.7 s · connect 0.3 s |
| Screens | 2: the main window and Settings |
| User-facing settings | 11, and 4 per event |
| Strings, per language | 153 |
| Status-line problem states that name a next step | 14 / 15 |
| README | 2,089 words, 419 of them before § Set up |

A problem state names a next step when its line links to Settings or says Gengfu is trying again
by itself, as [StatusLine.cs](../../src/Gengfu.App/StatusLine.cs) decides; only "nobody heard the
last call" has neither. In the walks, one of the 14 led nowhere: a mistyped address is retried
forever (P1 #6).

### 0.4 Usage evidence

Gengfu collects nothing about use, by the maintainer's choice. What exists is each PC's own
`gengfu.log` and the release downloads:

| Release | Setup | Full package | Delta |
| --- | --- | --- | --- |
| 0.1.0 | 1 | 3 | — |
| 0.2.0 | 2 | 3 | 3 |
| 0.3.0 | 2 | 1 | 2 |

0.3.0's counts include this review's walks: one Setup and one delta. Subtract them when the next
release counts the copies in use.

#### Checks run (Phase 0)

- Journey inventory — finding → #1.
- Questions and feedback since the last review — clean: one message, positive.
- Walks of alternatives — n/a: no `competitors.md`; `idea.md` names ts6-crier, the bot Gengfu
  replaced, without a walk of it.
- Surface counts — clean: recorded above.
- Ground rules — clean: vision, design language and README read; no earlier review of any kind,
  so nothing carries over.
- Usage evidence — finding → #2.

#### Findings (Phase 0)

| # | Finding | Location | Severity |
| --- | --- | --- | --- |
| 1 | The inventory held eight journeys, past the template's seven, and none was a Recovery journey. First setup started at "Download and run Setup", while v0.4's done statement starts at the video. | [vision.md](../product/vision.md) § Critical user journeys | **Decision pending** |
| 2 | Nothing says which screens and settings players use, so a surface diet has no evidence. Release downloads count copies, not use. | — | **Acceptable** |

#### Action items (Phase 0)

- [x] **`cuj-set` [DONE]** — Refine the journeys. Addresses P0 #1. Decided (A) 2026-11-11 and
  written into `vision.md`, in this review's commit.
  - **(A) Six journeys:** 1 First contact, 2 Match night, 3 A call nobody heard, 4 The guild's
    calls, 5 Update, 6 Share a preset.
  - **(B) Keep the eight**, and add a call nobody heard as a ninth.
  - **Recommended: (A).** Mid-match fix and Early end are moments of a match night, and a strict
    server is one cause of a call nobody heard. Update and Share a preset keep their numbers, which
    #40 cites. Written into `vision.md` as (A), pending the maintainer's pick.
- [x] **`design-review-pass` [DONE]** — Pair a design review with this one? design_workflow.md §
  Triggers pairs one with a UX review before opening to strangers. Decided (A) 2026-11-11.
  - **(A) Not for 0.4.0.** Revisit when the screens double or a walk finds a visual inconsistency.
  - **(B) Run one** before the release.
  - **Recommended: (A).** Gengfu has two screens, its design language is codified at every canvas
    round, and the walks found journey friction, not visual incoherence.

What can go: three of the eight journeys, merged into the others.

## Phase 1 — Journey walks

### 1.1 How the walks ran

- **Where.** The maintainer's PC, which they use only to test Gengfu. Its 0.2.0 install walked the
  Update journey first; then Gengfu was uninstalled and both of its folders deleted, after a backup
  of the settings outside the repo.
- **Install.** Setup from the 0.3.0 release, marked as downloaded from the internet and opened
  through File Explorer, so Windows treated it as a browser download.
- **Server.** A local TeamSpeak 3.13.7 server, the guild's version, with a listener in a GvG group
  that logged every whisper it heard and how long it lasted.
- **Two passes per journey.** English, dark, by mouse; and Russian, light, by keyboard alone. The
  first Russian pass started from settings holding only its language and theme, standing in for a
  Russian, light-mode Windows; later passes switched both in Settings.
- **Not a newcomer.** Neither the walker nor the maintainer meets Gengfu cold. Every point where the
  walk needed something the README doesn't say counts as a finding; Phase 8 puts a real newcomer
  in front of the final surface.

### 1.2 The README test

First contact passed on the README alone: every step it describes matched the screen, SmartScreen's
*More info* and *Run anyway* included. The browser's own download warning is the one step not
walked. One statement was wrong: "What it does" still added calls "in the settings file", which
Settings › Events has done since 0.3.0. It is fixed (P1 #19). What shapes the new front page is
marked for #46.

### 1.3 Walk records

**First contact, cold — English, dark, mouse.** 13 steps and 4 decisions; 1 min 36 s from *Run
anyway* to the test call heard.

| Step | The player | What shows |
| --- | --- | --- |
| 1 | Opens the Releases page from the README | Not walked: the browser |
| 2 | Picks `Gengfu-win-Setup.exe` among seven files (decision) | Not walked: the browser |
| 3 | Keeps the file past the browser's warning | Not walked: the browser |
| 4 | Opens the file | SmartScreen: "Windows protected your PC", its one button *Don't run* |
| 5 | *More info* (decision: trust it) | App and "Unknown publisher"; *Run anyway* appears |
| 6 | *Run anyway* | Setup asks nothing; Gengfu is open 1.7 s later, with Start and desktop shortcuts |
| 7 | The status line's *Settings* | A red "TeamSpeak isn't set up"; focus on Back |
| 8 | Types the server address (decision: its form) | — |
| 9 | Enter | Connected 0.3 s later; the group list reads "Choose a group" |
| 10 | Opens the group list | Nine groups, five in view; GvG last |
| 11 | Picks GvG (decision: which group) | Test call comes on, below the fold |
| 12 | Back | "Connected · whispering to the GvG group" |
| 13 | Test call | Heard 0.13 s later: "Jungle in thirty seconds", 1.6 s |

**First contact — Russian, light, keyboard.** 17 keys and the address typed; the test call heard in
a Russian voice, 2.2 s. Tab reaches the status line's link first, and the focus ring showed on each
stop captured. After the Test call, focus fell to the window (P1 #3).

**Match night — Russian, light, keyboard.** Space started the clock at 34:59. Matching a game at
26:05: minutes typed and Enter, then Tab, landed in the minutes again, so the seconds became minutes
and the clock read 05:46 (P1 #1). Typed again with Tab between the fields, the clock read 26:03 and
every call came back. The 25:30 call went out as the clock turned, and the line under the clock
read "Объявляю: Лес через тридцать секунд". Stop kept focus on the button and set the clock back to
35:00.

**Match night — English, dark, mouse.** Start, then three clicks on the seconds' lower arrow: 34:59
to 34:54, one second a click, each logged. The arrows are 84 × 20 px hit areas drawn as faint
6 × 3 px chevrons.

**Update — English, dark, mouse.** The installed 0.2.0, started from Start: the notice "Update
installed — it applies the next time Gengfu starts." showed within 5 s; *Restart* had 0.3.0 open
2 s later; Settings › General read "Gengfu 0.3.0 · Up to date". One action, none needed: the update
would apply at the next start anyway.

**Share a preset — English.** The guildmate's half takes 7 steps: Settings, scroll to General, *Open
settings file*, find `events`, select it to its closing bracket, paste, save; the officer's half
takes 6 more. A paste one line short left the main window unchanged; Settings' bar read
"settings.json has an error on line 18 (events[2])", and the fixed paste showed Boss at 28:30 and
18:30 (P1 #8, #9). The file was edited directly: on this PC no app opens .json files, so the link
opens Notepad, which here holds the maintainer's unsaved work.

**Share a preset — Russian, light.** A Russian preset one line short gave "В settings.json ошибка в
строке 18 (events[2])…", whole on screen; fixed, the events read Лес and Босс. Back in English, the
English voice couldn't speak Босс's own words, "Босс скоро": its row read "Windows couldn't speak the
text", and its calls play the gong (P1 #18).

**A call nobody heard — English, dark, mouse.** With the Squad group chosen, where nobody was online,
Settings' Test call raised a caution bar naming two causes, partly below the fold. The main line
read "Connected · nobody heard the last call", with no link (P1 #5). Choosing GvG cleared the
caution at once, and the Test call was heard. Six actions and one decision, which needs knowing the
squad's group.

**A call nobody heard — Russian, light, keyboard.** The same walk by keyboard: the Russian bar fits
whole, and Tab to Test call had scrolled it into view; the main line "Подключено · последнее
объявление никто не услышал" fits, without a link. A third cause showed the same line: a group the
server had deleted and made again under the same name, which the status line still called "the GvG
group" until a Test call reached nobody.

**Strict server — English, dark, mouse.** With GvG asking whisper power 100, a Test call read
"Connected · nobody heard the last call". The admin's key, pasted under *Use a privilege key*, read
"Key used — Gengfu is in Bots now."; the next Test call was heard and both cautions cleared. Five
actions after the key arrives; that the key is the fix comes from the README alone.

**Strict server — Russian, light, keyboard.** The key's link is 5 Tab stops into Settings; Enter
opens its field with focus in it, and Enter in the field uses the key: "Ключ применён — Gengfu
теперь в группе Bots." Tab on to Test call, Space: heard.

**A mistyped address — English, dark, mouse.** "localhots" read "Can't find localhots — trying
again…" in caution, with no link, retrying (P1 #6). The fixed address connected, but the group was
gone: "Connected · choose who hears the calls" (P1 #7).

**A mistyped address — Russian, light, keyboard.** "Не нахожу localhots — повторю через 0:01", with
no link; fixed, "Подключено · выберите, кто слышит". Choosing the group again by keyboard, End landed
on the newest group, Bots, and the status line named it, which is how the slip showed.

**The guild's calls — Russian, light, keyboard.** Add event is 13 Tab stops into Settings. The new
event opened with its name selected; Tab from the name reached the bin, and the spawn times typed
there pressed it at the first space (P1 #2). Undo, which had focus, brought it back. "25, 15" read
back as "25:00, 15:00", the words followed as "Босс через тридцать секунд", and its Test call was
heard, 2.3 s. The main list showed both Boss calls.

**The guild's calls — English, dark, mouse.** Add event, the name typed over "New event", Spawn
times clicked and "22, 12" entered as 22:00 · 12:00, the words followed as "Supply in thirty
seconds"; its Test call was heard, 1.7 s, and the main list showed Supply's calls.

**Warm start.** From Start to "Connected · whispering to the GvG group" in under a second, with no
action.

#### Checks run (Phase 1)

- Fresh environment — clean: the install and both folders removed; Setup from the release, marked
  as from the internet.
- Browser download and its warning — unrun: a fresh Edge profile on this PC signed itself into the
  maintainer's Microsoft account and began to sync, so it was closed and deleted. Phase 8's
  newcomer walks it in their own browser.
- First contact, English, dark, mouse — finding → #4, #10, #11, #13, #14, #15, #19.
- First contact, Russian, light, keyboard — finding → #3, #16.
- Match night, Russian, light, keyboard — finding → #1.
- Match night, English, dark, mouse — clean.
- Update, English, dark, mouse, 0.2.0 to 0.3.0 — finding → #17.
- Update, Russian, light, keyboard — unrun: no newer version to update to; Phase 8 walks 0.4.0's.
- Share a preset, English — finding → #8, #9.
- Share a preset, Russian, light — finding → #18.
- Share a preset, the *Open settings file* click — unrun: it opens Notepad here, which holds the
  maintainer's unsaved work; the file was edited directly.
- A call nobody heard, English, dark, mouse — finding → #5, #12.
- A call nobody heard, Russian, light, keyboard — finding → #5.
- Strict server, English, dark, mouse — finding → #5.
- Strict server, Russian, light, keyboard — clean.
- A mistyped address, English, dark, mouse — finding → #6, #7.
- A mistyped address, Russian, light, keyboard — clean: as in English.
- The guild's calls, Russian, light, keyboard — finding → #2, #3.
- The guild's calls, English, dark, mouse — clean.
- Warm start — clean.
- README test, First contact — finding → #19.
- Russian copy on the screens walked — clean: nothing cut off.
- The guild's own server and TS6 — unrun: the local TS3 3.13.7, the guild's version, stood in.

#### Findings (Phase 1)

| # | Finding | Location | Journey | Severity |
| --- | --- | --- | --- | --- |
| 1 | After Enter in the clock's minutes, focus goes to the clock as a whole, and the next Tab lands in the minutes again, not the seconds. Typed seconds then become minutes: "05" moved the clock from 26:48 to 05:46, four calls turned *Not sent*, and the 05:30 call went out 16 s later, at the wrong time. Typing the right time put every call back. | [ClockDigits.xaml.cs](../../src/Gengfu.App/Controls/ClockDigits.xaml.cs); design language § Clock digits | 2, weekly | **Real bug** |
| 2 | Tab from an event's name lands on its bin, beside the name, before Spawn times. Spawn times typed there press the bin at the first space, which removes the event; Undo, focused, brings it back. | [EventRow.xaml](../../src/Gengfu.App/Views/EventRow.xaml) | 4, rare | **Minor** |
| 3 | A Test call pressed by keyboard turns its button off while it calls, so focus falls to the window: Space can't repeat it, and the next Tab starts from the first stop. Start and Stop keep focus for this reason; Test call doesn't. | [MainView.xaml.cs](../../src/Gengfu.App/Views/MainView.xaml.cs), [SettingsView.xaml.cs](../../src/Gengfu.App/Views/SettingsView.xaml.cs), [EventRow.xaml.cs](../../src/Gengfu.App/Views/EventRow.xaml.cs) | 1, 3, 4, rare | **Minor** |
| 4 | SmartScreen's dialog offers only *Don't run* until its small *More info* link is pressed, so a newcomer without the README likely stops there, which v0.4's done statement is about; untested, and Phase 8 watches for it. A signature satisfies Smart App Control, but SmartScreen builds reputation per certificate and file, so a new signature may meet this dialog for a while. | Setup; #41 | 1, rare | **Real bug** |
| 5 | "Connected · nobody heard the last call" has no Settings link, by design: the fix is on the server or in the game. In the walks it stood for three causes, two fixed in Settings (a group nobody in it was online, a group the server had remade) and one on the server (whisper power). Settings is also where the causes are explained. | [StatusLine.cs](../../src/Gengfu.App/StatusLine.cs); design language § Status line | 3, rare | **Decision pending** |
| 6 | "Can't find localhots — trying again" retries a mistyped address forever, with no Settings link. The line shows the typo; the fix is in Settings. | [StatusLine.cs](../../src/Gengfu.App/StatusLine.cs) | 1, rare | **Decision pending** |
| 7 | Connecting to an address spelled differently clears the chosen listeners before that server answers. A typo and its fix leave "choose who hears the calls", as would, by the code, `localhost` and `localhost:9987`, one server spelled two ways. | [Session.cs](../../src/Gengfu.App/Session.cs) `ConnectAsync` | 1, rare | **Real bug** |
| 8 | An error in a hand-edited settings file doesn't reach the main window, which looks unchanged; only Settings' bar and the log say what happened. The bar's "(events[2])" counts from zero in a file with two events, and its line is the closing brace after the missing bracket. | design language § Settings file bar | 6, rare | **Decision pending** |
| 9 | Sharing a preset is the one journey that needs hand-edited JSON, and *Open settings file* is its only door in the app. On a PC with no app for .json files, as here, the link opens Notepad. | [SettingsView.xaml.cs](../../src/Gengfu.App/Views/SettingsView.xaml.cs) | 6, rare | **Decision pending** |
| 10 | The release page offers seven files, and the player needs the third. `releases/latest/download/Gengfu-win-Setup.exe` always serves the latest Setup, so a front page can link straight to it and skip the page. | [README.md](../../README.md) § Install; #46 | 1, rare | **Minor** |
| 11 | While a Test call plays, the line under the clock keeps "Press Start when the match begins", and only the button says *Calling…*. A match call shows "Calling now:" and its words, so a Test call could show what the squad hears, the gong included. | [MainView.xaml.cs](../../src/Gengfu.App/Views/MainView.xaml.cs) | 1, 3, 4, rare | **Minor** |
| 12 | The caution bar under Settings' Test call opens partly below the fold when the button is clicked, cut mid-sentence until scrolled. | [SettingsView.xaml.cs](../../src/Gengfu.App/Views/SettingsView.xaml.cs) | 3, rare | **Minor** |
| 13 | Once a group is chosen, nothing on screen points to the next step: Settings' Test call is below the fold, and the main window's is one Back away. | Settings | 1, rare | **Minor** |
| 14 | On a first start, Settings opens with focus on Back, not on the empty server address. | Settings | 1, rare | **Minor** |
| 15 | The first screen's most prominent control is Start while TeamSpeak isn't set up; the red status line and its link are the one pointer to setup. Pressing Start first was not walked. | main window | 1, rare | **Minor** |
| 16 | A Russian player on English Windows starts in English, and Language sits in General, after the events: 14 Tab stops into Settings on a first start, or a scroll near the end. | Settings § General | 1, rare | **Minor** |
| 17 | The update notice names no version, and nothing after Restart says what changed. That is #43, What's new, which is postponed. | main window; #43 | 5, rare | **Acceptable** |
| 18 | Events keep their names and words across a language switch, so a Russian preset in an English Gengfu says "Босс in thirty seconds", and an event with its own Russian words can't be spoken by the English voice: its row reads "Windows couldn't speak the text", and its calls play the gong. The hint doesn't say that a voice for those words, in Calls › Voice, would speak them. | Settings § Events | 4, 6, rare | **Minor** |
| 19 | The README's "What it does" said calls for other spawns are added "in the settings file"; Settings › Events has added them since 0.3.0. Fixed. | [README.md](../../README.md) § What it does | 1, rare | **Drift** |

#### Action items (Phase 1)

- [x] **`keyboard-traps` [DEFERRED → #54]** — Fix the three keyboard traps. Addresses P1 #1, #2, #3. After Enter in
  a clock field, Tab moves to the next field; Tab from an event's name no longer reaches the bin
  before the event's fields, in a way the issue picks; a Test call keeps focus while it calls.
  - **(A)** One issue in v0.4, fixed before #47 films the app.
  - **(B)** One issue in the Backlog, without a milestone.
  - **Recommended: (A).** The first trap sends a call at the wrong time on the weekly journey; the
    fixes are small and change no screen.
  - Decided (A) 2026-11-11: filed as #54 (bug, ui, tier-2, v0.4).
- [x] **`listeners-survive-typo` [DEFERRED → #55]** — Clear the chosen listeners only once a
  different server answers. Addresses P1 #7. Filed as #55 (bug, settings, tier-3, v0.4).
- [x] **`status-links` [DEFERRED → #56]** — Decide which status lines link to Settings. Addresses P1 #5, #6.
  - **(A)** Link "nobody heard the last call" and "Can't find" an address to Settings too, and
    restate the rule: a Settings link follows wherever the player's next step is there, Connect
    after a fix on the server included, as for a ban today.
  - **(B)** Keep the rule: no link where the fix may lie outside Settings.
  - **Recommended: (A).** In the walks, two of the three causes behind "nobody heard" and the one
    behind "Can't find" were fixed in Settings, and Settings opens on the server and the group.
  - Decided (A) 2026-11-11: filed as #56 (feature, ui, tier-3, v0.4).
- [x] **`settings-file-link` [DEFERRED → #57]** — Keep or drop *Open settings file*, a question the maintainer raised
  at #44's Clarify. Addresses P1 #8, #9.
  - **(A)** Drop it now. Sharing a preset loses its only door, and players find `settings.json`
    by its path.
  - **(B)** Keep it until #40 brings sharing into Settings, and drop it with #40. Meanwhile a file
    error stays shown in Settings only.
  - **(C)** Keep it for good, and have the main window say when the file has an error.
  - **Recommended: (B).** Since the event editor, the file serves sharing alone, and #40 is where
    sharing gets a path without JSON.
  - Decided (A) 2026-11-11: one person per guild runs Gengfu, with two or three backups, so a
    shared preset is rare. Filed as #57 (feature, settings, tier-3, v0.4).
- [x] **`share-preset-journey` [DONE]** — Drop the Share a preset journey too? The maintainer raised
  it with `settings-file-link` (A), "at least till I come up with a good idea". Decided (A)
  2026-11-11: `vision.md` keeps five journeys, in this review's commit, and #40 closed as not
  planned; this file's "6 Share a preset" is the journey as walked.
  - **(A)** Drop it: `vision.md` keeps five journeys, the README's sharing paragraph goes with the
    link's task, and #40 closes as not planned. A backup re-creates the events in Settings › Events.
  - **(B)** Keep it, the file opened by its path once the link is gone; #40 stays in the Backlog.
  - **Recommended: (A).** With one runner and two or three backups per guild, a preset changes hands
    a few times at most, and the event editor makes it a minute's typing.
- [x] **`front-page-input` [DONE]** — For #46: link straight to the latest Setup, and these walks'
  README results. Addresses P1 #10. Commented on #46.
- [x] **`smartscreen-evidence` [DONE]** — For #41: SmartScreen as walked, and that a new signature
  may not clear it at once. Addresses P1 #4. Commented on #41.
- [x] **`readme-truth` [DONE]** — "What it does" says calls are added in Settings. Addresses P1 #19.
  In this review's commit.
- [ ] **`small-polish` [DEFERRED]** — P1 #11, #12, #14, and #18's hint naming the voice: a few lines
  each; they land with the next change to their screen.
- [ ] **`phase8-watch`** — What Phase 8's newcomer is watched for: whether they get past SmartScreen
  without the README's words (#4), P1 #13, #15, #16, the browser's warning, and whether they find
  the clock's arrows.

What can go: the release page as a step (#46), and the hand-edited JSON in sharing (#40). Three
controls served no journey here — Disconnect, Call ahead and Chosen channels — but without usage
data that is a question for Phase 8, not a cut.

## Phase 8 — Close-out

Not run yet. Once #41, #46 and #47 land, and before the 0.4.0 release:

- A guildmate who has never installed Gengfu walks First contact on their own PC, from the new
  front page and in their own browser, while the maintainer watches the screen share without
  helping and notes every pause. Steps and time go in the delta table, with `phase8-watch`'s
  points.
- Update with 0.4.0's update, in Russian, by keyboard.
- The delta table against Phase 0, the simplification ledger, and this register brought up to date.
- `design_language.md` gains every pattern this review decided; `vision.md`'s journeys are checked
  against the walks.

## Decision register

| Slug | Question | Options → recommended | Origin | Status | Recorded at |
| --- | --- | --- | --- | --- | --- |
| `review-timing` | Run the review now, or after 0.4.0's features land? | (A) Phases 0–1 now, 8 after #41, #46, #47 · (B) all after → **A** | #42 Clarify | Decided (A) 2026-11-11 | #42, its comment of 2026-11-11 |
| `review-depth` | Which phases? | (A) 0, 1, 8 · (B) all → **A** | #42 Clarify | Decided (A) 2026-11-11 | #42, its comment of 2026-11-11 |
| `cuj-set` | Six journeys, or the eight plus a ninth? | (A) six · (B) nine → **A** | P0 #1 | Decided (A) 2026-11-11 | [vision.md](../product/vision.md) |
| `design-review-pass` | Pair a design review with this one before 0.4.0? | (A) not for 0.4.0 · (B) run one → **A** | design_workflow.md § Triggers | Accepted risk — revisit when the screens double or a walk finds a visual inconsistency | this row |
| `keyboard-traps` | File the keyboard traps in v0.4? | (A) v0.4 · (B) Backlog → **A** | P1 #1–#3 | Decided (A) 2026-11-11 | #54 |
| `status-links` | Which status lines link to Settings? | (A) also nobody heard and can't find · (B) as now → **A** | P1 #5, #6 | Decided (A) 2026-11-11 | #56 |
| `settings-file-link` | Keep *Open settings file*? | (A) drop now · (B) keep until #40 · (C) keep → **B** | P1 #8, #9; #44 Clarify | Decided (A) 2026-11-11 | #57 |
| `share-preset-journey` | Drop the Share a preset journey too? | (A) drop, close #40 · (B) keep, by the file's path → **A** | `settings-file-link` | Decided (A) 2026-11-11 | [vision.md](../product/vision.md); #40's closing comment |
