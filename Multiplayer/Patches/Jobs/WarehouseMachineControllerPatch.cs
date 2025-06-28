using DV.Logic.Job;
using DV.ThingTypes;
using HarmonyLib;
using Multiplayer.Components.Networking;
using Multiplayer.Components.Networking.Jobs;
using Multiplayer.Components.Networking.Train;
using Multiplayer.Networking.Data;
using static WarehouseMachineController;

namespace Multiplayer.Patches.Jobs;

[HarmonyPatch(typeof(WarehouseMachineController))]
public class WarehouseMachineControllerPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(WarehouseMachineController.Awake))]
    public static void Awake(WarehouseMachineController __instance)
    {
        __instance.gameObject.AddComponent<NetworkedWarehouseMachineController>();
    }

        if (!NetworkLifecycle.Instance.IsHost())
        {
            SendValidationRequest(__instance, WarehouseAction.Unload);
        }

    }

    [HarmonyPrefix]
    [HarmonyPatch("StartUnloadSequence")]
    public static bool StartUnloadSequence_Prefix(WarehouseMachineController __instance)
    {
        if (NetworkLifecycle.Instance.IsHost())
            return true;

        SendValidationRequest(__instance, WarehouseAction.Unload);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch("StartLoadSequence")]
    public static bool StartLoadSequence_Prefix(WarehouseMachineController __instance)
    {
        if (NetworkLifecycle.Instance.IsHost())
            return true;

        SendValidationRequest(__instance, WarehouseAction.Load);
        return false;
    }

    private static void SendValidationRequest(WarehouseMachineController machine, WarehouseAction action)
    {
        string id = machine?.warehouseMachine?.ID;
        var netController =  NetworkedWarehouseMachineController.GetFromWarehouseMachineController(machine);

        if (string.IsNullOrEmpty(id))
        {
            NetworkLifecycle.Instance.Client.LogError($"Failed to validate {action} for {machine?.name} at {machine?.warehouseTrackName}. Warehouse not found!");
            return;
        }

        if (netController == null)
        {
            NetworkLifecycle.Instance.Client.LogError($"Failed to find NetworkedWarehouseMachineController {machine?.warehouseTrackName}. Warehouse not found!");
            return;
        }

        NetworkLifecycle.Instance.Client.SendWarehouseRequest(action, netController.NetId);
    }
}
