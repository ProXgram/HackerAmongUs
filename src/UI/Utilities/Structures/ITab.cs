namespace HAU;

public interface ITab
{
    string name { get; }
    void Draw();
}

public interface IMenuPage
{
    void OpenPage(int page);
}
