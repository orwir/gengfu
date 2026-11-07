# 0004. MIT for Gengfu; vendored code keeps its licenses

## Status

Accepted — 2026-11-07

## Context

The repository is public and should be easy to reuse. It vendors TSLib (OSL-3.0), the TeamSpeak
protocol declarations (MIT or Apache-2.0) and libopus (BSD-3-Clause). TSLib's maintainers state that
linking it does not require the linking work to be published under OSL-3.0.

## Decision

Gengfu's own code is MIT. Everything under `lib/` keeps its upstream license and notices, listed in
[lib/README.md](../../lib/README.md); changes to TSLib stay OSL-3.0.

## Consequences

- Anyone can reuse or fork Gengfu's code with only the MIT notice.
- Distributed builds contain OSL-3.0 code; its source, including our patches, is published here.
- The README and `lib/README.md` credit TSLib and its license.
