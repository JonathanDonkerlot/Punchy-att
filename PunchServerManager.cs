using System;
using System.Collections.Generic;
using Alta.Character;
using Alta.Networking;
using Alta.Networking.Scripts.Player;
using MelonLoader;
using Punchy.Core;

namespace Punchy
{
    /// <summary>
    /// Tracks all active server-side player characters and manages per-hand PunchHandDetector lifetimes cleanly.
    /// </summary>
    public sealed class PunchServerManager
    {
        private sealed class TrackedPlayer
        {
            public PlayerController Controller;
            public Player NetworkPlayer;
            public HandCooldownTracker CooldownTracker;
            public PunchHandDetector LeftDetector;
            public PunchHandDetector RightDetector;
        }

        public static PunchServerManager Instance { get; private set; }

        private readonly Dictionary<int, TrackedPlayer> _trackedPlayers = new Dictionary<int, TrackedPlayer>();
        private readonly List<int> _pruneBuffer = new List<int>();
        private readonly PunchyConfig _config;
        private readonly MelonLogger.Instance _log;

        public PunchServerManager(PunchyConfig config, MelonLogger.Instance log)
        {
            _config = config;
            _log = log;
            Instance = this;
        }

        public void RegisterPlayer(PlayerController pc)
        {
            if (!NetworkSceneManager.IsServer || pc == null)
                return;

            // Ignore MainMenuPlayerController, CustomizationPlayerController, FlyCamPlayer, etc.
            if (!(pc is PlayerCharacter))
                return;

            int key = pc.GetInstanceID();
            if (_trackedPlayers.TryGetValue(key, out TrackedPlayer existing))
            {
                if (existing.LeftDetector != null && existing.RightDetector != null)
                {
                    existing.NetworkPlayer = pc.NetworkPlayer;
                    return;
                }

                CleanupTrackedEntry(existing);
                _trackedPlayers.Remove(key);
            }

            Controller left = pc.LeftController;
            Controller right = pc.RightController;
            if (left == null && right == null)
                return;

            var cooldownTracker = new HandCooldownTracker();
            var entry = new TrackedPlayer
            {
                Controller = pc,
                NetworkPlayer = pc.NetworkPlayer,
                CooldownTracker = cooldownTracker,
                LeftDetector = AttachHandDetector(pc, left, PunchHandSide.Left, cooldownTracker),
                RightDetector = AttachHandDetector(pc, right, PunchHandSide.Right, cooldownTracker)
            };

            _trackedPlayers[key] = entry;
            string playerName = pc.NetworkPlayer?.UserInfo?.Username ?? pc.name;
            _log?.Msg($"[Punchy] Registered bare-hand punch detectors for '{playerName}'.");
        }

        public void UnregisterPlayer(PlayerController pc)
        {
            if (pc == null)
                return;

            int key = pc.GetInstanceID();
            if (_trackedPlayers.TryGetValue(key, out TrackedPlayer entry))
            {
                CleanupTrackedEntry(entry);
                _trackedPlayers.Remove(key);
            }
        }

        public void UnregisterByPlayer(Player player)
        {
            if (player == null)
                return;

            if (player.PlayerController != null)
                UnregisterPlayer(player.PlayerController);

            _pruneBuffer.Clear();
            foreach (KeyValuePair<int, TrackedPlayer> kvp in _trackedPlayers)
            {
                if (kvp.Value.NetworkPlayer == player || kvp.Value.Controller == null)
                    _pruneBuffer.Add(kvp.Key);
            }

            for (int i = 0; i < _pruneBuffer.Count; i++)
            {
                int key = _pruneBuffer[i];
                if (_trackedPlayers.TryGetValue(key, out TrackedPlayer entry))
                {
                    CleanupTrackedEntry(entry);
                    _trackedPlayers.Remove(key);
                }
            }
        }

        public void ReconcileConnectedPlayers()
        {
            if (!NetworkSceneManager.IsServer)
                return;

            _pruneBuffer.Clear();
            foreach (KeyValuePair<int, TrackedPlayer> kvp in _trackedPlayers)
            {
                TrackedPlayer entry = kvp.Value;
                if (entry.Controller == null || (entry.LeftDetector == null && entry.RightDetector == null))
                    _pruneBuffer.Add(kvp.Key);
            }

            for (int i = 0; i < _pruneBuffer.Count; i++)
            {
                int key = _pruneBuffer[i];
                if (_trackedPlayers.TryGetValue(key, out TrackedPlayer entry))
                {
                    CleanupTrackedEntry(entry);
                    _trackedPlayers.Remove(key);
                }
            }

            try
            {
                HashSet<IPlayer> allPlayers = Player.AllPlayers;
                if (allPlayers == null)
                    return;

                foreach (IPlayer iplayer in allPlayers)
                {
                    if (iplayer is Player player && player != null && player.PlayerController != null)
                        RegisterPlayer(player.PlayerController);
                }
            }
            catch (Exception ex)
            {
                _log?.Warning($"[Punchy] Error reconciling connected players: {ex.Message}");
            }
        }

        public void NotifyDetectorDestroyed(PunchHandDetector detector)
        {
            if (detector == null)
                return;

            foreach (KeyValuePair<int, TrackedPlayer> kvp in _trackedPlayers)
            {
                TrackedPlayer entry = kvp.Value;
                if (entry.LeftDetector == detector)
                    entry.LeftDetector = null;
                if (entry.RightDetector == detector)
                    entry.RightDetector = null;
            }
        }

        public void ClearAll()
        {
            foreach (KeyValuePair<int, TrackedPlayer> kvp in _trackedPlayers)
                CleanupTrackedEntry(kvp.Value);

            _trackedPlayers.Clear();
            _pruneBuffer.Clear();
        }

        private PunchHandDetector AttachHandDetector(
            PlayerController pc,
            Controller controller,
            PunchHandSide side,
            HandCooldownTracker cooldownTracker)
        {
            if (controller == null)
                return null;

            PunchHandDetector detector = controller.gameObject.GetComponent<PunchHandDetector>();
            if (detector == null)
                detector = controller.gameObject.AddComponent<PunchHandDetector>();

            detector.Initialize(pc, controller, side, cooldownTracker, _config, _log);
            return detector;
        }

        private static void CleanupTrackedEntry(TrackedPlayer entry)
        {
            if (entry == null)
                return;

            if (entry.LeftDetector != null)
            {
                UnityEngine.Object.Destroy(entry.LeftDetector);
                entry.LeftDetector = null;
            }

            if (entry.RightDetector != null)
            {
                UnityEngine.Object.Destroy(entry.RightDetector);
                entry.RightDetector = null;
            }
        }
    }
}
