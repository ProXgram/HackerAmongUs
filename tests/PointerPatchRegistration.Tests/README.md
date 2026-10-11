# Pointer patch registration checks

Run from the repository root on Windows with a .NET SDK and the .NET Framework 4.8 runtime:

```powershell
dotnet build tests/PointerPatchRegistration.Tests/PointerPatchRegistration.Tests.csproj -c Release
tests/PointerPatchRegistration.Tests/bin/Release/net48/PointerPatchRegistration.Tests.exe
```

Uses the pinned HarmonyX 2.10.2 assembly to run `PatchAll` on the production pointer and guide patches. Checks cover registration of all nine pointer originals and `MatchInfoGuide.Open`, interception, input outside the guard window, blocked guide updates and normal guide refresh. Test patches are removed on completion.

This runs real Harmony in a separate .NET Framework process with game stubs. Native IL2CPP input behavior still requires a live close-click check; release timing is covered by `PointerInputGuard.Tests`.
