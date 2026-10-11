# Build HAU

Install Python 3.10+ and a .NET SDK that supports `net6.0`. From the repository root:

```powershell
dotnet build src/HAU.csproj -c Release -p:AmongUs=
```

The build prepares and verifies its game references automatically. The DLL is
`src/bin/Release/net6.0/HAU.dll`. An installed copy of Among Us is not required.
`AmongUs=` prevents the build from copying files into a game installation.

HAU 1.0.2 targets Among Us 2026.9.29, Unity 2022.3.44, BepInEx 6.0.0-be.788
and Il2CppInterop 1.5.3. References come from the pinned official Interop CLI,
Unity libraries and `AmongUs.GameLibs.Steam` package. Package restore uses the
BepInEx and Samboy feeds listed in `nuget.config`.

Build inputs and logs are cached in `artifacts/interop/` and excluded from
release archives. To prepare references separately, run:

```powershell
python scripts/prepare-interop.py
```

If preparation was interrupted, stop the previous process before removing
`artifacts/interop/prepare.lock` and retrying. To choose another Python executable,
add `-p:InteropPython=C:/path/to/python.exe` to the build command.

[Packaging instructions](../installer/README.md) · [License](../LICENSE)
