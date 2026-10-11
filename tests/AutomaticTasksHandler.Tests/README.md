# Automatic Tasks HUD checks

Run from the repository root with the .NET 10 SDK:

```powershell
dotnet run --project tests/AutomaticTasksHandler.Tests/AutomaticTasksHandler.Tests.csproj -c Release
```

Links the task handler and scheduler. A stub HUD singleton records acquisition and creates a HUD on its first read. Checks cover avoiding HUD acquisition while disabled or in invalid contexts, handling eligible Normal/Freeplay contexts, pausing during intro/meeting/exile and stopping acquisition after a return to the main menu.

This suite checks control flow and HUD acquisition order. It does not run Unity, complete native tasks or send network requests.
