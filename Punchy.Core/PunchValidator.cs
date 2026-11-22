namespace Punchy.Core
{
    public readonly struct PunchHandContext
    {
        public PunchHandSide Hand { get; }
        public double CurrentTime { get; }
        public bool AttackerAlive { get; }
        public bool IsHoldingItem { get; }
        public FistPoseState PoseState { get; }
        public HandMotionSample Motion { get; }

        public PunchHandContext(
            PunchHandSide hand,
            double currentTime,
            bool attackerAlive,
            bool isHoldingItem,
            FistPoseState poseState,
            HandMotionSample motion)
        {
            Hand = hand;
            CurrentTime = currentTime;
            AttackerAlive = attackerAlive;
            IsHoldingItem = isHoldingItem;
            PoseState = poseState;
            Motion = motion;
        }
    }

    public readonly struct PunchDecision
    {
        public bool IsValid { get; }
        public PunchRejectReason RejectReason { get; }
        public float HealthDamage { get; }
        public float CrippleDamage { get; }
        public float Speed { get; }
        public bool UsedPoseFallback { get; }

        public PunchDecision(
            bool isValid,
            PunchRejectReason rejectReason,
            float healthDamage,
            float crippleDamage,
            float speed,
            bool usedPoseFallback)
        {
            IsValid = isValid;
            RejectReason = rejectReason;
            HealthDamage = healthDamage;
            CrippleDamage = crippleDamage;
            Speed = speed;
            UsedPoseFallback = usedPoseFallback;
        }

        public static PunchDecision Reject(PunchRejectReason reason, float speed = 0f, bool usedPoseFallback = false) =>
            new PunchDecision(false, reason, 0f, 0f, speed, usedPoseFallback);
    }

    /// <summary>
    /// Decoupled punch validation orchestrator combining empty-hand, fist-pose, velocity, trajectory, cooldown, self-hit, and PvP checks.
    /// </summary>
    public static class PunchValidator
    {
        public static PunchDecision ValidateHandSwing(
            in PunchHandContext ctx,
            HandCooldownTracker cooldownTracker,
            in PunchConfigSnapshot config)
        {
            PunchConfigSnapshot safe = config.Sanitize();

            if (!safe.Enabled)
                return PunchDecision.Reject(PunchRejectReason.ModDisabled);

            if (!ctx.AttackerAlive)
                return PunchDecision.Reject(PunchRejectReason.AttackerNotAlive);

            if (ctx.IsHoldingItem)
                return PunchDecision.Reject(PunchRejectReason.HandHoldingItem);

            if (cooldownTracker != null &&
                cooldownTracker.IsInItemReleaseGrace(ctx.Hand, ctx.CurrentTime, safe.ItemReleaseGraceSeconds))
            {
                return PunchDecision.Reject(PunchRejectReason.ItemReleaseGrace);
            }

            if (ctx.Motion.WasTeleportReset || !ctx.Motion.HasPrevious)
                return PunchDecision.Reject(PunchRejectReason.TeleportReset);

            double dummyFistTime = -1d;
            FistPoseState pose = ctx.PoseState;
            bool isClosedFist = cooldownTracker != null
                ? FistPoseEvaluator.EvaluateWithLagGrace(
                    in pose,
                    ctx.CurrentTime,
                    ref cooldownTracker.GetLastClosedFistTimeRef(ctx.Hand),
                    in safe,
                    out bool usedFallback)
                : FistPoseEvaluator.EvaluateWithLagGrace(
                    in pose,
                    ctx.CurrentTime,
                    ref dummyFistTime,
                    in safe,
                    out usedFallback);

            if (!isClosedFist)
                return PunchDecision.Reject(PunchRejectReason.FistNotClosed, ctx.Motion.Speed, usedFallback);

            if (ctx.Motion.Speed < safe.MinPunchSpeed)
                return PunchDecision.Reject(PunchRejectReason.SpeedTooLow, ctx.Motion.Speed, usedFallback);

            if (usedFallback && ctx.Motion.StepDistance <= 0.001f && ctx.Motion.Direction.SqrMagnitude <= 1e-4f)
                return PunchDecision.Reject(PunchRejectReason.SpeedTooLow, ctx.Motion.Speed, true);

            if (cooldownTracker != null &&
                cooldownTracker.IsOnCooldown(ctx.Hand, ctx.CurrentTime, safe.PunchCooldownSeconds))
            {
                return PunchDecision.Reject(PunchRejectReason.HandOnCooldown, ctx.Motion.Speed, usedFallback);
            }

            float healthDamage = PunchDamageCalculator.CalculateDamage(ctx.Motion.Speed, in safe);
            float crippleDamage = PunchDamageCalculator.CalculateCrippleDamage(healthDamage, in safe);

            return new PunchDecision(
                isValid: true,
                rejectReason: PunchRejectReason.None,
                healthDamage: healthDamage,
                crippleDamage: crippleDamage,
                speed: ctx.Motion.Speed,
                usedPoseFallback: usedFallback);
        }

        public static PunchDecision ValidateHitAndComputeDamage(
            in PunchHandContext ctx,
            in PunchTargetDescriptor target,
            bool serverPvPEnabled,
            HandCooldownTracker cooldownTracker,
            in PunchConfigSnapshot config)
        {
            PunchDecision swing = ValidateHandSwing(in ctx, cooldownTracker, in config);
            if (!swing.IsValid)
                return swing;

            if (!PunchTargetFilter.IsValidTarget(in target, config.AllowPvPPunching, serverPvPEnabled, out PunchRejectReason rejectReason))
                return PunchDecision.Reject(rejectReason, swing.Speed, swing.UsedPoseFallback);

            return swing;
        }
    }
}
