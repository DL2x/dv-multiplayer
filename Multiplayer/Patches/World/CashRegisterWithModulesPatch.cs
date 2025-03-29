using DV.CashRegister;
using HarmonyLib;
using Multiplayer.Components.Networking.World;
using Multiplayer.Utils;

namespace Multiplayer.Patches.World;

[HarmonyPatch(typeof(CashRegisterWithModules))]
public class CashRegisterWithModulesPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(CashRegisterWithModules.Awake))]
    private static void Awake(CashRegisterWithModules __instance)
    {
        __instance.GetOrAddComponent<NetworkedCashRegisterWithModules>();
    }
}
