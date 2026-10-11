# Install HAU 1.0.2

For Among Us **19.0.0 / 2026.9.29** on Windows 10/11, 64-bit.

## Automatic installer

For **Steam / Microsoft Store / Xbox App**:

1. [Download HAUSetup.exe](https://github.com/ProXgram/HackerAmongUs/releases/download/hau-v1.0.2/HAUSetup.exe).
2. Close Among Us, open the installer and click **Install HAU**.
3. Launch Among Us normally. Wait for the first launch to finish, then press **Delete**.

If the game is not found, click **Browse** and select `Among Us.exe`. The
installer includes the loader, preserves settings and backs up the previous
menu. No separate .NET download is needed. Epic Games uses the manual ZIP.

## Manual installation

- [Steam ZIP](https://github.com/ProXgram/HackerAmongUs/releases/download/hau-v1.0.2/HAU-1.0.2-Steam.zip)
- [Microsoft Store / Epic Games / Xbox App ZIP](https://github.com/ProXgram/HackerAmongUs/releases/download/hau-v1.0.2/HAU-1.0.2-MicrosoftStore-EpicGames-XboxApp.zip)

1. Close Among Us.
2. Open the game folder using the table below.
3. Extract your ZIP and copy **all its contents** directly beside `Among Us.exe`.
4. Launch the game normally. The first launch takes longer. Press **Delete** when it finishes.

| Launcher | Find the game folder |
| --- | --- |
| Steam | **Library** → right-click **Among Us** → **Manage** → **Browse local files** |
| Microsoft Store / Xbox App | **Xbox App** → **Among Us** → **Manage** → **Files** → **Browse** → **Content** |
| Epic Games | **Library** → **Among Us** → **Manage** → **Installation** → folder icon |

For Microsoft Store purchases, use the game's Xbox App entry. Select the
editable game folder; no Windows permission changes are needed.

## Replace an older menu

The installer handles replacement. For manual installation, move old menu DLLs
out of `BepInEx/plugins` first. Keep only one menu active. Older names include
`MalumMenu.dll`, `HaddadMenu.dll` and `MalumMenuEnhanced.dll`.

Keep backups outside the plugins folder and retain configuration/profile files
to import supported settings.

## Controls

- **Delete:** open or close HAU.
- **F8:** stop movement automation and color cycling.
- **Movement:** sprint key and boost up to 10×.
- **Appearance:** outfit studio and nickname editor.
- **Tasks:** manually start Automatic Tasks or Task Walker each round.
- **Host Controls:** shared rules and hosted actions.

Close the menu and chat before using movement tools. Local role previews do
not change your online assignment. Ghost Voting requires the host and each
voting ghost to enable the mod; multiplayer vote counting is untested.

## Updates

Steam, Microsoft Store and Xbox App check for compatible updates at startup.
Close Among Us normally to let a ready update install, then reopen it.
Settings and other plugins are preserved. Update status and the switch are
under **Settings → Automatic updates**; switch changes apply next launch.

A game update may need a compatible HAU release first. If HAU cannot load, run
the latest installer. Epic Games users replace files with the latest manual ZIP.

## Tested

HAU 1.0.2 is installed on Xbox / Microsoft Store. Skeld Freeplay checks passed
for reactor, oxygen, lights and communications activation and repair, plus
native sabotage-map clicks. Online play, Steam and Epic Games remain untested.

[Release](https://github.com/ProXgram/HackerAmongUs/releases/tag/hau-v1.0.2) ·
[Source](https://github.com/ProXgram/HackerAmongUs/releases/download/hau-v1.0.2/HAU-1.0.2-Source.zip) ·
[Features](../FEATURES.md) · [License](../LICENSE)
