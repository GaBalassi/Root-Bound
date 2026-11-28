using System.Collections.Generic;
using NUnit.Framework;
using Rootbound.Player;
using UnityEngine;

namespace Rootbound.Tests
{
    public sealed class RootPowerTargetingTests
    {
        [Test]
        public void TargetingPrefersFacingDirectionAndIgnoresDistantAnchors()
        {
            var candidates = new List<RootPowerTargeting.Candidate>
            {
                new RootPowerTargeting.Candidate(0, new Vector2(-4f, 1f), RootTargetKind.Enemy),
                new RootPowerTargeting.Candidate(1, new Vector2(4.5f, 2f), RootTargetKind.Anchor),
                new RootPowerTargeting.Candidate(2, new Vector2(12f, 2f), RootTargetKind.Anchor)
            };
            Assert.AreEqual(1, RootPowerTargeting.SelectBestIndex(Vector2.zero, 1, 8.2f, candidates));
            Assert.AreEqual(0, RootPowerTargeting.SelectBestIndex(Vector2.zero, -1, 8.2f, candidates));
        }

        [Test]
        public void TargetingReturnsNoTargetWhenEmptyTooCloseOrOutOfRange()
        {
            Assert.AreEqual(-1, RootPowerTargeting.SelectBestIndex(Vector2.zero, 1, 8.2f, null));
            var candidates = new List<RootPowerTargeting.Candidate>
            {
                new RootPowerTargeting.Candidate(0, new Vector2(14f, 0f), RootTargetKind.Anchor),
                new RootPowerTargeting.Candidate(1, new Vector2(0.1f, 0f), RootTargetKind.NaturalSwitch)
            };
            Assert.AreEqual(-1, RootPowerTargeting.SelectBestIndex(Vector2.zero, 1, 8.2f, candidates));
        }
    }
}
