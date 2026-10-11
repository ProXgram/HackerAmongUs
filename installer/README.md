# HAU installer

The Windows installer supports Steam, Microsoft Store and Xbox App for Among
Us 19.0.0 / 2026.9.29. It includes its .NET runtime, verifies downloaded files,
preserves other plugins and backs up replaced menus. Epic Games uses manual ZIPs.

Supported old settings are imported into `HAU.cfg` and `HAUProfile.txt` when
those files are absent. Existing HAU settings stay intact. Loader upgrades
require a recognized inventory and support rollback; unknown or mixed files
stop the upgrade.

## Build packages

Test the finished DLL first. From the repository root:

```powershell
python scripts/package-hau.py --version 1.0.2 --dll src/bin/Release/net6.0/HAU.dll
```

Use the report's ZIP hash in `installer/HAU.Setup/SetupForm.cs`, then publish
with the .NET 10 SDK:

```powershell
dotnet publish installer/HAU.Setup/HAU.Setup.csproj -c Release -o artifacts/setup/hau/publish
```

The output is `HAUSetup.exe`. Build both manual ZIPs with the verified hashes:

```powershell
python scripts/package-manual.py --version 1.0.2 --plugin-sha <ZIP-SHA256> --dll-sha <DLL-SHA256>
python scripts/package-manual.py --version 1.0.2 --platform steam --plugin-sha <ZIP-SHA256> --dll-sha <DLL-SHA256>
```

## Publish 1.0.2

1. Finish testing and place `HAUSetup.exe` in `downloads/setup/`.
2. Stage the signed manifest:

   ```powershell
   python scripts/sign-update-manifest.py --version 1.0.2 --private-key <KEY-OUTSIDE-THE-REPOSITORY> --game-version 2026.9.29 --stage
   python scripts/package-setup.py --release 1.0.2
   ```

3. Publish `hau-v1.0.2` with `HAUSetup.exe`, the plugin ZIP, both manual ZIPs and the source ZIP.
4. Verify the public files and signature before promoting `updates/hau/1.0.2.json` to `updates/hau/latest.json`.

Keep signing keys outside the repository. Leave earlier releases and the legacy
`updates/latest.json` unchanged.

## Updates and licenses

On supported launchers, the updater checks the signed HAU feed at startup,
verifies compatibility and file hashes, and installs after Among Us closes.
It does not close or restart the game. Epic Games uses manual ZIP updates.

HAU retains [GPL-3.0](../LICENSE) and [source attribution](../CREDITS.md).
Runtime notices remain in `HAU.Setup/Legal`, `ManualLegal` and `ManualLegal788`;
the installer displays them under **Credits & licenses**. Release archives
include the required notices and corresponding source.

Installer/update checks are in `tests/Setup.Core.Tests`, `tests/AutoUpdate.Tests`,
`tests/AutomaticUpdate.Tests` and `tests/UpdateManifest.Tests`.
The Core option `--verify-release-plugin <ZIP>` checks the packaged plugin
against isolated Steam and Xbox installation fixtures.
