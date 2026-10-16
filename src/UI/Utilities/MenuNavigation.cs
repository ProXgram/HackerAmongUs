#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace HAU;

public sealed class MenuDestination
{
    public string Id { get; }
    public string Title { get; }
    public string Tab { get; }
    public int Page { get; }
    public string Location { get; }
    public string Keywords { get; }

    public MenuDestination(string id, string title, string tab, int page, string section, string keywords)
    {
        Id = id;
        Title = title;
        Tab = tab;
        Page = page;
        Location = string.IsNullOrEmpty(section) ? tab : tab + " / " + section;
        Keywords = keywords;
    }
}

public static class MenuNavigation
{
    public static IReadOnlyList<MenuDestination> Destinations { get; } = Array.AsReadOnly(new[]
    {
        new MenuDestination("sprint", "Sprint", "Movement", 0, "Sprint & speed", "boost fast run key shift 2x 4x 10x"),
        new MenuDestination("speed", "Walking speed", "Movement", 0, "Sprint & speed", "slow fast speed multiplier ghost default"),
        new MenuDestination("noclip", "Noclip", "Movement", 0, "Sprint & speed", "walk through walls invert controls"),
        new MenuDestination("follow", "Follow & orbit", "Movement", 1, "Follow & fun", "follow player shadow turbo orbit target chase"),
        new MenuDestination("zigzag", "Zigzag dash", "Movement", 1, "Follow & fun", "stunt dash zig zag movement"),
        new MenuDestination("lag", "Lag Mode", "Movement", 1, "Follow & fun", "lagging teleport correction jump backwards"),
        new MenuDestination("teleport", "Teleport to player or cursor", "Movement", 2, "Teleport", "right click tp mouse warp"),
        new MenuDestination("spots", "Saved teleport spots", "Movement", 2, "Teleport", "bookmark save point a b teleport yo yo yoyo interval"),
        new MenuDestination("outfits", "Outfit picker", "Outfits", 0, "Outfit & name", "appearance skin hat visor pet nameplate cosmetic item search"),
        new MenuDestination("random-outfit", "Random outfit", "Outfits", 0, "Outfit & name", "randomize clothes restore previous cosmetics"),
        new MenuDestination("nickname", "Change nickname", "Outfits", 0, "Outfit & name", "name username any name rename"),
        new MenuDestination("rainbow", "Rainbow color preview", "Outfits", 1, "Colors", "rainbow skin colour color changing rgb"),
        new MenuDestination("colors", "Lobby color cycling", "Outfits", 1, "Colors", "colour color changing lobby body"),
        new MenuDestination("effects", "Task animations & effects", "Outfits", 2, "Effects", "shield garbage asteroid medbay scan cameras security visual"),
        new MenuDestination("moonwalk", "Moonwalk", "Outfits", 2, "Effects", "freeze walk animation movement"),
        new MenuDestination("player-esp", "Player Info ESP", "ESP", 0, "Player ESP", "labels name host platform level information"),
        new MenuDestination("role-esp", "Role ESP", "ESP", 0, "Player ESP", "see role impostor reveal crew"),
        new MenuDestination("ghost-esp", "Ghost ESP", "ESP", 0, "Player ESP", "see dead ghosts chat protection"),
        new MenuDestination("fullbright", "Fullbright", "ESP", 0, "Player ESP", "vision no shadows darkness light see"),
        new MenuDestination("votes", "Reveal votes", "ESP", 0, "Player ESP", "anonymous voting meeting voters"),
        new MenuDestination("lobbies", "Lobby browser info", "ESP", 0, "Player ESP", "find lobby host code platform age information"),
        new MenuDestination("camera", "Zoom, Freecam & spectate", "ESP", 1, "Camera", "camera scroll mouse wheel watch player zoom out"),
        new MenuDestination("map", "Players on map", "ESP", 2, "Map & tracers", "markers crewmates impostors ghosts colours"),
        new MenuDestination("tracers", "Player & body tracers", "ESP", 2, "Map & tracers", "lines distance color crew imp ghost body corpse"),
        new MenuDestination("automatic-tasks", "Automatic Tasks", "Tasks", 0, "", "auto tasks complete finish own manual start"),
        new MenuDestination("task-walker", "Task Walker", "Tasks", 0, "", "walk navigation path route task consoles finish"),
        new MenuDestination("task-list", "Task arrows & list", "Tasks", 0, "", "show tasks window arrow direction"),
        new MenuDestination("multi-role", "Multi Role", "Abilities", 0, "Multi Role", "all in one vent track scientist vitals detective notes interrogate"),
        new MenuDestination("vents", "Vent settings", "Abilities", 1, "Role settings", "engineer unlock vents walk endless time no cooldown"),
        new MenuDestination("vitals", "Scientist Vitals", "Abilities", 1, "Role settings", "endless battery no vitals cooldown"),
        new MenuDestination("tracking", "Tracker settings", "Abilities", 1, "Role settings", "endless tracking delay cooldown infinite reach"),
        new MenuDestination("detective", "Detective settings", "Abilities", 1, "Role settings", "interrogate infinite reach notes"),
        new MenuDestination("impostor-tools", "Impostor ability settings", "Abilities", 1, "Role settings", "kill reach targeting shapeshift animation duration timer"),
        new MenuDestination("role-preview", "Local role preview", "Abilities", 2, "Previews", "fake role impostor preview assigned local"),
        new MenuDestination("sabotage", "Sabotage & repair", "Sabotage", 0, "Sabotage & doors", "reactor seismic laboratory oxygen o2 lights electrical communications comms map"),
        new MenuDestination("doors", "Door controls", "Sabotage", 0, "Sabotage & doors", "open close doors window"),
        new MenuDestination("fungle", "Fungle mixup & spores", "Sabotage", 0, "Sabotage & doors", "mushroom sabotage spore"),
        new MenuDestination("meeting-tools", "Meetings & reports", "Sabotage", 1, "Meetings & reports", "request emergency meeting close skip ejection my screen auto report bodies door minigames"),
        new MenuDestination("chat", "Chat access & messages", "Chat", 0, "", "always chat links commas delay extra characters longer messages clipboard copy cut paste"),
        new MenuDestination("host-rules", "Host round & kill rules", "Host Controls", 0, "Lobby rules", "no game end unfixable lights force start kill cooldown anyone vanished protection"),
        new MenuDestination("always-impostor", "Always Impostor", "Host Controls", 0, "Lobby rules", "host role assignment imp"),
        new MenuDestination("ghost-voting", "Ghost Voting", "Host Controls", 1, "Meetings", "dead vote ghost votes host support"),
        new MenuDestination("host-meetings", "Host meeting controls", "Host Controls", 1, "Meetings", "force skip meeting everyone eject vote immune"),
        new MenuDestination("host-player", "Host player actions", "Host Controls", 2, "Player actions", "kill telekill all crew impostors everyone kick from vents"),
        new MenuDestination("practice", "Freeplay role experiments", "Practice", 0, "", "judge infinite refill alive ghost impostor role preview"),
        new MenuDestination("menu-settings", "Menu key & appearance", "Settings", 0, "Menu", "keybind delete insert f9 f10 mouse pointer tool windows accent colour color rgb branding transparent transparency background opacity"),
        new MenuDestination("profiles", "Saved tool profiles", "Settings", 2, "Profiles & tools", "save load profile startup configuration"),
        new MenuDestination("updates", "Automatic updates", "Settings", 1, "Updates", "auto updater download check latest version upgrade"),
        new MenuDestination("advanced", "Advanced settings", "Settings", 3, "Profiles & tools / Advanced", "leave penalty local account ui unlock friends chat april fools clipboard copy lobby code click through config file reload reset panic"),
        new MenuDestination("logs", "Event logs", "Logs", 0, "", "console history deaths kills protection shapeshifts vent tasks meeting log record clear"),
        new MenuDestination("about", "About HAU", "About", 0, "", "version creator maintainer bepinex project github help"),
    });

    public static MenuDestination? Find(string id) => Destinations.FirstOrDefault(item => item.Id == id);

    public static IReadOnlyList<MenuDestination> Search(string? query)
    {
        query = (query ?? "").Trim();
        if (query.Length == 0) return Array.Empty<MenuDestination>();
        string[] words = query.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return Destinations.Where(item => words.All(word =>
                (item.Title + " " + item.Location + " " + item.Keywords).Contains(word, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(item => item.Title.Equals(query, StringComparison.OrdinalIgnoreCase) ? 0
                : item.Title.StartsWith(query, StringComparison.OrdinalIgnoreCase) ? 1
                : item.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ? 2 : 3)
            .ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public static string ResolveTab(string name) => name switch
    {
        "Appearance" => "Outfits",
        "Player Tools" or "Round tools" => "Sabotage",
        "Console" => "Logs",
        _ => name
    };
}
