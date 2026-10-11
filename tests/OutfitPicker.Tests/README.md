# Outfit picker checks

Run from the repository root with the .NET 10 SDK:

```powershell
dotnet run --project tests/OutfitPicker.Tests/OutfitPicker.Tests.csproj -c Release
```

Links the outfit selection handler with game stubs. Checks cover six categories, name/ID search, ordering, deduplication, catalog changes, selected/equipped state, slot preservation, occupied colors, save rollback, profiles, round guards, cooldowns, restore and preview fallbacks.

Requests are recorded locally. Native rendering, menu layout and multiplayer acceptance require separate game testing.
