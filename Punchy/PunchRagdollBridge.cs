using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Alta.Character;
using Alta.Impact;
using Alta.Intelligence;
using Alta.Networking;
using MelonLoader;
using Punchy.Core;
using RagdollLib.API;
using UnityEngine;

namespace Punchy
{
    /// <summary>
    /// Soft-dependency bridge routing validated server punches into RagdollLib.API.RagdollAPI
    /// with realistic, grounded impulse bounds per target class.
    /// </summary>
    public static class PunchRagdollBridge
    {
        private static bool _checkedAvailability;
        private static bool _isAvailable;

        public static bool IsAvailable
        {
            get
            {
                if (!_checkedAvailability)
                    DetectRagdollLib(null);
                return _isAvailable;
            }
        }

        public static void Initialize(MelonLogger.Instance log)
        {
            DetectRagdollLib(log);
        }

        private static void DetectRagdollLib(MelonLogger.Instance log)
        {
            _checkedAvailability = true;
            _isAvailable = false;

            try
            {
                Assembly[] loaded = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < loaded.Length; i++)
                {
                    if (string.Equals(loaded[i].GetName().Name, "RagdollLib", StringComparison.OrdinalIgnoreCase))
                    {
                        _isAvailable = true;
                        break;
                    }
                }

                if (!_isAvailable && MelonTypeBase<MelonMod>.RegisteredMelons != null)
                {
                    foreach (MelonMod mod in MelonTypeBase<MelonMod>.RegisteredMelons)
                    {
                        if (mod != null && string.Equals(mod.Info?.Name, "RagdollLib", StringComparison.OrdinalIgnoreCase))
                        {
                            _isAvailable = true;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _isAvailable = false;
                log?.Warning($"[Punchy] Failed checking RagdollLib availability: {ex.Message}");
            }

            if (_isAvailable)
            {
                log?.Msg("[Punchy] RagdollLib detected — realistic punch ragdoll physics enabled.");
            }
            else
            {
                log?.Msg("[Punchy] RagdollLib not detected — running without procedural ragdoll physics.");
            }
        }

        public static PunchRagdollTargetKind ClassifyTarget(
            HealthObject health,
            ImpactDetector detector,
            Collider hitCollider)
        {
            if (health is PlayerHealth)
                return PunchRagdollTargetKind.Player;

            if (hitCollider != null && hitCollider.GetComponentInParent<PlayerCharacter>() != null)
                return PunchRagdollTargetKind.Player;

            Transform scope = ResolveTargetRootTransform(health, detector, hitCollider);
            if (scope == null)
                return PunchRagdollTargetKind.None;

            if (scope.GetComponentInChildren<Spriggull>(true) != null ||
                scope.GetComponentInChildren<Babu>(true) != null ||
                scope.GetComponentInChildren<Schmeechee>(true) != null ||
                scope.GetComponentInChildren<FireBug>(true) != null ||
                scope.GetComponentInChildren<Dragonfly>(true) != null ||
                scope.GetComponentInChildren<Wyrm>(true) != null)
            {
                return PunchRagdollTargetKind.SmallCreature;
            }

            if (scope.GetComponentInChildren<Gotera>(true) != null ||
                scope.GetComponentInChildren<GreaterGotera>(true) != null ||
                scope.GetComponentInChildren<Turabada>(true) != null ||
                scope.GetComponentInChildren<Phantom>(true) != null)
            {
                return PunchRagdollTargetKind.LargeCreature;
            }

            PunchRagdollTargetKind byName = PunchRagdollCalculator.ClassifyCreatureByName(scope.gameObject.name);
            if (byName != PunchRagdollTargetKind.None)
                return byName;

            if (scope.GetComponentInChildren<Agent>(true) != null)
                return PunchRagdollTargetKind.LargeCreature;

            return PunchRagdollTargetKind.None;
        }

        public static bool TryApplyPunchRagdoll(
            HealthObject health,
            ImpactDetector detector,
            Collider hitCollider,
            Vector3 hitPoint,
            Vector3 punchDirection,
            float punchSpeed,
            in PunchConfigSnapshot config,
            MelonLogger.Instance log)
        {
            if (!NetworkSceneManager.IsServer || !config.EnableRagdoll || !IsAvailable)
                return false;

            PunchRagdollTargetKind targetKind = ClassifyTarget(health, detector, hitCollider);
            if (targetKind == PunchRagdollTargetKind.None)
                return false;

            var dir = new Vec3(punchDirection.x, punchDirection.y, punchDirection.z);
            PunchRagdollDecision decision = PunchRagdollCalculator.Evaluate(targetKind, punchSpeed, dir, in config);
            if (!decision.ShouldRagdoll)
                return false;

            Component targetComponent = health != null
                ? (Component)health
                : (detector != null ? (Component)detector : hitCollider);

            if (targetComponent == null)
                return false;

            var impulse = new Vector3(
                decision.ImpulseVector.X,
                decision.ImpulseVector.Y,
                decision.ImpulseVector.Z);

            try
            {
                bool triggered = TriggerRagdollNoInline(targetComponent, impulse, hitPoint, decision.DurationSeconds);
                if (!triggered && targetKind == PunchRagdollTargetKind.Player && config.DebugLogging)
                {
                    log?.Msg(
                        "[Punchy][Debug] Player haymaker met speed threshold, but RagdollLib returned false " +
                        "(ensure [RagdollLib] AllowPlayerRagdoll = true in UserData/MelonPreferences.cfg).");
                }
                return triggered;
            }
            catch (Exception ex)
            {
                log?.Warning($"[Punchy] RagdollLib invocation failed: {ex.Message}");
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool TriggerRagdollNoInline(
            Component target,
            Vector3 impulse,
            Vector3 worldHitPoint,
            float durationSeconds)
        {
            return RagdollAPI.TriggerRagdoll(target, impulse, worldHitPoint, durationSeconds);
        }

        private static Transform ResolveTargetRootTransform(
            HealthObject health,
            ImpactDetector detector,
            Collider hitCollider)
        {
            Component probe = health != null
                ? (Component)health
                : (detector != null ? (Component)detector : hitCollider);

            if (probe == null)
                return null;

            NetworkEntity netEntity = probe.GetComponentInParent<NetworkEntity>();
            if (netEntity != null)
            {
                NetworkEntity prefabRoot = netEntity.PrefabRoot != null ? netEntity.PrefabRoot : netEntity;
                return prefabRoot.transform;
            }

            if (health != null)
                return health.transform;

            Agent agent = probe.GetComponentInParent<Agent>();
            if (agent != null)
                return agent.transform.parent != null ? agent.transform.parent : agent.transform;

            return probe.transform;
        }
    }
}
