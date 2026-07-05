using UnityEngine;

/// <summary>튜토리얼 완료 여부 — PlayerPrefs.</summary>
public static class TutorialProgress
{
    const string CompleteKey = "BeatDefender.TutorialComplete";

    public static bool IsComplete => PlayerPrefs.GetInt(CompleteKey, 0) == 1;

    public static void MarkComplete()
    {
        PlayerPrefs.SetInt(CompleteKey, 1);
        PlayerPrefs.Save();
    }

    public static void Reset()
    {
        PlayerPrefs.DeleteKey(CompleteKey);
        PlayerPrefs.Save();
    }
}
