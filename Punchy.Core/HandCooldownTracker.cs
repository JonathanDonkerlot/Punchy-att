using System;

namespace Punchy.Core
{
    /// <summary>
    /// Tracks independent Left and Right hand hit cooldowns, item-release grace windows, and fist pose lag timestamps.
    /// </summary>
    public sealed class HandCooldownTracker
    {
        private const double TimeBoundaryEpsilon = 1e-6d;

        private struct HandState
        {
            public double LastHitTime;
            public double LastSurfaceHitTime;
            public double LastHeldItemTime;
            public double LastClosedFistTime;

            public static HandState CreateInitial() => new HandState
            {
                LastHitTime = double.NegativeInfinity,
                LastSurfaceHitTime = double.NegativeInfinity,
                LastHeldItemTime = double.NegativeInfinity,
                LastClosedFistTime = -1d
            };
        }

        private HandState _left = HandState.CreateInitial();
        private HandState _right = HandState.CreateInitial();

        public void ResetAll()
        {
            _left = HandState.CreateInitial();
            _right = HandState.CreateInitial();
        }

        public void ResetHand(PunchHandSide hand)
        {
            if (hand == PunchHandSide.Left)
                _left = HandState.CreateInitial();
            else
                _right = HandState.CreateInitial();
        }

        public void RecordHoldingState(PunchHandSide hand, bool isHoldingItem, double currentTime)
        {
            if (!isHoldingItem)
                return;

            if (hand == PunchHandSide.Left)
                _left.LastHeldItemTime = currentTime;
            else
                _right.LastHeldItemTime = currentTime;
        }

        public bool IsInItemReleaseGrace(PunchHandSide hand, double currentTime, float graceSeconds)
        {
            if (graceSeconds <= 0f)
                return false;

            double lastHeld = hand == PunchHandSide.Left ? _left.LastHeldItemTime : _right.LastHeldItemTime;
            if (double.IsNegativeInfinity(lastHeld))
                return false;

            double elapsed = currentTime - lastHeld;
            return elapsed >= 0d && (elapsed + TimeBoundaryEpsilon) < graceSeconds;
        }

        public bool IsOnCooldown(PunchHandSide hand, double currentTime, float cooldownSeconds)
        {
            if (cooldownSeconds <= 0f)
                return false;

            double lastHit = hand == PunchHandSide.Left ? _left.LastHitTime : _right.LastHitTime;
            if (double.IsNegativeInfinity(lastHit))
                return false;

            double elapsed = currentTime - lastHit;
            return elapsed >= 0d && (elapsed + TimeBoundaryEpsilon) < cooldownSeconds;
        }

        public void RecordHit(PunchHandSide hand, double currentTime)
        {
            if (hand == PunchHandSide.Left)
                _left.LastHitTime = currentTime;
            else
                _right.LastHitTime = currentTime;
        }

        public bool IsOnSurfaceCooldown(PunchHandSide hand, double currentTime, float cooldownSeconds)
        {
            if (cooldownSeconds <= 0f)
                return false;

            double lastSurfaceHit = hand == PunchHandSide.Left ? _left.LastSurfaceHitTime : _right.LastSurfaceHitTime;
            if (double.IsNegativeInfinity(lastSurfaceHit))
                return false;

            double elapsed = currentTime - lastSurfaceHit;
            return elapsed >= 0d && (elapsed + TimeBoundaryEpsilon) < cooldownSeconds;
        }

        public void RecordSurfaceHit(PunchHandSide hand, double currentTime)
        {
            if (hand == PunchHandSide.Left)
                _left.LastSurfaceHitTime = currentTime;
            else
                _right.LastSurfaceHitTime = currentTime;
        }

        public ref double GetLastClosedFistTimeRef(PunchHandSide hand)
        {
            if (hand == PunchHandSide.Left)
                return ref _left.LastClosedFistTime;
            return ref _right.LastClosedFistTime;
        }
    }
}
