using System;
using System.IO;
using MelonLoader;
using MelonLoader.Utils;
using Punchy.Core;
using UnityEngine;

[assembly: MelonInfo(typeof(Punchy.PunchyMod), "Punchy", "1.0.0", "Punchy")]
[assembly: MelonGame("Alta", "A Township Tale")]

namespace Punchy
{
    /// <summary>
    /// MelonLoader entry point for Punchy — authoritative server-side bare-handed punching for A Township Tale.
    /// </summary>
    public sealed class PunchyMod : MelonMod
    {
        private const float ReconcileIntervalSeconds = 2.0f;

        private readonly PunchyConfig _config = new PunchyConfig();
        private PunchServerManager _manager;
        private float _reconcileTimer;
        private bool _abortedIl2Cpp;

        public override void OnInitializeMelon()
        {
            if (File.Exists(Path.Combine(MelonEnvironment.GameRootDirectory, "GameAssembly.dll")))
            {
                _abortedIl2Cpp = true;
                LoggerInstance.Error("[Punchy] IL2CPP GameAssembly.dll detected; this build targets Mono PCVR only and will remain inactive.");
                return;
            }

            _config.Initialize();
            _manager = new PunchServerManager(_config, LoggerInstance);

            HarmonyPatches.Apply(HarmonyInstance, LoggerInstance);

            try
            {
                Player.OnPlayerRemoved += OnPlayerRemoved;
            }
            catch (Exception ex)
            {
                LoggerInstance.Warning($"[Punchy] Could not subscribe to Player.OnPlayerRemoved: {ex.Message}");
            }

            PunchConfigSnapshot snap = _config.Snapshot;
            LoggerInstance.Msg(
                $"[Punchy] Initialized (Server-Side Bare-Handed Combat) | " +
                $"Enabled={snap.Enabled}, BaseDmg={snap.BasePunchDamage:F2}, MaxDmg={snap.MaxPunchDamage:F2}, " +
                $"VelScaling={snap.EnableVelocityScaling}, MinSpeed={snap.MinPunchSpeed:F2} m/s, " +
                $"Cooldown={snap.PunchCooldownSeconds:F2}s, RequireFist={snap.RequireClosedFist}, AllowPvP={snap.AllowPvPPunching}");
        }

        public override void OnPreferencesLoaded()
        {
            if (_abortedIl2Cpp)
                return;

            _config.Refresh();
        }

        public override void OnUpdate()
        {
            if (_abortedIl2Cpp || !NetworkSceneManager.IsServer || _manager == null)
                return;

            _reconcileTimer += Time.unscaledDeltaTime;
            if (_reconcileTimer >= ReconcileIntervalSeconds)
            {
                _reconcileTimer = 0f;
                _manager.ReconcileConnectedPlayers();
            }
        }

        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            if (_abortedIl2Cpp)
                return;

            _reconcileTimer = 0f;
            _manager?.ClearAll();
            PunchVfxSystem.Cleanup();
        }

        public override void OnDeinitializeMelon()
        {
            if (_abortedIl2Cpp)
                return;

            try
            {
                Player.OnPlayerRemoved -= OnPlayerRemoved;
            }
            catch
            {
                // Ignore during shutdown.
            }

            _manager?.ClearAll();
            PunchVfxSystem.Cleanup();

            try
            {
                HarmonyInstance?.UnpatchSelf();
            }
            catch
            {
                // Ignore during shutdown.
            }
        }

        private void OnPlayerRemoved(Player player)
        {
            _manager?.UnregisterByPlayer(player);
        }
    }
}
