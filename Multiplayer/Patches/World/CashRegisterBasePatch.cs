using DV.CashRegister;
using HarmonyLib;
using Multiplayer.Components.Networking;
using Multiplayer.Components.Networking.World;
using Multiplayer.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace Multiplayer.Patches.World;

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
        {
            Multiplayer.LogWarning($"Attempting to SetCash, but NetworkedCashRegisterWithModules not found for {cashRegisterWithModules.GetObjectPath()}");
            return;
        }

        if (netCashRegister.IsShopRegister)
            return;

        netCashRegister.SetCash(amount);
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(CashRegisterBase.OnEnable))]
    private static bool OnEnable(CashRegisterBase __instance)
    {
        //Multiplayer.LogDebug(() => $"CashRegisterBase.OnEnable({__instance.GetObjectPath()}) {__instance.GetType()}");
        if (__instance is not CashRegisterWithModules)
            return true;

        return NetworkLifecycle.Instance.IsHost();
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(CashRegisterBase.OnDisable))]
    private static bool OnDisable(CashRegisterBase __instance)
    {
        //Multiplayer.LogDebug(() => $"CashRegisterBase.OnDisable({__instance.GetObjectPath()}) {__instance.GetType()}");
        if (__instance is not CashRegisterWithModules)
            return true;

        // Prevent clients from cancelling/returning cash on cash registers when loading the game or leaving the area
        __instance.StopAllCoroutines();
        return NetworkLifecycle.Instance.IsHost();
    }
}

