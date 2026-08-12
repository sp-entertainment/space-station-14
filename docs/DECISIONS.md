# Decisions

## 2026-07-27 — Graphical client bridge first

**Context:** The first proof must establish one agent as an ordinary networked player without privileged state or direct mutation.

**Options:** Modified graphical client, headless client, server bot, or pixel automation.

**Decision:** Add a launch-gated TCP loopback bridge to the existing graphical client. Project only client-held state and dispatch movement and interaction through `InputSystem`; dispatch speech through `IChatManager`.

**Rationale:** This is the smallest path that preserves the proven client lifecycle and normal server validation.

**Consequences:** Each agent client needs a unique local port. RobustToolbox owns only the generic bounded line transport and JSON codec because its content sandbox correctly forbids direct socket and serializer access. Headless extraction, provider integration, broader observations/actions, and multi-agent orchestration remain out of scope until repeated M1 runs pass.
