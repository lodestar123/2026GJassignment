using UnityEngine;

static class BeatDefenderFontBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void ApplyPretendardFont()
    {
        BeatDefenderFonts.ApplyAllInScene();
    }
}
