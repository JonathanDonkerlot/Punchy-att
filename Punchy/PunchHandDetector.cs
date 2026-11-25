using System;
using System.Collections.Generic;
using Alta.Impact;
using Alta.Networking;
using MelonLoader;
using Punchy.Core;
using UnityEngine;

namespace Punchy
{
    /// <summary>
    /// Server-authoritative per-hand punch detector attached directly to LeftController and RightController GameObjects.
    /// </summary>
    public sealed class PunchHandDetector : MonoBehaviour
    {
        private const float FistRadiusMeters = 0.14f;
        private const int MaxPhysicsResults = 32;
        private const double DebugLogThrottleSeconds = 0.5d;

        private readonly RaycastHit[] _sphereCastHits = new RaycastHit[MaxPhysicsResults];
        private readonly Collider[] _overlapColliders = new Collider[MaxPhysicsResults];
        private readonly HashSet<int> _evaluatedTargetsThisTick = new HashSet<int>();
        private readonly HandVelocityTracker _velocityTracker = new HandVelocityTracker(0.06d);

        private PlayerController _owner;
        private Controller _controller;
        private PunchHandSide _handSide;
        private HandCooldownTracker _cooldownTracker;
        private PunchyConfig _config;
        private MelonLogger.Instance _log;
        private bool _initialized;
        private double _lastDebugLogTime = -999d;

        public PlayerController Owner => _owner;
        public PunchHandSide HandSide => _handSide;

        public void Initialize(
            PlayerController owner,
            Controller controller,
            PunchHandSide handSide,
            HandCooldownTracker cooldownTracker,
            PunchyConfig config,
            MelonLogger.Instance log)
        {
            _owner = owner;
            _controller = controller;
            _handSide = handSide;
            _cooldownTracker = cooldownTracker;
            _config = config;
            _log = log;
            _velocityTracker.Reset();
            _initialized = true;
        }

        private void FixedUpdate()
        {
            if (!_initialized || _config == null)
                return;

            PunchConfigSnapshot cfg = _config.Snapshot;
            if (!cfg.Enabled)
                return;

            bool isServer = NetworkSceneManager.IsServer;
            bool isClientRagdoll = !isServer && PunchRagdollBridge.IsAvailable && cfg.EnableRagdoll;
            if (!isServer && !isClientRagdoll)
                return;

            if (_owner == null || _controller == null)
            {
                Destroy(this);
                return;
            }

            bool attackerAlive = _owner.IsAlive && !_owner.IsDowned && !_owner.IsSpirit;
            if (!attackerAlive)
            {
                _velocityTracker.Reset();
                return;
            }

            double now = Time.fixedTime;

            bool isHolding = HandGestureBridge.IsHandHoldingOrInteracting(_controller);
            _cooldownTracker?.RecordHoldingState(_handSide, isHolding, now);

            Vector3 worldPos = _controller.transform.position;
            HandMotionSample motion = _velocityTracker.Sample(
                now,
                new Vec3(worldPos.x, worldPos.y, worldPos.z),
                cfg.MaxTeleportStepMeters,
                cfg.MaxValidHandSpeed);

            FistPoseState pose = HandGestureBridge.ReadPoseState(_controller);

            var context = new PunchHandContext(
                hand: _handSide,
                currentTime: now,
                attackerAlive: attackerAlive,
                isHoldingItem: isHolding,
                poseState: pose,
                motion: motion);

            PunchDecision swingDecision = PunchValidator.ValidateHandSwing(in context, _cooldownTracker, in cfg);
            if (!swingDecision.IsValid)
            {
                if (cfg.DebugLogging &&
                    motion.Speed >= cfg.MinPunchSpeed &&
                    (now - _lastDebugLogTime) >= DebugLogThrottleSeconds)
                {
                    _lastDebugLogTime = now;
                    bool interactorNull = _controller.Interactor == null;
                    _log?.Msg(
                        $"[Punchy][Debug] {_handSide} fast swing blocked: {swingDecision.RejectReason} | " +
                        $"speed={motion.Speed:F2} grip={pose.GripAxis:F2} free={pose.IsFreeGestureMode} " +
                        $"explicit={pose.HasExplicitGesture} mask={pose.ActiveFingerMask} " +
                        $"hasGesture={pose.HasGestureData} holding={isHolding} interactorNull={interactorNull}");
                }
                return;
            }

            DetectAndApplyHit(in context, in motion, in cfg, isServer);
        }

        private void DetectAndApplyHit(in PunchHandContext context, in HandMotionSample motion, in PunchConfigSnapshot cfg, bool isServer)
        {
            _evaluatedTargetsThisTick.Clear();
            bool serverPvPEnabled = isServer ? PunchDamageBridge.IsServerPvPEnabled() : cfg.AllowPvPPunching;

            Vector3 prevPos = new Vector3(motion.PreviousPosition.X, motion.PreviousPosition.Y, motion.PreviousPosition.Z);
            Vector3 currPos = new Vector3(motion.CurrentPosition.X, motion.CurrentPosition.Y, motion.CurrentPosition.Z);
            Vector3 stepDelta = currPos - prevPos;
            float stepDist = stepDelta.magnitude;

            Vector3 punchDir = motion.Direction.SqrMagnitude > 1e-4f
                ? new Vector3(motion.Direction.X, motion.Direction.Y, motion.Direction.Z)
                : (stepDist > 1e-4f ? stepDelta / stepDist : _controller.transform.forward);

            Vector3 handVel = new Vector3(motion.Velocity.X, motion.Velocity.Y, motion.Velocity.Z);

            bool hasSurfaceHit = false;
            Collider surfaceCollider = null;
            Vector3 surfacePoint = Vector3.zero;
            Vector3 surfaceNormal = Vector3.zero;

            // 1. Swept sphere-cast along hand travel segment to avoid fast-punch tunneling.
            if (stepDist > 0.001f)
            {
                Vector3 stepDir = stepDelta / stepDist;
                int hitCount = Physics.SphereCastNonAlloc(
                    prevPos,
                    FistRadiusMeters,
                    stepDir,
                    _sphereCastHits,
                    stepDist,
                    ~0,
                    QueryTriggerInteraction.Ignore);

                if (hitCount > 0)
                {
                    if (hitCount > _sphereCastHits.Length)
                        hitCount = _sphereCastHits.Length;

                    // Process closest collider hits first without allocating.
                    for (int pass = 0; pass < hitCount; pass++)
                    {
                        int bestIdx = -1;
                        float bestDist = float.MaxValue;

                        for (int i = 0; i < hitCount; i++)
                        {
                            Collider c = _sphereCastHits[i].collider;
                            if (c == null)
                                continue;
                            float d = _sphereCastHits[i].distance;
                            if (d < bestDist)
                            {
                                bestDist = d;
                                bestIdx = i;
                            }
                        }

                        if (bestIdx < 0)
                            break;

                        RaycastHit hitInfo = _sphereCastHits[bestIdx];
                        _sphereCastHits[bestIdx] = default;

                        Vector3 hitPoint = hitInfo.point.sqrMagnitude > 1e-6f ? hitInfo.point : currPos;
                        Vector3 hitNormal = hitInfo.normal.sqrMagnitude > 1e-6f ? hitInfo.normal : -punchDir;

                        if (TryDispatchCandidateCollider(
                                hitInfo.collider,
                                hitPoint,
                                hitNormal,
                                punchDir,
                                handVel,
                                serverPvPEnabled,
                                isServer,
                                in context,
                                in cfg))
                        {
                            return;
                        }

                        if (!hasSurfaceHit &&
                            isServer &&
                            cfg.EnableSurfaceImpactVfx &&
                            !PunchDamageBridge.IsSelfCollider(hitInfo.collider, _owner))
                        {
                            hasSurfaceHit = true;
                            surfaceCollider = hitInfo.collider;
                            surfacePoint = hitPoint;
                            surfaceNormal = hitNormal;
                        }
                    }
                }
            }

            // 2. OverlapSphere fallback catches close-quarters punches already intersecting target colliders.
            int overlapCount = Physics.OverlapSphereNonAlloc(
                currPos,
                FistRadiusMeters,
                _overlapColliders,
                ~0,
                QueryTriggerInteraction.Ignore);

            if (overlapCount > _overlapColliders.Length)
                overlapCount = _overlapColliders.Length;

            for (int i = 0; i < overlapCount; i++)
            {
                Collider col = _overlapColliders[i];
                _overlapColliders[i] = null;
                if (col == null)
                    continue;

                Vector3 closest = SafeClosestPoint(col, currPos);
                Vector3 normalVec = currPos - closest;
                Vector3 hitNormal = normalVec.sqrMagnitude > 1e-6f ? normalVec.normalized : -punchDir;

                if (TryDispatchCandidateCollider(
                        col,
                        closest,
                        hitNormal,
                        punchDir,
                        handVel,
                        serverPvPEnabled,
                        isServer,
                        in context,
                        in cfg))
                {
                    return;
                }

                if (!hasSurfaceHit &&
                    isServer &&
                    cfg.EnableSurfaceImpactVfx &&
                    !PunchDamageBridge.IsSelfCollider(col, _owner))
                {
                    hasSurfaceHit = true;
                    surfaceCollider = col;
                    surfacePoint = closest;
                    surfaceNormal = hitNormal;
                }
            }

            if (hasSurfaceHit &&
                cfg.EnableSurfaceImpactVfx &&
                _cooldownTracker != null &&
                !_cooldownTracker.IsOnSurfaceCooldown(_handSide, context.CurrentTime, 0.35f))
            {
                PunchDamageBridge.PlaySurfaceImpactFeedback(
                    _owner,
                    _controller,
                    surfaceCollider,
                    surfacePoint,
                    surfaceNormal,
                    punchDir,
                    motion.Speed,
                    in cfg);
                _cooldownTracker.RecordSurfaceHit(_handSide, context.CurrentTime);
            }
        }

        private bool TryDispatchCandidateCollider(
            Collider candidate,
            Vector3 hitPoint,
            Vector3 hitNormal,
            Vector3 punchDir,
            Vector3 handVel,
            bool serverPvPEnabled,
            bool isServer,
            in PunchHandContext context,
            in PunchConfigSnapshot cfg)
        {
            if (candidate == null)
                return false;

            if (!PunchDamageBridge.TryResolveTarget(
                    candidate,
                    _owner,
                    out ImpactDetector detector,
                    out HealthObject health,
                    out DestroyableStaticObject destroyable,
                    out PunchTargetDescriptor descriptor))
            {
                return false;
            }

            int targetKey = health != null
                ? health.GetInstanceID()
                : (destroyable != null ? destroyable.GetInstanceID() : candidate.GetInstanceID());

            if (!_evaluatedTargetsThisTick.Add(targetKey))
                return false;

            PunchDecision decision = PunchValidator.ValidateHitAndComputeDamage(
                in context,
                in descriptor,
                serverPvPEnabled,
                _cooldownTracker,
                in cfg);

            if (!decision.IsValid)
            {
                if (cfg.DebugLogging &&
                    (context.CurrentTime - _lastDebugLogTime) >= DebugLogThrottleSeconds)
                {
                    _lastDebugLogTime = context.CurrentTime;
                    _log?.Msg(
                        $"[Punchy][Debug] {_handSide} candidate '{candidate.gameObject.name}' rejected: {decision.RejectReason}");
                }
                return false;
            }

            if (isServer)
            {
                if (!PunchDamageBridge.ApplyPunchHit(
                        _owner,
                        _controller,
                        _handSide,
                        candidate,
                        detector,
                        health,
                        destroyable,
                        hitPoint,
                        hitNormal,
                        punchDir,
                        handVel,
                        decision,
                        in cfg,
                        _log))
                {
                    return false;
                }
            }
            else
            {
                // Visual-only client RagdollLib simulation: never apply damage or health changes on clients.
                if (!PunchRagdollBridge.TryApplyPunchRagdoll(
                        _owner,
                        health,
                        detector,
                        candidate,
                        hitPoint,
                        punchDir,
                        decision.Speed,
                        in cfg,
                        _log))
                {
                    return false;
                }
            }

            _cooldownTracker?.RecordHit(_handSide, context.CurrentTime);
            return true;
        }

        private static Vector3 SafeClosestPoint(Collider col, Vector3 point)
        {
            if (col is BoxCollider || col is SphereCollider || col is CapsuleCollider ||
                (col is MeshCollider mc && mc.convex))
            {
                return col.ClosestPoint(point);
            }

            return col.ClosestPointOnBounds(point);
        }

        private void OnDestroy()
        {
            _initialized = false;
            _owner = null;
            _controller = null;
            _cooldownTracker = null;
            _velocityTracker.Reset();
            PunchServerManager.Instance?.NotifyDetectorDestroyed(this);
        }
    }
}
