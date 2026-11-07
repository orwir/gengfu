# 0003. Gengfu resolves server addresses itself

## Status

Accepted — 2026-11-07

## Context

TSLib's address lookup queries hard-coded public DNS servers, gives TSDNS one second, and cannot
read TSDNS answers that use `$PORT` — "keep the port the user typed". Our host answers exactly that
way, so connecting by hostname failed where the official TeamSpeak client succeeds.

## Decision

`Gengfu.TeamSpeak` resolves addresses the way the official client does, then hands TSLib a plain
`ip:port`:

1. An IP literal is used as is.
2. Otherwise the `_ts3._udp` SRV record of the host.
3. Otherwise TSDNS, found through `_tsdns._tcp` SRV records on the host and its parent domains, with
   `$PORT` replaced by the typed port.
4. Otherwise the system's DNS.

A port the user typed always wins.

## Consequences

- Hostnames from hosts that rely on TSDNS work.
- It has to be checked against real hosts: by name, our host's TSDNS points to a different IP than
  the one the spike connected to. The TeamSpeak issue verifies connecting by name.
- Another fix to offer TSLib upstream, postponed with the first.
