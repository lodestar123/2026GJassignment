/// <summary>
/// BALANCE §10 — 승리/패배 점수·등급.
/// </summary>
public static class ScoreCalculator
{
    public struct Result
    {
        public bool Cleared;
        public int TotalScore;
        public string Grade;
        public int RhythmAccuracyPercent;
        public int SurvivalBonus;
        public int DefenseBonus;
        public int CombatBonus;
        public int RhythmBonus;
        public int TimeBonus;
    }

    public static int ComputeRhythmAccuracyPercent(int perfect, int good, int miss)
    {
        int total = perfect + good + miss;
        if (total <= 0)
            return 0;

        int weighted = perfect * 100 + good * 50;
        return UnityEngine.Mathf.Clamp(
            UnityEngine.Mathf.RoundToInt((float)weighted / total),
            0,
            100);
    }

    public static Result Calculate(RunStats stats, BaseHealth core, GameManager game, bool victory)
    {
        stats ??= RunStats.Instance;
        core ??= BaseHealth.Instance;
        game ??= GameManager.Instance;

        int eighthKills = stats != null ? stats.EighthNoteKills : 0;
        int downbeatKills = stats != null ? stats.DownbeatKills : 0;
        int eliteKills = stats != null ? stats.EliteKills : 0;
        int perfect = stats != null ? stats.PerfectCount : 0;
        int good = stats != null ? stats.GoodCount : 0;
        int miss = stats != null ? stats.MissCount : 0;
        int rhythmPercent = ComputeRhythmAccuracyPercent(perfect, good, miss);

        int combat = eighthKills * 80 + downbeatKills * 200 + eliteKills * 320;
        int rhythm = perfect * 15 + good * 5;
        float elapsed = game != null ? game.ElapsedSeconds : 0f;

        if (victory)
        {
            int survival = 5000;
            int defense = (core != null ? core.CurrentHp : 0) * 200;
            int total = survival + defense + combat + rhythm;
            return new Result
            {
                Cleared = true,
                TotalScore = total,
                Grade = GradeFromScore(total, true),
                RhythmAccuracyPercent = rhythmPercent,
                SurvivalBonus = survival,
                DefenseBonus = defense,
                CombatBonus = combat,
                RhythmBonus = rhythm,
                TimeBonus = 0
            };
        }

        int timeBonus = UnityEngine.Mathf.FloorToInt(elapsed * 10f);
        int defeatTotal = combat + rhythm + timeBonus;
        return new Result
        {
            Cleared = false,
            TotalScore = defeatTotal,
            Grade = "D",
            RhythmAccuracyPercent = rhythmPercent,
            SurvivalBonus = 0,
            DefenseBonus = 0,
            CombatBonus = combat,
            RhythmBonus = rhythm,
            TimeBonus = timeBonus
        };
    }

    static string GradeFromScore(int score, bool cleared)
    {
        if (!cleared)
            return "D";

        if (score >= 18000) return "S";
        if (score >= 14000) return "A";
        if (score >= 10000) return "B";
        return "C";
    }
}
