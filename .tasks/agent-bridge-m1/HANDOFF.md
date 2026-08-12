# Agent Bridge M1 Handoff

> Purpose: publish the current prototype safely, clone it on another computer, and resume without losing local or submodule work.

## Goal

One graphical agent client joins a local round, observes an interaction-reachable door, moves, opens it through ordinary input and server validation, and says a fixed sentence visible to a human client.

## Repository State

- **Main fork:** `https://github.com/sp-entertainment/space-station-14`
- **Working branch:** `prototype`, tracking `origin/prototype`
- **Official upstream:** `https://github.com/space-wizards/space-station-14.git`
- **RobustToolbox fork:** `https://github.com/sp-entertainment/RobustToolbox`
- **Content baseline:** `dc52779df93c0e5d3c2aacfcd438eea68b86a8a4`
- **RobustToolbox baseline:** `5960554571b266f49ac6bce8677a4869032da9de`
- **RobustToolbox prototype:** `d8cd05947c69bbbbbb42576f733bc6eb2bab51a4`

Both repositories use a `prototype` branch. The main repository pins the published RobustToolbox prototype commit, so a recursive clone can recover the complete implementation.

## Completed Work

- Added a disabled-by-default graphical client bridge controlled by `agent.bridge_port`.
- Added a bounded, loopback-only JSON Lines protocol and deterministic runner.
- Exposed only the attached player and interaction-reachable doors.
- Routed movement, door interaction, and speech through ordinary client and server gameplay paths.
- Added accepted and rejected action results, duplicate detection, stale-action rejection, and message bounds.
- Added protocol tests for supported, malformed, stale, unknown, and oversized actions.
- Added generic loopback transport and JSON encoding support to RobustToolbox after the content sandbox rejected direct socket and serializer access.
- Verified a graphical client can pass sandbox checks and emit the v0 handshake and observation over `127.0.0.1`.
- Created the GitHub fork and moved ongoing work from local `master` to `prototype`.
- Forked RobustToolbox, committed its engine changes, and configured the main repository to fetch the fork's `prototype` branch.

## Validation

- **Build:** passed on 2026-08-11 with 0 errors and 9 existing upstream dependency warnings.
- **Focused tests:** `8/8` passed on 2026-08-11 with `dotnet test Content.Tests/Content.Tests.csproj --no-build --no-restore --nologo --filter AgentProtocolTest`.
- **Static checks:** Python syntax and Git whitespace checks passed on 2026-08-11.
- **Runtime smoke test:** v0 hello and observation verified from a graphical client against a local server.
- **Pending acceptance:** the complete two-client movement, door, and speech scenario has not been recorded or repeated 20 times.

## Published Changes

Main repository contents include:

- `AGENTS.md`
- `README.md`
- `CONTRIBUTING.md`
- `.tasks/agent-bridge-m1/`
- `AgentHarness/`
- `Content.Client/Agent/`
- `Content.Shared/Agent/`
- `Content.Tests/Agent/`
- `docs/`
- The `RobustToolbox` submodule pinned to its published prototype commit

RobustToolbox changes include:

- `Robust.Client/ClientIoC.cs`
- `Robust.Client/Network/LoopbackLineServer.cs`
- `Robust.Client/Serialization/JsonCodec.cs`

## Next Steps

1. Run the manual two-client scenario:
   - Start `Content.Server`.
   - Start one normal graphical client.
   - Start one agent-enabled client with `--cvar agent.bridge_port=47614`.
   - Join both clients near a door.
   - Run `python3 AgentHarness/m1_runner.py --port 47614`.
2. Capture the machine-readable pass line, server log, and human-visible movement, door, and speech evidence.
3. Repeat the complete scenario successfully 20 times before starting headless extraction, provider integration, or multi-agent work.

## Clone And Resume

```shell
gh repo clone sp-entertainment/space-station-14 -- --branch prototype --recurse-submodules
cd space-station-14
git submodule update --init --recursive
dotnet test Content.Tests/Content.Tests.csproj --filter AgentProtocolTest
```

Continue from `.tasks/agent-bridge-m1/TASKS.md` and this handoff. Preserve the client-scoped, server-authoritative boundary and do not expand scope before the 20-run promotion gate passes.
