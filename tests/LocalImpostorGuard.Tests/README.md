# Local impostor guard checks

Run from the repository root with the .NET 10 SDK:

```powershell
dotnet run --project tests/LocalImpostorGuard.Tests/LocalImpostorGuard.Tests.csproj -c Release
```

Links `LocalImpostorHandler.cs` and `LocalImpostorKillPatches.cs` with game stubs. Checks cover guest kill suppression after a local-only role change, original-role restoration, partial setter failures, assigned special roles, replacements, host migration, Freeplay and warning lifetime. Button and outgoing-RPC prefixes are called directly.

These checks do not install runtime patches or contact a host/server. They do not establish that a guest can kill, change an authoritative role or avoid disconnection.
