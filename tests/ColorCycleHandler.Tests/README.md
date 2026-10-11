# Color cycle checks

Run from the repository root with the .NET 10 SDK:

```powershell
dotnet run --project .\tests\ColorCycleHandler.Tests\ColorCycleHandler.Tests.csproj -c Release
```

Links the color handler with local game, Unity and config stubs. Checks cover context/ownership gates, occupied palettes, protocol bounds, cooldowns, invalid values, clock changes and preview restoration across disguises and object replacements. Logged handler exceptions fail the tests.

Color requests are recorded without changing the assigned outfit. Native cosmetic behavior, UI wiring and multiplayer acceptance require separate game testing.
