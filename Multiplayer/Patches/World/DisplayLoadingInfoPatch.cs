using HarmonyLib;
using Multiplayer.Components.Networking;

namespace Multiplayer.Patches.World;

[HarmonyPatch(typeof(DisplayLoadingInfo), nameof(DisplayLoadingInfo.Start))]
public static class DisplayLoadingInfo_Start_Patch
{
    private static void Postfix(DisplayLoadingInfo __instance)
    {
        // Only block the vanilla "loading finished" handler when multiplayer is active.
        // Otherwise singleplayer save loading can get stuck on the loading screen.
        var lifecycle = NetworkLifecycle.Instance;
        if (lifecycle == null || (!lifecycle.IsClientRunning && !lifecycle.IsServerRunning))
            return;

        WorldStreamingInit.LoadingFinished -= __instance.OnLoadingFinished;
    }
}
