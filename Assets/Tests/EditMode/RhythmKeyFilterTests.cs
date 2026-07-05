using NUnit.Framework;
using UnityEngine;

namespace BeatDefender.Tests
{
    public class RhythmKeyFilterTests
    {
        [TearDown]
        public void TearDown()
        {
            RhythmKeyFilter.UnregisterReservedKey(KeyCode.Space);
        }

        [Test]
        public void IsRhythmKey_Escape_ReturnsFalse()
        {
            Assert.IsFalse(RhythmKeyFilter.IsRhythmKey(KeyCode.Escape));
        }

        [Test]
        public void IsRhythmKey_Space_ReturnsTrue()
        {
            Assert.IsTrue(RhythmKeyFilter.IsRhythmKey(KeyCode.Space));
        }

        [Test]
        public void IsRhythmKey_AlphaKey_ReturnsTrue()
        {
            Assert.IsTrue(RhythmKeyFilter.IsRhythmKey(KeyCode.A));
        }

        [Test]
        public void IsRhythmKey_LeftShift_ReturnsFalse()
        {
            Assert.IsFalse(RhythmKeyFilter.IsRhythmKey(KeyCode.LeftShift));
        }

        [Test]
        public void IsRhythmKey_MouseButton_ReturnsFalse()
        {
            Assert.IsFalse(RhythmKeyFilter.IsRhythmKey(KeyCode.Mouse0));
        }

        [Test]
        public void RegisterReservedKey_ExcludesFromRhythmKeys()
        {
            RhythmKeyFilter.RegisterReservedKey(KeyCode.Space);
            Assert.IsFalse(RhythmKeyFilter.IsRhythmKey(KeyCode.Space));
        }
    }
}
