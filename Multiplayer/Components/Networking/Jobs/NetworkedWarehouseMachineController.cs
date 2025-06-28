using DV.Logic.Job;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using Multiplayer.Components.Networking.Train;
using Multiplayer.Networking.Data;
using Multiplayer.Networking.Packets.Clientbound.Jobs;
using System.Collections.Generic;
using static WarehouseMachineController;



namespace Multiplayer.Components.Networking.Jobs;

public class NetworkedWarehouseMachineController : IdMonoBehaviour<ushort, NetworkedWarehouseMachineController>
{
    #region Lookup Cache
    private static readonly Dictionary<WarehouseMachineController, NetworkedWarehouseMachineController> warehouseMachineControllerToNetworked = [];
    private static readonly Dictionary<WarehouseMachine, NetworkedWarehouseMachineController> warehouseMachineToNetworked = [];

    public static bool Get(ushort netId, out NetworkedWarehouseMachineController obj)
    {
        bool b = Get(netId, out IdMonoBehaviour<ushort, NetworkedWarehouseMachineController> rawObj);
        obj = (NetworkedWarehouseMachineController)rawObj;
        return b;
    }

    public static NetworkedWarehouseMachineController GetFromWarehouseMachineController(WarehouseMachineController warehouseMachineController)
    {
        warehouseMachineControllerToNetworked.TryGetValue(warehouseMachineController, out var netWarehouseMachineController);
        return netWarehouseMachineController;
    }

    public static NetworkedWarehouseMachineController GetFromWarehouseMachine(WarehouseMachine warehouseMachine)
    {
        //fast path lookup
        if (warehouseMachineToNetworked.TryGetValue(warehouseMachine, out NetworkedWarehouseMachineController networkedWarehouseMachineController))
            return networkedWarehouseMachineController;

        //cache miss, try to find parent WarehouseMachineController
        var warehouseMachineController = GetFomId(warehouseMachine.ID);
        if (warehouseMachineController != null)
        {
            //Warehouse Machine Controller found, check for NetworkedWarehouseMachineController
            networkedWarehouseMachineController = GetFromWarehouseMachineController(warehouseMachineController);
            if (networkedWarehouseMachineController != null)
                warehouseMachineToNetworked[warehouseMachine] = GetFromWarehouseMachineController(warehouseMachineController);
        }

        return networkedWarehouseMachineController;
    }

    private static WarehouseMachineController GetFomId(string ID)
    {
        foreach (var warehouse in WarehouseMachineController.allControllers)
        {
            if (warehouse.warehouseMachine.ID == ID)
            {
                return warehouse;
            }
        }
        return null;
    }

    #endregion
    protected override bool IsIdServerAuthoritative => false;

    public string Id => WarehouseMachine?.ID;
    public WarehouseMachineController WarehouseMachineController { get; private set; }
    public WarehouseMachine WarehouseMachine => WarehouseMachineController?.warehouseMachine;

    protected override void Awake()
    {
        base.Awake();
        WarehouseMachineController = GetComponent<WarehouseMachineController>();
        warehouseMachineControllerToNetworked[WarehouseMachineController] = this;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        warehouseMachineControllerToNetworked.Remove(WarehouseMachineController);

        if (WarehouseMachineController.warehouseMachine != null)
            warehouseMachineToNetworked.Remove(WarehouseMachineController.warehouseMachine);
    }

    public void ServerProcessWarehouseAction(WarehouseAction action)
    {
        Multiplayer.LogDebug(() => $"ServerProcessWarehouseAction() {Id}, Action Type: {action}");
        switch (action)
        {
            case WarehouseAction.Load:
                WarehouseMachineController.StartLoadSequence();
                break;

            case WarehouseAction.Unload:
                WarehouseMachineController.StartUnloadSequence();
                break;
        }
    }
}
