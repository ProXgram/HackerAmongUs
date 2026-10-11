# Infinite Judge checks

Run from the repository root with the .NET 10 SDK:

```powershell
dotnet run --project tests/InfiniteJudge.Tests/InfiniteJudge.Tests.csproj -c Release
```

Links `InfiniteJudgeHandler.cs` and `InfiniteJudgePatches.cs` with game stubs. Checks cover meeting-only rearm in Normal Freeplay, native-charge preservation, unused-grant cleanup, consumption/refunds, late enablement, foreign callbacks, replacements and reset. Meeting-lock, task-gate, target and nonce state must stay unchanged.

Online host/guest fixtures verify that the feature never refills charges. Leaving Freeplay removes unused feature grants while preserving native charges and independent refunds.

These are modeled lifecycle checks. They do not install runtime patches or verify actual Judge actions, task unlocking, native meeting limits or server behavior.
