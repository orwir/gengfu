# Security policy

How to report a vulnerability in Gengfu, and what is in scope.

## Reporting

Use GitHub's private reporting: **Security › Report a vulnerability** on this repository. Please
don't open a public issue for a security problem. Expect a first reply within 7 days; this is a
spare-time project.

## In scope

- The Gengfu app and its updater
- How Gengfu stores the TeamSpeak server password and its bot identity
- The release workflow and the artifacts it publishes

## Out of scope

- TeamSpeak servers and clients themselves
- Vulnerabilities in vendored code under `lib/` — report those upstream too (see `lib/README.md`)
