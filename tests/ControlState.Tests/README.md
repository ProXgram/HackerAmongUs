# Control state and profile checks

Run from the repository root with the .NET 10 SDK:

```powershell
dotnet run --project tests/ControlState.Tests -c Release
```

Links the profile reader/writer, control policy and keyboard listener. Checks cover one-shot actions, old profiles, missing fields, stable field IDs and shortcuts, host/guest/Freeplay rules, focus/menu/chat pauses, panic and F8 cleanup.

Temporary files use a unique test directory. The suite does not load the game, modify its saved profile or contact a network session.
