# Sprint checks

Run from the repository root with the .NET 10 SDK:

```powershell
dotnet run --project .\tests\SprintHandler.Tests\SprintHandler.Tests.csproj -c Release
```

Links `SprintHandler.cs` with local game and Unity stubs. Checks cover stable speed, signed-speed restoration, multiplier limits, invalid values, configured keys, lobby/round transitions, pauses, cleanup, replaced or destroyed objects, and avoiding HUD creation. Logged handler exceptions fail the tests.

These checks cover local handler behavior. Native input, lifecycle wiring, object lifetime and multiplayer movement require separate game testing.
