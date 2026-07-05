using UnityEngine;

/// <summary>
/// StartScene — GameScene과 동일한 BeatClock + RhythmCommandDetector + RhythmTimelineUI 연동.
/// </summary>
[DefaultExecutionOrder(-120)]
public class StartSceneRhythmBootstrap : MonoBehaviour
{
    void Awake()
    {
        EnsureBeatClock();
        EnsureRhythmCommandDetector();
        EnsureRhythmTimelineUi();
    }

    void EnsureBeatClock()
    {
        if (BeatClock.Instance != null)
            return;

        if (GetComponent<BeatClock>() == null)
            gameObject.AddComponent<BeatClock>();
    }

    void EnsureRhythmCommandDetector()
    {
        var detector = GetComponent<RhythmCommandDetector>();
        if (detector == null)
            detector = gameObject.AddComponent<RhythmCommandDetector>();

        detector.SetCommandsEnabled(false);
    }

    void EnsureRhythmTimelineUi()
    {
        var rail = transform.Find("Decor/BeatRail");
        if (rail == null)
            return;

        if (rail.GetComponent<RhythmTimelineUI>() == null)
            rail.gameObject.AddComponent<RhythmTimelineUI>();
    }
}
