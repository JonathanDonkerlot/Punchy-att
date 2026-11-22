using Punchy.Core;
using Xunit;

namespace Punchy.Tests
{
    public sealed class PunchValidatorTests
    {
        private static HandMotionSample FastSwing(float speed = 3.5f)
        {
            return new HandMotionSample(
                hasPrevious: true,
                previousPosition: new Vec3(0f, 1.4f, 0f),
                currentPosition: new Vec3(0f, 1.4f, 0.08f),
                stepDelta: new Vec3(0f, 0f, 0.08f),
                stepDistance: 0.08f,
                velocity: new Vec3(0f, 0f, speed),
                speed: speed,
                direction: new Vec3(0f, 0f, 1f),
                wasTeleportReset: false);
        }

        private static PunchTargetDescriptor EnemyCreature(bool alive = true) => new PunchTargetDescriptor(
            attackerPlayerId: 100,
            attackerEntityRootId: 1000,
            attackerHealthId: 101,
            targetHealthId: 201,
            targetEntityRootId: 2000,
            targetPlayerId: 0,
            isTargetPlayer: false,
            isTargetInAttackerHierarchy: false,
            isTargetDockedOnAttacker: false,
            isTargetHeldByAttacker: false,
            isTargetAliveOrBreakable: alive);

        private static PunchTargetDescriptor EnemyPlayer(bool alive = true) => new PunchTargetDescriptor(
            attackerPlayerId: 100,
            attackerEntityRootId: 1000,
            attackerHealthId: 101,
            targetHealthId: 301,
            targetEntityRootId: 3000,
            targetPlayerId: 300,
            isTargetPlayer: true,
            isTargetInAttackerHierarchy: false,
            isTargetDockedOnAttacker: false,
            isTargetHeldByAttacker: false,
            isTargetAliveOrBreakable: alive);

        [Fact]
        public void ValidBareHandedClosedFistPunch_SucceedsAndScalesDamage()
        {
            var tracker = new HandCooldownTracker();
            PunchConfigSnapshot cfg = PunchConfigSnapshot.Default.Sanitize();
            var ctx = new PunchHandContext(
                PunchHandSide.Right,
                currentTime: 2.0d,
                attackerAlive: true,
                isHoldingItem: false,
                FistPoseState.GradualGrip(0.95f),
                FastSwing(4.1f));

            PunchDecision decision = PunchValidator.ValidateHitAndComputeDamage(
                in ctx,
                EnemyCreature(),
                serverPvPEnabled: true,
                tracker,
                in cfg);

            Assert.True(decision.IsValid);
            Assert.Equal(PunchRejectReason.None, decision.RejectReason);
            Assert.Equal(0.125f, decision.HealthDamage, precision: 4);
            Assert.Equal(0.0625f, decision.CrippleDamage, precision: 4);
            Assert.False(decision.UsedPoseFallback);
        }

        [Fact]
        public void HoldingWeaponOrTorch_RejectsBareHandPunch()
        {
            var tracker = new HandCooldownTracker();
            PunchConfigSnapshot cfg = PunchConfigSnapshot.Default.Sanitize();
            var ctx = new PunchHandContext(
                PunchHandSide.Right,
                currentTime: 2.0d,
                attackerAlive: true,
                isHoldingItem: true,
                FistPoseState.GradualGrip(1.0f),
                FastSwing(5.5f));

            PunchDecision decision = PunchValidator.ValidateHitAndComputeDamage(
                in ctx,
                EnemyCreature(),
                serverPvPEnabled: true,
                tracker,
                in cfg);

            Assert.False(decision.IsValid);
            Assert.Equal(PunchRejectReason.HandHoldingItem, decision.RejectReason);
        }

        [Fact]
        public void SelfHitScenarios_AreAllRejected()
        {
            var tracker = new HandCooldownTracker();
            PunchConfigSnapshot cfg = PunchConfigSnapshot.Default.Sanitize();
            var ctx = new PunchHandContext(
                PunchHandSide.Left,
                currentTime: 2.0d,
                attackerAlive: true,
                isHoldingItem: false,
                FistPoseState.GradualGrip(1.0f),
                FastSwing(4.0f));

            PunchTargetDescriptor[] selfCases =
            {
                // Same hierarchy collider
                new PunchTargetDescriptor(100, 1000, 101, 201, 2000, 0, false, isTargetInAttackerHierarchy: true, false, false, true),
                // Belt-docked item on attacker
                new PunchTargetDescriptor(100, 1000, 101, 202, 2001, 0, false, false, isTargetDockedOnAttacker: true, false, true),
                // Item held in other hand of attacker
                new PunchTargetDescriptor(100, 1000, 101, 203, 2002, 0, false, false, false, isTargetHeldByAttacker: true, true),
                // Same HealthObject ID
                new PunchTargetDescriptor(100, 1000, 101, 101, 2003, 200, true, false, false, false, true),
                // Same Entity PrefabRoot ID
                new PunchTargetDescriptor(100, 1000, 101, 204, 1000, 0, false, false, false, false, true),
                // Same Player ID
                new PunchTargetDescriptor(100, 1000, 101, 205, 2005, 100, true, false, false, false, true)
            };

            foreach (PunchTargetDescriptor target in selfCases)
            {
                PunchDecision decision = PunchValidator.ValidateHitAndComputeDamage(
                    in ctx, in target, serverPvPEnabled: true, tracker, in cfg);

                Assert.False(decision.IsValid);
                Assert.Equal(PunchRejectReason.SelfHit, decision.RejectReason);
            }
        }

        [Fact]
        public void PvPDisabledInModOrServer_RejectsPlayerButAllowsCreature()
        {
            var tracker = new HandCooldownTracker();
            PunchConfigSnapshot allowModPvP = PunchConfigSnapshot.Default.Sanitize();
            PunchConfigSnapshot blockModPvP = new PunchConfigSnapshot(allowPvPPunching: false).Sanitize();

            var ctx = new PunchHandContext(
                PunchHandSide.Right,
                currentTime: 2.0d,
                attackerAlive: true,
                isHoldingItem: false,
                FistPoseState.GradualGrip(1.0f),
                FastSwing(3.0f));

            PunchDecision blockedByMod = PunchValidator.ValidateHitAndComputeDamage(
                in ctx, EnemyPlayer(), serverPvPEnabled: true, tracker, in blockModPvP);
            Assert.False(blockedByMod.IsValid);
            Assert.Equal(PunchRejectReason.PvPDisabled, blockedByMod.RejectReason);

            PunchDecision blockedByServer = PunchValidator.ValidateHitAndComputeDamage(
                in ctx, EnemyPlayer(), serverPvPEnabled: false, tracker, in allowModPvP);
            Assert.False(blockedByServer.IsValid);
            Assert.Equal(PunchRejectReason.PvPDisabled, blockedByServer.RejectReason);

            PunchDecision creatureAllowed = PunchValidator.ValidateHitAndComputeDamage(
                in ctx, EnemyCreature(), serverPvPEnabled: false, tracker, in blockModPvP);
            Assert.True(creatureAllowed.IsValid);
        }

        [Fact]
        public void CooldownPreventsWiggleExploitAndAllowsOppositeHand()
        {
            var tracker = new HandCooldownTracker();
            PunchConfigSnapshot cfg = PunchConfigSnapshot.Default.Sanitize();

            var rightCtx1 = new PunchHandContext(
                PunchHandSide.Right, 10.00d, true, false, FistPoseState.GradualGrip(1.0f), FastSwing(3.0f));
            PunchDecision firstRight = PunchValidator.ValidateHitAndComputeDamage(
                in rightCtx1, EnemyCreature(), true, tracker, in cfg);
            Assert.True(firstRight.IsValid);
            tracker.RecordHit(PunchHandSide.Right, 10.00d);

            // 150ms later, right hand wiggle must fail
            var rightCtx2 = new PunchHandContext(
                PunchHandSide.Right, 10.15d, true, false, FistPoseState.GradualGrip(1.0f), FastSwing(4.0f));
            PunchDecision secondRight = PunchValidator.ValidateHitAndComputeDamage(
                in rightCtx2, EnemyCreature(), true, tracker, in cfg);
            Assert.False(secondRight.IsValid);
            Assert.Equal(PunchRejectReason.HandOnCooldown, secondRight.RejectReason);

            // Meanwhile left hand can jab immediately
            var leftCtx = new PunchHandContext(
                PunchHandSide.Left, 10.15d, true, false, FistPoseState.GradualGrip(1.0f), FastSwing(3.2f));
            PunchDecision firstLeft = PunchValidator.ValidateHitAndComputeDamage(
                in leftCtx, EnemyCreature(), true, tracker, in cfg);
            Assert.True(firstLeft.IsValid);
        }

        [Fact]
        public void ModDisabledOrDeadAttackerOrDeadTarget_RejectedWithExpectedReasons()
        {
            var tracker = new HandCooldownTracker();
            PunchConfigSnapshot disabledCfg = new PunchConfigSnapshot(enabled: false).Sanitize();
            PunchConfigSnapshot enabledCfg = PunchConfigSnapshot.Default.Sanitize();

            var aliveCtx = new PunchHandContext(
                PunchHandSide.Right, 3.0d, true, false, FistPoseState.GradualGrip(1.0f), FastSwing());
            var deadCtx = new PunchHandContext(
                PunchHandSide.Right, 3.0d, false, false, FistPoseState.GradualGrip(1.0f), FastSwing());

            Assert.Equal(
                PunchRejectReason.ModDisabled,
                PunchValidator.ValidateHitAndComputeDamage(in aliveCtx, EnemyCreature(), true, tracker, in disabledCfg).RejectReason);

            Assert.Equal(
                PunchRejectReason.AttackerNotAlive,
                PunchValidator.ValidateHitAndComputeDamage(in deadCtx, EnemyCreature(), true, tracker, in enabledCfg).RejectReason);

            Assert.Equal(
                PunchRejectReason.TargetDeadOrInvalid,
                PunchValidator.ValidateHitAndComputeDamage(in aliveCtx, EnemyCreature(alive: false), true, tracker, in enabledCfg).RejectReason);
        }

        [Fact]
        public void UnavailablePoseState_FallsBackToVelocityAndTrajectoryValidation()
        {
            var tracker = new HandCooldownTracker();
            PunchConfigSnapshot cfg = PunchConfigSnapshot.Default.Sanitize();

            var fastUnavailable = new PunchHandContext(
                PunchHandSide.Left, 4.0d, true, false, FistPoseState.Unavailable, FastSwing(3.0f));
            PunchDecision accepted = PunchValidator.ValidateHitAndComputeDamage(
                in fastUnavailable, EnemyCreature(), true, tracker, in cfg);
            Assert.True(accepted.IsValid);
            Assert.True(accepted.UsedPoseFallback);

            var slowUnavailable = new PunchHandContext(
                PunchHandSide.Left, 4.0d, true, false, FistPoseState.Unavailable, FastSwing(1.2f));
            PunchDecision rejected = PunchValidator.ValidateHitAndComputeDamage(
                in slowUnavailable, EnemyCreature(), true, tracker, in cfg);
            Assert.False(rejected.IsValid);
            Assert.Equal(PunchRejectReason.SpeedTooLow, rejected.RejectReason);
        }
    }
}
