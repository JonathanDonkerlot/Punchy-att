using Punchy.Core;
using Xunit;

namespace Punchy.Tests
{
    public sealed class PunchVfxCalculatorTests
    {
        [Fact]
        public void DisabledModOrDisabledVfx_ReturnsShouldPlayFalse()
        {
            PunchConfigSnapshot modOff = new PunchConfigSnapshot(enabled: false).Sanitize();
            PunchConfigSnapshot vfxOff = new PunchConfigSnapshot(enablePunchVfx: false).Sanitize();
            PunchConfigSnapshot surfaceOff = new PunchConfigSnapshot(enableSurfaceImpactVfx: false).Sanitize();

            Assert.False(PunchVfxCalculator.Calculate(4.0f, isDamageableHit: true, in modOff).ShouldPlay);
            Assert.False(PunchVfxCalculator.Calculate(4.0f, isDamageableHit: true, in vfxOff).ShouldPlay);
            Assert.False(PunchVfxCalculator.Calculate(4.0f, isDamageableHit: false, in surfaceOff).ShouldPlay);
            Assert.True(PunchVfxCalculator.Calculate(4.0f, isDamageableHit: true, in surfaceOff).ShouldPlay);
        }

        [Fact]
        public void CombatHit_ScalesIntensityAndMarksHeavyImpactAtThreshold()
        {
            PunchConfigSnapshot cfg = PunchConfigSnapshot.Default.Sanitize();

            PunchVfxParams minHit = PunchVfxCalculator.Calculate(cfg.MinPunchSpeed, isDamageableHit: true, in cfg);
            PunchVfxParams midHit = PunchVfxCalculator.Calculate(4.1f, isDamageableHit: true, in cfg);
            PunchVfxParams heavyHit = PunchVfxCalculator.Calculate(6.0f, isDamageableHit: true, in cfg);

            Assert.True(minHit.ShouldPlay);
            Assert.Equal(0f, minHit.Intensity, precision: 4);
            Assert.Equal(0.95f, minHit.Scale, precision: 4);
            Assert.Equal(12, minHit.SparkCount);
            Assert.Equal(6, minHit.DustCount);
            Assert.True(minHit.ShowShockwaveRing);
            Assert.False(minHit.IsHeavyImpact);

            Assert.InRange(midHit.Intensity, 0.49f, 0.51f);
            Assert.False(midHit.IsHeavyImpact);
            Assert.True(midHit.SparkCount > minHit.SparkCount);

            Assert.Equal(1f, heavyHit.Intensity, precision: 4);
            Assert.Equal(1.45f, heavyHit.Scale, precision: 4);
            Assert.Equal(24, heavyHit.SparkCount);
            Assert.Equal(12, heavyHit.DustCount);
            Assert.True(heavyHit.ShowShockwaveRing);
            Assert.True(heavyHit.IsHeavyImpact);
        }

        [Fact]
        public void SurfaceHit_IsSubtlerAndSkipsShockwaveBelowThreshold()
        {
            PunchConfigSnapshot cfg = PunchConfigSnapshot.Default.Sanitize();

            PunchVfxParams lightTap = PunchVfxCalculator.Calculate(2.5f, isDamageableHit: false, in cfg);
            PunchVfxParams hardSlam = PunchVfxCalculator.Calculate(5.5f, isDamageableHit: false, in cfg);

            Assert.True(lightTap.ShouldPlay);
            Assert.False(lightTap.ShowShockwaveRing);
            Assert.False(lightTap.IsHeavyImpact);
            Assert.InRange(lightTap.SparkCount, 4, 8);

            Assert.True(hardSlam.ShouldPlay);
            Assert.True(hardSlam.ShowShockwaveRing);
            Assert.False(hardSlam.IsHeavyImpact);
            Assert.True(hardSlam.Scale < 1.0f);
        }

        [Fact]
        public void VfxScaleConfig_AppliesMultiplierAndClampsOutOfRangeValues()
        {
            PunchConfigSnapshot scaled = new PunchConfigSnapshot(punchVfxScale: 1.5f).Sanitize();
            PunchConfigSnapshot tooBig = new PunchConfigSnapshot(punchVfxScale: 99f).Sanitize();
            PunchConfigSnapshot invalidZero = new PunchConfigSnapshot(punchVfxScale: 0f).Sanitize();

            PunchVfxParams p = PunchVfxCalculator.Calculate(2.2f, isDamageableHit: true, in scaled);
            Assert.Equal(0.95f * 1.5f, p.Scale, precision: 4);

            Assert.Equal(3.0f, tooBig.PunchVfxScale, precision: 4);
            Assert.Equal(1.0f, invalidZero.PunchVfxScale, precision: 4);
        }

        [Fact]
        public void SurfaceCooldown_IsTrackedIndependentlyFromCombatCooldown()
        {
            var tracker = new HandCooldownTracker();

            tracker.RecordHit(PunchHandSide.Right, 2.00d);
            Assert.True(tracker.IsOnCooldown(PunchHandSide.Right, 2.10d, 0.40f));
            Assert.False(tracker.IsOnSurfaceCooldown(PunchHandSide.Right, 2.10d, 0.35f));

            tracker.RecordSurfaceHit(PunchHandSide.Right, 2.10d);
            Assert.True(tracker.IsOnSurfaceCooldown(PunchHandSide.Right, 2.25d, 0.35f));
            Assert.False(tracker.IsOnSurfaceCooldown(PunchHandSide.Left, 2.25d, 0.35f));
            Assert.False(tracker.IsOnSurfaceCooldown(PunchHandSide.Right, 2.45d, 0.35f));
        }
    }
}
