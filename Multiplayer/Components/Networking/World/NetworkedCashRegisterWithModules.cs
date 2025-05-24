using DV.CashRegister;
using Multiplayer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Multiplayer.Components.Networking.World;

public class NetworkedCashRegisterWithModules : IdMonoBehaviour<ushort, NetworkedCashRegisterWithModules>
{
    #region Lookup Cache
    private static readonly Dictionary<CashRegisterWithModules, NetworkedCashRegisterWithModules> cashRegisterToNetworkedCashRegister = [];

    public static bool Get(ushort netId, out NetworkedCashRegisterWithModules obj)
    {
        bool b = Get(netId, out IdMonoBehaviour<ushort, NetworkedCashRegisterWithModules> rawObj);
        obj = (NetworkedCashRegisterWithModules)rawObj;
        return b;
    }

    public static void InitialiseCashRegisters()
    {

        //Find all CashRegistersWithModules that are placed on the map
        //sort them by their hierarchy path for consistent ordering
        var registers = Resources.FindObjectsOfTypeAll<CashRegisterWithModules>()
            .Where(p => p.transform.parent != null)
            .OrderBy(p => p.GetObjectPath(), StringComparer.InvariantCulture)
            .ToArray();

        Multiplayer.LogDebug(() => $"InitialiseCashRegisters() Found: {registers?.Length}");

        foreach (var register in registers)
        {
            var netRegister = register.GetOrAddComponent<NetworkedCashRegisterWithModules>();
            netRegister.Register = register;

            cashRegisterToNetworkedCashRegister[register] = netRegister;

            Multiplayer.LogDebug(() => $"InitialiseCashRegisters() Register: {register?.GetObjectPath()}, netId: {netRegister.NetId}");
        }
    }

    #endregion

    protected override bool IsIdServerAuthoritative => false;

    #region Server Variables

    #endregion

    #region Client Variables

    #endregion

    #region Common Variables
    CashRegisterWithModules Register;
    #endregion
}
