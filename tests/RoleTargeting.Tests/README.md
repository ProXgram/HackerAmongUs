# Role targeting checks

Run from the repository root with the .NET 10 SDK:

```powershell
dotnet run --project tests/RoleTargeting.Tests/RoleTargeting.Tests.csproj -c Release
```

Links the Impostor, Detective and Tracker range prefixes from `RoleBehaviourPatches.cs`. Checks cover missing or stale references, role ownership, native predicates, dead/disconnected actors, collider eligibility, nearest-target selection, context fallback and existing Impostor rules.

Game types are local stubs. These checks do not install Harmony patches or establish IL2CPP compatibility or multiplayer acceptance.
