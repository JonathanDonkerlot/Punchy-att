using Punchy.Core;
using Xunit;

namespace Punchy.Tests
{
    public sealed class PunchDamageCalculatorTests
    {
        [Fact]
        public void DefaultBaseDamage_RequiresExactly20PunchesFor2HpPlayer()
        {
            PunchConfigSnapshot cfg = PunchConfigSnapshot.Default.Sanitize();
            float dmg = PunchDamageCalculator.CalculateDamage(cfg.MinPunchSpeed, in cfg);

            Assert.Equal(0.1f, dmg, precision: 4);
            Assert.Equal(2.0f, dmg * 20f, precision: 4);
        }

        [Fact]
        public void VelocityScaling_InterpolatesAndClampsBetweenBaseAndMax()
        {
            PunchConfigSnapshot cfg = PunchConfigSnapshot.Default.Sanitize();

            float atMin = PunchDamageCalculator.CalculateDamage(2.2f, in cfg);
            float midSpeed = (2.2f + 6.0f) * 0.5f;
            float atMid = PunchDamageCalculator.CalculateDamage(midSpeed, in cfg);
            float atMax = PunchDamageCalculator.CalculateDamage(6.0f, in cfg);
            float aboveMax = PunchDamageCalculator.CalculateDamage(15.0f, in cfg);

            Assert.Equal(0.10f, atMin, precision: 4);
            Assert.Equal(0.125f, atMid, precision: 4);
            Assert.Equal(0.15f, atMax, precision: 4);
            Assert.Equal(0.15f, aboveMax, precision: 4);
        }

        [Fact]
        public void VelocityScalingDisabled_AlwaysReturnsBaseDamage()
        {
            PunchConfigSnapshot cfg = new PunchConfigSnapshot(
                basePunchDamage: 0.1f,
                maxPunchDamage: 0.15f,
                enableVelocityScaling: false).Sanitize();

            Assert.Equal(0.1f, PunchDamageCalculator.CalculateDamage(2.2f, in cfg), precision: 4);
            Assert.Equal(0.1f, PunchDamageCalculator.CalculateDamage(10.0f, in cfg), precision: 4);
        }

        [Fact]
        public void InvertedConfigDamage_IsSanitizedSafely()
        {
            PunchConfigSnapshot cfg = new PunchConfigSnapshot(
                basePunchDamage: 0.25f,
                maxPunchDamage: 0.10f,
                enableVelocityScaling: true).Sanitize();

            Assert.Equal(0.25f, cfg.MaxPunchDamage, precision: 4);
            Assert.Equal(0.25f, PunchDamageCalculator.CalculateDamage(8.0f, in cfg), precision: 4);
        }

        [Theory]
        [InlineData(false, false, false, true)]
        [InlineData(false, true, false, true)]
        [InlineData(true, true, true, true)]
        [InlineData(true, false, true, false)]
        [InlineData(true, true, false, false)]
        [InlineData(true, false, false, false)]
        public void PvPAllowEvaluation_RespectsBothModAndServerRules(
            bool targetIsPlayer,
            bool modAllowPvP,
            bool serverPvPEnabled,
            bool expectedAllowed)
        {
            Assert.Equal(expectedAllowed, PunchDamageCalculator.EvaluatePvPAllowed(targetIsPlayer, modAllowPvP, serverPvPEnabled));
        }
    }
}
