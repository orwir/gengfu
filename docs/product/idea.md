# Idea: Gengfu

- Status: GRADUATED -> https://github.com/orwir/gengfu
- Type: software
- Captured: 2026-11-07

## Phase 0 — Capture (5 minutes)

- [x] Problem — what hurts, concretely: in Where Winds Meet GvG (35:00 countdown:
      5 min prep, then 30 min of play) jungle mobs respawn every 5 minutes and
      kills earn coins. Mid-fight people forget the 25:00, 20:00, 15:00, 10:00
      and 05:00 spawns (30:00 needs no call — everyone leaves base then), so
      each needs a call ≥30 s ahead. Existing bots start on a chat command
      whenever someone remembers, so calls land up to a minute early or after
      the spawn — and nobody can see or fix the offset.
- [x] Who has it: competitive WWM guilds on TeamSpeak — mine first (top-1 EU),
      then any guild: repo and app are public. Discord guilds next.
- [x] Solution shape, one sentence: Gengfu (更夫, the Song-era watchman who struck
      a gong to call the hour) — a Windows app that joins TeamSpeak as a bot and
      whispers jungle calls to every squad, on a clock that mirrors the in-game
      GvG timer and can be synced to it at any moment.

GATE (cooldown): re-read after >= 1 week. Still want it? Yes -> Phase 1.
No -> stays parked, or kill it.
Skip the cooldown only for a hard external deadline — write down what it is.
-> Skipped 2026-11-07: demand is live — guilds need it every war night, and the
   bot in use now (ts6-crier) is unreliable.

## Phase 1 — Worth it? (1–2 hours, research only)

- [x] Existing solutions checked — best candidates and the *specific* reason each
      doesn't fit:
      1. ts6-crier (OSS TS6 voice bot, the one we use): TS6-only, buggy,
         maintainer unresponsive; its timer starts on a chat command, and the
         offset from the game clock can't be seen or corrected.
      2. GaleByte (WWM Discord bot, scheduled messages), Guild Order War Board
         (war rosters): Discord/web only — no TeamSpeak.
      3. Other guilds' bots: mostly private, quality varies.
- [x] Build cost, honest guess: 2–3 weeks of evenings for v0.1, AI-assisted; $0.
- [x] Running cost: none — runs on a player's PC and takes one TS slot; no
      hosting, no paid APIs. Maintenance: TS server updates, game timing
      changes, issues from public users. Maybe later: code signing, so Windows
      stops warning on download.
- [x] Opportunity cost: fine if v0.1 takes no more than 3 weeks of evenings.
- [x] Motivation: the result — reliable calls for my guild every war night, and
      for any guild that wants them. Nothing to buy; the free option is the bot
      we're replacing.

GATE: an existing solution covers ~80% and closing the gap costs more than living
with it -> use/buy it, KILL with reason "solved by X". Otherwise -> Phase 2.
-> Passed: nothing covers TeamSpeak voice calls on a game-synced clock; the
   closest (ts6-crier) is what we're replacing.

## Phase 2 — Define (1–2 hours)

- [x] Done statement: "v0.1 is done when a guildmate (not me) sets Gengfu up on
      our TeamSpeak in under 5 minutes, and in a real GvG every squad hears all
      five jungle calls 30 s (±1 s) before each spawn."
- [x] IN — the MVP, max 7 bullets:
      1. Clock that mirrors the in-game timer (35:00 -> 00:00); the next call and
         its exact game time always visible.
      2. Start sets the clock to 35:00; match it to the game at any moment with
         arrows on the minutes and seconds, or by typing them. Global hotkeys
         optional: they need admin rights, since WWM runs elevated.
      3. TeamSpeak bot for TS3 and TS6 servers: joins as a normal client and
         whispers calls to a server group (role) or to chosen channels — a config
         choice — so every squad channel hears them.
      4. Calls from text (Windows TTS, any installed language) or an audio file;
         built-in GvG preset: jungle at 25/20/15/10/05, called 30 s ahead.
      5. Simple GUI in English and Russian: connect (server, nickname, who
         hears the calls), clock screen with the schedule, Test button.
      6. One small human-readable config file; presets shareable without secrets.
      7. Self-updating build from GitHub Releases: a new version downloads and
         installs in the background, and the user is told it applies on restart.
- [x] OUT — explicitly not in v0.1:
      - Discord (v0.2: needs DAVE-capable voice and a bot token per guild)
      - text, poke or chat notifications; ServerQuery
      - auto-sync by reading the timer off the screen (OCR)
      - remote control from TS chat (others starting or correcting the clock)
      - headless / VPS / Linux host
      - in-app event editor (custom events live in the config file)
      - code signing
      - overlay / always-on-top mini clock
      - UI languages beyond English and Russian; macOS / Linux desktop
- [x] Riskiest assumption: whoever runs Gengfu can sync its clock to the in-game
      timer within ±1 s at any moment of the match, in a few seconds, with a time
      picker — no special timestamp to catch (prep is busy, 30:00 is a rush) and
      no hotkey needed. Runner-up: TSLib (C#, from the aging TS3AudioBot)
      connects to TS3 and TS6 servers and whispers to a server group from a
      .NET 10 app.
- [x] Constraints: $0; no deadline; v0.1 within 3 weeks of evenings. Runs on a
      gaming PC next to WWM (Windows 10/11): light on CPU/RAM, never steals
      focus. No server-admin rights needed: one TS slot, default guest
      permissions, no ServerQuery. Public and free: license compatible with
      TSLib's OSL-3.0. AI-assisted: stack picked for fit, not familiarity. UX is
      designed in Claude Design and approved by eye before it is built.

GATE: I can state the done statement and the riskiest assumption from memory, and
the IN list fits a few weekends. Bigger -> cut or split. Can't name the risk ->
not ready to build.
-> Passed 2026-11-07.

## Phase 3 — Spike (timeboxed)

Timebox: one weekend.

- [x] Prototype *only* the riskiest assumption — in Claude Design: an
      interactive time picker beside a simulated game timer. Done 2026-11-07:
      Start sets 35:00, then arrows or typing on minutes and seconds — approved
      in the prototype. Canvas: https://claude.ai/artifact/5VwWCavteqkpQQadJMEeBR;
      codified in docs/guides/design_language.md. The ±1 s check in a real GvG
      comes with v0.1.
- [x] Runner-up — a very small .NET console app against our TS server:
      connect, then whisper a Windows-TTS clip to a server group and to chosen
      channels. Done 2026-11-07 (spike/tslib-whisper): connects to our TS6
      server; normal voice, channel whisper and server-group whisper all heard.
      Found on the way:
      - TSLib's develop branch (the .NET 10 one) lost the group-whisper flag
        in 2021 (commit 2126993e); master still has it. A one-line patch, to
        offer upstream.
      - TSLib can't resolve TSDNS answers like "ip:$PORT", so Gengfu needs its
        own address lookup. Connecting by hostname is still unverified: our
        host's TSDNS gives a different IP than the one we connected to.
      - Windows has no Russian voice unless the user adds one, so the default
        calls need recorded clips or a setup hint.
- [x] Stack / platform picked while spiking, reasons noted: C# / .NET 10 (LTS)
      + WPF. TSLib is native C# and works with our TS6 server under .NET 10
      (with the patch above); TTS, audio decoding and later OCR come with
      Windows/.NET; light next to the game; the core can run headless on Linux
      later; Velopack covers self-update from GitHub Releases. Discord in
      v0.2: DisCatSharp.Voice ≥10.7 (has DAVE) or a discord.js sidecar.
- [x] Result, one line: survived — the sync UX is approved in the prototype,
      and TSLib reaches every squad on our TS6 server by group and channel
      whisper.

GATE: the assumption survived? Yes -> Phase 4. No -> back to Phase 2 with what was
learned, or KILL with reason "disproved: ____".
-> Passed 2026-11-07.

## Phase 4 — Commit (handoff)

- [x] Run docs/guides/project_kickstart.md (an agent can drive it), seeded
      from this file: done statement -> v0.1 milestone, IN list -> first issues,
      the spike's stack choice -> first ADRs.
- [x] Seed docs/product/vision.md from this file (problem, done statement,
      IN/OUT as v0.1 scope) — vision.md is the living intent doc.
- [x] Set Status: GRADUATED with the repo link, then move this file to
      docs/product/idea.md. Frozen from here — the origins artifact, never
      edited again.

From here the kickstart playbook owns the process.
