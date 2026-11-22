using MelonLoader;
using Punchy.Core;

namespace Punchy
{
    /// <summary>
    /// MelonPreferences configuration wrapper under [Punchy] in UserData/MelonPreferences.cfg.
    /// </summary>
    public sealed class PunchyConfig
    {
        private MelonPreferences_Category _category;

        public MelonPreferences_Entry<bool> Enabled { get; private set; }
        public MelonPreferences_Entry<float> BasePunchDamage { get; private set; }
        public MelonPreferences_Entry<float> MaxPunchDamage { get; private set; }
        public MelonPreferences_Entry<bool> EnableVelocityScaling { get; private set; }
        public MelonPreferences_Entry<float> MinPunchSpeed { get; private set; }
        public MelonPreferences_Entry<float> PunchCooldownSeconds { get; private set; }
        public MelonPreferences_Entry<bool> RequireClosedFist { get; private set; }
        public MelonPreferences_Entry<bool> AllowPvPPunching { get; private set; }
        public MelonPreferences_Entry<bool> EnablePunchVfx { get; private set; }
        public MelonPreferences_Entry<float> PunchVfxScale { get; private set; }
        public MelonPreferences_Entry<bool> EnableSurfaceImpactVfx { get; private set; }

        public PunchConfigSnapshot Snapshot { get; private set; } = PunchConfigSnapshot.Default;

        public void Initialize()
        {
            _category = MelonPreferences.CreateCategory("Punchy", "Punchy");

            Enabled = _category.CreateEntry(
                "Enabled",
                true,
                "Enabled",
                "Enables server-side bare-handed punching.",
                false,
                false,
                null);

            BasePunchDamage = _category.CreateEntry(
                "BasePunchDamage",
                0.1f,
                "Base Punch Damage",
                "Base health damage applied at MinPunchSpeed (0.1 = 20 punches per 2.0 HP player).",
                false,
                false,
                null);

            MaxPunchDamage = _category.CreateEntry(
                "MaxPunchDamage",
                0.15f,
                "Max Punch Damage",
                "Maximum scaled damage at high punch velocity when EnableVelocityScaling is true.",
                false,
                false,
                null);

            EnableVelocityScaling = _category.CreateEntry(
                "EnableVelocityScaling",
                true,
                "Enable Velocity Scaling",
                "Scales punch damage between BasePunchDamage and MaxPunchDamage based on hand speed.",
                false,
                false,
                null);

            MinPunchSpeed = _category.CreateEntry(
                "MinPunchSpeed",
                2.2f,
                "Min Punch Speed (m/s)",
                "Minimum hand world-space speed in m/s required to register a punch.",
                false,
                false,
                null);

            PunchCooldownSeconds = _category.CreateEntry(
                "PunchCooldownSeconds",
                0.40f,
                "Punch Cooldown (seconds)",
                "Minimum seconds between hits per individual hand.",
                false,
                false,
                null);

            RequireClosedFist = _category.CreateEntry(
                "RequireClosedFist",
                true,
                "Require Closed Fist",
                "Requires closed fist from networked grip/gesture state (falls back gracefully if unavailable).",
                false,
                false,
                null);

            AllowPvPPunching = _category.CreateEntry(
                "AllowPvPPunching",
                true,
                "Allow PvP Punching",
                "Allows bare-handed punches to damage other players when server PvP is enabled.",
                false,
                false,
                null);

            EnablePunchVfx = _category.CreateEntry(
                "EnablePunchVfx",
                true,
                "Enable Punch VFX",
                "Spawns layered shockwave, spark, and dust VFX on punch impacts (non-headless builds).",
                false,
                false,
                null);

            PunchVfxScale = _category.CreateEntry(
                "PunchVfxScale",
                1.0f,
                "Punch VFX Scale",
                "Scale multiplier (0.1 to 3.0) for kinetic punch impact particles.",
                false,
                false,
                null);

            EnableSurfaceImpactVfx = _category.CreateEntry(
                "EnableSurfaceImpactVfx",
                true,
                "Enable Surface Impact VFX",
                "Plays subtle dust/spark VFX and thud SFX when punching solid world surfaces.",
                false,
                false,
                null);

            Refresh();
        }

        public void Refresh()
        {
            var raw = new PunchConfigSnapshot(
                enabled: Enabled?.Value ?? true,
                basePunchDamage: BasePunchDamage?.Value ?? 0.1f,
                maxPunchDamage: MaxPunchDamage?.Value ?? 0.15f,
                enableVelocityScaling: EnableVelocityScaling?.Value ?? true,
                minPunchSpeed: MinPunchSpeed?.Value ?? 2.2f,
                punchCooldownSeconds: PunchCooldownSeconds?.Value ?? 0.40f,
                requireClosedFist: RequireClosedFist?.Value ?? true,
                allowPvPPunching: AllowPvPPunching?.Value ?? true,
                enablePunchVfx: EnablePunchVfx?.Value ?? true,
                punchVfxScale: PunchVfxScale?.Value ?? 1.0f,
                enableSurfaceImpactVfx: EnableSurfaceImpactVfx?.Value ?? true);

            Snapshot = raw.Sanitize();
        }
    }
}
