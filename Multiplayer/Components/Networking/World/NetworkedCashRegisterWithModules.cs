using DV.CashRegister;
using DV.Interaction;
using DV.InventorySystem;
using Multiplayer.Networking.Data;
using Multiplayer.Networking.Packets.Common;
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
    bool isBuying;
    bool buyAccepted;

    bool isCancelling;
    bool cancelAccepted;

    #endregion

    #region Common Variables
    CashRegisterWithModules CashRegister;
    #endregion

    public IEnumerator Buy()
    {
        if (isBuying || isCancelling)
            yield break;

        DisableInteraction();

        NetworkLifecycle.Instance.Client.SendCashRegisterAction(NetId, CashRegisterAction.Buy);

        isBuying = true;
        buyAccepted = false;
        float timeOut = Time.time + NetworkLifecycle.Instance.Client.RPC_Timeout;

        yield return new WaitUntil(() => Time.time >= timeOut || isBuying == false);

        if (!buyAccepted)
            CashRegister?.cancelAudio?.Play(transform.position, 1f, 1f, 0f, 1f, 500f, default, null, transform, false, 0f, null);

        isBuying = false;
        buyAccepted = false;

        EnableInteraction();
    }

    public IEnumerator Cancel()
    {
        if (isBuying || isCancelling)
            yield break;

        DisableInteraction();

        NetworkLifecycle.Instance.Client.SendCashRegisterAction(NetId, CashRegisterAction.Cancel);
        isCancelling = true;
        cancelAccepted = false;
        float timeOut = Time.time + NetworkLifecycle.Instance.Client.RPC_Timeout;

        yield return new WaitUntil(() => Time.time >= timeOut || isCancelling == false);

        if (cancelAccepted)
            CashRegister?.cancelAudio?.Play(transform.position, 1f, 1f, 0f, 1f, 500f, default, null, transform, false, 0f, null);

        isCancelling = false;
        cancelAccepted = false;

        EnableInteraction();
    }

    public void SetCash()
    {
        if (isBuying || isCancelling)
            return;

        NetworkLifecycle.Instance.Client.SendCashRegisterAction(NetId, CashRegisterAction.SetFunds, CashRegister.DepositedCash);
    }

    private void DisableInteraction()
    {
        CashRegister.buyButton.InteractionAllowed = false;
        CashRegister.cancelButton.InteractionAllowed = false;
    }

    private void EnableInteraction()
    {
        CashRegister.buyButton.InteractionAllowed = true;
        CashRegister.cancelButton.InteractionAllowed = true;
    }

    #endregion

    #region Common

    #endregion
}
