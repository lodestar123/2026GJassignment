using System.Collections.Generic;
using NUnit.Framework;

namespace BeatDefender.Tests
{
    public class RhythmPatternLibraryTests
    {
        [Test]
        public void GoldPulse_ExpectedHitTimes_AtReferenceMeasure()
        {
            var pattern = RhythmPatternLibrary.All[0];
            Assert.AreEqual(CommandType.GoldPulse, pattern.Type);

            float[] times = pattern.GetExpectedHitTimes(BeatClock.ReferenceMeasureDuration);
            Assert.AreEqual(2, times.Length);
            Assert.AreEqual(0f, times[0], 0.0001f);
            Assert.AreEqual(0.5f, times[1], 0.0001f);
        }

        [Test]
        public void JudgeHitTimes_AllPerfect_ReturnsPerfect()
        {
            var actual = new List<float> { 0f, 0.5f };
            var expected = new[] { 0f, 0.5f };

            Assert.AreEqual(
                JudgmentResult.Perfect,
                RhythmPatternLibrary.JudgeHitTimes(actual, expected, timeScale: 1f));
        }

        [Test]
        public void JudgeHitTimes_WithinGoodNotPerfect_ReturnsGood()
        {
            float perfect = RhythmPatternLibrary.JudgmentPerfectSeconds;
            float good = RhythmPatternLibrary.JudgmentGoodSeconds;
            // PERFECT(±0.11) 초과 · GOOD(±0.22) 이내 — 0.5×perfect는 여전히 PERFECT 창 안쪽
            float offset = (perfect + good) * 0.5f;
            var actual = new List<float> { offset, 0.5f + offset };
            var expected = new[] { 0f, 0.5f };

            Assert.AreEqual(
                JudgmentResult.Good,
                RhythmPatternLibrary.JudgeHitTimes(actual, expected, timeScale: 1f));
        }

        [Test]
        public void JudgeHitTimes_OutsideGoodWindow_ReturnsMiss()
        {
            float good = RhythmPatternLibrary.JudgmentGoodSeconds;
            var actual = new List<float> { good + 0.01f, 0.5f };
            var expected = new[] { 0f, 0.5f };

            Assert.AreEqual(
                JudgmentResult.Miss,
                RhythmPatternLibrary.JudgeHitTimes(actual, expected, timeScale: 1f));
        }

        [Test]
        public void JudgeHitTimes_TapCountMismatch_ReturnsMiss()
        {
            var actual = new List<float> { 0f };
            var expected = new[] { 0f, 0.5f };

            Assert.AreEqual(
                JudgmentResult.Miss,
                RhythmPatternLibrary.JudgeHitTimes(actual, expected, timeScale: 1f));
        }

        [Test]
        public void ByTapCount_LookupMatchesAllPatterns()
        {
            Assert.IsTrue(RhythmPatternLibrary.ByTapCount.ContainsKey(2));
            Assert.IsTrue(RhythmPatternLibrary.ByTapCount.ContainsKey(3));
            Assert.IsTrue(RhythmPatternLibrary.ByTapCount.ContainsKey(5));
            Assert.IsTrue(RhythmPatternLibrary.ByTapCount.ContainsKey(6));
            Assert.AreEqual(4, RhythmPatternLibrary.All.Count);
        }

        [Test]
        public void TryGetByTapCount_ResolvesUniqueCommand()
        {
            Assert.IsTrue(RhythmPatternLibrary.TryGetByTapCount(2, out var two));
            Assert.AreEqual(CommandType.GoldPulse, two.Type);

            Assert.IsTrue(RhythmPatternLibrary.TryGetByTapCount(3, out var three));
            Assert.AreEqual(CommandType.RhythmShot, three.Type);

            Assert.IsTrue(RhythmPatternLibrary.TryGetByTapCount(5, out var five));
            Assert.AreEqual(CommandType.OverloadStrike, five.Type);

            Assert.IsTrue(RhythmPatternLibrary.TryGetByType(CommandType.ChainZap, out var chain));
            Assert.AreEqual(5, chain.TapCount);
        }

        [Test]
        public void TryGetByTapCount_UnknownCount_ReturnsFalse()
        {
            Assert.IsFalse(RhythmPatternLibrary.TryGetByTapCount(4, out _));
            Assert.IsFalse(RhythmPatternLibrary.TryGetByTapCount(0, out _));
        }

        [Test]
        public void ShouldBackfillLateTap_GoldPulseComplete_NewCycleBeat_DoesNotBackfill()
        {
            var pending = new List<float> { 0f, 0.5f };
            Assert.IsFalse(RhythmPatternLibrary.ShouldBackfillLateTap(
                pending, relEndedCycle: 1.02f, newCycleRel: 0.02f,
                measureDuration: 1f, scale: 1f));
        }

        [Test]
        public void ShouldBackfillLateTap_OverloadFourTaps_BoundaryFifth_Backfills()
        {
            var pending = new List<float> { 0f, 0.25f, 0.5f, 0.75f };
            Assert.IsTrue(RhythmPatternLibrary.ShouldBackfillLateTap(
                pending, relEndedCycle: 1.05f, newCycleRel: 0.05f,
                measureDuration: 1f, scale: 1f));
        }

        [Test]
        public void ShouldBackfillLateTap_EmptyPending_NeverBackfills()
        {
            Assert.IsFalse(RhythmPatternLibrary.ShouldBackfillLateTap(
                new List<float>(), relEndedCycle: 1.05f, newCycleRel: 0.05f,
                measureDuration: 1f, scale: 1f));
        }

        [Test]
        public void IsCompletePattern_GoldPulseTwoTaps_ReturnsTrue()
        {
            var taps = new List<float> { 0f, 0.5f };
            Assert.IsTrue(RhythmPatternLibrary.IsCompletePattern(taps, 1f, 1f));
        }

[Test]
        public void CanExtendToLongerPattern_TwoGoldTaps_NoLongerSharesPrefixWithAnyPattern()
        {
            // GoldPulse(0, 0.5)는 더 이상 RhythmShot(0, 0.25, 0.75)의 접두가 아니므로,
            // 어떤 시각에도 확장 대상이 없어야 한다 — 2타 커맨드가 즉시 확정될 수 있는 근거.
            var taps = new List<float> { 0f, 0.5f };
            Assert.IsFalse(RhythmPatternLibrary.CanExtendToLongerPattern(
                taps, nowRel: 0.55f, measureDuration: 1f, scale: 1f));
            Assert.IsFalse(RhythmPatternLibrary.CanExtendToLongerPattern(
                taps, nowRel: 0.98f, measureDuration: 1f, scale: 1f));
        }

        [Test]
        public void CanExtendToLongerPattern_RhythmShotThreeTaps_DoesNotExtendToOverload()
        {
            var taps = new List<float> { 0f, 0.5f, 0.75f };
            Assert.IsFalse(RhythmPatternLibrary.CanExtendToLongerPattern(
                taps, nowRel: 0.8f, measureDuration: 1f, scale: 1f));
        }

        [Test]
        public void TryMatchCompletePattern_ChainZap_FiveTaps()
        {
            var taps = new List<float> { 0f, 0.15f, 0.35f, 0.55f, 0.75f };
            Assert.IsTrue(RhythmPatternLibrary.TryGetByType(CommandType.ChainZap, out var chainPattern));
            Assert.IsTrue(RhythmPatternLibrary.TryMatchSinglePattern(
                taps, 1f, 1f, chainPattern, out var judgment));
            Assert.AreEqual(CommandType.ChainZap, chainPattern.Type);
            Assert.AreEqual(JudgmentResult.Perfect, judgment);
        }

        [Test]
        public void CanExtendPattern_GoldPulseTwoTaps_ReturnsFalse()
        {
            Assert.IsTrue(RhythmPatternLibrary.TryGetByType(CommandType.GoldPulse, out var gold));
            var taps = new List<float> { 0f, 0.5f };
            Assert.IsFalse(RhythmPatternLibrary.CanExtendPattern(
                taps, nowRel: 0.55f, measureDuration: 1f, scale: 1f, gold));
        }

        [Test]
        public void CanExtendPattern_GoldPulseOneTap_WaitsForSecondSlot()
        {
            Assert.IsTrue(RhythmPatternLibrary.TryGetByType(CommandType.GoldPulse, out var gold));
            var taps = new List<float> { 0f };
            Assert.IsTrue(RhythmPatternLibrary.CanExtendPattern(
                taps, nowRel: 0.2f, measureDuration: 1f, scale: 1f, gold));
            Assert.IsFalse(RhythmPatternLibrary.CanExtendPattern(
                taps, nowRel: 0.75f, measureDuration: 1f, scale: 1f, gold));
        }

        [Test]
        public void TryMatchSinglePattern_RhythmShot_RequiresThreeTaps()
        {
            Assert.IsTrue(RhythmPatternLibrary.TryGetByType(CommandType.RhythmShot, out var shot));
            var two = new List<float> { 0f, 0.5f };
            Assert.IsFalse(RhythmPatternLibrary.TryMatchSinglePattern(
                two, 1f, 1f, shot, out _));

            var three = new List<float> { 0f, 0.5f, 0.75f };
            Assert.IsTrue(RhythmPatternLibrary.TryMatchSinglePattern(
                three, 1f, 1f, shot, out var judgment));
            Assert.AreEqual(JudgmentResult.Perfect, judgment);
        }
    }
}
