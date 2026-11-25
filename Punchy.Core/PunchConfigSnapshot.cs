using System;

namespace Punchy.Core
{
    /// <summary>
    /// Immutable snapshot of all Punchy configuration values and validation invariants.
    /// </summary>
    public readonly struct PunchConfigSnapshot
    {
        public bool Enabled { get; }
        public float BasePunchDamage { get; }
        public float MaxPunchDamage { get; }
        public bool EnableVelocityScaling { get; }
        public float MinPunchSpeed { get; }
        public float PunchCooldownSeconds { get; }
        public bool RequireClosedFist { get; }
        public bool AllowPvPPunching { get; }

        public float MaxVelocityForMaxDamage { get; }
        public float FistGripThreshold { get; }
        public float FistFreeCurlAverageThreshold { get; }
        public float FistFreeCurlMinFingerThreshold { get; }
        public float FistPoseLagGraceSeconds { get; }
        public float ItemReleaseGraceSeconds { get; }
        public float MaxTeleportStepMeters { get; }
        public float MaxValidHandSpeed { get; }
        public float CrippleDamageRatio { get; }
        public bool EnablePunchVfx { get; }
        public float PunchVfxScale { get; }
        public bool EnableSurfaceImpactVfx { get; }
        public bool DebugLogging { get; }

        public bool EnableRagdoll { get; }
        public bool RagdollPlayersOnHaymaker { get; }
        public float PlayerRagdollMinSpeed { get; }
        public float PlayerRagdollBaseImpulse { get; }
        public float PlayerRagdollMaxImpulse { get; }
        public float PlayerRagdollDuration { get; }
        public bool RagdollSmallCreatures { get; }
        public float SmallCreatureRagdollMinSpeed { get; }
        public float SmallCreatureRagdollBaseImpulse { get; }
        public float SmallCreatureRagdollMaxImpulse { get; }
        public float SmallCreatureRagdollDuration { get; }
        public bool RagdollLargeCreatures { get; }
        public float LargeCreatureRagdollMinSpeed { get; }
        public float LargeCreatureRagdollBaseImpulse { get; }
        public float LargeCreatureRagdollMaxImpulse { get; }
        public float LargeCreatureRagdollDuration { get; }
        public float RagdollUpwardBoost { get; }

        public PunchConfigSnapshot(
            bool enabled = true,
            float basePunchDamage = 0.1f,
            float maxPunchDamage = 0.15f,
            bool enableVelocityScaling = true,
            float minPunchSpeed = 2.2f,
            float punchCooldownSeconds = 0.40f,
            bool requireClosedFist = true,
            bool allowPvPPunching = true,
            float maxVelocityForMaxDamage = 6.0f,
            float fistGripThreshold = 0.45f,
            float fistFreeCurlAverageThreshold = 0.65f,
            float fistFreeCurlMinFingerThreshold = 0.50f,
            float fistPoseLagGraceSeconds = 0.12f,
            float itemReleaseGraceSeconds = 0.15f,
            float maxTeleportStepMeters = 1.5f,
            float maxValidHandSpeed = 25.0f,
            float crippleDamageRatio = 0.5f,
            bool enablePunchVfx = true,
            float punchVfxScale = 1.0f,
            bool enableSurfaceImpactVfx = true,
            bool debugLogging = false,
            bool enableRagdoll = true,
            bool ragdollPlayersOnHaymaker = true,
            float playerRagdollMinSpeed = 5.0f,
            float playerRagdollBaseImpulse = 155.0f,
            float playerRagdollMaxImpulse = 225.0f,
            float playerRagdollDuration = 3.0f,
            bool ragdollSmallCreatures = true,
            float smallCreatureRagdollMinSpeed = 2.2f,
            float smallCreatureRagdollBaseImpulse = 135.0f,
            float smallCreatureRagdollMaxImpulse = 195.0f,
            float smallCreatureRagdollDuration = 3.0f,
            bool ragdollLargeCreatures = true,
            float largeCreatureRagdollMinSpeed = 5.5f,
            float largeCreatureRagdollBaseImpulse = 175.0f,
            float largeCreatureRagdollMaxImpulse = 250.0f,
            float largeCreatureRagdollDuration = 2.8f,
            float ragdollUpwardBoost = 0.32f)
        {
            Enabled = enabled;
            BasePunchDamage = basePunchDamage;
            MaxPunchDamage = maxPunchDamage;
            EnableVelocityScaling = enableVelocityScaling;
            MinPunchSpeed = minPunchSpeed;
            PunchCooldownSeconds = punchCooldownSeconds;
            RequireClosedFist = requireClosedFist;
            AllowPvPPunching = allowPvPPunching;
            MaxVelocityForMaxDamage = maxVelocityForMaxDamage;
            FistGripThreshold = fistGripThreshold;
            FistFreeCurlAverageThreshold = fistFreeCurlAverageThreshold;
            FistFreeCurlMinFingerThreshold = fistFreeCurlMinFingerThreshold;
            FistPoseLagGraceSeconds = fistPoseLagGraceSeconds;
            ItemReleaseGraceSeconds = itemReleaseGraceSeconds;
            MaxTeleportStepMeters = maxTeleportStepMeters;
            MaxValidHandSpeed = maxValidHandSpeed;
            CrippleDamageRatio = crippleDamageRatio;
            EnablePunchVfx = enablePunchVfx;
            PunchVfxScale = punchVfxScale;
            EnableSurfaceImpactVfx = enableSurfaceImpactVfx;
            DebugLogging = debugLogging;
            EnableRagdoll = enableRagdoll;
            RagdollPlayersOnHaymaker = ragdollPlayersOnHaymaker;
            PlayerRagdollMinSpeed = playerRagdollMinSpeed;
            PlayerRagdollBaseImpulse = playerRagdollBaseImpulse;
            PlayerRagdollMaxImpulse = playerRagdollMaxImpulse;
            PlayerRagdollDuration = playerRagdollDuration;
            RagdollSmallCreatures = ragdollSmallCreatures;
            SmallCreatureRagdollMinSpeed = smallCreatureRagdollMinSpeed;
            SmallCreatureRagdollBaseImpulse = smallCreatureRagdollBaseImpulse;
            SmallCreatureRagdollMaxImpulse = smallCreatureRagdollMaxImpulse;
            SmallCreatureRagdollDuration = smallCreatureRagdollDuration;
            RagdollLargeCreatures = ragdollLargeCreatures;
            LargeCreatureRagdollMinSpeed = largeCreatureRagdollMinSpeed;
            LargeCreatureRagdollBaseImpulse = largeCreatureRagdollBaseImpulse;
            LargeCreatureRagdollMaxImpulse = largeCreatureRagdollMaxImpulse;
            LargeCreatureRagdollDuration = largeCreatureRagdollDuration;
            RagdollUpwardBoost = ragdollUpwardBoost;
        }

        public static PunchConfigSnapshot Default => new PunchConfigSnapshot(
            enabled: true,
            basePunchDamage: 0.1f,
            maxPunchDamage: 0.15f,
            enableVelocityScaling: true,
            minPunchSpeed: 2.2f,
            punchCooldownSeconds: 0.40f,
            requireClosedFist: true,
            allowPvPPunching: true,
            enablePunchVfx: true,
            punchVfxScale: 1.0f,
            enableSurfaceImpactVfx: true,
            debugLogging: false,
            enableRagdoll: true,
            ragdollPlayersOnHaymaker: true,
            playerRagdollMinSpeed: 5.0f,
            playerRagdollBaseImpulse: 155.0f,
            playerRagdollMaxImpulse: 225.0f,
            playerRagdollDuration: 3.0f,
            ragdollSmallCreatures: true,
            smallCreatureRagdollMinSpeed: 2.2f,
            smallCreatureRagdollBaseImpulse: 135.0f,
            smallCreatureRagdollMaxImpulse: 195.0f,
            smallCreatureRagdollDuration: 3.0f,
            ragdollLargeCreatures: true,
            largeCreatureRagdollMinSpeed: 5.5f,
            largeCreatureRagdollBaseImpulse: 175.0f,
            largeCreatureRagdollMaxImpulse: 250.0f,
            largeCreatureRagdollDuration: 2.8f,
            ragdollUpwardBoost: 0.32f);

        /// <summary>
        /// Clamps invalid or inverted numeric preferences into safe operational ranges.
        /// </summary>
        public PunchConfigSnapshot Sanitize()
        {
            float baseDmg = SanitizeNonNegative(BasePunchDamage, 0.1f);
            float maxDmg = Math.Max(baseDmg, SanitizeNonNegative(MaxPunchDamage, baseDmg));
            float minSpeed = Math.Max(0.1f, SanitizeNonNegative(MinPunchSpeed, 2.2f));
            float maxSpeedForMaxDmg = Math.Max(minSpeed + 0.01f, SanitizeNonNegative(MaxVelocityForMaxDamage, 6.0f));
            float cooldown = SanitizeNonNegative(PunchCooldownSeconds, 0.40f);
            float gripThresh = Clamp01(FistGripThreshold);
            float freeAvg = Clamp01(FistFreeCurlAverageThreshold);
            float freeMin = Clamp01(FistFreeCurlMinFingerThreshold);
            float poseGrace = SanitizeNonNegative(FistPoseLagGraceSeconds, 0.12f);
            float releaseGrace = SanitizeNonNegative(ItemReleaseGraceSeconds, 0.15f);
            float maxTeleport = Math.Max(0.25f, SanitizeNonNegative(MaxTeleportStepMeters, 1.5f));
            float maxValidSpeed = Math.Max(minSpeed + 1.0f, SanitizeNonNegative(MaxValidHandSpeed, 25.0f));
            float crippleRatio = SanitizeNonNegative(CrippleDamageRatio, 0.5f);
            float vfxScale = SanitizeRange(PunchVfxScale, 0.1f, 3.0f, 1.0f);

            float playerMinSpeed = SanitizeRangeInclusive(PlayerRagdollMinSpeed, 0.5f, 20.0f, 5.0f);
            float playerBaseImp = SanitizeRangeInclusive(PlayerRagdollBaseImpulse, 5.0f, 350.0f, 155.0f);
            float playerMaxImp = SanitizeRangeInclusive(PlayerRagdollMaxImpulse, playerBaseImp, 350.0f, Math.Max(playerBaseImp, 225.0f));
            float playerDuration = SanitizeRangeInclusive(PlayerRagdollDuration, 0.5f, 15.0f, 3.0f);

            float smallMinSpeed = SanitizeRangeInclusive(SmallCreatureRagdollMinSpeed, 0.5f, 20.0f, 2.2f);
            float smallBaseImp = SanitizeRangeInclusive(SmallCreatureRagdollBaseImpulse, 5.0f, 350.0f, 135.0f);
            float smallMaxImp = SanitizeRangeInclusive(SmallCreatureRagdollMaxImpulse, smallBaseImp, 350.0f, Math.Max(smallBaseImp, 195.0f));
            float smallDuration = SanitizeRangeInclusive(SmallCreatureRagdollDuration, 0.5f, 15.0f, 3.0f);

            float largeMinSpeed = SanitizeRangeInclusive(LargeCreatureRagdollMinSpeed, 0.5f, 20.0f, 5.5f);
            float largeBaseImp = SanitizeRangeInclusive(LargeCreatureRagdollBaseImpulse, 5.0f, 350.0f, 175.0f);
            float largeMaxImp = SanitizeRangeInclusive(LargeCreatureRagdollMaxImpulse, largeBaseImp, 350.0f, Math.Max(largeBaseImp, 250.0f));
            float largeDuration = SanitizeRangeInclusive(LargeCreatureRagdollDuration, 0.5f, 15.0f, 2.8f);

            float upwardBoost = SanitizeRangeInclusive(RagdollUpwardBoost, 0.0f, 0.50f, 0.32f);

            return new PunchConfigSnapshot(
                Enabled,
                baseDmg,
                maxDmg,
                EnableVelocityScaling,
                minSpeed,
                cooldown,
                RequireClosedFist,
                AllowPvPPunching,
                maxSpeedForMaxDmg,
                gripThresh,
                freeAvg,
                freeMin,
                poseGrace,
                releaseGrace,
                maxTeleport,
                maxValidSpeed,
                crippleRatio,
                EnablePunchVfx,
                vfxScale,
                EnableSurfaceImpactVfx,
                DebugLogging,
                EnableRagdoll,
                RagdollPlayersOnHaymaker,
                playerMinSpeed,
                playerBaseImp,
                playerMaxImp,
                playerDuration,
                RagdollSmallCreatures,
                smallMinSpeed,
                smallBaseImp,
                smallMaxImp,
                smallDuration,
                RagdollLargeCreatures,
                largeMinSpeed,
                largeBaseImp,
                largeMaxImp,
                largeDuration,
                upwardBoost);
        }

        private static float SanitizeRangeInclusive(float value, float min, float max, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return ClampToBounds(fallback, min, max);
            return ClampToBounds(value, min, max);
        }

        private static float ClampToBounds(float value, float min, float max)
        {
            if (value < min)
                return min;
            return value > max ? max : value;
        }

        private static float SanitizeRange(float value, float min, float max, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
                return fallback;
            if (value < min)
                return min;
            return value > max ? max : value;
        }

        private static float SanitizeNonNegative(float value, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return fallback;
            return value < 0f ? 0f : value;
        }

        private static float Clamp01(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return 0f;
            if (value < 0f)
                return 0f;
            return value > 1f ? 1f : value;
        }
    }
}
