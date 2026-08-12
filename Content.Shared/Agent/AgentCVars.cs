using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared.Agent;

[CVarDefs]
public sealed class AgentCVars : CVars
{
    public static readonly CVarDef<int> BridgePort =
        CVarDef.Create("agent.bridge_port", 0, CVar.CLIENTONLY);
}
