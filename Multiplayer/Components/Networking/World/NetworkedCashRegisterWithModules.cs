using DV.CashRegister;
using DV.Interaction;
using DV.InventorySystem;
using Multiplayer.Networking.Data;
using Multiplayer.Networking.Packets.Common;
using Multiplayer.Utils;
using System;
using System.Collections;
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

    public static bool TryGet(CashRegisterWithModules cashRegister, out NetworkedCashRegisterWithModules networkedCashRegisterWithModules)
    {
        return cashRegisterToNetworkedCashRegister.TryGetValue(cashRegister, out networkedCashRegisterWithModules);
    }

    public static void InitialiseCashRegisters()
    {

        //Find all CashRegistersWithModules that are placed on the map
        //sort them by their hierarchy path for consistent ordering
        var registers = CashRegisterBase.allCashRegisters
            .OfType<CashRegisterWithModules>()
            .OrderBy(r => r.transform.position.x)
            .ThenBy(r => r.transform.position.y)
            .ThenBy(r => r.transform.position.z)
            .ToArray();

        Multiplayer.LogDebug(() => $"InitialiseCashRegisters() Found: {registers?.Length}");

        foreach (var register in registers)
        {
            var netRegister = register.GetOrAddComponent<NetworkedCashRegisterWithModules>();
            netRegister.CashRegister = register;

            if (netRegister.NetId == 0)
                netRegister.Awake();

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

    #region Unity

    protected override void Awake()
    {
        Multiplayer.LogDebug(()=>$"CashRegisterWithModules.Awake() {transform.GetObjectPath()}, {transform.position}, netId: {NetId}");

        if (NetId == 0)
            base.Awake();
    }

    protected override void OnDestroy()
    {
        cashRegisterToNetworkedCashRegister.Remove(CashRegister);
        base.OnDestroy();
    }
    #endregion

    #region Server
    public void Server_ProcessCashRegisterAction(ServerPlayer player, CommonCashRegisterWithModulesActionPacket packet)
    {
        float sqrDistance = (player.WorldPosition - transform.position).sqrMagnitude;
        bool success = false;

        NetworkLifecycle.Instance.Server?.LogDebug(() => $"Server_ProcessAction({player.Username}, {packet.Action}, {packet.Amount})");

        if (sqrDistance > GrabberRaycasterDV.FPS_INTERACTION_RANGE_SQR)
        {
            NetworkLifecycle.Instance.Server?.LogDebug(() => $"Server_ProcessAction({player.Username}, {packet.Action}, {packet.Amount}) {CashRegister.GetObjectPath()}. Player too far! Player pos: {player.WorldPosition}, register pos: {transform.position}, sqrMag: {sqrDistance}");
            return;
        }

        switch (packet.Action)
        {
            case CashRegisterAction.Cancel:
                if (CashRegister.cancelButton.InteractionAllowed)
                {
                    CashRegister?.Cancel();
                    success = true;
                }

                break;

            case CashRegisterAction.Buy:
                if (CashRegister.buyButton.InteractionAllowed)
                    success = CashRegister?.Buy() ?? false;

                break;

            case CashRegisterAction.SetFunds:
                double spend = 0;

                NetworkLifecycle.Instance.Server?.LogDebug(() => $"Server_ProcessAction({player.Username}, {packet.Action}, {packet.Amount}) Wallet: {Inventory.Instance.PlayerMoney}");
                if (packet.Amount > 0)
                {
                    if (Inventory.Instance.PlayerMoney >= packet.Amount)
                        spend = packet.Amount;
                    else
                        spend = Inventory.Instance.PlayerMoney;

                    success = Inventory.Instance.RemoveMoney(spend);

                    if(success)
                        CashRegister?.AddCash(spend);
                }
                else
                {
                    NetworkLifecycle.Instance.Server?.LogDebug(() => $"Server_ProcessAction({player.Username}, {packet.Action}, {packet.Amount}) amount negative!");
                }
                break;
        }

        if (success)
            NetworkLifecycle.Instance.Server.SendCashRegisterAction(packet);
        else
            NetworkLifecycle.Instance.Server.SendCashRegisterAction
                (
                    new CommonCashRegisterWithModulesActionPacket
                    {
                        NetId = NetId,
                        Action = CashRegisterAction.Reject,
                        Amount = CashRegister.DepositedCash
                    },
                    player.Peer
                );
    }

    #endregion

    #region Client

    public void Client_ProcessCashRegisterAction(CashRegisterAction action, double amount)
    {
        switch (action)
        {
            case CashRegisterAction.Cancel:

                if (isCancelling)
                    cancelAccepted = true;

                isCancelling = false;
                isBuying = false;

                CashRegister?.cancelAudio?.Play(transform.position, 1f, 1f, 0f, 1f, 500f, default, null, transform, false, 0f, null);

                break;

            case CashRegisterAction.Buy:

                if (isBuying)
                    buyAccepted = true;

                isCancelling = false;
                isBuying = false;

                CashRegister?.buyAudio?.Play(transform.position, 1f, 1f, 0f, 1f, 500f, default, null, transform, false, 0f, null);

                break;

            case CashRegisterAction.SetFunds:

                if (CashRegister.DepositedCash == 0 && amount > 0)
                {

                }
                break;

            case CashRegisterAction.Reject:
                isBuying = false;
                isCancelling = false;
               
                break;
        }
    }

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

    public void SetCash(double amount)
    {
        if (isBuying || isCancelling || NetworkLifecycle.Instance.IsProcessingPacket)
            return;

        NetworkLifecycle.Instance.Client.SendCashRegisterAction(NetId, CashRegisterAction.SetFunds, amount);
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
