# Real Harmony pointer-registration integration

```powershell
dotnet build tests/PointerPatchRegistration.Tests/PointerPatchRegistration.Tests.csproj -c Release
tests/PointerPatchRegistration.Tests/bin/Release/net48/PointerPatchRegistration.Tests.exe
```

Uses the actual pinned HarmonyX 2.10.2 assembly, rather than attribute stubs.
Links the production PassiveUiElement and MatchInfoGuide.Open patch classes,
runs real PatchAll, checks all nine pointer originals and the independent guide
hook with GetPatchInfo, then calls those originals to prove interception and
outside-window pass-through. Guide tests also check that blocked input cannot
clear/rebuild entries and that ordinary opens retain the existing refresh behavior.
All test patches are removed on completion.

Runs in its own .NET Framework process. No installed game, IL2CPP, network traffic
or user interface is controlled. The separate PointerInputGuard suite tests the
production release algorithm; a native close-click test completes verification.
