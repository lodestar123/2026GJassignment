using NUnit.Framework;
using UnityEngine;

namespace BeatDefender.Tests
{
    public class RhythmInputSettingsTests
    {
        GameObject _go;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("RhythmInputSettings_Test");
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
                Object.DestroyImmediate(_go);

            if (RhythmInputSettings.Instance != null)
                Object.DestroyImmediate(RhythmInputSettings.Instance.gameObject);
        }

        [Test]
        public void BaselineInputOffset_Is024Seconds()
        {
            Assert.AreEqual(0.24f, RhythmInputSettings.BaselineInputOffsetSeconds, 0.0001f);
            Assert.AreEqual(0.24f, RhythmInputSettings.DefaultInputOffsetSeconds, 0.0001f);
        }

        [Test]
        public void DefaultInputOffsetAdjustment_IsMinus024()
        {
            Assert.AreEqual(-0.24f, RhythmInputSettings.DefaultInputOffsetAdjustment, 0.0001f);
        }

        [Test]
        public void ZeroAdjustment_UsesBaseline024()
        {
            var settings = _go.AddComponent<RhythmInputSettings>();
            settings.SetInputOffsetAdjustment(0f, persist: false);

            Assert.AreEqual(0.24f, settings.InputOffsetSeconds, 0.0001f);
            Assert.AreEqual(0.76f, settings.AdjustTapTime(1f), 0.0001f);
        }

        [Test]
        public void PositiveAdjustment_AddsToBaseline()
        {
            var settings = _go.AddComponent<RhythmInputSettings>();
            settings.SetInputOffsetAdjustment(0.05f, persist: false);

            Assert.AreEqual(0.29f, settings.InputOffsetSeconds, 0.0001f);
            Assert.AreEqual(0.71f, settings.AdjustTapTime(1f), 0.0001f);
        }

        [Test]
        public void SetInputOffsetSeconds_SetsRelativeAdjustment()
        {
            var settings = _go.AddComponent<RhythmInputSettings>();
            settings.SetInputOffsetSeconds(0.24f, persist: false);

            Assert.AreEqual(0f, settings.InputOffsetAdjustment, 0.0001f);
            Assert.AreEqual(0.24f, settings.InputOffsetSeconds, 0.0001f);
        }

        [Test]
        public void ClampAdjustment_ClampsToMinMax()
        {
            Assert.AreEqual(
                RhythmInputSettings.MinInputOffsetAdjustment,
                RhythmInputSettings.ClampAdjustment(-1f),
                0.0001f);
            Assert.AreEqual(
                RhythmInputSettings.MaxInputOffsetAdjustment,
                RhythmInputSettings.ClampAdjustment(1f),
                0.0001f);
        }

        [Test]
        public void JudgedElapsed_SubtractsOffsetFromWall()
        {
            var settings = _go.AddComponent<RhythmInputSettings>();
            settings.SetInputOffsetAdjustment(0.05f, persist: false);

            float measureStart = 10f;
            float wallTime = 10.7f;
            Assert.AreEqual(0.7f, RhythmInputSettings.GetWallElapsedInMeasure(wallTime, measureStart), 0.0001f);
            Assert.AreEqual(0.45f, RhythmInputSettings.GetJudgedElapsedInMeasure(wallTime, measureStart), 0.0001f);
        }
    }
}
