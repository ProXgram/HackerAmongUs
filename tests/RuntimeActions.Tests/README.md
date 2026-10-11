# Runtime action checks

Run from the repository root with the .NET 10 SDK:

```powershell
dotnet run --project tests/RuntimeActions.Tests/RuntimeActions.Tests.csproj -c Release
```

Links the action, camera, preview and game-end handlers with offline game stubs. Checks cover host/guest rules, invalid contexts, session changes, teleport ownership, meeting requests, role choices, Freeplay, camera restoration, target departure, pauses and effect cadence.

Sabotage-map checks cover queued menu/keybind requests, closing HAU before opening the map, preserving map mode, single execution, invalid-context rejection and join/start/end cleanup. Pointer-release protection has its own `PointerInputGuard.Tests` suite.

The stubs record requests and state changes; native rendering, patch bindings and multiplayer acceptance require separate game testing.
