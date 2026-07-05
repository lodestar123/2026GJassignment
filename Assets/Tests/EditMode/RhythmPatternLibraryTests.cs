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
            Assert.IsTrue(RhythmPatternLibrary.ByTapCount.ContainsKey(1));
            Assert.IsTrue(RhythmPatternLibrary.ByTapCount.ContainsKey(2));
            Assert.IsTrue(RhythmPatternLibrary.ByTapCount.ContainsKey(3));
            Assert.IsTrue(RhythmPatternLibrary.ByTapCount.ContainsKey(5));
            Assert.AreEqual(4, RhythmPatternLibrary.All.Count);
        }

        [Test]
        public void TryGetByTapCount_ResolvesUniqueCommand()
        {
            Assert.IsTrue(RhythmPatternLibrary.TryGetByTapCount(1, out var one));
            Assert.AreEqual(CommandType.BPMBoost, one.Type);

            Assert.IsTrue(RhythmPatternLibrary.TryGetByTapCount(2, out var two));
            Assert.AreEqual(CommandType.GoldPulse, two.Type);

            Assert.IsTrue(RhythmPatternLibrary.TryGetByTapCount(3, out var three));
            Assert.AreEqual(CommandType.RhythmShot, three.Type);

            Assert.IsTrue(RhythmPatternLibrary.TryGetByTapCount(5, out var five));
            Assert.AreEqual(CommandType.OverloadStrike, five.Type);
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
        public void CanExtendToLongerPattern_TwoGoldTaps_BeforeThirdWindow_ReturnsTrue()
        {
            var taps = new List<float> { 0f, 0.5f };
            Assert.IsTrue(RhythmPatternLibrary.CanExtendToLongerPattern(
                taps, nowRel: 0.55f, measureDuration: 1f, scale: 1f));
        }

        [Test]
        public void CanExtendToLongerPattern_TwoGoldTaps_AfterThirdWindow_ReturnsFalse()
        {
            var taps = new List<float> { 0f, 0.5f };
            Assert.IsFalse(RhythmPatternLibrary.CanExtendToLongerPattern(
                taps, nowRel: 0.98f, measureDuration: 1f, scale: 1f));
        }
    }
}
