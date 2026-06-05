using HarmonyLib;

namespace Multiplayer.Patches.Headless;

[HarmonyPatch(typeof(SaveGameManager))]
public static class SaveGameManagerHeadlessPatch
{
    [HarmonyPatch("StashScreenshot")]
    [HarmonyPrefix]
    private static bool StashScreenshot_Prefix()
    {
        if (!global::Multiplayer.RuntimeConfiguration.IsHeadlessDedicated)
            return true;

        global::Multiplayer.Multiplayer.Log("Headless dedicated mode: skipping save screenshot stash");
        return false;
    }

    [HarmonyPatch("OnGamePausing")]
    [HarmonyPrefix]
    private static bool OnGamePausing_Prefix()
    {
        if (!global::Multiplayer.RuntimeConfiguration.IsHeadlessDedicated)
            return true;

        global::Multiplayer.Multiplayer.Log("Headless dedicated mode: skipping pause screenshot capture");
        return false;
    }
}
