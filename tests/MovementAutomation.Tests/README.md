# Movement automation checks

Run from the repository root with the .NET 10 SDK:

```powershell
dotnet run --project .\tests\MovementAutomation.Tests\MovementAutomation.Tests.csproj -c Release
```

Links `MovementAutomation.cs` with local player, physics, navigation and task fixtures. Checks cover ownership, signed-speed restoration, mode changes, pauses, object replacement, lobby/round transitions, saved positions, yo-yo limits, Task Walker arrival and timer waits, and bounded recovery.

Follow/Orbit cases cover pursuit, speed changes, step limits, blocked destinations, alternative endpoints, pending searches and timeouts. Lag Mode cases cover walking/stationary phases, two spaced teleports, collision sweeps, correction limits, blocked attempts and cancellation. Simulated velocity is applied to fixture positions to check actual displacement.

The router, task helper and RPCs are fixtures. These tests do not establish native collision or multiplayer behavior; the real router and task handler have separate suites. Keyboard focus-loss hooks are not linked here, so a controller pause test does not imply that modes survive alt-tab.
