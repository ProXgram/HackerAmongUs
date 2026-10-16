# HAU features

**Hacker Among Us 1.0.3 by ProXgram.** Press **Delete** to open the menu and **F8**
to stop movement automation and color cycling. Buttons perform one action;
switches enable a setting or show its current state.

## Movement

- **Sprint:** hold your chosen key for a boost up to 10×. Left Shift is the default. Releasing it restores your previous speed.
- **Speed:** change walking speed or restore its default.
- **Noclip:** move through walls locally.
- **Follow / Orbit:** select a player and set the boost. Both walk a ground route; blocked paths can pause them.
- **Zigzag Dash:** alternate directions while holding a movement key.
- **Lag Mode:** mix walking, pauses, forward jumps and backward corrections. Walls can shorten or block jumps.
- **Teleport:** jump to a selected player or teleport to the cursor with right-click. Teleport Yo-Yo alternates saved spots A and B for up to 12 jumps.

Close the menu and chat before moving. Only one movement mode runs at a time.
Remote movement display and network acceptance depend on the game.

## Sabotage and Chat

**Sabotage → Meetings & reports** has meeting requests, local meeting closure,
Auto Report Bodies and door controls. Closing a meeting on your screen does not
finish it for everyone.

Sabotage buttons request reactor/seismic, oxygen, lights or communications
changes. Fungle has separate mushroom and spore buttons. **Open sabotage map**
closes HAU so native icons can be clicked. These controls require an active
Normal round or Freeplay. Guest requests do not change your assigned role, and
the host can reject them. The status reports a request, not a confirmed result.

**Chat** has local chat access, send-delay and textbox options, clipboard
controls and extra characters. The server still validates submitted messages.

## Outfits

**Outfit studio** has color, hat, skin, visor, pet and nameplate categories.
Search, browse ten results per page and equip or clear one slot. Save a look,
wear it again or restore the original outfit during the same session.
Unavailable thumbnails use a placeholder; those items remain selectable.

Equip before a round or in Freeplay. Random Outfit has a separate restore
button. Cosmetic choices do not establish ownership or guarantee what other
players see. Edit your nickname before a round and press **Apply my nickname**.

Rainbow preview cycles color locally. Lobby color cycle sends color requests
before a round and stops at round start; shared visibility is untested. Task
effects show supported shield, garbage, asteroid, scan and camera-use effects
without completing tasks. Moonwalk changes movement appearance locally.

## ESP

- **Player ESP:** Player Info, Roles, Ghosts, Fullbright / No Shadows, Reveal Votes and Lobby Browser Info.
- **Camera:** wider view, mouse-wheel zoom, Freecam and Spectate.
- **Map & tracers:** map markers and direction lines for players and bodies, with player or distance colors.

Displays use information available on your client. Spectating pauses your
movement. Freecam and Spectate cannot run together; camera tools pause in
menus/chat and restore the camera when their player or round changes.

## Tasks

**Automatic Tasks** completes your own assigned tasks after you enable it.
It starts off each round and cannot be enabled by loading a profile.

**Task Walker** walks to your own consoles and advances supported task steps
on arrival. Start it, then close the menu and chat. It turns Automatic Tasks
off. Timed or unsupported steps may need manual input. F8 stops the walker.

Task arrows point toward your tasks. The task window can show observed player
tasks, but completion buttons apply only to your own assignments.

## Abilities

**Multi Role** combines vent access, selected-player tracking, Scientist Vitals,
Detective Notes and nearby Interrogate in a draggable panel. Enable it and
close the menu during a living player's Normal round or Freeplay.

Notes record confirmed deaths observed while enabled and their captured
rooms. Interrogate uses recorded location data. Earlier unseen deaths are not
reconstructed. Escape or **Close Detective Notes** closes the notebook.

Role settings adjust available Engineer, Scientist, Tracker, Detective and
Impostor abilities. They do not grant a real online role. Vent and other network
requests remain subject to host validation.

**Previews** change your local role interface. A guest Impostor preview does
not grant counted kills. Host previews keep the original crew/impostor team.
Real hosted assignment is in Host Controls; experiments are in Practice.

## Host Controls and Practice

Host Controls groups lobby rules, roles, kill settings, protection, meetings,
ejection and player actions. Most require hosting or Freeplay. Ejection and
meeting-end actions need an active meeting; kill actions are unavailable in
lobbies.

**Ghost Voting** needs the host and each voting ghost to enable the mod.
Multiplayer vote counting is untested.

Practice contains Freeplay role and alive/ghost controls. Judge practice
permits one use per meeting and does not force an online Judge assignment.

## Settings and updates

Search at the top of the menu finds tools and opens their exact page.
Settings contains menu keys, colors, transparency, profiles, update options and logs. Click-through
is off by default so HAU controls do not also click the game underneath.
Imported preferences keep their saved value.

Configuration: `BepInEx/config/HAU.cfg`.
Profile: `BepInEx/config/HAUProfile.txt`.
Profiles retain keys and persistent switches but do not replay action buttons,
start task automation or restore sabotage states and Spectate targets.
Guest profiles cannot activate host-only controls.

Steam, Microsoft Store and Xbox App check for compatible updates at startup
and periodically while open. Settings also has a manual check button. Downloads
show progress and retry temporary connection failures. Ready updates install
after Among Us closes. Epic Games uses manual ZIP updates.
A game update may need a compatible HAU release first.

About shows version details. Logs records selected events from the current
session, keeping up to 300 entries.

[Install](docs/INSTALL.md) · [Release notes](FORK.md) · [License](LICENSE)
