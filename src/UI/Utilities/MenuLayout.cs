namespace HAU;

/// <summary>Pixel geometry for the fixed sidebar and responsive content viewport.</summary>
public static class MenuLayout
{
    public const float SidebarWidth = 156f;
    public const float HorizontalChrome = 191f;
    public const float VerticalChrome = 132f;
    public const float DesiredWidth = 700f;
    public const float DesiredHeight = 590f;

    public static LayoutBounds Fit(float screenWidth, float screenHeight, float x, float y)
    {
        float width = System.Math.Min(DesiredWidth, System.Math.Max(1f, screenWidth - 12f));
        float height = System.Math.Min(DesiredHeight, System.Math.Max(1f, screenHeight - 12f));
        float left = System.Math.Clamp(x, 0f, System.Math.Max(0f, screenWidth - width));
        float top = System.Math.Clamp(y, 0f, System.Math.Max(0f, screenHeight - height));
        return new LayoutBounds(left, top, width, height);
    }

    public readonly struct LayoutBounds
    {
        public float X { get; }
        public float Y { get; }
        public float Width { get; }
        public float Height { get; }
        public LayoutBounds(float x, float y, float width, float height)
        { X = x; Y = y; Width = width; Height = height; }
    }
}
