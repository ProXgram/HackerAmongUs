# GUI close-release regression harness

```powershell
dotnet run --project tests/PointerInputGuard.Tests/PointerInputGuard.Tests.csproj -c Release
```

Links the production pointer-release guard and actual PassiveUiElement prefix.
The regression scenario uses the observed close point outside the main menu:
after the outfit picker becomes hidden, the consumed release cannot activate a
native button underneath. A regression uses the observed AU19 native event:
both GUI event types are `Used` after the button consumes the left mouse-up.
An independently observed left release arms the guard; `Used` alone does not.
It also verifies a delayed release frame, expiration,
outside-window input, click-through preferences, keyboard/programmatic closes,
pointer jitter, malformed coordinates and frame-clock changes.

The GUI state and native-button callbacks are offline contracts. This does not
run Unity/Harmony or replace a live close-click check.
