# Outfit picker checks

Run `dotnet run --project tests/OutfitPicker.Tests/OutfitPicker.Tests.csproj -c Release`.

The suite compiles the real outfit selection handler against small native-contract substitutes. It checks six categories, localized-name and ID search, stable ordering, deduplication, catalog invalidation, selected versus equipped state, per-slot preservation, occupied lobby colors, save-failure rollback, profile changes, round and transition guards, click cooldowns, restore and session looks, and cached preview fallbacks.

Requests are recorded without acceptance or outfit changes. These checks establish selection behavior and request boundaries, not multiplayer acceptance. GUI layout and native rendering still need an actual game check; cosmetic previews use assets already loaded by the game.
