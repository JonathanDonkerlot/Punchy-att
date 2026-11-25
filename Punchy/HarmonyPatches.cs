using System;
using System.Reflection;
using HarmonyLib;
using MelonLoader;

namespace Punchy
{
    /// <summary>
    /// Harmony lifecycle hooks for PlayerController linking, unlinking, and destruction.
    /// </summary>
    public static class HarmonyPatches
    {
        public static bool IsHooked { get; private set; }

        public static void Apply(HarmonyLib.Harmony harmony, MelonLogger.Instance log)
        {
            if (harmony == null)
                return;

            try
            {
                MethodInfo linkMethod = AccessTools.Method(
                    typeof(PlayerController),
                    "LinkToPlayer",
                    new[] { typeof(Player), typeof(PlayerInput), typeof(PlayerInput) });

                MethodInfo unlinkMethod = AccessTools.Method(
                    typeof(PlayerController),
                    "UnlinkFromPlayer",
                    Type.EmptyTypes);

                MethodInfo destroyMethod = AccessTools.Method(
                    typeof(PlayerController),
                    "OnDestroy",
                    Type.EmptyTypes);

                if (linkMethod == null || unlinkMethod == null || destroyMethod == null)
                {
                    log?.Error("[Punchy] Failed to locate one or more PlayerController lifecycle methods for Harmony patching.");
                    IsHooked = false;
                    return;
                }

                harmony.Patch(
                    linkMethod,
                    postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Postfix_LinkToPlayer)));

                harmony.Patch(
                    unlinkMethod,
                    prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Prefix_UnlinkFromPlayer)));

                harmony.Patch(
                    destroyMethod,
                    prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Prefix_OnDestroy)));

                IsHooked = true;
                log?.Msg("[Punchy] Harmony patches attached: LinkToPlayer, UnlinkFromPlayer, OnDestroy.");
            }
            catch (Exception ex)
            {
                IsHooked = false;
                log?.Error($"[Punchy] Exception applying Harmony patches: {ex}");
            }
        }

        public static void Postfix_LinkToPlayer(PlayerController __instance)
        {
            try
            {
                if (__instance != null && (NetworkSceneManager.IsServer || PunchRagdollBridge.IsAvailable))
                    PunchServerManager.Instance?.RegisterPlayer(__instance);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[Punchy] Error in Postfix_LinkToPlayer: {ex.Message}");
            }
        }

        public static void Prefix_UnlinkFromPlayer(PlayerController __instance)
        {
            try
            {
                if (__instance != null)
                    PunchServerManager.Instance?.UnregisterPlayer(__instance);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[Punchy] Error in Prefix_UnlinkFromPlayer: {ex.Message}");
            }
        }

        public static void Prefix_OnDestroy(PlayerController __instance)
        {
            try
            {
                if (__instance != null)
                    PunchServerManager.Instance?.UnregisterPlayer(__instance);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[Punchy] Error in Prefix_OnDestroy: {ex.Message}");
            }
        }
    }
}
