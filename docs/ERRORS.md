# Errors

## 2026-07-27 — Initial focused build

**Issue:** The first agent protocol test build exposed missing explicit `System` imports and a Robust analyzer restriction on invoking methods through `DoorComponent.State`.

**Resolution:** Added the required imports and converted door states with an explicit switch, preserving read-only component access.

## 2026-07-27 — Client sandbox rejected transport

**Issue:** A compiled client failed runtime module verification because content assemblies may not access raw sockets or `System.Text.Json`.

**Resolution:** Kept gameplay policy in `Content.Client` and moved only a generic bounded loopback line server and JSON codec into RobustToolbox. A graphical client then passed sandbox verification and emitted the v0 handshake and observation over `127.0.0.1`.
