using DV.CashRegister;
using HarmonyLib;
using Multiplayer.Components.Networking;
using Multiplayer.Components.Networking.World;
using Multiplayer.Utils;

namespace Multiplayer.Patches.World;

[HarmonyPatch(typeof(CashRegisterWithModules))]
public class CashRegisterWithModulesPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(CashRegisterWithModules.OnBuyPressed))]
    private static bool OnBuyPressed(CashRegisterWithModules __instance)
    {
        if (NetworkLifecycle.Instance.IsHost())
            return true;

        if (!NetworkedCashRegisterWithModules.TryGet(__instance, out var netCashRegister))
        {
            Multiplayer.LogWarning($"CashRegisterWithModules.OnBuyPressed({__instance.GetObjectPath()}) NetworkedCashRegisterWithModules not found!");
            return false;
        }

        CoroutineManager.Instance.StartCoroutine(netCashRegister.Buy());

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(CashRegisterWithModules.Cancel))]
    private static bool Cancel(CashRegisterWithModules __instance)
    {
        if (NetworkLifecycle.Instance.IsHost())
            return true;

        if (!NetworkedCashRegisterWithModules.TryGet(__instance, out var netCashRegister))
        {
            Multiplayer.LogWarning($"CashRegisterWithModules.Cancel({__instance.GetObjectPath()}) NetworkedCashRegisterWithModules not found!");
            return false;
        }

        CoroutineManager.Instance.StartCoroutine(netCashRegister.Cancel());

        return false;
    }
}

[HarmonyPatch(typeof(CashRegisterBase))]
public class CashRegisterBasePatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(CashRegisterBase.SetCash))]
    private static void SetCash(CashRegisterBase __instance, double amount)
    {
        if (__instance is not CashRegisterWithModules cashRegisterWithModules)
            return;

        Multiplayer.LogDebug(() => $"SetCash() {__instance.GetObjectPath()}, Deposited: {amount}");

        if (!NetworkedCashRegisterWithModules.TryGet(cashRegisterWithModules, out var netCashRegister))
            Multiplayer.LogWarning($"CashRegisterWithModules.SetCash({cashRegisterWithModules.GetObjectPath()}) NetworkedCashRegisterWithModules not found!");
        else
            netCashRegister.SetCash(amount);
    }
}
