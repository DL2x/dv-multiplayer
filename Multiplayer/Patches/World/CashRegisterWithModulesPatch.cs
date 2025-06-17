using DV.CashRegister;
using HarmonyLib;
using Multiplayer.Components.Networking;
using Multiplayer.Components.Networking.World;
using Multiplayer.Networking.Packets.Common;
using Multiplayer.Utils;
using System;

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

    [HarmonyPostfix]
    [HarmonyPatch(nameof(CashRegisterWithModules.OnBuyPressed))]
    private static void OnBuyPressed_Postfix(CashRegisterWithModules __instance)
    {
        if (!NetworkLifecycle.Instance.IsHost())
            return;

        if (!NetworkedCashRegisterWithModules.TryGet(__instance, out var netCashRegister))
        {
            Multiplayer.LogWarning($"CashRegisterWithModules.OnBuyPressed_Postfix({__instance.GetObjectPath()}) NetworkedCashRegisterWithModules not found!");
            return;
        }

        // Send buy action to all clients
        NetworkLifecycle.Instance.Server.SendCashRegisterAction(new CommonCashRegisterWithModulesActionPacket
        {
            NetId = netCashRegister.NetId,
            Action = CashRegisterAction.Buy,
            Amount = __instance.DepositedCash
        });
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(CashRegisterWithModules.Cancel))]
    private static bool Cancel(CashRegisterWithModules __instance)
    {

        Multiplayer.LogDebug(()=>$"CashRegisterWithModules.Cancel({__instance.GetObjectPath()})\r\n{Environment.StackTrace}");

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

    [HarmonyPostfix]
    [HarmonyPatch(nameof(CashRegisterWithModules.Cancel))]
    private static void Cancel_Postfix(CashRegisterWithModules __instance)
    {
        if (!NetworkLifecycle.Instance.IsHost())
            return;

        if (!NetworkedCashRegisterWithModules.TryGet(__instance, out var netCashRegister))
        {
            Multiplayer.LogWarning($"CashRegisterWithModules.Cancel_Postfix({__instance.GetObjectPath()}) NetworkedCashRegisterWithModules not found!");
            return;
        }

        // Send cancel action to all clients
        NetworkLifecycle.Instance.Server.SendCashRegisterAction(new CommonCashRegisterWithModulesActionPacket
        {
            NetId = netCashRegister.NetId,
            Action = CashRegisterAction.Cancel,
            Amount = __instance.DepositedCash
        });
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

    [HarmonyPrefix]
    [HarmonyPatch(nameof(CashRegisterBase.OnEnable))]
    private static bool OnEnable(CashRegisterBase __instance)
    {
        if (__instance is not CashRegisterWithModules)
            return true;

        //prevent clients from clearing cash registers when loading
        return NetworkLifecycle.Instance.IsHost();
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(CashRegisterBase.OnDisable))]
    private static bool OnDisable(CashRegisterBase __instance)
    {
        if (__instance is not CashRegisterWithModules)
            return true;

        //prevent clients from clearing cash registers when loading the game or leaving the area
        __instance.StopAllCoroutines();
        return NetworkLifecycle.Instance.IsHost();
    }
}
