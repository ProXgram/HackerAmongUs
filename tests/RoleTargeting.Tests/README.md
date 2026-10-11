# Role-targeting regression harness

Run from the repository root:

```powershell
dotnet run --project tests/RoleTargeting.Tests/RoleTargeting.Tests.csproj -c Release
```

Links the actual Impostor, Detective and Tracker range prefixes from
`src/Patches/RoleBehaviourPatches.cs`. Local game contracts exercise empty and
null snapshots, malformed or stale native references, current owning-role
bindings, native predicate dispatch, dead/disconnected actors, missing or
disabled colliders, nearest eligible selection, toggle/context fallback and
preservation of existing Impostor target rules.

These checks do not install the mod, execute IL2CPP/Harmony, send network
requests or prove multiplayer acceptance. Native API compatibility is checked
separately against the game's published interop SDK.
