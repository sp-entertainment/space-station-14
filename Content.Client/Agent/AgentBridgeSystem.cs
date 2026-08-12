using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using Content.Client.Chat.Managers;
using Content.Shared.Agent;
using Content.Shared.Chat;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Interaction;
using Robust.Client.GameObjects;
using Robust.Client.Input;
using Robust.Client.Network;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client.Agent;

public sealed partial class AgentBridgeSystem : EntitySystem
{
    private const int MaxQueuedActions = 16;
    private static readonly IReadOnlyList<string> SupportedActions = ["move", "interact", "say", "wait", "stop"];

    [Dependency] private IConfigurationManager _configuration = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IInputManager _input = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private InputSystem _inputSystem = default!;
    [Dependency] private LoopbackLineServer _bridge = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private readonly ConcurrentQueue<string> _actions = new();
    private readonly HashSet<string> _requestIds = [];
    private readonly Queue<string> _requestIdOrder = [];
    private readonly HashSet<EntityUid> _observedDoors = [];
    private BoundKeyFunction? _moving;
    private TimeSpan _movementEnds;
    private TimeSpan _nextObservation;
    private int _queuedActions;
    private int _observationRequested;
    private uint _observationSequence;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DoorComponent, DoorStateChangedEvent>(OnDoorStateChanged);

        var port = _configuration.GetCVar(AgentCVars.BridgePort);
        if (port is <= 0 or > ushort.MaxValue)
            return;

        _bridge.Start(port, AgentProtocol.MaxMessageBytes, OnLine, OnConnectionChanged);
        Log.Info($"Agent bridge listening on 127.0.0.1:{port}");
    }

    public override void Shutdown()
    {
        ReleaseMovement();
        _bridge.Stop();
        base.Shutdown();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_bridge.IsConnected)
        {
            ReleaseMovement();
            return;
        }

        if (_moving != null && _timing.RealTime >= _movementEnds)
            ReleaseMovement();

        for (var i = 0; i < 4 && _actions.TryDequeue(out var line); i++)
        {
            Interlocked.Decrement(ref _queuedActions);
            HandleAction(line);
        }

        if (Interlocked.Exchange(ref _observationRequested, 0) != 0)
            _nextObservation = TimeSpan.Zero;

        if (_timing.RealTime < _nextObservation)
            return;

        _nextObservation = _timing.RealTime + TimeSpan.FromMilliseconds(250);
        SendObservation();
    }

    private void OnConnectionChanged(bool connected)
    {
        if (!connected)
            return;

        _actions.Clear();
        Interlocked.Exchange(ref _queuedActions, 0);
        SendRaw("""{"v":0,"type":"hello","actions":["move","interact","say","wait","stop"]}""");
        Interlocked.Exchange(ref _observationRequested, 1);
    }

    private void OnLine(string line)
    {
        if (Interlocked.Increment(ref _queuedActions) > MaxQueuedActions)
        {
            Interlocked.Decrement(ref _queuedActions);
            Send(new AgentResult(AgentProtocol.Version, "", 0, "rejected", "queue-full"));
            return;
        }

        _actions.Enqueue(line);
    }

    private void HandleAction(string line)
    {
        var tick = _timing.CurTick.Value;
        if (!AgentProtocol.TryParseAction(line, tick, out var action, out var error))
        {
            Send(new AgentResult(AgentProtocol.Version, action?.Id ?? "", tick, "rejected", error));
            return;
        }

        if (_player.LocalEntity == null)
        {
            Send(new AgentResult(AgentProtocol.Version, action.Id, tick, "disconnected", "no-attached-player"));
            return;
        }

        if (!_requestIds.Add(action.Id))
        {
            Send(new AgentResult(AgentProtocol.Version, action.Id, tick, "rejected", "duplicate"));
            return;
        }

        _requestIdOrder.Enqueue(action.Id);
        if (_requestIdOrder.Count > 256)
            _requestIds.Remove(_requestIdOrder.Dequeue());

        switch (action.Type)
        {
            case "move":
                Move(action);
                break;
            case "interact":
                Interact(action);
                break;
            case "say":
                _chat.SendMessage(action.Text!, ChatSelectChannel.Local);
                Accept(action.Id);
                break;
            case "stop":
                ReleaseMovement();
                Accept(action.Id);
                break;
            case "wait":
                Accept(action.Id);
                break;
        }
    }

    private void Move(AgentAction action)
    {
        var direction = new Vector2(action.Direction![0], action.Direction[1]);
        var function = MathF.Abs(direction.X) > MathF.Abs(direction.Y)
            ? direction.X > 0 ? EngineKeyFunctions.MoveRight : EngineKeyFunctions.MoveLeft
            : direction.Y > 0 ? EngineKeyFunctions.MoveUp : EngineKeyFunctions.MoveDown;

        ReleaseMovement();
        DispatchInput(function, BoundKeyState.Down);
        _moving = function;
        _movementEnds = _timing.RealTime + TimeSpan.FromMilliseconds(action.DurationMs);
        Accept(action.Id);
    }

    private void Interact(AgentAction action)
    {
        if (!AgentProtocol.TryParseEntity(action.Target, out var target) ||
            !_observedDoors.Contains(target) ||
            !Exists(target) ||
            !_interaction.InRangeUnobstructed(_transform.GetMapCoordinates(_player.LocalEntity!.Value), target))
        {
            Send(new AgentResult(AgentProtocol.Version, action.Id, _timing.CurTick.Value, "rejected", "client-unresolvable"));
            return;
        }

        Accept(action.Id);
        DispatchInput(EngineKeyFunctions.Use, BoundKeyState.Down, target);
        DispatchInput(EngineKeyFunctions.Use, BoundKeyState.Up, target);
    }

    private void ReleaseMovement()
    {
        if (_moving is not { } function)
            return;

        DispatchInput(function, BoundKeyState.Up);
        _moving = null;
    }

    private void DispatchInput(BoundKeyFunction function, BoundKeyState state, EntityUid? target = null)
    {
        if (_player.LocalEntity is not { } player)
            return;

        var uid = target ?? EntityUid.Invalid;
        var coordinates = target is { } entity
            ? Transform(entity).Coordinates
            : Transform(player).Coordinates;
        var functionId = _input.NetworkBindMap.KeyFunctionID(function);
        var message = new ClientFullInputCmdMessage(
            _timing.CurTick,
            _timing.TickFraction,
            functionId,
            coordinates,
            ScreenCoordinates.Invalid,
            state,
            uid);

        _inputSystem.HandleInputCommand(_player.LocalSession, function, message);
    }

    private void SendObservation()
    {
        if (_player.LocalEntity is not { } player)
            return;

        var playerPosition = _transform.GetMapCoordinates(player);
        var visible = new List<AgentVisibleEntity>();
        _observedDoors.Clear();

        var query = EntityQueryEnumerator<DoorComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var door, out _))
        {
            if (!_interaction.InRangeUnobstructed(playerPosition, uid))
                continue;

            var relative = _transform.GetMapCoordinates(uid).Position - playerPosition.Position;
            _observedDoors.Add(uid);
            visible.Add(new AgentVisibleEntity(
                AgentProtocol.EntityId(uid),
                "door",
                Name(uid),
                [relative.X, relative.Y],
                DoorStateName(door.State)));
        }

        Send(new AgentObservation(
            AgentProtocol.Version,
            ++_observationSequence,
            _timing.CurTick.Value,
            new AgentSelf(AgentProtocol.EntityId(player), [playerPosition.X, playerPosition.Y]),
            visible,
            SupportedActions));
    }

    private void OnDoorStateChanged(Entity<DoorComponent> door, ref DoorStateChangedEvent args)
    {
        if (_observedDoors.Contains(door))
        {
            Send(new AgentEvent(
                AgentProtocol.Version,
                _timing.CurTick.Value,
                AgentProtocol.EntityId(door),
                args.State.ToString().ToLowerInvariant()));
        }
    }

    private void Accept(string requestId)
    {
        Send(new AgentResult(AgentProtocol.Version, requestId, _timing.CurTick.Value, "accepted"));
    }

    private static string DoorStateName(DoorState state)
    {
        return state switch
        {
            DoorState.Closed => "closed",
            DoorState.Closing => "closing",
            DoorState.Open => "open",
            DoorState.Opening => "opening",
            DoorState.Welded => "welded",
            DoorState.Denying => "denying",
            DoorState.Emagging => "emagging",
            _ => "unknown",
        };
    }

    private void Send<T>(T message)
    {
        SendRaw(AgentProtocol.Serialize(message));
    }

    private void SendRaw(string line)
    {
        _bridge.SendLine(line);
    }
}
