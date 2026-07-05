using UnityEngine;

/// <summary>
/// 런 간 Personal Best — PlayerPrefs.
/// </summary>
public static class RunMetaProgress
{
    const string BestScoreKey = "BeatDefender.BestScore";
    const string BestRhythmKey = "BeatDefender.BestRhythmPercent";
    const string BestPerfectKey = "BeatDefender.BestPerfectCount";

    public struct RecordResult
    {
        public int BestScore;
        public int BestRhythmPercent;
        public int BestPerfectCount;
        public bool NewBestScore;
        public bool NewBestRhythm;
        public bool NewBestPerfect;
    }

    public static int BestScore => PlayerPrefs.GetInt(BestScoreKey, 0);
    public static int BestRhythmPercent => PlayerPrefs.GetInt(BestRhythmKey, 0);
    public static int BestPerfectCount => PlayerPrefs.GetInt(BestPerfectKey, 0);

    public static RecordResult RecordRun(RunStats stats, ScoreCalculator.Result result)
    {
        int score = result.TotalScore;
        int rhythm = result.RhythmAccuracyPercent;
        int perfect = stats != null ? stats.PerfectCount : 0;

        int prevScore = BestScore;
        int prevRhythm = BestRhythmPercent;
        int prevPerfect = BestPerfectCount;

        bool newScore = score > prevScore;
        bool newRhythm = rhythm > prevRhythm;
        bool newPerfect = perfect > prevPerfect;

        if (newScore)
            PlayerPrefs.SetInt(BestScoreKey, score);
        if (newRhythm)
            PlayerPrefs.SetInt(BestRhythmKey, rhythm);
        if (newPerfect)
            PlayerPrefs.SetInt(BestPerfectKey, perfect);

        if (newScore || newRhythm || newPerfect)
            PlayerPrefs.Save();

        return new RecordResult
        {
            BestScore = newScore ? score : prevScore,
            BestRhythmPercent = newRhythm ? rhythm : prevRhythm,
            BestPerfectCount = newPerfect ? perfect : prevPerfect,
            NewBestScore = newScore,
            NewBestRhythm = newRhythm,
            NewBestPerfect = newPerfect
        };
    }
}
