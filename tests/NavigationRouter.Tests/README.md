# Navigation router checks

Run from the repository root with the .NET 10 SDK:

```powershell
dotnet run --project .\tests\NavigationRouter.Tests\NavigationRouter.Tests.csproj -c Release
dotnet build .\tests\NavigationRouter.Tests\sdk-check\SdkCheck.csproj -c Release
```

The main suite links `NavigationRouter.cs` with synthetic physics. Checks cover detours, doorways, blocked routes, corner cutting, moving goals, cached routes, geometry changes, contact recovery, retry limits, pending searches, completed failures, steering distance and diagnostics. Moving-target cases check incremental search budgets and validate each simulated movement segment.

The separate SDK check compiles the same helper against AmongUs.GameLibs.Steam `2026.9.29` for .NET 6, including collider geometry, collision masks, filtered physics queries, native arrays and clock APIs. Its output stays under this test folder and is not installed.

Synthetic geometry and successful compilation do not establish native map clearance, frame performance, input delivery or multiplayer behavior. Movement-controller integration is covered separately by `MovementAutomation.Tests`.
