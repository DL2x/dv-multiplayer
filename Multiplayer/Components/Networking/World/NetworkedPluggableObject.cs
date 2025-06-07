using DV.CabControls;
using DV.Interaction;
using Multiplayer.Components.Networking.Player;
using Multiplayer.Components.Networking.Train;
using Multiplayer.Networking.Data;
using Multiplayer.Networking.Packets.Common;
using Multiplayer.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Multiplayer.Components.Networking.World;

public class NetworkedPluggableObject : IdMonoBehaviour<ushort, NetworkedPluggableObject>
{
    #region Lookup Cache
    private static readonly Dictionary<NetworkedPluggableObject, NetworkedPitStopStation> plugToStation = [];
    public static bool Get(ushort netId, out NetworkedPluggableObject obj)
    {
        bool b = Get(netId, out IdMonoBehaviour<ushort, NetworkedPluggableObject> rawObj);
        obj = (NetworkedPluggableObject)rawObj;
        return b;
    }
    #endregion

    protected override bool IsIdServerAuthoritative => false;

    #region Server Variables
    public PlugInteractionType CurrentInteraction {  get; set; }
    public ServerPlayer HeldBy { get; private set; }
    public ushort TrainCarNetId { get; private set; }
    public bool IsConnectedLeft { get; private set; }

    #endregion
    public PluggableObject PluggableObject { get; private set; }
    public NetworkedPitStopStation Station { get; private set; }

    private GrabHandlerGizmoItem grabHandler;

    private bool handlersInitialised = false;

    private byte playerHolding = 0;
    private bool isGrabbed = false;

    private bool Refreshed = false;

    #region Unity
    protected override void Awake()
    {
        if (NetId == 0)
            base.Awake();

        PluggableObject = GetComponent<PluggableObject>();
        Multiplayer.LogDebug(() => $"NetworkedPluggableObject.Awake() {PluggableObject?.controlBase?.spec?.name}, {transform.parent.name}");
        Multiplayer.LogDebug(() => $"NetworkedPluggableObject.Awake() {this.GetObjectPath()}, netId: {NetId}");

        if (NetworkLifecycle.Instance.IsHost())
            Refreshed = true;
    }

    protected IEnumerator Start()
    {
        Multiplayer.LogDebug(() => $"NetworkedPluggableObject.Start() {PluggableObject?.controlBase?.spec?.name}, {transform.parent.name}");
        yield return new WaitUntil(() => PluggableObject?.controlBase != null);

        Multiplayer.LogDebug(() => $"NetworkedPluggableObject.Start() Controlbase {PluggableObject?.controlBase?.spec?.name}, {transform.parent.name}");

        grabHandler = this.GetComponent<GrabHandlerGizmoItem>();

        PluggableObject.controlBase.Grabbed += OnGrabbed;
        PluggableObject.controlBase.Ungrabbed += OnUngrabbed;
        PluggableObject.PluggedIn += OnPlugged;

        handlersInitialised = true;
    }

    protected override void OnDestroy()
    {
        if (UnloadWatcher.isUnloading)
            plugToStation.Clear();
        else
            plugToStation.Remove(this);

        if (PluggableObject?.controlBase != null && handlersInitialised)
        {
            PluggableObject.controlBase.Grabbed -= OnGrabbed;
            PluggableObject.controlBase.Ungrabbed -= OnUngrabbed;
            PluggableObject.PluggedIn -= OnPlugged;
        }

        base.OnDestroy();
    }
    #endregion

    #region Server

    public bool ValidateInteraction(CommonPitStopPlugInteractionPacket packet, ServerPlayer player)
    {
        PlugInteractionType interactionType = (PlugInteractionType)packet.InteractionType;
        //todo: implement validation code (player distance, player interacting, etc.)

        //validate and update
        CurrentInteraction = interactionType;

        if (interactionType == PlugInteractionType.DockSocket)
        {
            TrainCarNetId = packet.TrainCarNetId;
            IsConnectedLeft = packet.IsLeftSide;
            HeldBy = null;
        }
        else
        {
            HeldBy = null;
            if (interactionType == PlugInteractionType.DockHome)
            {
                //todo
            }
            else if (interactionType == PlugInteractionType.Dropped)
            {
                //todo
            }
            else if (interactionType == PlugInteractionType.PickedUp)
            {
                HeldBy = player;
            }
        }

        return true;
    }

    #endregion

    #region Common

    public void ProcessPacket(CommonPitStopPlugInteractionPacket packet)
    {
        var interaction = (PlugInteractionType)packet.InteractionType;
        ProcessInteraction(interaction, packet.PlayerId, packet.TrainCarNetId, packet.IsLeftSide, packet.Position, packet.Rotation);
    }

    public void ProcessBulkUpdate(PitStopPlugData data)
    {
        var interaction = data.State;
        ProcessInteraction(interaction, data.PlayerId, data.TrainCarNetId, data.IsLeftSide, data.Position, data.Rotation);
        Refreshed = true;
    }

    public void ProcessInteraction(PlugInteractionType interaction, byte playerId, ushort trainNetId, bool isLeftSide, Vector3? newPosition, Quaternion? newRotation)
    {

        bool result;

        NetworkedPlayer player = null;

        switch (interaction)
        {
            case PlugInteractionType.Rejected:
                //todo implement rejection
                break;

            case PlugInteractionType.PickedUp:
                //Handle the picked up state
                isGrabbed = true;
                playerHolding = playerId;
                PluggableObject.controlGrabbed = true;
                BlockInteraction(true);

                PluggableObject.Unplug();

                Multiplayer.LogDebug(() => $"ProcessPacket() NetId: {NetId}, Picked Up, player: {playerHolding}");

                //attach to a player
                if (NetworkLifecycle.Instance.IsClientRunning &&
                    NetworkLifecycle.Instance.Client.ClientPlayerManager.TryGetPlayer(playerHolding, out player))
                {
                    var target = grabHandler?.customGrabAnchor?.GetGrabAnchor();
                    player.HoldItem(gameObject, target?.localPos, target?.localRot);
                }
                break;

            case PlugInteractionType.Dropped:
                Multiplayer.LogDebug(() => $"ProcessPacket() NetId: {NetId}, Dropped");

                if (isGrabbed)
                {
                    if (NetworkLifecycle.Instance.IsClientRunning &&
                    NetworkLifecycle.Instance.Client.ClientPlayerManager.TryGetPlayer(playerHolding, out player))
                    {
                        player.DropItem();
                    }
                }

                isGrabbed = false;
                playerHolding = 0;
                PluggableObject.controlGrabbed = false;
                BlockInteraction(false);

                PluggableObject.Unplug();

                if (newPosition == null || newRotation == null)
                    return;

                transform.position = (Vector3)newPosition + WorldMover.currentMove;
                transform.rotation = (Quaternion)newRotation;

                break;

            case PlugInteractionType.DockHome:
                Multiplayer.LogDebug(() => $"ProcessPacket() NetId: {NetId}, DockHome");

                if (isGrabbed)
                {
                    if (NetworkLifecycle.Instance.IsClientRunning &&
                    NetworkLifecycle.Instance.Client.ClientPlayerManager.TryGetPlayer(playerHolding, out player))
                    {
                        player.DropItem();
                    }
                }

                isGrabbed = false;
                playerHolding = 0;
                PluggableObject.controlGrabbed = false;
                BlockInteraction(false);

                PluggableObject.Unplug();

                result = PluggableObject.InstantSnapTo(PluggableObject.startAttachedTo);
                Multiplayer.LogDebug(() => $"ProcessPacket() NetId: {NetId}, DockHome, result: {result}");
                break;

            case PlugInteractionType.DockSocket:
                Multiplayer.LogDebug(() => $"ProcessPacket() NetId: {NetId}, DockSocket, trainCar: {trainNetId}, isLeft: {isLeftSide}");

                if (isGrabbed)
                {
                    if (NetworkLifecycle.Instance.IsClientRunning &&
                    NetworkLifecycle.Instance.Client.ClientPlayerManager.TryGetPlayer(playerHolding, out player))
                    {
                        player.DropItem();
                    }
                }

                if (NetworkedTrainCar.GetTrainCar(trainNetId, out var trainCar))
                {
                    isGrabbed = false;
                    playerHolding = 0;
                    PluggableObject.controlGrabbed = false;
                    BlockInteraction(false);

                    PluggableObject.Unplug();

                    var sockets = trainCar.GetComponentsInChildren<PlugSocket>();
                    if (isLeftSide)
                    {
                        result = PluggableObject.InstantSnapTo(sockets[0]);
                        Multiplayer.LogDebug(() => $"ProcessPacket() NetId: {NetId}, DockSocket, trainCar: {trainNetId}, isLeft: {isLeftSide}, result: {result}");
                    }
                    else
                    {
                        result = PluggableObject.InstantSnapTo(sockets[1]);
                        Multiplayer.LogDebug(() => $"ProcessPacket() NetId: {NetId}, DockSocket, trainCar: {trainNetId}, isLeft: {isLeftSide}, result: {result}");
                    }
                }
                else
                {
                    Multiplayer.LogDebug(() => $"ProcessPacket() NetId: {NetId}, DockSocket, trainCar: {trainNetId}. TrainCar not found!");
                }
                break;
        }
    }

    private void BlockInteraction(bool block)
    {
        if (block)
        {
            PluggableObject.DisableStandaloneComponents();
            PluggableObject.DisableColliders();
        }
        else
            PluggableObject.EnableStandaloneComponents();
            PluggableObject.EnableColliders();
    }

    public void InitPitStop(NetworkedPitStopStation netPitStop)
    {
        if (NetId == 0)
            base.Awake();

        if(plugToStation.TryGetValue(this, out _))
        {
            Multiplayer.LogWarning($"Lookup cache 'plugToStation' already contains NetworkedPitStopStation \"{netPitStop?.StationName}\", skipping Init");
            return;
        }

        Station = netPitStop;
        plugToStation.Add(this, netPitStop);
    }
    #endregion

    #region Client
    private void OnGrabbed(ControlImplBase control)
    {
        Multiplayer.LogDebug(() => $"NetworkedPluggableObject.OnGrabbed() pre [{transform.parent.name}, {NetId}] station: {Station?.StationName}");

        if (NetworkLifecycle.Instance.IsProcessingPacket)
            return;

        Multiplayer.LogDebug(() => $"NetworkedPluggableObject.OnGrabbed() post [{transform.parent.name}, {NetId}] station: {Station?.StationName}");

        //Prevent new players/players entering the area from sending packets until initalised
        if (!Refreshed)
            return;

        Multiplayer.LogDebug(() => $"NetworkedPluggableObject.OnGrabbed() [{transform.parent.name}, {NetId}] station: {Station?.StationName}");
        NetworkLifecycle.Instance.Client?.SendPitStopPlugInteractionPacket(NetId, PlugInteractionType.PickedUp);
    }

    private void OnUngrabbed(ControlImplBase control)
    {
        if (NetworkLifecycle.Instance.IsProcessingPacket)
            return;

        //Prevent new players/players entering the area from sending packets until initalised
        if (!Refreshed)
            return;

        Multiplayer.LogDebug(() => $"NetworkedPluggableObject.OnUngrabbed() [{transform.parent.name}, {NetId}] station: {Station?.StationName}");
        NetworkLifecycle.Instance.Client?.SendPitStopPlugInteractionPacket(NetId, PlugInteractionType.Dropped, transform.position - WorldMover.currentMove, transform.rotation);
    }

    private void OnPlugged(PluggableObject plug, PlugSocket socket)
    {
        if (NetworkLifecycle.Instance.IsProcessingPacket)
            return;

        //Prevent new players/players entering the area from sending packets until initalised
        if (!Refreshed)
            return;

        Multiplayer.LogDebug(() => $"NetworkedPluggableObject.OnPlugged() [{transform.parent.name}, {NetId}] station: {Station?.StationName}");

        PlugInteractionType interaction;
        bool left = false;
        ushort carNetId = 0;

        if (socket == plug.startAttachedTo)
            interaction = PlugInteractionType.DockHome;
        else
        {
            var trainCar = TrainCar.Resolve(socket.gameObject);
            if(trainCar != null)
            {
                if(!NetworkedTrainCar.TryGetFromTrainCar(trainCar, out var netTrainCar))
                {
                    Multiplayer.LogDebug(() => $"NetworkedPluggableObject.OnPlugged() NetworkedTrainCar: {trainCar?.ID} Not Found! Socket: {socket.GetObjectPath()}");
                    return;
                }

                carNetId = netTrainCar.NetId;

                interaction = PlugInteractionType.DockSocket;
                var sockets = trainCar.GetComponentsInChildren<PlugSocket>();
                if (socket == sockets[0])
                    left = true;
            }
            else
            {
                Multiplayer.LogDebug(() => $"NetworkedPluggableObject.OnPlugged() Socket not recognised: {socket.GetObjectPath()}");
                return;
            }
        }

        NetworkLifecycle.Instance.Client?.SendPitStopPlugInteractionPacket(NetId, interaction, trainCarNetId: carNetId, isConnectedLeft: left);
    }
    #endregion
}
