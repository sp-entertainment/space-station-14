#!/usr/bin/env python3
"""Deterministic M1 runner for an already joined agent client near a door."""

import argparse
import json
import math
import socket
import time


def send(stream, message):
    stream.write((json.dumps(message, separators=(",", ":")) + "\n").encode())
    stream.flush()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--port", type=int, default=47614)
    parser.add_argument("--timeout", type=float, default=15)
    args = parser.parse_args()
    deadline = time.monotonic() + args.timeout

    with socket.create_connection(("127.0.0.1", args.port), timeout=args.timeout) as connection:
        stream = connection.makefile("rwb", buffering=0)
        observation = wait_for(stream, deadline, lambda message: message.get("type") == "observe")
        door = next((entity for entity in observation["visible"] if entity["kind"] == "door"), None)
        if door is None:
            fail("no interaction-reachable door in observation")

        x, y = door["rel"]
        length = math.hypot(x, y)
        direction = [x / length, y / length] if length else [1, 0]
        send(stream, action("move-1", observation["tick"], "move", direction=direction, duration_ms=100))
        require_accepted(stream, deadline, "move-1")

        observation = wait_for(stream, deadline, lambda message: message.get("type") == "observe")
        door = next((entity for entity in observation["visible"] if entity["kind"] == "door"), None)
        if door is None:
            fail("door left interaction range after movement")

        send(stream, action("interact-1", observation["tick"], "interact", target=door["id"]))
        require_accepted(stream, deadline, "interact-1")
        send(stream, action("say-1", observation["tick"], "say", text="Agent bridge online."))
        require_accepted(stream, deadline, "say-1")

        wait_for(
            stream,
            deadline,
            lambda message: message.get("type") == "event"
            and message.get("target") == door["id"]
            and message.get("outcome") in {"opening", "open"},
        )

    print(json.dumps({"status": "passed", "scenario": "m1", "door": door["id"]}))


def action(request_id, tick, action_type, **fields):
    return {"v": 0, "id": request_id, "based_on": tick, "type": action_type, **fields}


def require_accepted(stream, deadline, request_id):
    result = wait_for(
        stream,
        deadline,
        lambda message: message.get("type") == "result" and message.get("id") == request_id,
    )
    if result.get("status") != "accepted":
        fail(f"{request_id} rejected: {result.get('reason', 'unknown')}")


def wait_for(stream, deadline, predicate):
    while time.monotonic() < deadline:
        line = stream.readline()
        if not line:
            fail("bridge disconnected")
        message = json.loads(line)
        if predicate(message):
            return message
    fail("scenario timed out")


def fail(reason):
    print(json.dumps({"status": "failed", "scenario": "m1", "reason": reason}))
    raise SystemExit(1)


if __name__ == "__main__":
    main()
