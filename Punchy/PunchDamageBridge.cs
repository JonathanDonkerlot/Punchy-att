using System;
using System.Collections.Generic;
using System.Reflection;
using Alta.Global;
using Alta.Impact;
using Alta.Networking;
using Alta.Networking.Servers;
using Alta.Utilities;
using HarmonyLib;
using MelonLoader;
using Punchy.Core;
using UnityEngine;

namespace Punchy
{
    /// <summary>
    /// Bridges Punchy's validated hits into ATT's native ImpactDetector / HealthObject / AudioManager pipeline.
    /// </summary>
    public static class PunchDamageBridge
    {
        private static readonly FieldInfo BaseEffectField =
            AccessTools.Field(typeof(ImpactDetectorSettings), "baseEffect");

        private static readonly FieldInfo GoodEffectField =
            AccessTools.Field(typeof(ImpactDetectorSettings), "good");

        private static PooledObjectDefinition _fallbackHitEffect;

        public static bool IsServerPvPEnabled()
        {
            try
            {
                ServerHandler server = ServerHandler.Current;
#pragma warning disable CS0618 // ServerHandler.ServerConfig.Settings still returns ServerSettings in this build.
                ServerSettings settings = server?.ServerConfig?.Settings;
                return settings == null || settings.IsPVPEnabled;
#pragma warning restore CS0618
            }
            catch
            {
                return true;
            }
        }

        public static void GetPvPMultipliers(out float healthMultiplier, out float crippleMultiplier)
        {
            healthMultiplier = 1f;
            crippleMultiplier = 1f;

            try
            {
#pragma warning disable CS0618 // Matches PlayerHealth.ReceiveDamage IL reading ServerSettings.PVPMultiplier/PVPCrippleMultiplier.
                ServerSettings settings = ServerHandler.Current?.ServerConfig?.Settings;
                if (settings != null)
                {
                    healthMultiplier = settings.PVPMultiplier;
                    crippleMultiplier = settings.PVPCrippleMultiplier;
                }
#pragma warning restore CS0618
            }
            catch
            {
                // Keep defaults if settings are unavailable.
            }
        }

        public static bool TryResolveTarget(
            Collider collider,
            PlayerController attacker,
            out ImpactDetector detector,
            out HealthObject health,
            out DestroyableStaticObject destroyable,
            out PunchTargetDescriptor descriptor)
        {
            detector = null;
            health = null;
            destroyable = null;
            descriptor = default;

            if (collider == null || attacker == null)
                return false;

            Rigidbody attachedRb = collider.attachedRigidbody;

            if (attachedRb != null)
                detector = attachedRb.GetComponent<ImpactDetector>();
            if (detector == null)
                detector = collider.GetComponent<ImpactDetector>();
            if (detector == null)
                detector = collider.GetComponentInParent<ImpactDetector>();

            if (detector != null)
                health = detector.HealthTarget;
            if (health == null && attachedRb != null)
                health = attachedRb.GetComponent<HealthObject>();
            if (health == null)
                health = collider.GetComponentInParent<HealthObject>();

            if (health == null && detector != null)
            {
                destroyable = detector.GetComponent<DestroyableStaticObject>();
                if (destroyable == null)
                    destroyable = collider.GetComponentInParent<DestroyableStaticObject>();
            }

            if (health == null && destroyable == null && detector == null)
                return false;

            if (!ApplicationManager.IsHeadless && _fallbackHitEffect == null && detector != null && detector.Settings != null)
                TryCacheFallbackHitEffect(detector.Settings);

            NetworkEntity targetEntity = health != null ? health.Entity : null;
            if (targetEntity == null && detector != null)
                targetEntity = detector.GetComponentInParent<NetworkEntity>();
            if (targetEntity == null)
                targetEntity = collider.GetComponentInParent<NetworkEntity>();

            uint targetEntityRootId = 0;
            if (targetEntity != null)
            {
                NetworkEntity root = targetEntity.PrefabRoot != null ? targetEntity.PrefabRoot : targetEntity;
                targetEntityRootId = root != null ? root.Identifier : targetEntity.Identifier;
            }

            uint attackerEntityRootId = 0;
            if (attacker.Entity != null)
            {
                NetworkEntity attackerRoot = attacker.Entity.PrefabRoot != null ? attacker.Entity.PrefabRoot : attacker.Entity;
                attackerEntityRootId = attackerRoot != null ? attackerRoot.Identifier : attacker.Entity.Identifier;
            }

            bool inAttackerHierarchy;
            bool dockedOnAttacker;
            bool heldByAttacker;
            CheckSelfFlags(collider, targetEntity, attacker, out inAttackerHierarchy, out dockedOnAttacker, out heldByAttacker);

            PlayerHealth targetPlayerHealth = health as PlayerHealth;
            bool isTargetPlayer = targetPlayerHealth != null;
            int targetPlayerId = (isTargetPlayer && targetPlayerHealth.Player != null)
                ? targetPlayerHealth.Player.GetInstanceID()
                : 0;

            bool isAliveOrBreakable;
            if (health != null)
            {
                if (targetPlayerHealth != null)
                {
                    PlayerController targetPc = targetPlayerHealth.Player;
                    isAliveOrBreakable = health.enabled &&
                                         health.Health > 0f &&
                                         targetPc != null &&
                                         targetPc.IsAlive &&
                                         !targetPc.IsDowned &&
                                         !targetPc.IsSpirit &&
                                         !targetPc.IsIgnoringDamage;
                }
                else if (health.MaxHealth > 0f)
                {
                    isAliveOrBreakable = health.enabled && health.Health > 0f;
                }
                else
                {
                    // Targets with MaxHealth <= 0 (e.g., training/combat dummies) remain punchable via ImpactDetector.
                    isAliveOrBreakable = detector != null;
                }
            }
            else
            {
                isAliveOrBreakable = detector != null;
            }

            int attackerPlayerId = attacker.GetInstanceID();
            int attackerHealthId = attacker.Health != null ? attacker.Health.GetInstanceID() : 0;
            int targetHealthId = health != null ? health.GetInstanceID() : 0;

            descriptor = new PunchTargetDescriptor(
                attackerPlayerId: attackerPlayerId,
                attackerEntityRootId: attackerEntityRootId,
                attackerHealthId: attackerHealthId,
                targetHealthId: targetHealthId,
                targetEntityRootId: targetEntityRootId,
                targetPlayerId: targetPlayerId,
                isTargetPlayer: isTargetPlayer,
                isTargetInAttackerHierarchy: inAttackerHierarchy,
                isTargetDockedOnAttacker: dockedOnAttacker,
                isTargetHeldByAttacker: heldByAttacker,
                isTargetAliveOrBreakable: isAliveOrBreakable);

            return true;
        }

        public static bool IsSelfCollider(Collider collider, PlayerController attacker)
        {
            if (collider == null || attacker == null)
                return true;

            NetworkEntity targetEntity = collider.GetComponentInParent<NetworkEntity>();
            CheckSelfFlags(collider, targetEntity, attacker, out bool inHierarchy, out bool docked, out bool held);
            return inHierarchy || docked || held;
        }

        private static void CheckSelfFlags(
            Collider collider,
            NetworkEntity targetEntity,
            PlayerController attacker,
            out bool inAttackerHierarchy,
            out bool dockedOnAttacker,
            out bool heldByAttacker)
        {
            inAttackerHierarchy = collider.transform.IsChildOf(attacker.transform);
            dockedOnAttacker = false;
            heldByAttacker = false;

            Pickup pickup = targetEntity != null ? targetEntity.CommonPickup : null;
            if (pickup == null)
                pickup = collider.GetComponentInParent<Pickup>();

            if (pickup == null)
                return;

            dockedOnAttacker = pickup.IsDocked &&
                               pickup.Dock != null &&
                               pickup.Dock.PlayerController == attacker;

            if (pickup.IsInteractedWith)
            {
                List<Interactor> interactors = pickup.Interactors;
                if (interactors != null)
                {
                    for (int i = 0; i < interactors.Count; i++)
                    {
                        Interactor interactor = interactors[i];
                        if (interactor != null && interactor.Controller != null && interactor.Controller.PlayerController == attacker)
                        {
                            heldByAttacker = true;
                            break;
                        }
                    }
                }
            }
        }

        public static bool ApplyPunchHit(
            PlayerController attacker,
            Controller handController,
            PunchHandSide handSide,
            Collider hitCollider,
            ImpactDetector detector,
            HealthObject health,
            DestroyableStaticObject destroyable,
            Vector3 hitPoint,
            Vector3 hitNormal,
            Vector3 punchDirection,
            Vector3 handVelocity,
            PunchDecision decision,
            in PunchConfigSnapshot config,
            MelonLogger.Instance log)
        {
            if (!NetworkSceneManager.IsServer || attacker == null || hitCollider == null)
                return false;

            if (health == null && destroyable == null && detector == null)
                return false;

            try
            {
                Vector3 safeDir = punchDirection.sqrMagnitude > 1e-6f ? punchDirection.normalized : Vector3.forward;
                Vector3 safeNormal = hitNormal.sqrMagnitude > 1e-6f ? hitNormal.normalized : -safeDir;
                float hpBefore = health != null ? health.Health : -1f;

                // Construct plain ImpactHit (never ImpactHitWithTool with Tool==null, which NREs in HealthObject.ReceiveDamage).
                var hit = new ImpactHit(
                    baseHealthDamage: decision.HealthDamage,
                    baseCrippleDamage: decision.CrippleDamage,
                    multipliers: 1f,
                    effectiveness: 1f,
                    detector: detector,
                    impactType: ImpactType.Blunt,
                    point: hitPoint,
                    normal: safeNormal,
                    direction: safeDir,
                    struckCollider: hitCollider,
                    wielder: attacker.Entity,
                    playerWielder: attacker);

                hit.RelativeVelocity = handVelocity;

                if (health is PlayerHealth)
                {
                    GetPvPMultipliers(out float pvpHealthMult, out float pvpCrippleMult);
                    hit.HealthMultiplier *= pvpHealthMult;
                    hit.CripplingMultiplier *= pvpCrippleMult;
                }

                bool dispatchedThroughDetector = detector != null && (health == null || detector.HealthTarget == health);
                if (dispatchedThroughDetector)
                {
                    detector.OnImpactHit(hit);
                }
                else if (health != null)
                {
                    health.DetectImpact(hit);
                }
                else
                {
                    return false;
                }

                float hpAfter = health != null ? health.Health : -1f;

                if (health != null && health.IsCombatant && attacker.Entity != null)
                {
                    NetworkEntity attackeeEntity = health.Entity != null ? health.Entity : attacker.Entity;
                    float distSqr = (attacker.transform.position - health.transform.position).sqrMagnitude;
                    attacker.OnCausedCombat(attackeeEntity, distSqr, hit.HealthDamage);
                }

                // Trigger procedural ragdoll via RagdollLib when installed and velocity threshold is met.
                bool ragdolled = PunchRagdollBridge.TryApplyPunchRagdoll(
                    attacker,
                    health,
                    detector,
                    hitCollider,
                    hitPoint,
                    safeDir,
                    decision.Speed,
                    in config,
                    log);

                // Subtle physical knockback impulse on dynamic non-player rigidbodies when not already ragdolled.
                if (!ragdolled && !(health is PlayerHealth))
                {
                    Rigidbody targetRb = hitCollider.attachedRigidbody;
                    if (targetRb != null && !targetRb.isKinematic)
                    {
                        float impulseMagnitude = Mathf.Clamp(decision.Speed * 0.55f, 0.8f, 3.5f);
                        targetRb.AddForceAtPosition(safeDir * impulseMagnitude, hitPoint, ForceMode.Impulse);
                    }
                }

                PunchVfxParams vfx = PunchVfxCalculator.Calculate(decision.Speed, isDamageableHit: true, in config);

                bool nativeSynced = false;
                if (detector != null && !(health is PlayerHealth))
                {
                    float syncEff = 0.65f + (0.34f * vfx.Intensity);
                    nativeSynced = NativeImpactSyncBridge.BroadcastNativeImpactEffects(
                        detector,
                        health,
                        attacker,
                        hitPoint,
                        safeNormal,
                        syncEff);
                }

                if (config.DebugLogging)
                {
                    string targetName = health != null
                        ? health.gameObject.name
                        : (detector != null ? detector.gameObject.name : hitCollider.gameObject.name);
                    log?.Msg(
                        $"[Punchy] HIT {targetName} | hand={handSide} speed={decision.Speed:F2} dmg={decision.HealthDamage:F3} " +
                        $"hp={hpBefore:F2}->{hpAfter:F2} ragdoll={ragdolled} nativeSync={nativeSynced} detector={(detector != null)}");
                }

                PlayFeedbackIfNonHeadless(attacker, handController, hitCollider, detector, hitPoint, safeNormal, safeDir, in vfx);
                return true;
            }
            catch (Exception ex)
            {
                log?.Error($"[PunchDamageBridge] Exception applying punch hit: {ex}");
                return false;
            }
        }

        public static void PlaySurfaceImpactFeedback(
            PlayerController attacker,
            Controller handController,
            Collider hitCollider,
            Vector3 hitPoint,
            Vector3 hitNormal,
            Vector3 punchDirection,
            float handSpeed,
            in PunchConfigSnapshot config)
        {
            if (attacker == null || hitCollider == null)
                return;

            Vector3 safeDir = punchDirection.sqrMagnitude > 1e-6f ? punchDirection.normalized : Vector3.forward;
            Vector3 safeNormal = hitNormal.sqrMagnitude > 1e-6f ? hitNormal.normalized : -safeDir;

            if (attacker.IsLocalPlayer && handController != null)
            {
                try
                {
                    handController.TriggerHaptic(0.35f, 0.05f, 100f);
                }
                catch
                {
                    // Ignore if XR device does not support haptics.
                }
            }

            if (ApplicationManager.IsHeadless)
                return;

            PunchVfxParams vfx = PunchVfxCalculator.Calculate(handSpeed, isDamageableHit: false, in config);
            PunchVfxSystem.PlayImpactVfx(hitPoint, safeNormal, safeDir, in vfx);

            PlayBluntImpactAudio(hitPoint, hitCollider.gameObject, preferThump: true);
        }

        private static void PlayFeedbackIfNonHeadless(
            PlayerController attacker,
            Controller handController,
            Collider hitCollider,
            ImpactDetector detector,
            Vector3 hitPoint,
            Vector3 hitNormal,
            Vector3 punchDirection,
            in PunchVfxParams vfx)
        {
            if (attacker.IsLocalPlayer && handController != null)
            {
                try
                {
                    handController.TriggerHaptic(0.60f, 0.08f, 120f);
                }
                catch
                {
                    // Ignore if XR device does not support haptics.
                }
            }

            if (ApplicationManager.IsHeadless)
                return;

            GameObject contextGo = hitCollider != null
                ? hitCollider.gameObject
                : (detector != null ? detector.gameObject : null);

            PlayBluntImpactAudio(hitPoint, contextGo, preferThump: false);

            // Layer Punchy's kinetic shockwave/spark/dust burst over native ImpactDetector effects.
            PunchVfxSystem.PlayImpactVfx(hitPoint, hitNormal, punchDirection, in vfx);

            // ImpactDetector.OnImpactHit already invokes ImpactDetectorSettings.PlayEffects when detector.Settings != null.
            if (detector == null || detector.Settings == null)
                PlayFallbackParticle(hitPoint, hitNormal);
        }

        private static void PlayBluntImpactAudio(Vector3 hitPoint, GameObject contextGo, bool preferThump)
        {
            try
            {
                AudioManager audio = AudioManager.Instance;
                if (audio == null)
                    return;

                SoundEffect primary = preferThump ? SoundEffect.WoodThump : SoundEffect.FistBump;
                SoundEffect secondary = preferThump ? SoundEffect.FistBump : SoundEffect.WoodThump;

                SoundEffectSet resolved = SoundEffectExtensions.Resolve(primary, contextGo);
                if (resolved == null)
                    resolved = SoundEffectExtensions.Resolve(secondary, contextGo);
                if (resolved == null)
                    resolved = SoundEffectExtensions.Resolve(SoundEffect.ShieldBashWood, contextGo);

                if (resolved != null)
                    audio.PlaySoundEffectAt(resolved, hitPoint, contextGo, Array.Empty<ISoundProperty>());
            }
            catch
            {
                // Ignore if sound bank is unloaded or headless.
            }
        }

        private static void PlayFallbackParticle(Vector3 hitPoint, Vector3 hitNormal)
        {
            PooledObjectDefinition def = _fallbackHitEffect;
            if (def == null)
            {
                try
                {
                    def = GlobalSettings<PickupTypeSettings>.Instance?.DefaultBreakEffect;
                }
                catch
                {
                    def = null;
                }
            }

            if (def == null)
                return;

            try
            {
                Quaternion rot = hitNormal.sqrMagnitude > 1e-4f
                    ? Quaternion.LookRotation(hitNormal)
                    : Quaternion.identity;
                def.Get(hitPoint, rot);
            }
            catch
            {
                // Ignore pooled particle fallback errors.
            }
        }

        private static void TryCacheFallbackHitEffect(ImpactDetectorSettings settings)
        {
            try
            {
                if (BaseEffectField != null)
                {
                    var baseDef = BaseEffectField.GetValue(settings) as PooledObjectDefinition;
                    if (baseDef != null)
                    {
                        _fallbackHitEffect = baseDef;
                        return;
                    }
                }

                if (GoodEffectField != null)
                {
                    object goodSettings = GoodEffectField.GetValue(settings);
                    if (goodSettings is ImpactDetectorSettings.ImpactSettings impactSettings &&
                        impactSettings.ObjectDefinition != null)
                    {
                        _fallbackHitEffect = impactSettings.ObjectDefinition;
                    }
                }
            }
            catch
            {
                // Non-critical cache only.
            }
        }
    }
}
