using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DV.Logic.Job;
using Multiplayer.Components.Networking.World;
using Multiplayer.Networking.Data;
using Newtonsoft.Json.Linq;
using UnityEngine;


namespace Multiplayer.Components.Networking.Jobs;

public class NetworkedWarehouseMachineController
{
    public static WarehouseMachineController FindFomID(string ID)
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
}
