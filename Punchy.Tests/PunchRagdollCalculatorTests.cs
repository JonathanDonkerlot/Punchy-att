using System;
using Punchy.Core;
using Xunit;

namespace Punchy.Tests
{
    public sealed class PunchRagdollCalculatorTests
    {
        [Fact]
        public void NormalPunch_RagdollsSmallCreature_NotPlayerOrLargeCreature()
        {
            PunchConfigSnapshot cfg = PunchConfigSnapshot.Default.Sanitize();
            var dir = new Vec3(0f, 0f, 1f);

            PunchRagdollDecision small = PunchRagdollCalculator.Evaluate(
                PunchRagdollTargetKind.SmallCreature,
                punchSpeed: 2.5f,
                dir,
                in cfg);

            PunchRagdollDecision player = PunchRagdollCalculator.Evaluate(
                PunchRagdollTargetKind.Player,
                punchSpeed: 2.5f,
                dir,
                in cfg);

            PunchRagdollDecision large = PunchRagdollCalculator.Evaluate(
                PunchRagdollTargetKind.LargeCreature,
                punchSpeed: 2.5f,
                dir,
                in cfg);

            Assert.True(small.ShouldRagdoll);
            Assert.Equal(PunchRagdollTargetKind.SmallCreature, small.TargetKind);
            Assert.InRange(small.ImpulseMagnitude, cfg.SmallCreatureRagdollBaseImpulse, cfg.SmallCreatureRagdollMaxImpulse);
            Assert.Equal(cfg.SmallCreatureRagdollDuration, small.DurationSeconds, precision: 4);

            Assert.False(player.ShouldRagdoll);
            Assert.False(large.ShouldRagdoll);
        }

        [Fact]
        public void MediumPunch_DoesNotRagdollPlayer_RequiresHaymaker()
        {
            PunchConfigSnapshot cfg = PunchConfigSnapshot.Default.Sanitize();
            var dir = new Vec3(1f, 0f, 0f);

            PunchRagdollDecision decision = PunchRagdollCalculator.Evaluate(
                PunchRagdollTargetKind.Player,
                punchSpeed: 4.5f,
                dir,
                in cfg);

            Assert.False(decision.ShouldRagdoll);
            Assert.Equal(0f, decision.ImpulseMagnitude);
        }

        [Fact]
        public void Haymaker_RagdollsPlayer_WithRealisticCappedImpulse()
        {
            PunchConfigSnapshot cfg = PunchConfigSnapshot.Default.Sanitize();
            var dir = new Vec3(0f, 0f, 1f);

            PunchRagdollDecision thresholdHaymaker = PunchRagdollCalculator.Evaluate(
                PunchRagdollTargetKind.Player,
                punchSpeed: 5.2f,
                dir,
                in cfg);

            PunchRagdollDecision maxHaymaker = PunchRagdollCalculator.Evaluate(
                PunchRagdollTargetKind.Player,
                punchSpeed: 9.0f,
                dir,
                in cfg);

            PunchRagdollDecision extremeSpeed = PunchRagdollCalculator.Evaluate(
                PunchRagdollTargetKind.Player,
                punchSpeed: 20.0f,
                dir,
                in cfg);

            Assert.True(thresholdHaymaker.ShouldRagdoll);
            Assert.InRange(thresholdHaymaker.ImpulseMagnitude, cfg.PlayerRagdollBaseImpulse, cfg.PlayerRagdollMaxImpulse);
            Assert.Equal(cfg.PlayerRagdollDuration, thresholdHaymaker.DurationSeconds, precision: 4);

            Assert.True(maxHaymaker.ShouldRagdoll);
            Assert.Equal(cfg.PlayerRagdollMaxImpulse, maxHaymaker.ImpulseMagnitude, precision: 4);

            Assert.True(extremeSpeed.ShouldRagdoll);
            Assert.Equal(cfg.PlayerRagdollMaxImpulse, extremeSpeed.ImpulseMagnitude, precision: 4);
            Assert.True(extremeSpeed.ImpulseVector.Magnitude <= cfg.PlayerRagdollMaxImpulse + 0.01f);
        }

        [Fact]
        public void HeavyHaymaker_RagdollsLargeCreature_WithBoundedImpulse()
        {
            PunchConfigSnapshot cfg = PunchConfigSnapshot.Default.Sanitize();
            var dir = new Vec3(0.6f, 0.1f, 0.8f);

            PunchRagdollDecision decision = PunchRagdollCalculator.Evaluate(
                PunchRagdollTargetKind.LargeCreature,
                punchSpeed: 5.8f,
                dir,
                in cfg);

            Assert.True(decision.ShouldRagdoll);
            Assert.Equal(PunchRagdollTargetKind.LargeCreature, decision.TargetKind);
            Assert.InRange(decision.ImpulseMagnitude, cfg.LargeCreatureRagdollBaseImpulse, cfg.LargeCreatureRagdollMaxImpulse);
            Assert.Equal(cfg.LargeCreatureRagdollDuration, decision.DurationSeconds, precision: 4);
        }

        [Fact]
        public void SteepUppercutDirection_ClampsVerticalComponent_NeverLaunchesSkyward()
        {
            PunchConfigSnapshot cfg = PunchConfigSnapshot.Default.Sanitize();
            var pureUp = new Vec3(0f, 1f, 0f);

            PunchRagdollDecision smallDecision = PunchRagdollCalculator.Evaluate(
                PunchRagdollTargetKind.SmallCreature,
                punchSpeed: 5.0f,
                pureUp,
                in cfg);

            Assert.True(smallDecision.ShouldRagdoll);
            float verticalRatio = smallDecision.ImpulseVector.Y / smallDecision.ImpulseVector.Magnitude;
            Assert.InRange(verticalRatio, 0.10f, 0.50f);
        }

        [Fact]
        public void DisabledMasterOrCategoryToggles_NeverRagdoll()
        {
            var dir = new Vec3(0f, 0f, 1f);

            PunchConfigSnapshot masterOff = new PunchConfigSnapshot(enableRagdoll: false).Sanitize();
            Assert.False(PunchRagdollCalculator.Evaluate(PunchRagdollTargetKind.SmallCreature, 8.0f, dir, in masterOff).ShouldRagdoll);
            Assert.False(PunchRagdollCalculator.Evaluate(PunchRagdollTargetKind.Player, 8.0f, dir, in masterOff).ShouldRagdoll);

            PunchConfigSnapshot playersOff = new PunchConfigSnapshot(ragdollPlayersOnHaymaker: false).Sanitize();
            Assert.False(PunchRagdollCalculator.Evaluate(PunchRagdollTargetKind.Player, 8.0f, dir, in playersOff).ShouldRagdoll);
            Assert.True(PunchRagdollCalculator.Evaluate(PunchRagdollTargetKind.SmallCreature, 3.0f, dir, in playersOff).ShouldRagdoll);

            PunchConfigSnapshot smallOff = new PunchConfigSnapshot(ragdollSmallCreatures: false).Sanitize();
            Assert.False(PunchRagdollCalculator.Evaluate(PunchRagdollTargetKind.SmallCreature, 8.0f, dir, in smallOff).ShouldRagdoll);

            PunchConfigSnapshot largeOff = new PunchConfigSnapshot(ragdollLargeCreatures: false).Sanitize();
            Assert.False(PunchRagdollCalculator.Evaluate(PunchRagdollTargetKind.LargeCreature, 8.0f, dir, in largeOff).ShouldRagdoll);
        }

        [Fact]
        public void ConfigSanitize_EnforcesAntiMoonImpulseBoundsAndNaNSafety()
        {
            PunchConfigSnapshot extreme = new PunchConfigSnapshot(
                playerRagdollMinSpeed: -2f,
                playerRagdollBaseImpulse: 5000f,
                playerRagdollMaxImpulse: 9999f,
                playerRagdollDuration: float.NaN,
                smallCreatureRagdollBaseImpulse: 1f,
                smallCreatureRagdollMaxImpulse: 9000f,
                ragdollUpwardBoost: 5.0f).Sanitize();

            Assert.Equal(0.5f, extreme.PlayerRagdollMinSpeed, precision: 4);
            Assert.Equal(250.0f, extreme.PlayerRagdollBaseImpulse, precision: 4);
            Assert.Equal(250.0f, extreme.PlayerRagdollMaxImpulse, precision: 4);
            Assert.Equal(2.5f, extreme.PlayerRagdollDuration, precision: 4);
            Assert.Equal(5.0f, extreme.SmallCreatureRagdollBaseImpulse, precision: 4);
            Assert.Equal(250.0f, extreme.SmallCreatureRagdollMaxImpulse, precision: 4);
            Assert.Equal(0.35f, extreme.RagdollUpwardBoost, precision: 4);

            PunchConfigSnapshot zeroUp = new PunchConfigSnapshot(ragdollUpwardBoost: 0.0f).Sanitize();
            Assert.Equal(0.0f, zeroUp.RagdollUpwardBoost, precision: 4);
        }

        [Fact]
        public void ClassifyCreatureByName_MapsSmallLargeAndStaticTargetsAccurately()
        {
            Assert.Equal(PunchRagdollTargetKind.SmallCreature, PunchRagdollCalculator.ClassifyCreatureByName("Spriggull_Blue(Clone)"));
            Assert.Equal(PunchRagdollTargetKind.SmallCreature, PunchRagdollCalculator.ClassifyCreatureByName("Babu(Clone)"));
            Assert.Equal(PunchRagdollTargetKind.SmallCreature, PunchRagdollCalculator.ClassifyCreatureByName("Schmeechee"));
            Assert.Equal(PunchRagdollTargetKind.SmallCreature, PunchRagdollCalculator.ClassifyCreatureByName("Wyrm(Clone)"));

            Assert.Equal(PunchRagdollTargetKind.LargeCreature, PunchRagdollCalculator.ClassifyCreatureByName("Gotera(Clone)"));
            Assert.Equal(PunchRagdollTargetKind.LargeCreature, PunchRagdollCalculator.ClassifyCreatureByName("Turabada"));
            Assert.Equal(PunchRagdollTargetKind.LargeCreature, PunchRagdollCalculator.ClassifyCreatureByName("Phantom(Clone)"));

            Assert.Equal(PunchRagdollTargetKind.None, PunchRagdollCalculator.ClassifyCreatureByName("combat_dummy(Clone)"));
            Assert.Equal(PunchRagdollTargetKind.None, PunchRagdollCalculator.ClassifyCreatureByName(""));
            Assert.Equal(PunchRagdollTargetKind.None, PunchRagdollCalculator.ClassifyCreatureByName(null));
        }
    }
}
