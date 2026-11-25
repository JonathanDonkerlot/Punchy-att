using System;

namespace Punchy.Core
{
    public enum PunchRagdollTargetKind
    {
        None = 0,
        SmallCreature = 1,
        LargeCreature = 2,
        Player = 3
    }

    public readonly struct PunchRagdollDecision
    {
        public bool ShouldRagdoll { get; }
        public PunchRagdollTargetKind TargetKind { get; }
        public float ImpulseMagnitude { get; }
        public float UpwardBoost { get; }
        public float DurationSeconds { get; }
        public Vec3 ImpulseVector { get; }

        public PunchRagdollDecision(
            bool shouldRagdoll,
            PunchRagdollTargetKind targetKind,
            float impulseMagnitude,
            float upwardBoost,
            float durationSeconds,
            Vec3 impulseVector)
        {
            ShouldRagdoll = shouldRagdoll;
            TargetKind = targetKind;
            ImpulseMagnitude = impulseMagnitude;
            UpwardBoost = upwardBoost;
            DurationSeconds = durationSeconds;
            ImpulseVector = impulseVector;
        }

        public static PunchRagdollDecision None =>
            new PunchRagdollDecision(false, PunchRagdollTargetKind.None, 0f, 0f, 0f, Vec3.Zero);
    }

    /// <summary>
    /// Computes realistic, bounded ragdoll triggers and anti-moon impulse vectors by target category.
    /// </summary>
    public static class PunchRagdollCalculator
    {
        private const float PlayerScalingSpanSpeed = 3.0f;
        private const float SmallCreatureScalingSpanSpeed = 3.5f;
        private const float LargeCreatureScalingSpanSpeed = 3.0f;
        private const float MaxDirectionVerticalComponent = 0.50f;

        private static readonly string[] SmallCreatureNames =
        {
            "Spriggull",
            "Babu",
            "Schmeechee",
            "FireBug",
            "Dragonfly",
            "Wyrm"
        };

        private static readonly string[] LargeCreatureNames =
        {
            "Gotera",
            "Turabada",
            "Phantom"
        };

        /// <summary>
        /// Evaluates whether a validated punch should trigger a ragdoll and computes a capped physical impulse.
        /// </summary>
        public static PunchRagdollDecision Evaluate(
            PunchRagdollTargetKind targetKind,
            float punchSpeed,
            Vec3 punchDirection,
            in PunchConfigSnapshot config)
        {
            if (!config.EnableRagdoll || targetKind == PunchRagdollTargetKind.None)
                return PunchRagdollDecision.None;

            if (float.IsNaN(punchSpeed) || float.IsInfinity(punchSpeed) || punchSpeed <= 0f)
                return PunchRagdollDecision.None;

            if (!TryGetCategorySettings(
                    targetKind,
                    in config,
                    out float minSpeed,
                    out float scalingSpan,
                    out float baseImpulse,
                    out float maxImpulse,
                    out float duration))
            {
                return PunchRagdollDecision.None;
            }

            if (punchSpeed < minSpeed)
                return PunchRagdollDecision.None;

            float t = Clamp01((punchSpeed - minSpeed) / scalingSpan);
            float effectiveMax = Math.Max(baseImpulse, maxImpulse);
            float magnitude = baseImpulse + ((effectiveMax - baseImpulse) * t);

            float upwardBoost = Clamp(config.RagdollUpwardBoost, 0f, 0.50f);
            Vec3 impulseDir = ComputeGroundedDirection(punchDirection, upwardBoost);
            Vec3 impulseVec = impulseDir * magnitude;

            return new PunchRagdollDecision(
                shouldRagdoll: true,
                targetKind: targetKind,
                impulseMagnitude: magnitude,
                upwardBoost: upwardBoost,
                durationSeconds: duration,
                impulseVector: impulseVec);
        }

        /// <summary>
        /// Classifies an ATT entity/prefab name into small vs. large ragdoll creature categories.
        /// </summary>
        public static PunchRagdollTargetKind ClassifyCreatureByName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return PunchRagdollTargetKind.None;

            for (int i = 0; i < SmallCreatureNames.Length; i++)
            {
                if (name.IndexOf(SmallCreatureNames[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    return PunchRagdollTargetKind.SmallCreature;
            }

            for (int i = 0; i < LargeCreatureNames.Length; i++)
            {
                if (name.IndexOf(LargeCreatureNames[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    return PunchRagdollTargetKind.LargeCreature;
            }

            return PunchRagdollTargetKind.None;
        }

        private static bool TryGetCategorySettings(
            PunchRagdollTargetKind targetKind,
            in PunchConfigSnapshot config,
            out float minSpeed,
            out float scalingSpan,
            out float baseImpulse,
            out float maxImpulse,
            out float duration)
        {
            switch (targetKind)
            {
                case PunchRagdollTargetKind.Player:
                    minSpeed = config.PlayerRagdollMinSpeed;
                    scalingSpan = PlayerScalingSpanSpeed;
                    baseImpulse = config.PlayerRagdollBaseImpulse;
                    maxImpulse = config.PlayerRagdollMaxImpulse;
                    duration = config.PlayerRagdollDuration;
                    return config.RagdollPlayersOnHaymaker;

                case PunchRagdollTargetKind.SmallCreature:
                    minSpeed = config.SmallCreatureRagdollMinSpeed;
                    scalingSpan = SmallCreatureScalingSpanSpeed;
                    baseImpulse = config.SmallCreatureRagdollBaseImpulse;
                    maxImpulse = config.SmallCreatureRagdollMaxImpulse;
                    duration = config.SmallCreatureRagdollDuration;
                    return config.RagdollSmallCreatures;

                case PunchRagdollTargetKind.LargeCreature:
                    minSpeed = config.LargeCreatureRagdollMinSpeed;
                    scalingSpan = LargeCreatureScalingSpanSpeed;
                    baseImpulse = config.LargeCreatureRagdollBaseImpulse;
                    maxImpulse = config.LargeCreatureRagdollMaxImpulse;
                    duration = config.LargeCreatureRagdollDuration;
                    return config.RagdollLargeCreatures;

                default:
                    minSpeed = 0f;
                    scalingSpan = 1f;
                    baseImpulse = 0f;
                    maxImpulse = 0f;
                    duration = 0f;
                    return false;
            }
        }

        private static Vec3 ComputeGroundedDirection(Vec3 punchDirection, float upwardBoost)
        {
            bool finite = !float.IsNaN(punchDirection.X) && !float.IsInfinity(punchDirection.X) &&
                          !float.IsNaN(punchDirection.Y) && !float.IsInfinity(punchDirection.Y) &&
                          !float.IsNaN(punchDirection.Z) && !float.IsInfinity(punchDirection.Z);

            Vec3 baseDir = finite && punchDirection.SqrMagnitude > 1e-6f
                ? punchDirection.Normalized
                : Vec3.Forward;

            // Project onto the horizontal plane first so steep uppercuts cannot launch targets vertically.
            var horizRaw = new Vec3(baseDir.X, 0f, baseDir.Z);
            Vec3 horiz = horizRaw.SqrMagnitude > 1e-6f ? horizRaw.Normalized : Vec3.Forward;

            float vert = Clamp((baseDir.Y * 0.25f) + upwardBoost, 0f, MaxDirectionVerticalComponent);
            Vec3 lifted = new Vec3(horiz.X, vert, horiz.Z);
            return lifted.SqrMagnitude > 1e-6f ? lifted.Normalized : Vec3.Forward;
        }

        private static float Clamp01(float value) => Clamp(value, 0f, 1f);

        private static float Clamp(float value, float min, float max)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return min;
            if (value < min)
                return min;
            return value > max ? max : value;
        }
    }
}
