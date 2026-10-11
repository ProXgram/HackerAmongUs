# Install Hacker Among Us (HAU)

**HAU 1.0.1 by Rifegul.**
For Among Us **19.0.0 / 2026.9.29**, on Windows 10/11, 64-bit.

## Automatic installation

For **Steam / Microsoft Store / Xbox App**.

1. [Download HAUSetup.exe](https://github.com/ProXgram/HackerAmongUs/releases/download/hau-v1.0.1/HAUSetup.exe).
2. Close Among Us. Open the downloaded file and click **Install**.
3. Open Among Us from your usual launcher. Wait for the first launch, then press **Delete**.

The installer finds your game. If it cannot, click **Browse** and select
`Among Us.exe`. It installs the loader too, preserves settings and backs up
the previous menu. No separate .NET download is needed.

Epic Games uses the manual ZIP below.

## Manual installation

Download the ZIP matching your PC launcher:

- **[Steam ZIP](https://github.com/ProXgram/HackerAmongUs/releases/download/hau-v1.0.1/HAU-1.0.1-Steam.zip)**
- **[Microsoft Store / Epic Games / Xbox App ZIP](https://github.com/ProXgram/HackerAmongUs/releases/download/hau-v1.0.1/HAU-1.0.1-MicrosoftStore-EpicGames-XboxApp.zip)**

1. Close Among Us.
2. Open your game folder using the table below.
3. Extract the ZIP. Copy **all files and folders inside it** directly into the folder containing `Among Us.exe`.
4. Open Among Us normally. The first launch takes longer while the mod prepares its files. Press **Delete** when it finishes.

The ZIP includes HAU and the loader. Copy `BepInEx`, `dotnet` and the loose
files beside `Among Us.exe`. Copy the contents of the ZIP, rather than its
outer folder.

## Find your game folder

| Your launcher | Steps |
| --- | --- |
| Steam | **Library** → right-click **Among Us** → **Manage** → **Browse local files**. |
| Microsoft Store / Xbox App | Open **Xbox App** → **Among Us** → **Manage** → **Files** → **Browse** → **Content**. |
| Epic Games | **Library** → **Among Us** → **Manage** → **Installation** → click the **folder icon**. |

If you bought Among Us through Microsoft Store, use its **Xbox App** entry to
browse the game files. Choose the editable game folder. These steps do not
require changing Windows permissions.

## Replacing an older menu

Use the installer for the simplest replacement. For manual installation, move
older menu DLLs out of `BepInEx/plugins` before copying HAU. Keep only one menu
active. Examples of older files are `MalumMenu.dll`, `HaddadMenu.dll` and
`MalumMenuEnhanced.dll`.

Keep backups outside `BepInEx/plugins`; a DLL left in that folder can still load.
Keep your configuration and profile files if you want to retain supported
settings.

## Your first launch

- **Delete** opens and closes HAU.
- **Movement → Sprint & speed** controls sprint, its key and its boost up to **10×**.
- **Appearance → Outfit & name** opens the searchable outfit studio and nickname editor.
- **Tasks** contains Automatic Tasks and AI Tasks. Enable them yourself; task completion does not start automatically each round.
- **Host Controls** contains shared rules. Most of these controls unlock only while hosting or in Freeplay.
- **F8** stops movement automation and color cycling.

Close the menu and chat before sprinting or using AI movement. Local role
previews do not assign a real online role. Ghost Voting needs the host and each
voting ghost to enable the mod.

## Updates

Steam / Microsoft Store / Xbox App editions check for compatible published
releases when Among Us opens. If an update is ready, close the game normally and
wait for it to finish before reopening. Settings and unrelated plugins are
preserved; the previous menu is backed up.

View status under **Delete → Settings → Automatic updates**. The update
switch takes effect on the next launch.

An Among Us update may need a new compatible mod build. The updater can install
that build after it is published; it cannot repair incompatible code itself.
If HAU cannot start, run the latest installer. Epic Games users replace the
files using the latest manual ZIP.

HAU 1.0.1 includes official BepInEx Unity IL2CPP x64 build 788. Startup and
Freeplay have been checked on Xbox / Microsoft Store. Steam, Epic Games and
multiplayer behavior remain untested for this release. These downloads are for
Windows PC.

[Download page](https://github.com/ProXgram/HackerAmongUs/releases/tag/hau-v1.0.1) ·
[Source](https://github.com/ProXgram/HackerAmongUs/releases/download/hau-v1.0.1/HAU-1.0.1-Source.zip) ·
[License](../LICENSE)
