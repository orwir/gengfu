# Gengfu — UX review, 2026-11

The first UX review of Gengfu, run for #42 before 0.4.0 opens Gengfu to players who have never
heard of it. This file is its plan and its record: the baseline, a walk of every journey, what
the walks found, and what was decided. It follows [ux_review.md](../guides/ux_review.md), and the
next review starts from its open items and its decision register.

**Goal.** Walk Gengfu's journeys as a newcomer from the promo video would, and fix or file what
gets in the way before 0.4.0 ships.

**Scope.** The installed app (main window and Settings), its install and update, and the README as
a newcomer's only guide; Phase 8 adds the front page and its teaser, which #46 and #47 wrote. Out:
signing (#41), which left v0.4 on 2026-11-11, so 0.4.0 ships unsigned.

**Phases.** 0, 1 and 8, the minimum viable review. Skipped deliberately: 2–7. Phase 7 works from
usage data Gengfu doesn't collect; what the walks saw of first runs, errors, copy and the keyboard
is recorded under Phase 1.

**Status.** Closed. Phases 0 and 1 ran on 2026-11-11 against 0.3.0. Phase 8 ran on 2026-11-12,
once #46 and #47 had landed and #41 had left v0.4: a re-walk of the final build, a newcomer's
walk, and Update in Russian at the 0.4.0 release's step 10.

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

0.3.0's counts include this review's walks, one Setup and one delta, which
[release.md](../guides/release.md) § Copies in use leaves out. 0.4.0's include one delta of this
review's, the update walk at the release's step 10: leave it out when the next release counts the
copies in use.

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
- [x] **`status-links` [DONE]** — Decide which status lines link to Settings. Addresses P1 #5, #6.
  - **(A)** Link "nobody heard the last call" and "Can't find" an address to Settings too, and
    restate the rule: a Settings link follows wherever the player's next step is there, Connect
    after a fix on the server included, as for a ban today.
  - **(B)** Keep the rule: no link where the fix may lie outside Settings.
  - **Recommended: (A).** In the walks, two of the three causes behind "nobody heard" and the one
    behind "Can't find" were fixed in Settings, and Settings opens on the server and the group.
  - Decided (A) 2026-11-11: filed as #56 (feature, ui, tier-3, v0.4).
  - Reversed to (B) the same day at #56's design review, and #56 closed as not planned: these
    lines mostly clear by themselves, so nothing should push a player to change a setup that
    works; Settings is a button away; and with an address as long as the guild's, the link pushed
    the countdown out of the line.
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
- [ ] **`small-polish` [DEFERRED]** — P1 #11, #12, #14, #18's hint naming the voice, and P8 #1: a few
  lines each; they land with the next change to their screen.
- [x] **`phase8-watch` [DONE]** — What Phase 8's newcomer is watched for: whether they get past
  SmartScreen without the README's words (#4), P1 #13, #15, #16, the browser's warning, and whether
  they find the clock's arrows. Answered by the newcomer's walk in 8.2.

What can go: the release page as a step (#46), and the hand-edited JSON in sharing (#40). Three
controls served no journey here — Disconnect, Call ahead and Chosen channels — but without usage
data that is a question for Phase 8, not a cut.

## Phase 8 — Close-out

### 8.1 How Phase 8 runs

- **Without #41.** Signing left v0.4 on 2026-11-11, and on 2026-11-12 the maintainer confirmed that
  it waits, so 0.4.0 ships unsigned and a newcomer meets SmartScreen as Phase 1 did; the front
  page's step 2 is the way past it (`unsigned-0.4`).
- **A re-walk of the final build.** First contact on develop's build, the app 0.4.0 ships,
  following only the new front pages, on a `GENGFU_HOME` folder against the local TeamSpeak 3.13.7
  server and its listener. Setup and SmartScreen haven't changed since Phase 1 walked them and
  weren't run again.
- **A newcomer, before the release** (`newcomer-walk-timing`). A guildmate who has never installed
  Gengfu walks First contact from the live front page, on their own PC and in their own browser,
  while the maintainer watches a screen share without helping and notes each step on an observer
  sheet, kept outside the repo until its notes are written in here. Until 0.4.0 ships the walk
  installs 0.3.0, whose First contact matches the final build's: since 0.3.0 only *Open settings
  file* left General, and Test call no longer greys out while it calls.
- **Update at the release** (`update-walk-timing`). An installed 0.3.0 skips test builds, and the
  notice a 0.3.0 player sees is drawn by 0.3.0 itself, unchanged since 0.2.0, so nothing in 0.4.0
  can change it. The Russian keyboard pass runs at the release's step 10, on this PC's installed
  0.3.0 switched to Russian and light.

### 8.2 Walk records

**First contact, final build — README.md, English, dark, mouse.** Step 3 matched the screen word
for word. The gear sits at the bottom right, and the red "TeamSpeak isn't set up" carries a
Settings link of its own. Settings opened with focus on Back (P1 #14); the address and Enter
connected in 0.5 s; the group list read "Choose a group", nine groups with five in view and GvG
last. Once GvG was chosen, Test call came on below the fold (P1 #13), so the walk went Back and
pressed the main window's: heard 0.12 s later, 1.58 s, 58 s after the first window opened. The
button read "Calling…" and stayed on; the line under the clock kept "Press Start when the match
begins" (P1 #11).

**First contact, final build — README.ru.md on English Windows, light, keyboard.** Gengfu opened
in English, as step 3 says it may, and step 3's sentence led to Settings › General › Language: 11
Tab stops from Back, F4, Русский, Enter. Settings stayed where it was with focus on Язык, and the
voice moved to a Russian one by itself. Then 10 Shift+Tab back up to the address, Enter (connected
in 0.5 s), 6 Tab to the group list, F4, End and Enter for GvG, 4 Tab to «Проверить: Лес» and
Space: «Лес через тридцать секунд» heard 0.18 s later, 2.24 s, with focus still on «Проверить»
(#54). In all 44 keys and the address, 25 of them to Language and back (P1 #16); without that
detour, 20. Step 3's «Выберите, кто слышит объявления» and «Проверить» matched the screen.

**First contact, a newcomer — 0.3.0 from the live front page, observed.** A guildmate who had
never installed Gengfu walked it on their own PC, on English Windows, while the maintainer watched
a screen share without helping and marked each step on the observer sheet. They heard the test
call with no hint, 2 min 08 s after opening the front page: Setup downloaded at 0:20, with no
browser warning; past SmartScreen at 0:47, on their own, since unsigned apps are nothing new to
them; Gengfu open at 0:54; Settings at 1:02; connected at 1:41, the longest step; the group picked
at 1:50; Test call at 1:58, found by scrolling down Settings; heard at 2:07. They skipped the
teaser and went straight to the Setup link. They didn't press Start first, stayed in English as
their Windows is, and didn't try the clock's arrows, which the maintainer finds visible enough.
Asked afterwards, they called Gengfu «джангл бот, чтобы мы не забывали про лес» ("a jungle bot,
so we don't forget the jungle"), found it all intuitive, and missed nothing.

**Update, Russian, light, keyboard — 0.3.0 to 0.4.0, at the release's step 10.** At the
maintainer's pick, this PC's installed 0.3.0 ran on its own settings, which point at the guild's
server and its ГВГ group since the maintainer's check. Started from Start 2½ min after 0.4.0 was
published, it connected in 1.3 s and downloaded 0.4.0's delta in 3.5 s. In Settings, three
Shift+Tab from Back reached Language: F4, End, Enter for Русский; Tab, F4, Down, Enter for
Светлая. General read «Gengfu 0.3.0» over «Версия 0.4.0 установлена · применится после
перезапуска». Back showed «Обновление установлено — применится после перезапуска.» beside
«Перезапустить», the main window's first Tab stop. Tab and Space from the gear had 0.4.0 open 1.8 s
later, in Russian and light, connected to the same group 1.1 s after that: «Подключено · шёпот
группе ГВГ», as before. Settings › General read «Gengfu 0.4.0 · Последняя версия · обновления
ставятся сами», below the fold for the keyboard (P8 #4). The settings file changed in one line: the
group's server is saved by its id, no longer its address (#55). gengfu.log holds Restart's line,
"Updates: Gengfu restarts to apply version 0.4.0", which no earlier walk had seen. While the update
applied, the maintainer saw only Velopack's progress bar, and the "OK" button UI Automation then
reported in focus was likely that window's own. Language and Theme went back to «Как в Windows»
afterwards, and Gengfu was closed: 5 min 20 s on the guild's server, with no calls.

#### Checks run (Phase 8)

- README test of README.md on the final build — clean.
- README test of README.ru.md on English Windows — clean: its sentence on Language holds.
- First contact on the final build, English, dark, mouse — clean: P1 #11 and #14 stand as
  deferred, and P1 #13 as a watch point.
- First contact on the final build, Russian front page, light, keyboard — finding → #1; P1 #16
  measured.
- Isolation — clean through the walks, 03:29–03:33: their copies ran on folders of their own, and
  the real `%AppData%\Gengfu` and the install kept their 2026-11-11 times. The installed 0.3.0 that
  ran at 03:40–03:46 was the maintainer's own check beside the newcomer's walk.
- Setup, SmartScreen and the browser's warning on this PC — unrun: Setup hasn't changed since
  0.3.0, and the newcomer walked all three.
- A newcomer's First contact from the live front page, teaser included — finding → #3; nothing
  stopped them at SmartScreen, in the browser, or at P1 #13, #15 or #16.
- Update, Russian, light, keyboard, 0.3.0 to 0.4.0 — finding → #4; #55's server id took on the
  guild's real file with nothing reading as changed.
- Isolation at the release's step 10 — not isolated, by the maintainer's pick: the walk ran on this
  PC's install and its real settings, backed up first. Afterwards the file differs from the backup
  only in #55's server id, and Gengfu is closed, as it was found.
- Self-enforcing rules — finding → #2.
- Design language — clean: it holds every pattern this review decided: #54's Tab and focus rules
  (caccfe3), `status-links` (B) (903b73e), and the settings file bar as the file's only door
  (150cdec).
- `vision.md` — clean: CUJ-1 reads as both walks went.

#### Findings (Phase 8)

| # | Finding | Location | Journey | Severity |
| --- | --- | --- | --- | --- |
| 1 | Who hears the calls' two radio buttons are two Tab stops, where Windows takes a radio group as one stop and moves between its buttons with the arrows. Every keyboard way through Settings passes both. | [SettingsView.xaml](../../src/Gengfu.App/Views/SettingsView.xaml) | 1, 3, rare | **Minor** |
| 2 | Nothing held the design language's "every string ships in English and Russian". A key missing in Russian falls back to English in silence, and a Russian string whose `{0}` values differ from its English one drops a value or throws when shown. | [Strings.ru.resx](../../src/Gengfu.App/Strings/Strings.ru.resx) | all | **Drift-prone** |
| 3 | The newcomer skipped the teaser and went straight to the Setup link, yet said what Gengfu does right afterwards, likely from what a guildmate already knows. v0.4's done line holds for a guildmate who didn't need the video; whether the video carries a stranger is untested. | [README.md](../../README.md) | 1, rare | **Acceptable** |
| 4 | Settings' version and update lines sit below its last Tab stop. *Open settings file* shared their row until #57; now Shift+Tab from Back ends on Theme, which scrolls into view with the lines still below the fold, so Tab alone never shows a keyboard player which version runs. | [SettingsView.xaml](../../src/Gengfu.App/Views/SettingsView.xaml) | 5, rare | **Minor** |

#### Action items (Phase 8)

- [x] **`newcomer-walk-timing` [DONE]** — When does a newcomer walk First contact? Decided (A)
  2026-11-12.
  - **(A) Before 0.4.0:** the maintainer runs it with one guildmate, on an observer sheet; the
    release waits for it.
  - **(B) After 0.4.0**, on the released build; what it finds goes to the next milestone.
  - **(C) No observed walk:** this phase's re-walk stands in, and the missing newcomer is an
    accepted risk.
  - **Recommended: (A).** It is the only check of v0.4's done line, and what it finds can still be
    fixed for 0.4.0.
- [x] **`update-walk-timing` [DONE]** — When does Update get its Russian keyboard pass? Decided (A)
  2026-11-12.
  - **(A) At the release's step 10**, on this PC's installed 0.3.0 switched to Russian; this review
    closes after it. Step 1 needs v0.4 empty, so #42 leaves the milestone there.
  - **(B) Now, via 0.2.0:** reinstall 0.2.0 and walk its update to 0.3.0, which shows the same
    notice.
  - **(C) Skip it:** the English walk from 0.2.0 stands for the journey.
  - **Recommended: (A).** It walks the real update at a step the release takes anyway, and no
    earlier walk could change what a 0.3.0 player sees.
- [x] **`unsigned-0.4` [DONE]** — Ship 0.4.0 unsigned? Addresses P1 #4. Decided (A) 2026-11-12, the
  maintainer's call: #41, out of v0.4 since 2026-11-11, waits.
  - **(A) Ship it unsigned:** the front page's step 2 carries players past SmartScreen; Smart App
    Control still blocks Gengfu.
  - **(B) Hold 0.4.0** for #41.
- [x] **`string-pairs` [DONE]** — One check per rule in
  [StringsTests.cs](../../tests/Gengfu.App.Tests/StringsTests.cs): both languages hold the same
  strings, none blank, and each Russian string takes the same values as its English one. Addresses
  P8 #2. In this phase's change.
- [x] **`newcomer-walk` [DONE]** — Run the newcomer's walk and write it in here. Ran 2026-11-12: its
  steps and times are in 8.2 and the delta table, `phase8-watch`'s points are answered, and it found
  nothing issue-worthy.
- [x] **`update-walk-ru` [DONE]** — Walk Update in Russian by keyboard at the 0.4.0 release's
  step 10, and record it in 8.2. Ran 2026-11-12 on this PC's installed 0.3.0; it found P8 #4.
- [x] **`version-line-reach` [DONE]** — Bring Settings' version line into a keyboard player's
  view? Addresses P8 #4. Decided (A) 2026-11-12: the note is on #43.
  - **(A) A note on #43:** What's new opens from the version line, and a link there is a Tab stop
    that scrolls the lines into view.
  - **(B) Its own issue**, Backlog: Settings scrolls to its end when its last stop takes focus.
  - **(C) Accept it:** a screen reader reads the lines, and a sighted keyboard player rarely needs
    the version.
  - **Recommended: (A).** #43 already puts a link on that line, and the gap costs little until it
    lands.
- [ ] **`unused-controls` [DEFERRED]** — No walk used Disconnect, Call ahead or Chosen channels (P1,
  What can go). Keep them? Decided (A) 2026-11-12: the maintainer asks the runners, and the next
  review decides on their answers.
  - **(A) Ask first:** the maintainer asks the guild's two or three runners which of them they have
    used, and the next review decides on their answers.
  - **(B) Drop Disconnect and Call ahead in 0.5**, and keep Chosen channels.
  - **(C) Keep all three** for good.
  - **Recommended: (A).** The runners are the only players with real use, and asking costs a
    message. Chosen channels likely stays either way: it serves a guild with no group for its
    squads, as a newcomer's guild without an admin's help may be. Call ahead also sets every call's
    words ("in thirty seconds"), so dropping it would simplify them, but a guild that calls a
    minute ahead would lose it.

What can go: nothing more before the release; `unused-controls` holds the three controls left to
decide.

### 8.3 Delta table

| Metric | Baseline (0.3.0, 2026-11-11) | Phase 8 (develop, 2026-11-12) | Δ |
| --- | --- | --- | --- |
| Journeys | 8 | 5 | −3 |
| First contact steps, cold | 13, of which 3 in the browser | 12: the front page links straight to Setup | −1 |
| First contact decisions, cold | 4 | 3: no pick among the release's seven files | −1 |
| First contact, the app's part | 7 steps, 2 decisions | 7 steps, 2 decisions | 0 |
| First contact time, at the walk's pace | 1 min 36 s, Run anyway → heard | 58 s, first window → heard | the walk's own pace: the app's steps are unchanged |
| First contact, a newcomer observed | — | heard with no hint: 2 min 08 s from the front page, 1 min 20 s from Run anyway | another walker: the baseline's walk took 1 min 36 s from Run anyway |
| First contact by keyboard, Russian | 17 keys and the address, from Russian settings | 44 keys and the address on English Windows, 25 of them to Language and back | other starting settings |
| What a player waits through in First contact | Setup 1.7 s · connect 0.3 s | connect 0.5 s; Setup not re-run | — |
| Update | Restart, then 0.3.0 open 2 s later (from 0.2.0, English, mouse) | Tab and Space on Restart, then 0.4.0 open 1.8 s later (from 0.3.0, Russian, keyboard) | 0 |
| Screens | 2 | 2 | 0 |
| User-facing settings | 11, and 4 per event | 11, and 4 per event | 0 |
| Strings, per language | 153 | 153, every one checked for its twin | 0 |
| Status-line problem states that name a next step | 14 / 15 | 14 / 15 | 0, by decision (`status-links`) |
| Front page | 2,089 words, 419 before § Set up | 330 words, 155 before § Install; the rest in the player guide | −1,759 words |

### 8.4 Simplification ledger

| Slug | What it removes | Status |
| --- | --- | --- |
| `cuj-set`, `share-preset-journey` | Eight journeys to five: Mid-match fix and Early end folded into Match night, Strict server into A call nobody heard, Share a preset dropped with #40 | Done, 6a1847d |
| `front-page-input` | The Releases page as a step, with its pick among seven files; 1,759 words of the front page, now in the player guide | Done in #46, 797cc1b |
| `settings-file-link` | *Open settings file* in Settings › General, and the rewrite of pre-0.3 settings files behind it | Done in #57, 150cdec |
| `unused-controls` | Disconnect, Call ahead and Chosen channels, which no walk used | Deferred: the runners are asked first |

## Decision register

| Slug | Question | Options → recommended | Origin | Status | Recorded at |
| --- | --- | --- | --- | --- | --- |
| `review-timing` | Run the review now, or after 0.4.0's features land? | (A) Phases 0–1 now, 8 after #41, #46, #47 · (B) all after → **A** | #42 Clarify | Decided (A) 2026-11-11 | #42, its comment of 2026-11-11 |
| `review-depth` | Which phases? | (A) 0, 1, 8 · (B) all → **A** | #42 Clarify | Decided (A) 2026-11-11 | #42, its comment of 2026-11-11 |
| `cuj-set` | Six journeys, or the eight plus a ninth? | (A) six · (B) nine → **A** | P0 #1 | Decided (A) 2026-11-11 | [vision.md](../product/vision.md) |
| `design-review-pass` | Pair a design review with this one before 0.4.0? | (A) not for 0.4.0 · (B) run one → **A** | design_workflow.md § Triggers | Accepted risk — revisit when the screens double or a walk finds a visual inconsistency | this row |
| `keyboard-traps` | File the keyboard traps in v0.4? | (A) v0.4 · (B) Backlog → **A** | P1 #1–#3 | Decided (A) 2026-11-11 | #54 |
| `status-links` | Which status lines link to Settings? | (A) also nobody heard and can't find · (B) as now → **A** | P1 #5, #6 | Decided (B) 2026-11-11 | #56's closing comment; [design_language.md](../guides/design_language.md) § Status line |
| `settings-file-link` | Keep *Open settings file*? | (A) drop now · (B) keep until #40 · (C) keep → **B** | P1 #8, #9; #44 Clarify | Decided (A) 2026-11-11 | #57 |
| `share-preset-journey` | Drop the Share a preset journey too? | (A) drop, close #40 · (B) keep, by the file's path → **A** | `settings-file-link` | Decided (A) 2026-11-11 | [vision.md](../product/vision.md); #40's closing comment |
| `newcomer-walk-timing` | When does a newcomer walk First contact? | (A) before 0.4.0 · (B) after · (C) none → **A** | Phase 8 | Decided (A) 2026-11-12 | this row |
| `update-walk-timing` | When does Update get its Russian keyboard pass? | (A) at the release's step 10 · (B) now, via 0.2.0 · (C) skip → **A** | Phase 1, Checks run | Decided (A) 2026-11-12 | this row |
| `unsigned-0.4` | Ship 0.4.0 unsigned? | (A) ship · (B) hold for #41 → the maintainer's call | P1 #4 | Accepted risk — revisit when #41 is scheduled, or a player is stopped by SmartScreen or Smart App Control | #41 |
| `unused-controls` | Keep Disconnect, Call ahead and Chosen channels? | (A) ask the runners first · (B) drop two in 0.5 · (C) keep → **A** | P1, What can go | Decided (A) 2026-11-12 | this row |
| `version-line-reach` | Bring Settings' version line into a keyboard player's view? | (A) a note on #43 · (B) its own issue · (C) accept → **A** | P8 #4 | Decided (A) 2026-11-12 | #43, its comment of 2026-11-12 |
