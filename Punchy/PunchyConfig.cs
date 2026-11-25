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
        public MelonPreferences_Entry<bool> DebugLogging { get; private set; }

        public MelonPreferences_Entry<bool> EnableRagdoll { get; private set; }
        public MelonPreferences_Entry<bool> RagdollPlayersOnHaymaker { get; private set; }
        public MelonPreferences_Entry<float> PlayerRagdollMinSpeed { get; private set; }
        public MelonPreferences_Entry<float> PlayerRagdollBaseImpulse { get; private set; }
        public MelonPreferences_Entry<float> PlayerRagdollMaxImpulse { get; private set; }
        public MelonPreferences_Entry<float> PlayerRagdollDuration { get; private set; }
        public MelonPreferences_Entry<bool> RagdollSmallCreatures { get; private set; }
        public MelonPreferences_Entry<float> SmallCreatureRagdollMinSpeed { get; private set; }
        public MelonPreferences_Entry<float> SmallCreatureRagdollBaseImpulse { get; private set; }
        public MelonPreferences_Entry<float> SmallCreatureRagdollMaxImpulse { get; private set; }
        public MelonPreferences_Entry<float> SmallCreatureRagdollDuration { get; private set; }
        public MelonPreferences_Entry<bool> RagdollLargeCreatures { get; private set; }
        public MelonPreferences_Entry<float> LargeCreatureRagdollMinSpeed { get; private set; }
        public MelonPreferences_Entry<float> LargeCreatureRagdollBaseImpulse { get; private set; }
        public MelonPreferences_Entry<float> LargeCreatureRagdollMaxImpulse { get; private set; }
        public MelonPreferences_Entry<float> LargeCreatureRagdollDuration { get; private set; }
        public MelonPreferences_Entry<float> RagdollUpwardBoost { get; private set; }

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

            DebugLogging = _category.CreateEntry(
                "DebugLogging",
                false,
                "Debug Logging",
                "Logs throttled server punch hit and swing rejection diagnostics to MelonLoader console.",
                false,
                false,
                null);

            EnableRagdoll = _category.CreateEntry(
                "EnableRagdoll",
                true,
                "Enable Ragdoll (RagdollLib)",
                "Triggers procedural RagdollLib physics on punch impacts when RagdollLib.dll is installed.",
                false,
                false,
                null);

            RagdollPlayersOnHaymaker = _category.CreateEntry(
                "RagdollPlayersOnHaymaker",
                true,
                "Ragdoll Players On Haymaker",
                "Allows heavy haymaker punches to ragdoll players (also requires AllowPlayerRagdoll = true under [RagdollLib]).",
                false,
                false,
                null);

            PlayerRagdollMinSpeed = _category.CreateEntry(
                "PlayerRagdollMinSpeed",
                5.0f,
                "Player Ragdoll Min Speed (m/s)",
                "Minimum haymaker hand speed in m/s required to ragdoll another player (normal punches do not ragdoll players).",
                false,
                false,
                null);

            PlayerRagdollBaseImpulse = _category.CreateEntry(
                "PlayerRagdollBaseImpulse",
                155.0f,
                "Player Ragdoll Base Impulse",
                "Realistic base impulse (N*s) applied to a player knocked down by a minimum-threshold haymaker.",
                false,
                false,
                null);

            PlayerRagdollMaxImpulse = _category.CreateEntry(
                "PlayerRagdollMaxImpulse",
                225.0f,
                "Player Ragdoll Max Impulse",
                "Maximum capped impulse (N*s) for fastest haymakers against players (capped at 350 N*s to prevent unrealistic launches).",
                false,
                false,
                null);

            PlayerRagdollDuration = _category.CreateEntry(
                "PlayerRagdollDuration",
                3.0f,
                "Player Ragdoll Duration (s)",
                "Seconds before a haymaker-ragdolled player recovers back to normal stance.",
                false,
                false,
                null);

            RagdollSmallCreatures = _category.CreateEntry(
                "RagdollSmallCreatures",
                true,
                "Ragdoll Small Creatures",
                "Allows punches to ragdoll small creatures (Spriggull, Babu, Schmeechee, FireBug, Dragonfly, Wyrm).",
                false,
                false,
                null);

            SmallCreatureRagdollMinSpeed = _category.CreateEntry(
                "SmallCreatureRagdollMinSpeed",
                2.2f,
                "Small Creature Ragdoll Min Speed (m/s)",
                "Minimum hand speed in m/s to ragdoll small creatures such as Spriggulls (2.2 m/s = any normal punch).",
                false,
                false,
                null);

            SmallCreatureRagdollBaseImpulse = _category.CreateEntry(
                "SmallCreatureRagdollBaseImpulse",
                135.0f,
                "Small Creature Ragdoll Base Impulse",
                "Realistic base impulse (N*s) for normal punches against Spriggulls and small creatures.",
                false,
                false,
                null);

            SmallCreatureRagdollMaxImpulse = _category.CreateEntry(
                "SmallCreatureRagdollMaxImpulse",
                195.0f,
                "Small Creature Ragdoll Max Impulse",
                "Maximum capped impulse (N*s) for heavy punches against small creatures (prevents launching to the moon).",
                false,
                false,
                null);

            SmallCreatureRagdollDuration = _category.CreateEntry(
                "SmallCreatureRagdollDuration",
                3.0f,
                "Small Creature Ragdoll Duration (s)",
                "Seconds before a ragdolled small creature recovers.",
                false,
                false,
                null);

            RagdollLargeCreatures = _category.CreateEntry(
                "RagdollLargeCreatures",
                true,
                "Ragdoll Large Creatures",
                "Allows heavy haymakers to stagger-ragdoll large creatures (Gotera, GreaterGotera, Turabada, Phantom).",
                false,
                false,
                null);

            LargeCreatureRagdollMinSpeed = _category.CreateEntry(
                "LargeCreatureRagdollMinSpeed",
                5.5f,
                "Large Creature Ragdoll Min Speed (m/s)",
                "Minimum haymaker hand speed in m/s required to ragdoll large creatures.",
                false,
                false,
                null);

            LargeCreatureRagdollBaseImpulse = _category.CreateEntry(
                "LargeCreatureRagdollBaseImpulse",
                175.0f,
                "Large Creature Ragdoll Base Impulse",
                "Realistic base impulse (N*s) applied when a haymaker ragdolls a large creature.",
                false,
                false,
                null);

            LargeCreatureRagdollMaxImpulse = _category.CreateEntry(
                "LargeCreatureRagdollMaxImpulse",
                250.0f,
                "Large Creature Ragdoll Max Impulse",
                "Maximum capped impulse (N*s) against large creatures.",
                false,
                false,
                null);

            LargeCreatureRagdollDuration = _category.CreateEntry(
                "LargeCreatureRagdollDuration",
                2.8f,
                "Large Creature Ragdoll Duration (s)",
                "Seconds before a ragdolled large creature recovers.",
                false,
                false,
                null);

            RagdollUpwardBoost = _category.CreateEntry(
                "RagdollUpwardBoost",
                0.32f,
                "Ragdoll Upward Boost",
                "Subtle upward lift ratio (0.0 to 0.50) blended into punch impulses for grounded knockdowns.",
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
                enableSurfaceImpactVfx: EnableSurfaceImpactVfx?.Value ?? true,
                debugLogging: DebugLogging?.Value ?? false,
                enableRagdoll: EnableRagdoll?.Value ?? true,
                ragdollPlayersOnHaymaker: RagdollPlayersOnHaymaker?.Value ?? true,
                playerRagdollMinSpeed: PlayerRagdollMinSpeed?.Value ?? 5.0f,
                playerRagdollBaseImpulse: PlayerRagdollBaseImpulse?.Value ?? 155.0f,
                playerRagdollMaxImpulse: PlayerRagdollMaxImpulse?.Value ?? 225.0f,
                playerRagdollDuration: PlayerRagdollDuration?.Value ?? 3.0f,
                ragdollSmallCreatures: RagdollSmallCreatures?.Value ?? true,
                smallCreatureRagdollMinSpeed: SmallCreatureRagdollMinSpeed?.Value ?? 2.2f,
                smallCreatureRagdollBaseImpulse: SmallCreatureRagdollBaseImpulse?.Value ?? 135.0f,
                smallCreatureRagdollMaxImpulse: SmallCreatureRagdollMaxImpulse?.Value ?? 195.0f,
                smallCreatureRagdollDuration: SmallCreatureRagdollDuration?.Value ?? 3.0f,
                ragdollLargeCreatures: RagdollLargeCreatures?.Value ?? true,
                largeCreatureRagdollMinSpeed: LargeCreatureRagdollMinSpeed?.Value ?? 5.5f,
                largeCreatureRagdollBaseImpulse: LargeCreatureRagdollBaseImpulse?.Value ?? 175.0f,
                largeCreatureRagdollMaxImpulse: LargeCreatureRagdollMaxImpulse?.Value ?? 250.0f,
                largeCreatureRagdollDuration: LargeCreatureRagdollDuration?.Value ?? 2.8f,
                ragdollUpwardBoost: RagdollUpwardBoost?.Value ?? 0.32f);

            Snapshot = raw.Sanitize();
        }
    }
}
