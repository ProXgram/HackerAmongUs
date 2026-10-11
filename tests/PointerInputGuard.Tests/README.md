# Pointer release checks

Run from the repository root with the .NET 10 SDK:

```powershell
dotnet run --project tests/PointerInputGuard.Tests/PointerInputGuard.Tests.csproj -c Release
```

Links the pointer-release guard and `PassiveUiElement` prefix. Checks cover consumed outfit-picker close clicks, `Used` GUI events, independently observed mouse releases, delayed frames, expiration, input outside the guard window, click-through settings, keyboard closes, pointer jitter, invalid coordinates and clock changes.

GUI events and button callbacks are stubs. These checks do not run Unity or install Harmony patches; native close-click behavior needs a game check.
