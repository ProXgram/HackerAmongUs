# HAU feature guide

**Hacker Among Us 1.0 by Rifegul.**
Open the menu with **Delete**. Your key can be changed in Settings.

The header shows whether you are a host, guest or in Freeplay. A **button**
performs one action; a **switch** stays enabled until you turn it off.
**F8** stops movement automation and color cycling.

## Home

Home shows your sprint setup, movement status, task automation and Multi Role.
Its shortcuts take you straight to the relevant section.

## Movement

### Sprint & speed

- **Sprint:** hold your chosen key for a temporary boost up to **10×**. Left Shift is the default key, and 10× is the default boost for a new HAU configuration.
- **Walking speed:** adjusts your normal speed separately from sprint. Restore default walking speed resets the current living or ghost speed.
- **Noclip:** allows local movement through walls. Automatic walking modes still use collision checks.
- **Reverse movement keys:** reverses the movement controls.

Close the menu and chat before moving. Sprint pauses in menus, meetings and
other blocked contexts, and restores the previous speed when the key is released.
Network acceptance and how remote players display movement remain separate
from the local movement behavior.

### Stunts

| Mode | Controls and behavior |
| --- | --- |
| Follow player | Choose the next player, set the follow/orbit boost and start following. It walks a ground route beside that player. |
| Orbit player | Uses the same selected player and boost to walk around them. A blocked route can pause the mode. |
| Zigzag dash | Alternates directions while a movement key is held. Its boost is separate from follow/orbit. |
| Lag mode | Mixes walking, pauses, forward jumps and backward corrections. Adjust correction timing and maximum jump distance. Walls shorten or block jumps. |

Only one movement mode runs at a time. The selected player and route status are
shown in the menu. Follow and orbit use at least 2× to catch distant players.

### Teleport

Choose a player for one teleport, enable right-click teleport to use the cursor,
or save spots A and B for Teleport Yo-Yo. Yo-Yo uses a 1–5 second jump interval
and stops after 12 jumps.

## Player Tools

**Round tools** contains emergency-meeting requests, closing the meeting on
your own screen, automatic body reporting and opening doors when used.
Closing your meeting screen does not end everyone else's meeting.

Sabotage switches request reactor/seismic, oxygen, lights or communications
changes. Separate buttons request Fungle mushroom/spore effects or open the
sabotage map. Door controls open in their own window. Map, role and host checks
still determine which requests are accepted.

**Chat** contains local chat access, send-delay and textbox settings, including
extra characters, longer text, clipboard paste and links. Changing the textbox
does not guarantee that the server will deliver a message.

## Appearance

### Outfit & name

**Outfit studio** has six categories: body color, hats, skins, visors, pets and
nameplates. Search the available catalog, browse ten results per page, inspect
an item and equip a single slot. A thumbnail appears when the game has already
loaded that item's image.

You can clear a cosmetic slot, save the current look, wear that saved look or
restore the original outfit. Saved-look and original-outfit snapshots apply to
the current player profile during this session. Equip before a round, from the
main menu or lobby, or in Freeplay. The game checks color and cosmetic requests.

Random Outfit selects a combination and provides its own restore button.
The extra-cosmetics switch affects the available choices; it does not establish
ownership or guarantee what unmodded players see.

The nickname editor keeps a draft until you press **Apply my nickname**. Change
it before a round; the game validates the submitted name.

### Colors and effects

- **Rainbow preview:** cycles your body color on your own screen.
- **Lobby color cycle:** requests available colors before a round and stops at round start. Shared visibility remains untested.
- **Task effects:** shield/garbage buttons, repeated asteroid firing, scan and camera-use effects. Availability depends on the map and game checks; an effect does not complete its task.
- **Moonwalk:** a local movement appearance effect.

## Information

**Player info** groups player/role labels, ghost visibility, vision shadows,
vote details and lobby information. Displays use data available on your client.

**Camera** contains a wider view, free camera and selecting a player to spectate.
Spectating pauses your player movement. Stop spectating to return to your player;
free camera and spectating cannot run together.

**Map & lines** groups map markers and direction lines for crewmates, impostors,
ghosts and bodies, with player-color and distance options.

## Tasks

**Automatic Tasks** completes your own assigned tasks after you enable it.
It is off at the beginning of every round. Profiles and keybind reloads do not
start it automatically.

**AI Tasks** walks to your own task consoles and advances supported task steps
after reaching them. Press Start AI Tasks, then close the menu and chat. The
status explains waits or route failures; unsupported timed stages remain manual.
Starting AI Tasks turns Automatic Tasks off. F8 stops AI movement.

Task arrows point toward your tasks. The task-list window shows observed player
tasks, but completion buttons apply only to your own tasks.

## Abilities

**Multi Role** combines vent access, selected-player tracking, Scientist Vitals,
Detective Notes and nearby Interrogate in a draggable panel. Enable it and close
the menu during a living player's normal round or Freeplay.

The bundle preserves your assigned role. Vitals uses the native display;
tracking uses observed player positions. Notes record confirmed deaths seen
while enabled and their captured rooms. Interrogate uses the nearby tracked
player's recorded location. Earlier unseen deaths do not become evidence.
Escape or Close Detective Notes closes the notebook.

**Role settings** groups local Engineer timers, Scientist battery/cooldown,
Tracker delay/range, native Detective range and Impostor ability options.
These controls require an available ability; they do not assign a real online
role. Vent and other network actions still depend on host validation.

**Previews** changes the role interface on your own screen. The guest Impostor
preview does not grant counted kills. Real hosted assignment belongs in Host
Controls; Freeplay experiments belong in Practice.

## Host Controls

Most controls are disabled unless you host the lobby or use Freeplay.

| Subpage | Controls |
| --- | --- |
| Lobby rules | Your role assignment before the next round, keeping a round running, lights rules, start-game action, kill rules and protection window. |
| Meetings | Start a meeting, finish a meeting for everyone, choose a player to eject and vote immunity. Eject/end actions require an active meeting. |
| Player actions | Choose a player for a kill action, group kill actions and keeping players out of vents. Kill actions are unavailable in the lobby. |

**Ghost Voting requires both the host and each voting ghost to enable this
mod.** A guest can enable their own ghost controls here; the host decides whether
votes are counted. Multiplayer vote counting remains untested.

## Practice

Freeplay contains Impostor and Judge practice controls, role previews and
alive/ghost previews. They do not force a real online Judge role. The current
Judge practice handler permits one use per meeting.

## Settings, About and Logs

**Settings** groups menu keys, tool-window behavior, accent colors, profiles and
automatic updates. Advanced settings hold optional local display/convenience
options and configuration-file actions.

Settings are stored in `BepInEx/config/HAU.cfg`; profiles use
`BepInEx/config/HAUProfile.txt`. Supported old settings can be imported without
removing their originals. Task automation still requires manual activation.

Automatic updates check at startup and apply compatible published releases
after Among Us closes. Steam / Microsoft Store / Xbox App support this; Epic
Games uses the manual ZIP. A game update needs a compatible HAU build published
first.

**About** shows HAU's edition, game version and project link.
**Logs** chooses which observed events to record and opens or clears the current
session log. It keeps up to 300 entries and cannot recover earlier unseen events.

## Verification limits

Xbox / Microsoft Store gameplay has been tested. Steam and Epic Games gameplay
remain untested. Offline regression checks do not prove live multiplayer
acceptance or what other clients see. Jester and the disabled historical
Overload experiment are not included as active HAU features.

[Installation](docs/INSTALL.md) · [Edition history](FORK.md) · [License](LICENSE)
