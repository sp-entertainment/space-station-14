# Agent Instructions

## Vision

> **Immutable:** Create a living multiplayer laboratory where autonomous agents participate as recognizable, accountable crew members alongside humans. Agents perceive only what their controlled character can perceive, act through ordinary game mechanisms, and remain subject to the same server rules, physics, permissions, and consequences.

## Project Map

- `Content.Client/Agent/` — launch-gated local bridge, client-authorized observations, and normal input dispatch.
- `AgentHarness/` — external scripted runner and scenario checks. Provider credentials never enter the game process.
- `Content.Client/`, `Content.Shared/`, `Content.Server/` — upstream SS14 runtime boundaries.
- `Content.Tests/Agent/` — protocol contract checks.
- `RobustToolbox/` — pinned upstream engine submodule; change only for a proven engine limitation.

## Documents

- `README.md` — human setup and M1 run commands.
- `CONTRIBUTING.md` — contribution rules.
- `docs/FEATURES.md` — current feature inventory.
- `docs/DECISIONS.md` — append-only decision history.
- `docs/ERRORS.md` — encountered issues and resolutions.
- `.tasks/agent-bridge-m1/TASKS.md` — current implementation status.

## Commands

- Initialize: `python3 RUN_THIS.py`
- Build: `dotnet build`
- Focused tests: `dotnet test Content.Tests/Content.Tests.csproj --filter AgentProtocolTest`
- Server: `dotnet run --project Content.Server`
- Client: `dotnet run --project Content.Client`
- Agent client: `dotnet run --project Content.Client -- --cvar agent.bridge_port=47614`
- Runner: `python3 AgentHarness/m1_runner.py --port 47614`

## Rules

- Keep agent behavior client-scoped and server-authoritative.
- Route actions through existing input, interaction, inventory, hands, and chat systems.
- Never expose server-only, admin, secret, or out-of-client state.
- Keep the bridge disabled by default, loopback-only, bounded, versioned, and removable.
- Complete one observable vertical slice before expanding actions, scale, or headless execution.
- Preserve upstream naming and structure. Avoid engine changes, provider frameworks, and speculative abstractions.
