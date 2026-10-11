# Runtime action boundaries

Run `dotnet run --project tests/RuntimeActions.Tests/RuntimeActions.Tests.csproj -c Release`.

This suite links the production action, camera, preview and game-end handlers. Offline native contracts record requests and state changes without networking or an installed game.

Checks cover guest versus host execution, invalid action contexts, pending callback/session changes, deferred teleport ownership, the ordinary guest meeting request, original special-role choices, online host team preservation, Freeplay experiments, captured camera/player restoration, target departure, focus/menu/chat pauses, external camera changes and per-frame/cadence effect dispatch. Native rendering, Harmony bindings and multiplayer acceptance require separate game verification.
