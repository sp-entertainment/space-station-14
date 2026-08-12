using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Robust.Client.Serialization;
using Robust.Shared.GameObjects;

namespace Content.Client.Agent;

internal static class AgentProtocol
{
    public const int Version = 0;
    public const int MaxMessageBytes = 4096;
    public const int MaxRequestIdLength = 64;
    public const int MaxSayLength = 128;
    public const uint MaxActionAgeTicks = 120;

    private static readonly JsonCodec Json = new();

    public static string Serialize<T>(T value)
    {
        return Json.Serialize(value);
    }

    public static bool TryParseAction(
        string line,
        uint currentTick,
        [NotNullWhen(true)] out AgentAction? action,
        out string error)
    {
        action = null;
        error = "protocol-invalid";

        if (string.IsNullOrWhiteSpace(line) ||
            Encoding.UTF8.GetByteCount(line) > MaxMessageBytes)
        {
            error = "message-size";
            return false;
        }

        if (!Json.TryDeserialize(line, out action))
            return false;

        if (action == null ||
            action.V != Version ||
            string.IsNullOrWhiteSpace(action.Id) ||
            action.Id.Length > MaxRequestIdLength ||
            string.IsNullOrWhiteSpace(action.Type))
        {
            return false;
        }

        if (action.BasedOn > currentTick ||
            currentTick - action.BasedOn > MaxActionAgeTicks)
        {
            error = "stale";
            return false;
        }

        switch (action.Type)
        {
            case "move":
                if (action.Direction is not { Length: 2 } ||
                    !float.IsFinite(action.Direction[0]) ||
                    !float.IsFinite(action.Direction[1]) ||
                    action.Direction[0] * action.Direction[0] + action.Direction[1] * action.Direction[1] is <= 0f or > 1.0001f ||
                    action.DurationMs is < 50 or > 1000)
                {
                    return false;
                }
                break;
            case "interact":
                if (!TryParseEntity(action.Target, out _))
                    return false;
                break;
            case "say":
                if (string.IsNullOrWhiteSpace(action.Text) || action.Text.Length > MaxSayLength)
                    return false;
                break;
            case "wait":
            case "stop":
                if (action.DurationMs is < 0 or > 1000)
                    return false;
                break;
            default:
                error = "unknown-action";
                return false;
        }

        error = "";
        return true;
    }

    public static bool TryParseEntity(string? value, out EntityUid uid)
    {
        uid = default;
        return value is { Length: > 1 } &&
               value[0] == 'e' &&
               EntityUid.TryParse(value.AsSpan(1), out uid) &&
               uid.Valid;
    }

    public static string EntityId(EntityUid uid)
    {
        return $"e{uid.Id}";
    }
}

internal sealed record AgentAction(
    int V,
    string Id,
    uint BasedOn,
    string Type,
    string? Target = null,
    float[]? Direction = null,
    int DurationMs = 0,
    string? Text = null);

internal sealed record AgentResult(int V, string Id, uint Tick, string Status, string? Reason = null)
{
    public string Type => "result";
}

internal sealed record AgentEvent(int V, uint Tick, string Target, string Outcome)
{
    public string Type => "event";
}

internal sealed record AgentObservation(
    int V,
    uint Seq,
    uint Tick,
    AgentSelf Self,
    IReadOnlyList<AgentVisibleEntity> Visible,
    IReadOnlyList<string> Actions)
{
    public string Type => "observe";
}

internal sealed record AgentSelf(string Id, float[] Pos);

internal sealed record AgentVisibleEntity(string Id, string Kind, string Name, float[] Rel, string State);
