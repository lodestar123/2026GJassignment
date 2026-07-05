using NUnit.Framework;
using UnityEngine;

public class ScoreCalculatorTests
{
    [Test]
    public void VictoryScore_IncludesSurvivalAndDefense()
    {
        var statsGo = new GameObject("Stats");
        var stats = statsGo.AddComponent<RunStats>();
        var coreGo = new GameObject("Core");
        var core = coreGo.AddComponent<BaseHealth>();
        var gameGo = new GameObject("Game");
        var game = gameGo.AddComponent<GameManager>();

        stats.RecordJudgment(JudgmentResult.Perfect);
        stats.RecordEnemyKill(EnemyKind.EighthNote);

        var result = ScoreCalculator.Calculate(stats, core, game, victory: true);

        Assert.IsTrue(result.Cleared);
        Assert.AreEqual(5000, result.SurvivalBonus);
        Assert.AreEqual(core.CurrentHp * 200, result.DefenseBonus);
        Assert.Greater(result.TotalScore, 5000);

        Object.DestroyImmediate(statsGo);
        Object.DestroyImmediate(coreGo);
        Object.DestroyImmediate(gameGo);
    }

    [Test]
    public void RhythmAccuracy_UsesPerfectAndGoodWeights()
    {
        int rhythm = ScoreCalculator.ComputeRhythmAccuracyPercent(38, 22, 0);
        Assert.AreEqual(82, rhythm);
    }

    [Test]
    public void DefeatScore_UsesGradeD()
    {
        var result = ScoreCalculator.Calculate(null, null, null, victory: false);
        Assert.AreEqual("D", result.Grade);
        Assert.IsFalse(result.Cleared);
    }
}
