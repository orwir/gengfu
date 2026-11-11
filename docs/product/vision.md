# Gengfu — vision

What Gengfu is for, who it serves, and what v0.1 has to achieve. This is the living intent and owns
the CUJ inventory; how the idea started is frozen in [idea.md](idea.md), and tracked work lives in
GitHub issues.

## Problem

In Where Winds Meet guild wars (GvG) the in-game timer counts down from 35:00: five minutes of
preparation, then thirty of battle. Jungle mobs respawn at 25:00, 20:00, 15:00, 10:00 and 05:00,
and killing them earns coins. Mid-fight players forget, so each spawn needs a voice call at least
30 seconds ahead. The 30:00 spawn needs none — everyone leaves base then.

Existing bots start their timer on a chat command whenever someone remembers. Their calls land up
to a minute early or after the spawn, and nobody can see or fix the offset.

## Who it's for

Competitive WWM guilds that run GvG comms on TeamSpeak — first the maintainer's guild (top-1 EU),
then any guild, since the app and repo are public.

Discord isn't planned. A Discord bot can't join a server by its address, as Gengfu joins
TeamSpeak: each guild would have to register a bot on Discord's developer portal, give Gengfu its
token and invite the bot to its server. Gengfu stays a desktop app with nothing hosted, so that
setup would fall on every guild.

## What Gengfu is

A Windows 11 app that joins TeamSpeak as a bot and whispers jungle calls to every squad, on a clock
that mirrors the in-game timer and can be matched to it at any moment.

Global hotkeys aren't planned. Gengfu's window is a moment away even on one screen, and the clock is
started and matched there, mostly in the five minutes of preparation. Hotkeys held in every window
would take those keys from every other app; held only while the game is in front, they would tie
Gengfu to the game's executable.

## Principles

- **The clock mirrors the game** — same format, 35:00 → 00:00; checking it takes one glance.
- **No sync claims.** Gengfu can't see the game timer, so it never says it is in sync; the clock
  itself is the feedback.
- **Native, compact, quiet** — one small Windows window, light and dark, no dialogs.
- **Two languages** — English and Russian, everywhere.
- **No server-admin rights** — one server slot and permission to whisper, nothing else.

## v0.1

**Done when** a guildmate (not the maintainer) sets Gengfu up on our TeamSpeak in under 5 minutes,
and in a real GvG every squad hears all five jungle calls 30 s (±1 s) before each spawn.

**In** — each item is a v0.1 issue:

1. A clock that mirrors the in-game timer; the next call and its exact game time always visible.
2. Start sets 35:00; match the game at any moment with arrows on minutes and seconds, or by typing
   them.
3. A TeamSpeak bot for TS3 and TS6 that whispers to a server group or to chosen channels.
4. Calls from text (Windows voices) or an audio file; a built-in GvG preset.
5. A simple GUI in English and Russian: connect, the clock screen with its calls, Test call.
6. One small human-readable settings file; presets shareable without secrets.
7. A self-updating build from GitHub Releases.

**Out:** Discord (not planned) · global hotkeys (not planned) · text, poke or chat notifications ·
ServerQuery · reading the timer off the screen · remote control from TeamSpeak chat · headless or
VPS hosting · an in-app event editor · code signing · an overlay · languages beyond English and
Russian · Windows 10 · macOS and Linux.

**Constraints:** no budget and no deadline; v0.1 within about three weeks of evenings.

## Critical user journeys

The initial set, taken from the approved design; refined at the first UX review.

| CUJ | Journey | Done when |
| --- | --- | --- |
| 1. First setup | Download and run Setup, open Settings, enter the server and a nickname, pick who hears the calls, press Test call | The squad hears the test call |
| 2. Match night | The match starts; press Start (clock at 35:00); match the game with the arrows or by typing | Calls go out 30 s before each spawn |
| 3. Mid-match fix | Notice the clock is off; click the seconds, type what the game shows, press Enter | Every later call moves with the clock |
| 4. Early end | The match ends early; press Stop | No more calls |
| 5. Update | A new version installs in the background; once the clock is stopped, a notice says it applies on next start | Settings › General shows the new version after a restart |
| 6. Share a preset | An officer sends the settings file's events; a guildmate opens Settings › Open settings file, pastes them in and saves | The guildmate's calls list shows the shared events |
| 7. Strict server | The server limits whispers, so its admin sends a privilege key; open Settings › Use a privilege key, paste it, press Use, then Test call | The squad hears the test call |
| 8. The guild's calls | Open Settings › Events, press Add event, type its name and spawn times, then press its Test call | The squad hears the event's own call, and the calls list has its spawns |
