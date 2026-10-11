# Hacker Among Us Setup

HAU is Rifegul's edition. Its single-file Windows installer needs no separate
.NET installation. Users choose Among Us and click **Install HAU**.

Automatic installation supports Steam, Microsoft Store and Xbox App on
Windows 10/11 x64, for Among Us 19.0.0 / 2026.9.29. Epic Games uses the manual
ZIP. The installer validates the game, downloads verified files, preserves
other plugins, and backs up previous menus outside `BepInEx/plugins`.

Legacy configuration and profile files are copied into `HAU.cfg` and
`HAUProfile.txt` only when those targets are absent. Section names become
`HAU.*`; old originals stay intact. Existing HAU settings are preserved.
Every installer change participates in backup and rollback. It never changes
permissions, requests administrator access, or replaces a newer HAU version.

## Build and package

Finish and test the HAU build first:

```powershell
python scripts/package-hau.py --version 1.0.1 --dll <FINISHED-HAU.dll>
```

The report gives the plugin ZIP and DLL SHA256 values. Pin the ZIP hash in
`HAU.Setup/SetupForm.cs`, then build with the .NET 10 SDK:

```powershell
dotnet publish installer/HAU.Setup/HAU.Setup.csproj -c Release -o artifacts/setup/hau/publish
```

The output is `HAUSetup.exe`. Package each manual variant with the verified
hashes; add `--platform steam` for the Steam ZIP:

```powershell
python scripts/package-manual.py --version 1.0.1 --plugin-sha <ZIP-SHA256> --dll-sha <DLL-SHA256>
```

The 1.0.1 manual packages contain the unchanged official BepInEx Unity IL2CPP
788 x64 loader, its audited licenses and corresponding source. The installer
can upgrade the exact verified 755 inventory with persistent backups and
rollback. Unknown or mixed loader files stop the upgrade. They contain no game files.
Manual upgrades must remove legacy menu DLLs and duplicate HAU copies before
launching. First launch imports saved settings without deleting originals.

## Automatic updates

HAU checks the signed `updates/hau/latest.json` at startup on supported
Steam / Microsoft Store / Xbox App installations. The helper verifies the
publisher signature, product, game support, version, file sizes and hashes.
It waits for Among Us to close and applies the same backup/rollback process.
It never closes or restarts the game. Epic Games uses manual ZIP updates.

A new Among Us release needs a tested compatible HAU release. If a game
update prevents the old plugin from loading, run the latest installer.

## Publish

1. Test the finished plugin and installer, then capture
   `docs/screenshots/hau-installer.png`, `hau-home.png` and
   `hau-outfit-picker.png`.
2. Put `HAUSetup.exe` in `downloads/setup/`. Sign the new manifest using
   `scripts/sign-update-manifest.py --version 1.0.1 --private-key
   <KEY-OUTSIDE-THE-REPOSITORY> --game-version 2026.9.29 --stage`.
3. Verify the signature, then run
   `scripts/package-setup.py --release 1.0.1` to create the final matching source.
4. Publish tag `hau-v1.0.1` in [HackerAmongUs](https://github.com/ProXgram/HackerAmongUs)
   with `HAU-1.0.1-Plugin.zip`, `HAUSetup.exe`, both manual ZIPs and the source ZIP
   before activating the signed manifest on `main`.
5. Verify every public asset's bytes, then copy the staged
   `updates/hau/1.0.1.json` to `updates/hau/latest.json` and publish the feed.

Historical MalumMenu Enhanced releases and `updates/latest.json` stay intact.
The HAU product channel permits its version reset to 1.0; true HAU downgrades
remain blocked.

## Licenses and checks

Our source retains [GPL-3.0](../LICENSE) and the original mod credits.
Required runtime notices are embedded under **Credits & licenses**; their
source files remain unchanged in `HAU.Setup/Legal` and `ManualLegal`. The 788
manual inventory is versioned separately in `ManualLegal788`, including
Il2CppInterop 1.5.3 under LGPL-3.0. Earlier releases and notices stay intact.

Maintainer checks are in `tests/Setup.Core.Tests`, `tests/AutoUpdate.Tests`,
`tests/AutomaticUpdate.Tests` and `tests/UpdateManifest.Tests`. The Core test
option `--verify-release-plugin <ZIP>` checks the actual HAU ZIP with the
verified loader in isolated Steam and Xbox fixtures.
