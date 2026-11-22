using System;
using HarmonyLib;
using Punchy.Core;

namespace Punchy
{
    /// <summary>
    /// Reads ATT's replicated HandGestureController state and empty-hand / interactor status safely on the server.
    /// </summary>
    public static class HandGestureBridge
    {
        private static readonly AccessTools.FieldRef<HandGestureController, HandGestureDefinition> CurrentGestureRef =
            CreateFieldRef<HandGestureDefinition>("currentGesture");

        private static readonly AccessTools.FieldRef<HandGestureController, GestureMap> GestureMapRef =
            CreateFieldRef<GestureMap>("gestureMap");

        private static readonly AccessTools.FieldRef<HandGestureController, float> GripAxisRef =
            CreateFieldRef<float>("gripAxis");

        private static readonly AccessTools.FieldRef<HandGestureController, bool> IsFreeGestureModeRef =
            CreateFieldRef<bool>("isFreeGestureMode");

        private static readonly AccessTools.FieldRef<HandGestureController, float[]> SkeletonInputRef =
            CreateFieldRef<float[]>("skeletonInput");

        private static AccessTools.FieldRef<HandGestureController, T> CreateFieldRef<T>(string fieldName)
        {
            try
            {
                return AccessTools.FieldRefAccess<HandGestureController, T>(fieldName);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Fail-safe empty-hand check: returns true if the controller is missing, interacting with anything, or holding a Pickup.
        /// </summary>
        public static bool IsHandHoldingOrInteracting(Controller controller)
        {
            if (controller == null)
                return true;

            Interactor interactor = controller.Interactor;
            if (interactor == null)
                return true;

            if (interactor.IsInteracting || interactor.InteractingWith != null)
                return true;

            if (controller.TryGetHeldPickup(out Pickup heldPickup) && heldPickup != null)
                return true;

            return false;
        }

        /// <summary>
        /// Captures ATT's replicated fist pose state from HandGestureController.
        /// Falls back to FistPoseState.Unavailable if the component or its fields are inaccessible.
        /// </summary>
        public static FistPoseState ReadPoseState(Controller controller)
        {
            if (controller == null)
                return FistPoseState.Unavailable;

            HandGestureController gestureController = controller.Hand != null
                ? controller.Hand.GestureController
                : null;

            if (gestureController == null)
                gestureController = controller.GetComponent<HandGestureController>();

            if (gestureController == null)
                gestureController = controller.GetControllerExtension<HandGestureController>();

            if (gestureController == null ||
                CurrentGestureRef == null ||
                GestureMapRef == null ||
                GripAxisRef == null ||
                IsFreeGestureModeRef == null ||
                SkeletonInputRef == null)
            {
                return FistPoseState.Unavailable;
            }

            try
            {
                bool isFreeMode = IsFreeGestureModeRef(gestureController);
                float[] skeletonInput = SkeletonInputRef(gestureController);
                float gripAxis = GripAxisRef(gestureController);
                HandGestureDefinition currentGesture = CurrentGestureRef(gestureController);
                GestureMap gestureMap = GestureMapRef(gestureController);
                HandGestureDefinition idleGesture = gestureMap != null ? gestureMap.IdleGesture : null;

                bool hasExplicitGesture = currentGesture != null &&
                                          idleGesture != null &&
                                          currentGesture != idleGesture;

                int activeFingerMask = currentGesture != null ? (int)currentGesture.ActiveFingers : 0;

                return new FistPoseState(
                    hasGestureData: true,
                    isFreeGestureMode: isFreeMode,
                    hasExplicitGesture: hasExplicitGesture,
                    activeFingerMask: activeFingerMask,
                    gripAxis: gripAxis,
                    fingerCurls: skeletonInput);
            }
            catch
            {
                return FistPoseState.Unavailable;
            }
        }
    }
}
