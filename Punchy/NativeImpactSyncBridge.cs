using System;
using System.Collections.Generic;
using Alta.Impact;
using Alta.Networking;
using Alta.Networking.Scripts.Player;
using HarmonyLib;
using UnityEngine;

namespace Punchy
{
    /// <summary>
    /// Broadcasts native ATT ImpactDetector.ImpactSyncArguments to unmodded clients in the chunk so
    /// creatures/objects play their exact native hit VFX (e.g., orange Spriggull impact burst) and impact audio
    /// with zero duplicate damage or durability loss.
    /// </summary>
    public static class NativeImpactSyncBridge
    {
        private const float ToolCacheRefreshSeconds = 4.0f;

        private static readonly AccessTools.FieldRef<ImpactDetector, ImpactDetectorCentral> DetectorCentralRef =
            CreateFieldRef<ImpactDetector, ImpactDetectorCentral>("detectorCentral");

        private static readonly AccessTools.FieldRef<ImpactDetector, MethodSyncStruct<ImpactDetector.ImpactSyncArguments>> SyncImpactRef =
            CreateFieldRef<ImpactDetector, MethodSyncStruct<ImpactDetector.ImpactSyncArguments>>("syncImpact");

        private static readonly AccessTools.FieldRef<ImpactTool, List<ImpactGenerator>> IndexedImpactorsRef =
            CreateFieldRef<ImpactTool, List<ImpactGenerator>>("indexedImpactors");

        private static ImpactTool[] _cachedTools;
        private static float _lastCacheRefreshTime = -999f;

        private static AccessTools.FieldRef<TOwner, TField> CreateFieldRef<TOwner, TField>(string fieldName)
        {
            try
            {
                return AccessTools.FieldRefAccess<TOwner, TField>(fieldName);
            }
            catch
            {
                return null;
            }
        }

        public static void ClearCache()
        {
            _cachedTools = null;
            _lastCacheRefreshTime = -999f;
        }

        public static bool BroadcastNativeImpactEffects(
            ImpactDetector detector,
            HealthObject health,
            PlayerController attacker,
            Vector3 hitPoint,
            Vector3 hitNormal,
            float effectiveness)
        {
            if (!NetworkSceneManager.IsServer || detector == null || health is PlayerHealth)
                return false;

            try
            {
                if (!TryFindProxyTool(attacker?.NetworkPlayer, detector, out uint toolId, out int impactorIdx, out int channelIdx))
                    return false;

                float clampedEff = Mathf.Clamp(effectiveness, 0.50f, 0.99f);
                Vector3 safeNormal = hitNormal.sqrMagnitude > 1e-4f ? hitNormal.normalized : Vector3.up;

                var args = new ImpactDetector.ImpactSyncArguments
                {
                    impactToolIdentifier = toolId,
                    impactorIndex = impactorIdx,
                    channelIndex = channelIdx,
                    effectiveness = clampedEff,
                    point = hitPoint,
                    normal = safeNormal
                };

                ImpactDetectorCentral central = DetectorCentralRef != null ? DetectorCentralRef(detector) : null;
                if (central != null)
                {
                    central.SendArguments(detector, args);
                    return true;
                }

                MethodSyncStruct<ImpactDetector.ImpactSyncArguments> sync = SyncImpactRef != null ? SyncImpactRef(detector) : null;
                if (sync != null)
                {
                    sync.SendToChunks(args, Array.Empty<IPlayer>());
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryFindProxyTool(
            Player attackerPlayer,
            ImpactDetector targetDetector,
            out uint toolEntityId,
            out int impactorIndex,
            out int channelIndex)
        {
            toolEntityId = 0;
            impactorIndex = 0;
            channelIndex = 0;

            if (IndexedImpactorsRef == null)
                return false;

            float now = Time.unscaledTime;
            if (_cachedTools == null || _cachedTools.Length == 0 || (now - _lastCacheRefreshTime) >= ToolCacheRefreshSeconds)
            {
                _cachedTools = UnityEngine.Object.FindObjectsOfType<ImpactTool>();
                _lastCacheRefreshTime = now;
            }

            if (_cachedTools == null || _cachedTools.Length == 0)
                return false;

            ImpactChannel[] targetChannels = targetDetector.Channels;
            int bestScore = int.MinValue;
            uint bestEntityId = 0;
            int bestImpactorIdx = 0;
            int bestChannelIdx = 0;

            for (int i = 0; i < _cachedTools.Length; i++)
            {
                ImpactTool tool = _cachedTools[i];
                if (tool == null)
                    continue;

                NetworkEntity entity = tool.Entity;
                if (entity == null || entity.IsBeingDestroyed || entity.CommonImpactTool != tool)
                    continue;

                // Must be unheld so PlayerWielder == null on clients (prevents local client echo back to server).
                if (tool.HasPickup && (tool.Pickup == null || tool.Pickup.IsInteractedWith))
                    continue;

                if (!IsSpawnedForAttacker(entity, attackerPlayer))
                    continue;

                List<ImpactGenerator> impactors = IndexedImpactorsRef(tool);
                if (impactors == null || impactors.Count == 0)
                    continue;

                for (int impIdx = 0; impIdx < impactors.Count; impIdx++)
                {
                    ImpactGenerator generator = impactors[impIdx];
                    if (generator == null)
                        continue;

                    List<ImpactChannel> genChannels = generator.Channels;
                    if (genChannels == null || genChannels.Count == 0)
                        continue;

                    int matchingChanIdx = FindMatchingChannelIndex(genChannels, targetChannels);
                    int chanIdx = matchingChanIdx >= 0 ? matchingChanIdx : 0;

                    int score = 0;
                    if (matchingChanIdx >= 0)
                        score += 100;
                    if (generator.ImpactType == ImpactType.Blunt)
                        score += 20;
                    if (!tool.HasPickup)
                        score += 10;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestEntityId = entity.Identifier;
                        bestImpactorIdx = impIdx;
                        bestChannelIdx = chanIdx;

                        if (score >= 130)
                            break;
                    }
                }

                if (bestScore >= 130)
                    break;
            }

            if (bestEntityId == 0)
                return false;

            toolEntityId = bestEntityId;
            impactorIndex = bestImpactorIdx;
            channelIndex = bestChannelIdx;
            return true;
        }

        private static bool IsSpawnedForAttacker(NetworkEntity entity, Player attackerPlayer)
        {
            if (attackerPlayer == null)
                return entity.PlayersSpawnedForCount > 0;

            IEnumerable<IPlayer> spawnedFor = entity.PlayersSpawnedFor;
            if (spawnedFor == null)
                return false;

            foreach (IPlayer p in spawnedFor)
            {
                if (ReferenceEquals(p, attackerPlayer))
                    return true;
            }

            return false;
        }

        private static int FindMatchingChannelIndex(List<ImpactChannel> generatorChannels, ImpactChannel[] targetChannels)
        {
            if (generatorChannels == null || targetChannels == null || targetChannels.Length == 0)
                return -1;

            for (int gi = 0; gi < generatorChannels.Count; gi++)
            {
                ImpactChannel gc = generatorChannels[gi];
                if (gc == null)
                    continue;

                for (int ti = 0; ti < targetChannels.Length; ti++)
                {
                    if (gc == targetChannels[ti])
                        return gi;
                }
            }

            return -1;
        }
    }
}
