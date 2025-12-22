using HarmonyLib;
using Multiplayer.Components.Networking;
using Multiplayer.Utils;

namespace Multiplayer.Patches.World;

[HarmonyPatch(typeof(DisplayLoadingInfo), nameof(DisplayLoadingInfo.Start))]
public static class DisplayLoadingInfo_Start_Patch
{
    private static void Postfix(DisplayLoadingInfo __instance)
    {
        // Block world loading for cracked Steam builds by preventing the loading screen from finishing.
        if (GameVersionDetector.IsCracked)
        {
            WorldStreamingInit.LoadingFinished -= __instance.OnLoadingFinished;
            Multiplayer.LogError("Cracked Steam build detected: blocking world load (OnLoadingFinished will never be called).");
            return;
        }

        // Only block the vanilla "loading finished" handler when multiplayer is active.
        // Otherwise singleplayer save loading can get stuck on the loading screen.
        var lifecycle = NetworkLifecycle.Instance;
        if (lifecycle == null || (!lifecycle.IsClientRunning && !lifecycle.IsServerRunning))
            return;

        WorldStreamingInit.LoadingFinished -= __instance.OnLoadingFinished;
    }
}
