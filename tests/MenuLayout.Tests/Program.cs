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
Console.WriteLine(checks + " menu layout checks passed.");
