using System;

namespace Punchy.Core
{
    /// <summary>
    /// Snapshot of ATT's replicated HandGestureController state for a single hand.
    /// </summary>
    public readonly struct FistPoseState
    {
        public bool HasGestureData { get; }
        public bool IsFreeGestureMode { get; }
        public bool HasExplicitGesture { get; }
        public int ActiveFingerMask { get; }
        public float GripAxis { get; }
        public float[] FingerCurls { get; }

        public FistPoseState(
            bool hasGestureData,
            bool isFreeGestureMode,
            bool hasExplicitGesture,
            int activeFingerMask,
            float gripAxis,
            float[] fingerCurls)
        {
            HasGestureData = hasGestureData;
            IsFreeGestureMode = isFreeGestureMode;
            HasExplicitGesture = hasExplicitGesture;
            ActiveFingerMask = activeFingerMask;
            GripAxis = gripAxis;
            FingerCurls = fingerCurls;
        }

        public static FistPoseState Unavailable =>
            new FistPoseState(false, false, false, 0, 0f, null);

        public static FistPoseState GradualGrip(float gripAxis) =>
            new FistPoseState(true, false, false, 0, gripAxis, null);

        public static FistPoseState ExplicitGesture(int activeFingerMask) =>
            new FistPoseState(true, false, true, activeFingerMask, 0f, null);

        public static FistPoseState FreeSkeleton(float[] fingerCurls) =>
            new FistPoseState(true, true, false, 0, 0f, fingerCurls);
    }

    /// <summary>
    /// Evaluates whether replicated VR hand input constitutes a closed fist, with graceful lag & fallback handling.
    /// </summary>
    public static class FistPoseEvaluator
    {
        public static bool EvaluateInstantClosedFist(
            in FistPoseState state,
            in PunchConfigSnapshot config,
            out bool usedFallback)
        {
            if (!config.RequireClosedFist || !state.HasGestureData)
            {
                usedFallback = true;
                return true;
            }

            usedFallback = false;

            if (state.IsFreeGestureMode)
            {
                float[] curls = state.FingerCurls;
                if (curls == null || curls.Length < 5)
                {
                    usedFallback = true;
                    return true;
                }

                float sum = 0f;
                int curledNonThumbCount = 0;

                for (int i = 0; i < 5; i++)
                {
                    float curl = Clamp01(curls[i]);
                    sum += curl;
                    if (i >= 1 && curl >= config.FistFreeCurlMinFingerThreshold)
                        curledNonThumbCount++;
                }

                float averageCurl = sum / 5f;
                return averageCurl >= config.FistFreeCurlAverageThreshold && curledNonThumbCount >= 3;
            }

            if (state.HasExplicitGesture)
                return state.ActiveFingerMask == 0;

            return Clamp01(state.GripAxis) >= config.FistGripThreshold;
        }

        public static bool EvaluateWithLagGrace(
            in FistPoseState state,
            double currentTime,
            ref double lastClosedFistTime,
            in PunchConfigSnapshot config,
            out bool usedFallback)
        {
            bool instantClosed = EvaluateInstantClosedFist(in state, in config, out usedFallback);

            if (usedFallback)
                return true;

            if (instantClosed)
            {
                lastClosedFistTime = currentTime;
                return true;
            }

            if (lastClosedFistTime >= 0d && config.FistPoseLagGraceSeconds > 0f)
            {
                double elapsed = currentTime - lastClosedFistTime;
                if (elapsed >= 0d && elapsed <= (double)config.FistPoseLagGraceSeconds + 1e-6d)
                    return true;
            }

            return false;
        }

        private static float Clamp01(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return 0f;
            if (value < 0f)
                return 0f;
            return value > 1f ? 1f : value;
        }
    }
}
