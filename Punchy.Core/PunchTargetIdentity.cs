namespace Punchy.Core
{
    public enum PunchRejectReason
    {
        None = 0,
        ModDisabled = 1,
        AttackerNotAlive = 2,
        HandHoldingItem = 3,
        ItemReleaseGrace = 4,
        FistNotClosed = 5,
        TeleportReset = 6,
        SpeedTooLow = 7,
        HandOnCooldown = 8,
        SelfHit = 9,
        TargetDeadOrInvalid = 10,
        PvPDisabled = 11
    }

    public readonly struct PunchTargetDescriptor
    {
        public int AttackerPlayerId { get; }
        public uint AttackerEntityRootId { get; }
        public int AttackerHealthId { get; }

        public int TargetHealthId { get; }
        public uint TargetEntityRootId { get; }
        public int TargetPlayerId { get; }
        public bool IsTargetPlayer { get; }
        public bool IsTargetInAttackerHierarchy { get; }
        public bool IsTargetDockedOnAttacker { get; }
        public bool IsTargetHeldByAttacker { get; }
        public bool IsTargetAliveOrBreakable { get; }

        public PunchTargetDescriptor(
            int attackerPlayerId,
            uint attackerEntityRootId,
            int attackerHealthId,
            int targetHealthId,
            uint targetEntityRootId,
            int targetPlayerId,
            bool isTargetPlayer,
            bool isTargetInAttackerHierarchy,
            bool isTargetDockedOnAttacker,
            bool isTargetHeldByAttacker,
            bool isTargetAliveOrBreakable)
        {
            AttackerPlayerId = attackerPlayerId;
            AttackerEntityRootId = attackerEntityRootId;
            AttackerHealthId = attackerHealthId;
            TargetHealthId = targetHealthId;
            TargetEntityRootId = targetEntityRootId;
            TargetPlayerId = targetPlayerId;
            IsTargetPlayer = isTargetPlayer;
            IsTargetInAttackerHierarchy = isTargetInAttackerHierarchy;
            IsTargetDockedOnAttacker = isTargetDockedOnAttacker;
            IsTargetHeldByAttacker = isTargetHeldByAttacker;
            IsTargetAliveOrBreakable = isTargetAliveOrBreakable;
        }
    }

    /// <summary>
    /// Enforces self-hit exclusion (body hierarchy, docked holsters, held pickups) and PvP gates.
    /// </summary>
    public static class PunchTargetFilter
    {
        public static bool IsValidTarget(
            in PunchTargetDescriptor target,
            bool modAllowPvP,
            bool serverPvPEnabled,
            out PunchRejectReason rejectReason)
        {
            if (target.IsTargetInAttackerHierarchy ||
                target.IsTargetDockedOnAttacker ||
                target.IsTargetHeldByAttacker ||
                (target.TargetHealthId != 0 && target.TargetHealthId == target.AttackerHealthId) ||
                (target.TargetEntityRootId != 0 && target.TargetEntityRootId == target.AttackerEntityRootId) ||
                (target.IsTargetPlayer && target.TargetPlayerId != 0 && target.TargetPlayerId == target.AttackerPlayerId))
            {
                rejectReason = PunchRejectReason.SelfHit;
                return false;
            }

            if (!target.IsTargetAliveOrBreakable)
            {
                rejectReason = PunchRejectReason.TargetDeadOrInvalid;
                return false;
            }

            if (!PunchDamageCalculator.EvaluatePvPAllowed(target.IsTargetPlayer, modAllowPvP, serverPvPEnabled))
            {
                rejectReason = PunchRejectReason.PvPDisabled;
                return false;
            }

            rejectReason = PunchRejectReason.None;
            return true;
        }
    }
}
