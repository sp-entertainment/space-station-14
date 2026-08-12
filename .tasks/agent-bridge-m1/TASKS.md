# Agent Bridge M1

## Objective

One graphical agent client joins a local round, observes an interaction-reachable door, moves, opens it through ordinary input/server validation, and says a fixed sentence visible to a human client.

## Status

Implementation, focused automated checks, and live graphical-client transport smoke test complete. Manual two-client door/speech round remains.

## Completed

- Pinned content commit `dc52779df93c0e5d3c2aacfcd438eea68b86a8a4`.
- Pinned RobustToolbox commit `5960554571b266f49ac6bce8677a4869032da9de`.
- Initialized all submodules and built the untouched solution.
- Traced movement, interaction, and speech entry paths.
- Added the disabled-by-default protocol, bridge, observation projection, action dispatch, and deterministic runner.
- Added protocol validation checks for supported, malformed, stale, unknown, and oversized actions.
- Proved the engine sandbox boundary and limited the engine change to a generic loopback line transport and JSON codec.
- Launched a graphical client against a local server and verified the v0 hello and observation over loopback.

## Remaining

1. Start a local server and one normal client.
2. Start an agent-enabled client on a dedicated test account and join near a door.
3. Run `python3 AgentHarness/m1_runner.py --port 47614`.
4. Capture the machine-readable pass line, server log, and human-visible door/speech evidence.
5. Repeat 20 times before starting headless extraction.

## Boundaries

- No direct state mutation or privileged observation.
- No headless mode, model provider, public-server deployment, or multi-agent scale before the promotion gate.
