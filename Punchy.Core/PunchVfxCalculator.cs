using System;

namespace Punchy.Core
{
    public readonly struct PunchVfxParams
    {
        public bool ShouldPlay { get; }
        public float Intensity { get; }
        public float Scale { get; }
        public int SparkCount { get; }
        public int DustCount { get; }
        public bool ShowShockwaveRing { get; }
        public bool IsHeavyImpact { get; }

        public PunchVfxParams(
            bool shouldPlay,
            float intensity,
            float scale,
            int sparkCount,
            int dustCount,
            bool showShockwaveRing,
            bool isHeavyImpact)
        {
            ShouldPlay = shouldPlay;
            Intensity = intensity;
            Scale = scale;
            SparkCount = sparkCount;
            DustCount = dustCount;
            ShowShockwaveRing = showShockwaveRing;
            IsHeavyImpact = isHeavyImpact;
        }

        public static PunchVfxParams None => new PunchVfxParams(false, 0f, 0f, 0, 0, false, false);
    }

    /// <summary>
    /// Computes velocity-scaled punch VFX parameters for both damageable hits and world surface hits.
    /// </summary>
    public static class PunchVfxCalculator
    {
        public static PunchVfxParams Calculate(
            float handSpeed,
            bool isDamageableHit,
            in PunchConfigSnapshot config)
        {
            PunchConfigSnapshot safe = config.Sanitize();

            if (!safe.Enabled || !safe.EnablePunchVfx || (!isDamageableHit && !safe.EnableSurfaceImpactVfx))
                return PunchVfxParams.None;

            float span = Math.Max(0.01f, safe.MaxVelocityForMaxDamage - safe.MinPunchSpeed);
            float intensity = (handSpeed - safe.MinPunchSpeed) / span;
            if (float.IsNaN(intensity) || float.IsInfinity(intensity))
                intensity = 0f;
            if (intensity < 0f)
                intensity = 0f;
            else if (intensity > 1f)
                intensity = 1f;

            bool isHeavy = isDamageableHit && intensity >= 0.70f;

            float scale;
            int sparkCount;
            int dustCount;
            bool showShockwaveRing;

            if (isDamageableHit)
            {
                scale = (0.95f + (0.50f * intensity)) * safe.PunchVfxScale;
                sparkCount = (int)Math.Round(12f + (12f * intensity));
                dustCount = (int)Math.Round(6f + (6f * intensity));
                showShockwaveRing = true;
            }
            else
            {
                scale = (0.60f + (0.25f * intensity)) * safe.PunchVfxScale;
                sparkCount = (int)Math.Round(4f + (4f * intensity));
                dustCount = (int)Math.Round(5f + (4f * intensity));
                showShockwaveRing = intensity >= 0.55f;
            }

            return new PunchVfxParams(
                shouldPlay: true,
                intensity: intensity,
                scale: scale,
                sparkCount: sparkCount,
                dustCount: dustCount,
                showShockwaveRing: showShockwaveRing,
                isHeavyImpact: isHeavy);
        }
    }
}
