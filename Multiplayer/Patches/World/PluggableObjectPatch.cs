using HarmonyLib;
using Multiplayer.Components.Networking;

namespace Multiplayer.Patches.World;

[HarmonyPatch(typeof(PluggableObject))]
public static class PluggableObjectPatch
{
    [HarmonyPatch(nameof(PluggableObject.Awake))]
    [HarmonyPrefix]
    public static bool Awake(PluggableObject __instance)
    {
        if (NetworkLifecycle.Instance.IsHost())
            return true;

        // Allow the client to setup the plug, but don't allow `InstantSnapTo(this.startAttachedTo);` to be called
        __instance.CheckInitialization();
        return false;
    }

    [HarmonyPatch(nameof(PluggableObject.InstantSnapTo))]
    [HarmonyPrefix]
    public static bool InstantSnapTo(PluggableObject __instance)
    {
        return false;
    }
}
