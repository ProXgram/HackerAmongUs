namespace HAU;

public class PlayerToolsTab : ITab
{
    public string name => "Player Tools";
    private int _page;
    private readonly ShipTab _ship = new();
    private readonly ChatTab _chat = new();

    public void Draw()
    {
        _page = UIHelpers.Subtabs(_page, "Round tools", "Chat");
        if (_page == 0) _ship.Draw();
        else _chat.Draw();
    }
}
