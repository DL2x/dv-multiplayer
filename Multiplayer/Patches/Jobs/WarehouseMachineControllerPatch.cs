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
    [HarmonyPatch("StartLoadSequence")]
    public static void StartLoadSequence_Prefix(WarehouseMachineController __instance)
    {
        __instance.displayTrainInRangeText.text = __instance.warehouseMachine.ID;

        if (!NetworkLifecycle.Instance.IsHost())
        {
            SendValidationRequest(__instance, WarehouseAction.Load);
        }

    }

    private static void SendValidationRequest(WarehouseMachineController machine,WarehouseAction action)
    {
        //find the current station we're at
        if (!string.IsNullOrEmpty(machine.warehouseTrackName))
        {
            string id = machine.warehouseMachine.ID;

            NetworkLifecycle.Instance.Client.SendWarehouseRequest(action, id);
            //CoroutineManager.Instance.StartCoroutine(AwaitResponse(machine, action));
        }
        else
        {
            NetworkLifecycle.Instance.Client.LogError($"Failed to validate {action} for {machine.warehouseMachine.ID}. Warehouse not found!");
        }
    }
}
