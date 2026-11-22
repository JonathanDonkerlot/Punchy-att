using System;

namespace Punchy.Core
{
    /// <summary>
    /// Computes authoritative bare-handed health/cripple damage and PvP eligibility.
    /// </summary>
    public static class PunchDamageCalculator
    {
        public static float CalculateDamage(float handSpeed, in PunchConfigSnapshot config)
        {
            PunchConfigSnapshot safe = config.Sanitize();
            float baseDmg = safe.BasePunchDamage;
            float maxDmg = safe.MaxPunchDamage;

            if (!safe.EnableVelocityScaling || maxDmg <= baseDmg)
                return baseDmg;

            float span = Math.Max(0.01f, safe.MaxVelocityForMaxDamage - safe.MinPunchSpeed);
            float t = (handSpeed - safe.MinPunchSpeed) / span;
            if (float.IsNaN(t) || float.IsInfinity(t))
                t = 0f;
            if (t < 0f)
                t = 0f;
            else if (t > 1f)
                t = 1f;

            return baseDmg + ((maxDmg - baseDmg) * t);
        }

        public static float CalculateCrippleDamage(float healthDamage, in PunchConfigSnapshot config)
        {
            PunchConfigSnapshot safe = config.Sanitize();
            return Math.Max(0f, healthDamage) * safe.CrippleDamageRatio;
        }

        public static bool EvaluatePvPAllowed(bool targetIsPlayer, bool modAllowPvP, bool serverPvPEnabled)
        {
            if (!targetIsPlayer)
                return true;

            return modAllowPvP && serverPvPEnabled;
        }
    }
}
