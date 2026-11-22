using Punchy.Core;
using Xunit;

namespace Punchy.Tests
{
    public sealed class HandCooldownTrackerTests
    {
        [Fact]
        public void SameHand_BlocksRapidWiggleWithinCooldownAndAllowsAfterCooldown()
        {
            var tracker = new HandCooldownTracker();

            Assert.False(tracker.IsOnCooldown(PunchHandSide.Right, 1.00d, 0.40f));
            tracker.RecordHit(PunchHandSide.Right, 1.00d);

            Assert.True(tracker.IsOnCooldown(PunchHandSide.Right, 1.10d, 0.40f));
            Assert.True(tracker.IsOnCooldown(PunchHandSide.Right, 1.39d, 0.40f));
            Assert.False(tracker.IsOnCooldown(PunchHandSide.Right, 1.40d, 0.40f));
        }

        [Fact]
        public void LeftAndRightHands_TrackCooldownIndependently()
        {
            var tracker = new HandCooldownTracker();

            tracker.RecordHit(PunchHandSide.Left, 5.00d);

            Assert.True(tracker.IsOnCooldown(PunchHandSide.Left, 5.10d, 0.40f));
            Assert.False(tracker.IsOnCooldown(PunchHandSide.Right, 5.10d, 0.40f));
        }

        [Fact]
        public void ItemReleaseGraceWindow_BlocksImmediateSwingAfterDroppingWeapon()
        {
            var tracker = new HandCooldownTracker();

            tracker.RecordHoldingState(PunchHandSide.Right, true, 10.00d);
            tracker.RecordHoldingState(PunchHandSide.Right, false, 10.05d);

            Assert.True(tracker.IsInItemReleaseGrace(PunchHandSide.Right, 10.05d, 0.15f));
            Assert.True(tracker.IsInItemReleaseGrace(PunchHandSide.Right, 10.14d, 0.15f));
            Assert.False(tracker.IsInItemReleaseGrace(PunchHandSide.Right, 10.16d, 0.15f));
            Assert.False(tracker.IsInItemReleaseGrace(PunchHandSide.Left, 10.05d, 0.15f));
        }
    }
}
