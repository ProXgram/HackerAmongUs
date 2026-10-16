using HAU;

int checks = 0;
void Assert(bool condition, string message)
{
    checks++;
    if (!condition) throw new Exception(message);
}
foreach (var screen in new[] { (320f, 240f), (640f, 480f), (800f, 600f), (1280f, 720f), (1920f, 1080f) })
foreach (var position in new[] { (-900f, -700f), (0f, 0f), (400f, 300f), (10000f, 10000f) })
{
    var bounds = MenuLayout.Fit(screen.Item1, screen.Item2, position.Item1, position.Item2);
    Assert(bounds.X >= 0 && bounds.Y >= 0, "Dragging cannot hide the title above or left of the screen.");
    Assert(bounds.X + bounds.Width <= screen.Item1, "The close controls stay horizontally on screen.");
    Assert(bounds.Y + bounds.Height <= screen.Item2, "The footer stays vertically on screen.");
    Assert(bounds.Width > 0 && bounds.Height > 0, "The viewport remains positive after a resolution change.");
}
var normal = MenuLayout.Fit(1280, 720, 290, 65);
Assert(normal.Width == 700 && normal.Height == 590, "Normal desktop geometry must retain the planned menu size.");
Assert(normal.Width - MenuLayout.HorizontalChrome >= 480, "Normal content must fit the three-way section navigation.");
Assert(normal.Height - MenuLayout.VerticalChrome >= 450, "Normal sidebar must fit all navigation rows.");
var pages = new Dictionary<string, int>
{
    ["Movement"] = 2, ["Outfits"] = 2, ["ESP"] = 2, ["Tasks"] = 0,
    ["Abilities"] = 2, ["Sabotage"] = 1, ["Chat"] = 0, ["Host Controls"] = 2,
    ["Practice"] = 0, ["Settings"] = 3, ["Logs"] = 0, ["About"] = 0
};
Assert(MenuNavigation.Destinations.Select(item => item.Id).Distinct().Count() == MenuNavigation.Destinations.Count,
    "Every feature shortcut must have a unique destination.");
foreach (var item in MenuNavigation.Destinations)
{
    Assert(pages.TryGetValue(item.Tab, out int maximum) && item.Page >= 0 && item.Page <= maximum,
        item.Id + " must route to an available tab and subpage.");
    Assert(MenuNavigation.Search(item.Title).Any(match => match.Id == item.Id), item.Title + " must be findable by name.");
}
foreach (var search in new[]
{
    ("ghost vote", "ghost-voting"), ("sprint", "sprint"), ("outfit", "outfits"),
    ("vent cooldown", "vents"), ("auto updater", "updates"), ("teleport", "teleport"),
    ("lag", "lag"), ("oxygen", "sabotage"), ("fullbright", "fullbright"),
    ("task walker", "task-walker"), ("notes", "multi-role"), ("transparency", "menu-settings"),
    ("  GHOST   VOTE ", "ghost-voting"), ("console", "logs"), ("copy paste", "chat")
})
    Assert(MenuNavigation.Search(search.Item1).Any(item => item.Id == search.Item2), search.Item1 + " must find its feature.");
Assert(MenuNavigation.Search(" ").Count == 0 && MenuNavigation.Search(null).Count == 0, "An empty search must not show every tool.");
Assert(MenuNavigation.Search("not-a-real-feature").Count == 0, "Unknown searches must show an empty result.");
Assert(MenuNavigation.Search("Sprint")[0].Id == "sprint", "An exact title must rank first.");
Assert(MenuNavigation.Find("updates")?.Page == 1, "Update search must open the Updates page directly.");
Assert(MenuNavigation.Find("follow")?.Page == 1, "Follow search must open Follow & fun directly.");
Assert(MenuNavigation.ResolveTab("Appearance") == "Outfits" && MenuNavigation.ResolveTab("Player Tools") == "Sabotage",
    "Existing category links must still resolve after the menu rename.");

var input = new MenuSearchInput();
input.Type("ignored");
Assert(input.Text == "", "Typing outside search editing must be ignored.");
input.BeginEditing();
input.Type("sprin");
input.Type("x\bt");
Assert(input.Text == "sprint", "Letters and Backspace must update the search draft.");
input.SelectAll();
input.Type("ghost vote");
Assert(input.Text == "ghost vote" && !input.AllSelected, "Ctrl+A typing must replace the entire previous query.");
Assert(input.Type("\rignored") && !input.IsEditing && input.Text == "ghost vote", "Enter must submit and stop consuming text.");
input.BeginEditing();
input.SelectAll();
input.Paste("auto\r\n updater\u0000");
Assert(input.Text == "auto updater" && input.IsEditing, "Clipboard text must be filtered without submitting the search.");
input.SelectAll();
input.Type("\b");
Assert(input.Text == "", "Backspace after Ctrl+A must clear the whole query.");
input.Type("hat\U0001F600");
input.Type("\b");
Assert(input.Text == "hat", "Backspace must remove a full Unicode surrogate pair.");
input.SelectAll();
input.Type(new string('x', MenuSearchInput.MaximumLength + 20));
Assert(input.Text.Length == MenuSearchInput.MaximumLength, "Search input must have a bounded length.");
input.StopEditing();
input.Type("ignored");
Assert(input.Text.Length == MenuSearchInput.MaximumLength, "Changing pages must stop search typing.");
input.Clear();
Assert(input.Text == "" && !input.IsEditing && !input.AllSelected, "Clear must restore the normal page without active input.");
Console.WriteLine(checks + " menu layout, navigation and search input checks passed.");
