# HAU release notes

## 1.0.2

- Restores explicit sabotage and repair requests without changing your role. The host still validates guest requests.
- Shows sabotage request status in Player Tools.
- Closes HAU when opening the sabotage map so native map icons can be clicked.
- Clears queued map requests when joining, starting or ending a game.
- Keeps kill permissions tied to the real assignment when a genuine Impostor uses a local role preview.

Installed and checked on Xbox / Microsoft Store. Reactor, oxygen, lights and
communications activation and repair, plus native sabotage-map clicks, passed
in Skeld Freeplay. Online play, Steam and Epic Games remain untested.

## Earlier HAU changes

HAU introduced the charcoal/cyan menu, separate ESP and Host Controls sections,
10× sprint controls and the outfit studio. Version 1.0.1 updated the loader to
BepInEx build 788 and fixed profile actions, target selection, camera cleanup,
outfit previews and clicks passing through closed menu windows.

Supported earlier settings can be imported into `HAU.cfg` and `HAUProfile.txt`.
Keep only one menu DLL active. Earlier published releases retain their names
and version numbers.

[Features](FEATURES.md) · [Install](docs/INSTALL.md) · [Credits](CREDITS.md) · [License](LICENSE)
