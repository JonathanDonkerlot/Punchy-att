using Punchy.Core;
using Xunit;

namespace Punchy.Tests
{
    public sealed class FistPoseEvaluatorTests
    {
        [Theory]
        [InlineData(0.00f, false)]
        [InlineData(0.50f, false)]
        [InlineData(0.69f, false)]
        [InlineData(0.70f, true)]
        [InlineData(1.00f, true)]
        public void GradualGripMode_RequiresThreshold(float gripAxis, bool expectedClosed)
        {
            PunchConfigSnapshot cfg = PunchConfigSnapshot.Default.Sanitize();
            FistPoseState state = FistPoseState.GradualGrip(gripAxis);

            bool closed = FistPoseEvaluator.EvaluateInstantClosedFist(in state, in cfg, out bool usedFallback);

            Assert.False(usedFallback);
            Assert.Equal(expectedClosed, closed);
        }

        [Theory]
        [InlineData(0, true)]     // All fingers curled = closed fist
        [InlineData(2, false)]    // IndexFinger extended (pointing)
        [InlineData(1, false)]    // Thumb extended (thumbs-up)
        [InlineData(31, false)]   // All fingers extended (open palm)
        public void ExplicitGestureMode_RequiresZeroExtendedFingerMask(int activeFingerMask, bool expectedClosed)
        {
            PunchConfigSnapshot cfg = PunchConfigSnapshot.Default.Sanitize();
            FistPoseState state = FistPoseState.ExplicitGesture(activeFingerMask);

            bool closed = FistPoseEvaluator.EvaluateInstantClosedFist(in state, in cfg, out bool usedFallback);

            Assert.False(usedFallback);
            Assert.Equal(expectedClosed, closed);
        }

        [Fact]
        public void FreeSkeletonMode_AcceptsClosedFistAndRejectsOpenOrPointing()
        {
            PunchConfigSnapshot cfg = PunchConfigSnapshot.Default.Sanitize();

            FistPoseState closedFist = FistPoseState.FreeSkeleton(new[] { 0.75f, 0.90f, 0.92f, 0.88f, 0.85f });
            FistPoseState openHand = FistPoseState.FreeSkeleton(new[] { 0.10f, 0.05f, 0.05f, 0.08f, 0.10f });
            // Index finger uncurled (0.05), middle uncurled (0.10) => not a fist even if thumb/ring/little curled
            FistPoseState peaceSign = FistPoseState.FreeSkeleton(new[] { 0.90f, 0.05f, 0.10f, 0.90f, 0.90f });

            Assert.True(FistPoseEvaluator.EvaluateInstantClosedFist(in closedFist, in cfg, out bool f1));
            Assert.False(f1);

            Assert.False(FistPoseEvaluator.EvaluateInstantClosedFist(in openHand, in cfg, out bool f2));
            Assert.False(f2);

            Assert.False(FistPoseEvaluator.EvaluateInstantClosedFist(in peaceSign, in cfg, out bool f3));
            Assert.False(f3);
        }

        [Fact]
        public void LagGraceWindow_ToleratesBriefOpenPacketAfterClosedFist()
        {
            PunchConfigSnapshot cfg = PunchConfigSnapshot.Default.Sanitize();
            double lastClosed = -1d;

            FistPoseState closed = FistPoseState.GradualGrip(0.9f);
            FistPoseState open = FistPoseState.GradualGrip(0.1f);

            Assert.True(FistPoseEvaluator.EvaluateWithLagGrace(in closed, 1.00d, ref lastClosed, in cfg, out _));
            Assert.True(FistPoseEvaluator.EvaluateWithLagGrace(in open, 1.08d, ref lastClosed, in cfg, out _));   // 0.08s <= 0.12s
            Assert.False(FistPoseEvaluator.EvaluateWithLagGrace(in open, 1.20d, ref lastClosed, in cfg, out _));  // 0.20s > 0.12s
        }

        [Fact]
        public void MissingGestureDataOrRequireClosedFistFalse_UsesGracefulFallback()
        {
            PunchConfigSnapshot requireFist = PunchConfigSnapshot.Default.Sanitize();
            FistPoseState unavailable = FistPoseState.Unavailable;

            bool resultMissing = FistPoseEvaluator.EvaluateInstantClosedFist(in unavailable, in requireFist, out bool fallbackMissing);
            Assert.True(resultMissing);
            Assert.True(fallbackMissing);

            PunchConfigSnapshot noFistRequired = new PunchConfigSnapshot(requireClosedFist: false).Sanitize();
            FistPoseState openHand = FistPoseState.GradualGrip(0.0f);

            bool resultDisabled = FistPoseEvaluator.EvaluateInstantClosedFist(in openHand, in noFistRequired, out bool fallbackDisabled);
            Assert.True(resultDisabled);
            Assert.True(fallbackDisabled);
        }
    }
}
