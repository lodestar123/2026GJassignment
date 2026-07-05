using TMPro;
using UnityEngine;

public static class BeatDefenderFonts
{
    const string ResourcePath = "Font/PretendardVariable SDF";

    static TMP_FontAsset _pretendard;

    public static TMP_FontAsset Pretendard
    {
        get
        {
            if (_pretendard == null)
                _pretendard = Resources.Load<TMP_FontAsset>(ResourcePath);
            return _pretendard;
        }
    }

    public static void Apply(TextMeshProUGUI text)
    {
        if (text == null)
            return;

        var font = Pretendard;
        if (font != null)
            text.font = font;
    }

    public static void ApplyAllInScene()
    {
        var font = Pretendard;
        if (font == null)
            return;

        foreach (var text in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            text.font = font;
    }
}
