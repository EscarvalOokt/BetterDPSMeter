using NUnit.Framework;

namespace BetterDPSMeter.Tests.Combat
{
    [TestFixture]
    public sealed class DpsCombatTrackerTests
    {
        private const double MinimumDurationSeconds = 0.5;
        private const double Tolerance = 0.000001;

        [Test]
        public void AddDamage_AccumulatesHitsAndDamageWithinWindow()
        {
            var tracker = new DpsCombatTracker
            {
                WindowSeconds = 10.0
            };

            tracker.AddDamage(0.0, 100, 5.0, MinimumDurationSeconds);
            tracker.AddDamage(1.0, 200, 5.0, MinimumDurationSeconds);

            DpsDisplaySnapshot snapshot = tracker.DisplaySnapshot;

            Assert.That(snapshot.HitsInWindow, Is.EqualTo(2));
            Assert.That(snapshot.DamageInWindow, Is.EqualTo(300));
            Assert.That(snapshot.WindowSecondsCovered, Is.EqualTo(1.0).Within(Tolerance));
            Assert.That(snapshot.AverageDps, Is.EqualTo(300));
            Assert.That(snapshot.IsLastCombatResult, Is.False);
        }

        [Test]
        public void UpdateState_RefreshesSnapshotAndTrimsExpiredDamageBetweenHits()
        {
            var tracker = new DpsCombatTracker
            {
                WindowSeconds = 2.0
            };

            tracker.AddDamage(0.0, 100, 5.0, MinimumDurationSeconds);
            tracker.AddDamage(1.0, 200, 5.0, MinimumDurationSeconds);

            tracker.UpdateState(2.1, 5.0, MinimumDurationSeconds);

            DpsDisplaySnapshot snapshot = tracker.DisplaySnapshot;

            Assert.That(snapshot.HitsInWindow, Is.EqualTo(1));
            Assert.That(snapshot.DamageInWindow, Is.EqualTo(200));
            Assert.That(snapshot.WindowSecondsCovered, Is.EqualTo(2.0).Within(Tolerance));
            Assert.That(snapshot.AverageDps, Is.EqualTo(100));
            Assert.That(snapshot.IsLastCombatResult, Is.False);
        }

        [Test]
        public void UpdateState_RecalculatesSingleHitDpsAsCoveredDurationGrows()
        {
            var tracker = new DpsCombatTracker
            {
                WindowSeconds = 10.0
            };

            tracker.AddDamage(0.0, 100, 5.0, MinimumDurationSeconds);

            Assert.That(tracker.DisplaySnapshot.WindowSecondsCovered, Is.EqualTo(0.0).Within(Tolerance));
            Assert.That(tracker.DisplaySnapshot.AverageDps, Is.EqualTo(0));

            tracker.UpdateState(0.5, 5.0, MinimumDurationSeconds);

            Assert.That(tracker.DisplaySnapshot.WindowSecondsCovered, Is.EqualTo(0.5).Within(Tolerance));
            Assert.That(tracker.DisplaySnapshot.AverageDps, Is.EqualTo(200));

            tracker.UpdateState(1.0, 5.0, MinimumDurationSeconds);

            Assert.That(tracker.DisplaySnapshot.WindowSecondsCovered, Is.EqualTo(1.0).Within(Tolerance));
            Assert.That(tracker.DisplaySnapshot.AverageDps, Is.EqualTo(100));
        }

        [Test]
        public void UpdateState_EndsCombatWithSessionAggregateWhenTimeoutExpires()
        {
            var tracker = new DpsCombatTracker
            {
                WindowSeconds = 10.0
            };

            tracker.AddDamage(0.0, 120, 3.0, MinimumDurationSeconds);
            tracker.AddDamage(1.0, 180, 3.0, MinimumDurationSeconds);
            tracker.UpdateState(3.0, 3.0, MinimumDurationSeconds);

            DpsDisplaySnapshot snapshot = tracker.DisplaySnapshot;

            Assert.That(snapshot.TimeoutRemaining, Is.EqualTo(1.0).Within(Tolerance));
            Assert.That(snapshot.IsLastCombatResult, Is.False);

            tracker.UpdateState(4.0, 3.0, MinimumDurationSeconds);

            Assert.That(snapshot.AverageDps, Is.EqualTo(300));
            Assert.That(snapshot.HitsInWindow, Is.EqualTo(2));
            Assert.That(snapshot.DamageInWindow, Is.EqualTo(300));
            Assert.That(snapshot.WindowSecondsCovered, Is.EqualTo(1.0).Within(Tolerance));
            Assert.That(snapshot.TimeoutRemaining, Is.EqualTo(0.0).Within(Tolerance));
            Assert.That(snapshot.IsLastCombatResult, Is.True);
        }

        [Test]
        public void AddDamage_AfterExpiredTimeout_StartsCleanCombatSession()
        {
            var tracker = new DpsCombatTracker
            {
                WindowSeconds = 10.0
            };

            tracker.AddDamage(0.0, 100, 3.0, MinimumDurationSeconds);
            tracker.AddDamage(4.0, 200, 3.0, MinimumDurationSeconds);

            DpsDisplaySnapshot snapshot = tracker.DisplaySnapshot;

            Assert.That(snapshot.HitsInWindow, Is.EqualTo(1));
            Assert.That(snapshot.DamageInWindow, Is.EqualTo(200));
            Assert.That(snapshot.WindowSecondsCovered, Is.EqualTo(0.0).Within(Tolerance));
            Assert.That(snapshot.AverageDps, Is.EqualTo(0));
            Assert.That(snapshot.TimeoutRemaining, Is.EqualTo(3.0).Within(Tolerance));
            Assert.That(snapshot.IsLastCombatResult, Is.False);
            Assert.That(tracker.LastHitTime, Is.EqualTo(4.0).Within(Tolerance));
        }

        [Test]
        public void RollingDuration_DoesNotCollapseWhenOldestHitIsTrimmed()
        {
            var tracker = new DpsCombatTracker
            {
                WindowSeconds = 2.0
            };

            tracker.AddDamage(0.0, 100, 5.0, MinimumDurationSeconds);
            tracker.AddDamage(2.1, 100, 5.0, MinimumDurationSeconds);
            tracker.AddDamage(2.11, 100, 5.0, MinimumDurationSeconds);

            DpsDisplaySnapshot snapshot = tracker.DisplaySnapshot;

            Assert.That(snapshot.HitsInWindow, Is.EqualTo(2));
            Assert.That(snapshot.DamageInWindow, Is.EqualTo(200));
            Assert.That(snapshot.WindowSecondsCovered, Is.EqualTo(2.0).Within(Tolerance));
            Assert.That(snapshot.AverageDps, Is.EqualTo(100));
            Assert.That(snapshot.IsLastCombatResult, Is.False);
        }

        [Test]
        public void UpdateState_AppliesDecreasedWindowSizeDuringActiveCombat()
        {
            var tracker = new DpsCombatTracker
            {
                WindowSeconds = 5.0
            };

            tracker.AddDamage(0.0, 100, 10.0, MinimumDurationSeconds);
            tracker.AddDamage(1.0, 200, 10.0, MinimumDurationSeconds);
            tracker.AddDamage(3.0, 300, 10.0, MinimumDurationSeconds);

            tracker.WindowSeconds = 2.0;
            tracker.UpdateState(3.1, 10.0, MinimumDurationSeconds);

            DpsDisplaySnapshot snapshot = tracker.DisplaySnapshot;

            Assert.That(snapshot.HitsInWindow, Is.EqualTo(1));
            Assert.That(snapshot.DamageInWindow, Is.EqualTo(300));
            Assert.That(snapshot.WindowSecondsCovered, Is.EqualTo(2.0).Within(Tolerance));
            Assert.That(snapshot.AverageDps, Is.EqualTo(150));
            Assert.That(snapshot.IsLastCombatResult, Is.False);
        }

        [Test]
        public void UpdateState_AppliesIncreasedWindowSizeUsingRetainedHistory()
        {
            var tracker = new DpsCombatTracker
            {
                WindowSeconds = 2.0
            };

            tracker.AddDamage(0.0, 100, 10.0, MinimumDurationSeconds);
            tracker.AddDamage(1.0, 200, 10.0, MinimumDurationSeconds);
            tracker.AddDamage(3.0, 300, 10.0, MinimumDurationSeconds);

            DpsDisplaySnapshot snapshot = tracker.DisplaySnapshot;

            Assert.That(snapshot.HitsInWindow, Is.EqualTo(2));
            Assert.That(snapshot.DamageInWindow, Is.EqualTo(500));
            Assert.That(snapshot.WindowSecondsCovered, Is.EqualTo(2.0).Within(Tolerance));
            Assert.That(snapshot.AverageDps, Is.EqualTo(250));

            tracker.WindowSeconds = 10.0;
            tracker.UpdateState(3.0, 10.0, MinimumDurationSeconds);

            Assert.That(snapshot.HitsInWindow, Is.EqualTo(3));
            Assert.That(snapshot.DamageInWindow, Is.EqualTo(600));
            Assert.That(snapshot.WindowSecondsCovered, Is.EqualTo(3.0).Within(Tolerance));
            Assert.That(snapshot.AverageDps, Is.EqualTo(200));
            Assert.That(snapshot.IsLastCombatResult, Is.False);
        }

        [Test]
        public void UpdateState_RestoresHistoryAfterWindowIsReducedAndIncreased()
        {
            var tracker = new DpsCombatTracker
            {
                WindowSeconds = 10.0
            };

            tracker.AddDamage(0.0, 100, 10.0, MinimumDurationSeconds);
            tracker.AddDamage(1.0, 200, 10.0, MinimumDurationSeconds);
            tracker.AddDamage(3.0, 300, 10.0, MinimumDurationSeconds);

            DpsDisplaySnapshot snapshot = tracker.DisplaySnapshot;

            Assert.That(snapshot.HitsInWindow, Is.EqualTo(3));
            Assert.That(snapshot.DamageInWindow, Is.EqualTo(600));
            Assert.That(snapshot.WindowSecondsCovered, Is.EqualTo(3.0).Within(Tolerance));
            Assert.That(snapshot.AverageDps, Is.EqualTo(200));

            tracker.WindowSeconds = 2.0;
            tracker.UpdateState(3.0, 10.0, MinimumDurationSeconds);

            Assert.That(snapshot.HitsInWindow, Is.EqualTo(2));
            Assert.That(snapshot.DamageInWindow, Is.EqualTo(500));
            Assert.That(snapshot.WindowSecondsCovered, Is.EqualTo(2.0).Within(Tolerance));
            Assert.That(snapshot.AverageDps, Is.EqualTo(250));

            tracker.WindowSeconds = 10.0;
            tracker.UpdateState(3.0, 10.0, MinimumDurationSeconds);

            Assert.That(snapshot.HitsInWindow, Is.EqualTo(3));
            Assert.That(snapshot.DamageInWindow, Is.EqualTo(600));
            Assert.That(snapshot.WindowSecondsCovered, Is.EqualTo(3.0).Within(Tolerance));
            Assert.That(snapshot.AverageDps, Is.EqualTo(200));
            Assert.That(snapshot.IsLastCombatResult, Is.False);
        }

        [Test]
        public void EndCombat_UsesSessionAggregateWhenRollingWindowIsEmpty()
        {
            var tracker = new DpsCombatTracker
            {
                WindowSeconds = 2.0
            };

            tracker.AddDamage(0.0, 100, 5.0, MinimumDurationSeconds);
            tracker.AddDamage(1.0, 200, 5.0, MinimumDurationSeconds);

            tracker.UpdateState(3.5, 5.0, MinimumDurationSeconds);

            DpsDisplaySnapshot snapshot = tracker.DisplaySnapshot;

            Assert.That(snapshot.HitsInWindow, Is.EqualTo(0));
            Assert.That(snapshot.DamageInWindow, Is.EqualTo(0));
            Assert.That(snapshot.WindowSecondsCovered, Is.EqualTo(2.0).Within(Tolerance));
            Assert.That(snapshot.AverageDps, Is.EqualTo(0));
            Assert.That(snapshot.TimeoutRemaining, Is.EqualTo(2.5).Within(Tolerance));
            Assert.That(snapshot.IsLastCombatResult, Is.False);

            tracker.UpdateState(6.0, 5.0, MinimumDurationSeconds);

            Assert.That(snapshot.HitsInWindow, Is.EqualTo(2));
            Assert.That(snapshot.DamageInWindow, Is.EqualTo(300));
            Assert.That(snapshot.WindowSecondsCovered, Is.EqualTo(1.0).Within(Tolerance));
            Assert.That(snapshot.AverageDps, Is.EqualTo(300));
            Assert.That(snapshot.TimeoutRemaining, Is.EqualTo(0.0).Within(Tolerance));
            Assert.That(snapshot.IsLastCombatResult, Is.True);
        }

        [Test]
        public void EndCombat_FinalDpsExcludesTimeoutTail()
        {
            var tracker = new DpsCombatTracker
            {
                WindowSeconds = 10.0
            };

            tracker.AddDamage(0.0, 100, 5.0, MinimumDurationSeconds);
            tracker.AddDamage(1.0, 200, 5.0, MinimumDurationSeconds);
            tracker.AddDamage(4.0, 300, 5.0, MinimumDurationSeconds);

            tracker.UpdateState(9.0, 5.0, MinimumDurationSeconds);

            DpsDisplaySnapshot snapshot = tracker.DisplaySnapshot;

            Assert.That(snapshot.HitsInWindow, Is.EqualTo(3));
            Assert.That(snapshot.DamageInWindow, Is.EqualTo(600));
            Assert.That(snapshot.WindowSecondsCovered, Is.EqualTo(4.0).Within(Tolerance));
            Assert.That(snapshot.AverageDps, Is.EqualTo(150));
            Assert.That(snapshot.TimeoutRemaining, Is.EqualTo(0.0).Within(Tolerance));
            Assert.That(snapshot.IsLastCombatResult, Is.True);
        }

        [Test]
        public void EndCombat_UsesMinimumDurationWhenAllHitsShareTimestamp()
        {
            var tracker = new DpsCombatTracker
            {
                WindowSeconds = 10.0
            };

            tracker.AddDamage(10.0, 100, 2.0, MinimumDurationSeconds);
            tracker.AddDamage(10.0, 200, 2.0, MinimumDurationSeconds);

            tracker.UpdateState(12.0, 2.0, MinimumDurationSeconds);

            DpsDisplaySnapshot snapshot = tracker.DisplaySnapshot;

            Assert.That(snapshot.HitsInWindow, Is.EqualTo(2));
            Assert.That(snapshot.DamageInWindow, Is.EqualTo(300));
            Assert.That(snapshot.WindowSecondsCovered, Is.EqualTo(MinimumDurationSeconds).Within(Tolerance));
            Assert.That(snapshot.AverageDps, Is.EqualTo(600));
            Assert.That(snapshot.TimeoutRemaining, Is.EqualTo(0.0).Within(Tolerance));
            Assert.That(snapshot.IsLastCombatResult, Is.True);
        }

        [Test]
        public void EndCombat_UsesMinimumDurationForSingleHitSession()
        {
            var tracker = new DpsCombatTracker
            {
                WindowSeconds = 10.0
            };

            tracker.AddDamage(10.0, 100, 2.0, MinimumDurationSeconds);

            tracker.UpdateState(12.0, 2.0, MinimumDurationSeconds);

            DpsDisplaySnapshot snapshot = tracker.DisplaySnapshot;

            Assert.That(snapshot.HitsInWindow, Is.EqualTo(1));
            Assert.That(snapshot.DamageInWindow, Is.EqualTo(100));
            Assert.That(snapshot.WindowSecondsCovered, Is.EqualTo(MinimumDurationSeconds).Within(Tolerance));
            Assert.That(snapshot.AverageDps, Is.EqualTo(200));
            Assert.That(snapshot.TimeoutRemaining, Is.EqualTo(0.0).Within(Tolerance));
            Assert.That(snapshot.IsLastCombatResult, Is.True);
        }
    }
}