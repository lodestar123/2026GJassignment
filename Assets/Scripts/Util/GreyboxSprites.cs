using UnityEngine;

/// <summary>
/// Greybox 런타임 스프라이트 — Phase C placeholder.
/// </summary>
public static class GreyboxSprites
{
    static Sprite _square;
    static Sprite _circle;

    public static Sprite Square
    {
        get
        {
            if (_square == null)
                _square = Create(8, 8, true);
            return _square;
        }
    }

    public static Sprite Circle
    {
        get
        {
            if (_circle == null)
                _circle = Create(8, 8, false);
            return _circle;
        }
    }

    public static Sprite Enemy => Circle;
    public static Sprite Tower => Square;
    public static Sprite Cell => Square;

    static Sprite _ring;

    public static Sprite Ring
    {
        get
        {
            if (_ring == null)
                _ring = CreateRing(64, 64);
            return _ring;
        }
    }

    static Sprite CreateRing(int size, int ppu)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var clear = new Color(0f, 0f, 0f, 0f);
        float center = (size - 1) * 0.5f;
        const float outerRadius = 0.46f;
        const float ringThickness = 0.035f;
        float outer = size * outerRadius;
        float inner = size * (outerRadius - ringThickness);

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = x - center;
            float dy = y - center;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            tex.SetPixel(x, y, dist <= outer && dist >= inner ? Color.white : clear);
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), ppu);
    }

    static Sprite Create(int size, int ppu, bool filled)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        var clear = new Color(0f, 0f, 0f, 0f);
        float center = (size - 1) * 0.5f;
        float radius = size * 0.42f;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            if (filled)
            {
                tex.SetPixel(x, y, Color.white);
            }
            else
            {
                float dx = x - center;
                float dy = y - center;
                tex.SetPixel(x, y, dx * dx + dy * dy <= radius * radius ? Color.white : clear);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), ppu);
    }
}
