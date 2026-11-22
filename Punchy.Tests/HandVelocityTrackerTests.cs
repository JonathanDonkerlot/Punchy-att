using Punchy.Core;
using Xunit;

namespace Punchy.Tests
{
    public sealed class HandVelocityTrackerTests
    {
        [Fact]
        public void FirstSample_HasNoPreviousAndZeroSpeed()
        {
            var tracker = new HandVelocityTracker();
            HandMotionSample s = tracker.Sample(1.0d, new Vec3(0f, 1f, 0f));

            Assert.False(s.HasPrevious);
            Assert.Equal(0f, s.Speed);
            Assert.False(s.WasTeleportReset);
        }

        [Fact]
        public void ConstantVelocityMotion_MeasuresExpectedSpeedBelowAndAboveThreshold()
        {
            var tracker = new HandVelocityTracker();
            tracker.Sample(1.00d, new Vec3(0f, 1.0f, 0f));
            HandMotionSample slow = tracker.Sample(1.05d, new Vec3(0f, 1.0f, 0.08f)); // 1.6 m/s

            Assert.True(slow.HasPrevious);
            Assert.InRange(slow.Speed, 1.59f, 1.61f);
            Assert.True(slow.Speed < 2.2f);

            var fastTracker = new HandVelocityTracker();
            fastTracker.Sample(1.00d, new Vec3(0f, 1.0f, 0f));
            fastTracker.Sample(1.02d, new Vec3(0f, 1.0f, 0.07f));
            HandMotionSample fast = fastTracker.Sample(1.04d, new Vec3(0f, 1.0f, 0.14f)); // 3.5 m/s

            Assert.True(fast.HasPrevious);
            Assert.InRange(fast.Speed, 3.49f, 3.51f);
            Assert.True(fast.Speed >= 2.2f);
            Assert.True(Vec3.Dot(fast.Direction, Vec3.Forward) > 0.99f);
        }

        [Fact]
        public void Clumped20HzNetworkPacketsUnder60HzFixedUpdate_MeasureTruePacketSpeedNot3xSpike()
        {
            var tracker = new HandVelocityTracker(windowSeconds: 0.06d);
            const float dt = 1f / 60f;

            // Player swings smoothly at 3.0 m/s, but server receives NetworkTransform at ~20Hz (every 3 FixedUpdates).
            // Packet arrives at t=0.00 (z=0.00), stays stationary for two ticks, then jumps 0.15m at t=0.05s.
            tracker.Sample(0.00d, new Vec3(0f, 1f, 0.00f));
            tracker.Sample(dt * 1d, new Vec3(0f, 1f, 0.00f));
            tracker.Sample(dt * 2d, new Vec3(0f, 1f, 0.00f));
            HandMotionSample packetTick = tracker.Sample(dt * 3d, new Vec3(0f, 1f, 0.15f));

            // Naive single-tick speed would be 0.15 / (1/60) = 9.0 m/s. Windowed speed must be ~3.0 m/s.
            Assert.InRange(packetTick.Speed, 2.95f, 3.05f);
        }

        [Fact]
        public void TeleportJump_TriggersTeleportResetAndRecoversCleanly()
        {
            var tracker = new HandVelocityTracker();
            tracker.Sample(1.00d, new Vec3(0f, 1f, 0f));
            HandMotionSample teleport = tracker.Sample(1.02d, new Vec3(10f, 1f, 0f), maxTeleportStepMeters: 1.5f);

            Assert.True(teleport.WasTeleportReset);
            Assert.False(teleport.HasPrevious);
            Assert.Equal(0f, teleport.Speed);

            HandMotionSample next = tracker.Sample(1.04d, new Vec3(10f, 1f, 0.08f), maxTeleportStepMeters: 1.5f);
            Assert.False(next.WasTeleportReset);
            Assert.True(next.HasPrevious);
            Assert.InRange(next.Speed, 3.95f, 4.05f);
        }

        [Fact]
        public void NonMonotonicOrDuplicateTimestamps_DoNotProduceNaNOrInfinity()
        {
            var tracker = new HandVelocityTracker();
            tracker.Sample(2.00d, new Vec3(0f, 1f, 0f));
            HandMotionSample dup = tracker.Sample(2.00d, new Vec3(0f, 1f, 0.2f));
            HandMotionSample back = tracker.Sample(1.90d, new Vec3(0f, 1f, 0.5f));

            Assert.Equal(0f, dup.Speed);
            Assert.Equal(0f, back.Speed);
            Assert.False(float.IsNaN(dup.Speed));
            Assert.False(float.IsInfinity(back.Speed));
        }
    }
}
