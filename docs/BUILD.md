# Build HAU

Install Python 3.10 or newer and a .NET SDK that can build `net6.0` projects. From the repository root, run:

```powershell
python scripts/prepare-interop.py
dotnet build src/HAU.csproj -c Release -p:AmongUs=
```

The build prepares references automatically if needed. `AmongUs=` keeps the build from copying a DLL into a game installation. The result is `src/bin/Release/net6.0/HAU.dll`.

HAU 1.0.1 targets Among Us 2026.9.29, Unity 2022.3.44, BepInEx 6.0.0-be.788 and Il2CppInterop 1.5.3. The old `BepInEx.IL2CPP.MSBuild` 2.1.0-rc.1 task uses a generator API removed in 1.5.3, so this project uses the matching official CLI instead. Its .NET 6 executable can run on a newer installed .NET runtime through `--roll-forward Major`; that setting applies only to the build tool. The mod and its packaged loader remain .NET 6.

The preparation script verifies the size and SHA-256 of three fixed inputs: the [official Interop CLI 1.5.3 release](https://github.com/BepInEx/Il2CppInterop/releases/tag/v1.5.3), the [BepInEx Unity 2022.3.44 libraries](https://unity.bepinex.dev/libraries/2022.3.44.zip), and `AmongUs.GameLibs.Steam` 2026.9.29 from the BepInEx NuGet feed. The SDK supplies dummy game assemblies; no installed game or native game executable is needed to build. The Samboy NuGet feed is required by the official build 788 package's Cpp2IL dependency.

Inputs, generated references, their hash receipt and the generator log stay in ignored `artifacts/interop/`. Every build verifies the cached inputs and generated DLLs before reusing them. A changed output is regenerated; a changed downloaded input fails its pinned check. Delete a failed input from `artifacts/interop/downloads/` to download the original again. The generated DLLs and CLI binaries are build dependencies and are excluded from release/source packages.

If preparation was interrupted, ensure the previous process has stopped before removing `artifacts/interop/prepare.lock` and retrying. To select a different Python executable, add `-p:InteropPython=C:/path/to/python.exe` to the build command.
