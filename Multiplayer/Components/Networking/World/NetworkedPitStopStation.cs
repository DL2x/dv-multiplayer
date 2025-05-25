using DV.Interaction;
using Multiplayer.Networking.Packets.Common;
using Multiplayer.Networking.TransportLayers;
using Multiplayer.Networking.Data;
using Multiplayer.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using DV.ThingTypes;
using System.Collections;
using System.Linq;
using Multiplayer.Networking.Packets.Clientbound.World;
using static CashRegisterModule;

namespace Multiplayer.Components.Networking.World;

/// <summary>
/// Handles networked interactions with pit stop stations, including vehicle selection and resource management.
/// </summary>
public class NetworkedPitStopStation : IdMonoBehaviour<ushort, NetworkedPitStopStation>
{
    #region Lookup Cache
    private static readonly Dictionary<PitStopStation, NetworkedPitStopStation> pitStopStationToNetworkedPitStopStation = [];

    public static bool Get(ushort netId, out NetworkedPitStopStation obj)
    {
        bool b = Get(netId, out IdMonoBehaviour<ushort, NetworkedPitStopStation> rawObj);
        obj = (NetworkedPitStopStation)rawObj;
        return b;
    }

    public static void InitialisePitStops()
    {

        //Find all pitstop stations that are placed on the map
        //sort them by their hierarchy path for consistent ordering
        var stations = Resources.FindObjectsOfTypeAll<PitStopStation>()
            .Where(p => p.transform.parent != null)
            .OrderBy(p => p.transform.position.x)
            .ThenBy(p => p.transform.position.y)
            .ThenBy(p => p.transform.position.z)
            .ToArray();

        Multiplayer.LogDebug(() => $"InitialisePitStops() Found: {stations?.Length}");

        foreach (var station in stations)
        {
            var netStation = station.GetOrAddComponent<NetworkedPitStopStation>();
            netStation.Station = station;

            if (netStation.NetId == 0)
                netStation.Awake();

            pitStopStationToNetworkedPitStopStation[station] = netStation;

            Multiplayer.LogDebug(() => $"InitialisePitStops() Station: {station?.GetObjectPath()}, netId: {netStation.NetId}");

            CoroutineManager.Instance.StartCoroutine(netStation.Init());

        }
    }
    #endregion

    protected override bool IsIdServerAuthoritative => false;

    const float MAX_DELTA = 0.2f;
    const float MIN_UPDATE_TIME = 0.1f;
    const float LOADING_TIMEOUT = 5f;
    const float ROTATION_SMOOTH_SPEED = 5f;
    const float FAUCET_SNAP_THRESHOLD = 0.005f;

    const float DEFAULT_DISABLER_SQR_DISTANCE = 250000f;
    const float NEARBY_REMOVAL_DELAY = 3f;

    #region Server variables
    private Dictionary<byte, float> playerToLastNearbyTime;
    private float disablerSqrDistance = DEFAULT_DISABLER_SQR_DISTANCE;

    private bool processingAsHost = false;
    #endregion

    #region Common variables
    public PitStopStation Station { get; set; }
    public string StationName { get; private set; }

    private ResourceType[] resourceTypes = Array.Empty<ResourceType>();

    private GrabHandlerHingeJoint carSelectorGrab;
    private GrabHandlerHingeJoint faucetPositionerGrab;
    private HingeJointAngleFix faucetPositioner;
    private readonly Dictionary<GrabHandlerHingeJoint, (LocoResourceModule module, Action grabbedHandler, Action ungrabbedHandler)> grabberLookup = [];
    private readonly Dictionary<ResourceType, GrabHandlerHingeJoint> grabbedHandlerLookup = [];
    private readonly Dictionary<ResourceType, NetworkedPluggableObject> resourceToPluggableObject = [];

    private readonly Dictionary<ResourceType, bool> isResourceGrabbedDict = [];
    private readonly Dictionary<ResourceType, bool> wasResourceGrabbedDict = [];
    private readonly Dictionary<ResourceType, bool> isResourceRemoteGrabbedDict = [];
    private readonly Dictionary<ResourceType, bool> wasResourceRemoteGrabbedDict = [];
    private readonly Dictionary<ResourceType, float> lastRemoteValueDict = [];
    private float lastUpdateTime = 0.0f;

    private bool isFaucetGrabbed = false;
    private float lastFaucetUpdateTime = 0.0f;
    private float lastFaucetSent = 0.0f;
    private float faucetTargetPercentage = 0.0f;
    private bool faucetTargetReached = true;

    private readonly Dictionary<ResourceType, RotaryAmplitudeChecker> grabbedAmplitudeChecker = [];
    private readonly Dictionary<ResourceType, float> lastUnitsToBuyDict = [];

    private bool Refreshed = false;
    #endregion

    #region Unity
    protected override void Awake()
    {
        if (NetId == 0)
            base.Awake();

        StationName = $"{transform.parent.parent.name} - {transform.parent.name}";

        if (NetworkLifecycle.Instance.IsHost())
        {
            playerToLastNearbyTime = [];

            var disabler = GetComponentInParent<PlayerDistanceGameObjectsDisabler>();
            if (disabler != null)
                disablerSqrDistance = disabler.disableSqrDistance;

            NetworkLifecycle.Instance.OnTick += PlayerDistanceChecker;

            //ensure host can interact
            Refreshed = true;
        }
    }

    protected void OnDisable()
    {
        if (!NetworkLifecycle.Instance.IsHost())
            Refreshed = false;
    }

    protected override void OnDestroy()
    {
        pitStopStationToNetworkedPitStopStation.Remove(Station);

        if (carSelectorGrab != null)
        {
            carSelectorGrab.Grabbed -= CarSelectorGrabbed;
            carSelectorGrab.UnGrabbed -= CarSelectorUnGrabbed;

            Station.pitstop.CarSelected -= CarSelected;
        }

        if (faucetPositionerGrab != null)
        {
            faucetPositionerGrab.Grabbed -= FaucetCrankGrabbed;
            faucetPositionerGrab.UnGrabbed -= FaucetCrankUnGrabbed;
        }

        foreach (var kvp in grabberLookup)
        {
            var grab = kvp.Key;
            var (_, grabbedHandler, ungrabbedHandler) = kvp.Value;
            grab.Grabbed -= grabbedHandler;
            grab.UnGrabbed -= ungrabbedHandler;
        }

        grabberLookup.Clear();
        grabbedHandlerLookup.Clear();
        base.OnDestroy();
    }

    protected void LateUpdate()
    {
        foreach (var resourceType in resourceTypes)
        {
            var module = Station.locoResourceModules.resourceModules.FirstOrDefault(x => x.resourceType == resourceType);

            if (module == null
                || !isResourceGrabbedDict.TryGetValue(resourceType, out var isResourceGrabbed)
                || !wasResourceGrabbedDict.TryGetValue(resourceType, out var wasResourceGrabbed)
                || !isResourceRemoteGrabbedDict.TryGetValue(resourceType, out var isResourceRemoteGrabbed)
                || !wasResourceRemoteGrabbedDict.TryGetValue(resourceType, out var wasResourceRemoteGrabbed)
                || !lastRemoteValueDict.TryGetValue(resourceType, out var lastRemoteValue)
                || !lastUnitsToBuyDict.TryGetValue(resourceType, out var lastUnitsToBuy)
                )
                continue;

            //Handle local grab interactions
            if (isResourceGrabbed || (wasResourceGrabbed && lastUnitsToBuy != module.Data.unitsToBuy))
            {
                //ensure the delta is big enough to be worth sending or we have reached a limit
                var delta = Math.Abs(lastUnitsToBuy - module.Data.unitsToBuy);
                var deltaTime = Time.time - lastUpdateTime;

                //Check if the units to buy have reached a limit (0 or AbsoluteMaxValue), as this overrides a delta below minimum
                var unitsToBuyChanged =
                       (module.Data.unitsToBuy == module.AbsoluteMinValue && lastUnitsToBuy != module.AbsoluteMinValue)
                    || (module.Data.unitsToBuy == module.AbsoluteMaxValue && lastUnitsToBuy != module.AbsoluteMaxValue);

                //Send the update if we've passed the time threshold AND we have a big enough change or hit a limit
                if (deltaTime > MIN_UPDATE_TIME && (delta > MAX_DELTA || unitsToBuyChanged))
                {
                    lastUnitsToBuyDict[resourceType] = module.Data.unitsToBuy;
                    lastUpdateTime = Time.time;

                    //if (!(NetworkLifecycle.Instance.IsHost() && processingAsHost))
                    NetworkLifecycle.Instance?.Client.SendPitStopInteractionPacket(
                            NetId,
                            PitStopStationInteractionType.ResourceUpdate,
                            resourceType,
                            lastUnitsToBuy
                        );
                }
            }
            //Local grab has ended, but needs to be finalised
            else if (wasResourceGrabbed)
            {
                Multiplayer.LogDebug(() => $"NetworkedPitStopStation.LateUpdate() wasGrabbed: {wasResourceGrabbed}, previous: {lastUnitsToBuy}, new: {module.Data.unitsToBuy}");
                lastUnitsToBuyDict[resourceType] = module.Data.unitsToBuy;

                //if (!(NetworkLifecycle.Instance.IsHost() && processingAsHost))
                NetworkLifecycle.Instance?.Client.SendPitStopInteractionPacket(
                    NetId,
                    PitStopStationInteractionType.ResourceUngrab,
                    resourceType,
                    lastUnitsToBuy
                );

                //Reset grab states
                wasResourceGrabbedDict[resourceType] = false;
            }

            //allow things to settle after remote grab released
            if (!isResourceRemoteGrabbed && wasResourceRemoteGrabbed)
            {
                float previous = module.Data.unitsToBuy;
                //grabbedModule.Data.unitsToBuy = lastRemoteValueDict; 
                SetUnits(module, lastRemoteValue);

                Multiplayer.LogDebug(() => $"NetworkedPitStopStation.LateUpdate() wasRemoteGrabbed: {wasResourceRemoteGrabbed}, previous: {previous}, new: {lastRemoteValue}");

                if (previous == lastRemoteValue)
                {
                    //settled, stop tracking remote
                    wasResourceRemoteGrabbedDict[resourceType] = false;
                }
            }
        }
    }

    protected void Update()
    {
        var deltaTime = Time.time - lastFaucetUpdateTime;

        // Handle faucet movement
        if (faucetPositioner && !faucetTargetReached)
        {
            var currentPercentage = faucetPositioner.Percentage;
            float newPercent = Mathf.Lerp(currentPercentage, faucetTargetPercentage, Time.deltaTime * ROTATION_SMOOTH_SPEED);

            //if we're close enough to the target, snap to it
            if (Mathf.Abs(currentPercentage - faucetTargetPercentage) < FAUCET_SNAP_THRESHOLD)
                newPercent = faucetTargetPercentage;

            SetFaucetRotation(newPercent);
            //if we're in snap range we can finalise the rotation
            faucetTargetReached = Mathf.Abs(newPercent - faucetTargetPercentage) < FAUCET_SNAP_THRESHOLD;
        }

        if (isFaucetGrabbed && (deltaTime > MIN_UPDATE_TIME) && lastFaucetSent != faucetPositioner.Percentage)
        {
            lastFaucetUpdateTime = Time.time;

            lastFaucetSent = faucetPositioner.Percentage;
            NetworkLifecycle.Instance?.Client.SendPitStopInteractionPacket
            (
                NetId,
                PitStopStationInteractionType.FaucetPosition,
                null,
                lastFaucetSent
            );
        }
    }
    #endregion

    #region Server

    public Dictionary<ResourceType, ushort> GetPluggables()
    {
        Dictionary<ResourceType, ushort> keyValuePairs = [];
        foreach (var kvp in resourceToPluggableObject)
            keyValuePairs.Add(kvp.Key, kvp.Value.NetId);

        return keyValuePairs;
    }

    public bool ValidateInteraction(CommonPitStopInteractionPacket packet, ITransportPeer peer)
    {
        //todo: implement validation code (player distance, player interacting, etc.)
        return true;
    }

    public void OnPlayerDisconnect(ITransportPeer peer)
    {
        //todo: when a player disconnects, if they are interacting with a lever, cancel the interaction
        //Multiplayer.LogWarning($"OnPlayerDisconnect()");
    }

    private void PlayerDistanceChecker(uint tick)
    {
        //if not active then there is no one close by
        if (gameObject == null || !gameObject.activeInHierarchy || Station == null || Station.pitstop == null)
            return;

        foreach (var player in NetworkLifecycle.Instance.Server.ServerPlayers)
        {
            if (player.Id == NetworkLifecycle.Instance.Server.SelfId || !player.IsLoaded)
                continue;

            float sqrDistance = (player.WorldPosition - transform.position).sqrMagnitude;

            bool initialised = playerToLastNearbyTime.TryGetValue(player.Id, out float lastVisit);

            if (sqrDistance > disablerSqrDistance)
            {
                // Too far away for too long, stop tracking
                if ((Time.time - lastVisit) > NEARBY_REMOVAL_DELAY)
                    playerToLastNearbyTime.Remove(player.Id);

                continue;
            }

            //player nearby recently, update time
            playerToLastNearbyTime[player.Id] = Time.time;

            if (!initialised)
            {
                if (!NetworkLifecycle.Instance.Server.TryGetPeer(player.Id, out var peer))
                    continue;

                if (Station.pitstop.IsCarInPitStop())
                {
                    // One struct per module type
                    var resourceCount = Station.locoResourceModules.resourceModules.Count();
                    LocoResourceModuleData[] stateData = new LocoResourceModuleData[resourceCount];
                    int i;
                    for (i = 0; i < resourceCount; i++)
                    {
                        stateData[i] = LocoResourceModuleData.From(Station.locoResourceModules.resourceModules[i]);
                    }

                    PitStopPlugData[] plugData = new PitStopPlugData[resourceToPluggableObject.Count];

                    i = 0;
                    foreach (var plug in resourceToPluggableObject)
                    {
                        plugData[i] = PitStopPlugData.From(plug.Value);
                        i++;
                    }

                    // Send current state
                    NetworkLifecycle.Instance.Server.SendPitStopBulkDataPacket(NetId, Station.pitstop.carList.Count, stateData, plugData, peer);
                }
            }
        }
    }

    public void ProcessInteractionPacketAsHost(CommonPitStopInteractionPacket packet, ITransportPeer peer)
    {
        Multiplayer.LogDebug(() => $"ProcessInteractionPacketAsHost() from peer: {peer.Id}, selfpeer: {NetworkLifecycle.Instance.Server.SelfId}");

        if (ValidateInteraction(packet, peer))
        {
            processingAsHost = true;
            ProcessInteractionPacketAsClient(packet);
            LateUpdate();
            processingAsHost = false;
            //Send to all other players
            foreach (var playerId in playerToLastNearbyTime.Keys)
            {
                if (NetworkLifecycle.Instance.Server.TryGetPeer(playerId, out var sendPeer) && sendPeer.Id != peer.Id)
                {
                    Multiplayer.LogDebug(() => $"ProcessInteractionPacketAsHost() sending to peer: {sendPeer.Id}");
                    NetworkLifecycle.Instance.Server.SendPitStopInteractionPacket(sendPeer, packet);
                }
            }
        }
        else
        {
            Multiplayer.LogDebug(() => $"ProcessInteractionPacketAsHost() failed validation");
            //Failed to validate, player needs to rollback interaction
            NetworkLifecycle.Instance.Server.SendPitStopInteractionPacket(
                peer,
                new CommonPitStopInteractionPacket
                {
                    NetId = packet.NetId,
                    InteractionType = (byte)PitStopStationInteractionType.Reject
                }
            );
        }
    }
    #endregion


    #region Common
    /// <summary>
    /// Looks up Pluggable object by resource type
    /// </summary>
    public bool TryGetPluggable(ResourceType type, out NetworkedPluggableObject netPluggable)
    {
        return resourceToPluggableObject.TryGetValue(type, out netPluggable);
    }

    /// <summary>
    /// Initializes the pit stop station and sets up event handlers for grab interactions.
    /// </summary>
    private IEnumerator Init()
    {
        Multiplayer.LogDebug(() => $"NetworkedPitStopStation.Init() station: {Station == null}, pitstop: {Station?.pitstop == null}");

        while (Station?.pitstop == null)
            yield return new WaitForEndOfFrame();

        //Wait for levers an knobs to load
        yield return new WaitUntil(() => GetComponentInChildren<GrabHandlerHingeJoint>(true) != null);
        carSelectorGrab = GetComponentInChildren<GrabHandlerHingeJoint>(true);

        if (carSelectorGrab != null)
        {
            Multiplayer.LogDebug(() => $"NetworkedPitStopStation.Init() Grab Handler found: {carSelectorGrab != null}, Name: {carSelectorGrab.name}");
            carSelectorGrab.Grabbed += CarSelectorGrabbed;
            carSelectorGrab.UnGrabbed += CarSelectorUnGrabbed;

            Station.pitstop.CarSelected += CarSelected;
        }

        // Water tower positioner handle
        var faucetGo = transform.parent.FindChildrenByName("FaucetCrank").FirstOrDefault();
        faucetPositionerGrab = faucetGo?.GetComponentInChildren<GrabHandlerHingeJoint>(true);
        faucetPositioner = faucetGo?.GetComponentInChildren<HingeJointAngleFix>(true);

        if (faucetPositionerGrab != null && faucetPositioner != null)
        {
            Multiplayer.LogDebug(() => $"NetworkedPitStopStation.Init() Grab Handler found: {carSelectorGrab != null}, Name: {carSelectorGrab.name}");
            faucetPositionerGrab.Grabbed += FaucetCrankGrabbed;
            faucetPositionerGrab.UnGrabbed += FaucetCrankUnGrabbed;
        }

        //build dictionaries
        var resourceModules = Station?.locoResourceModules?.resourceModules;
        if (resourceModules == null)
        {
            Multiplayer.LogWarning($"No resource modules found for station {StationName}");
            yield break;
        }
        
        resourceTypes = resourceModules?.Select(m => m.resourceType).ToArray();

        foreach (var resourceType in resourceTypes)
        {
            isResourceGrabbedDict[resourceType] = false;
            wasResourceGrabbedDict[resourceType] = false;
            isResourceRemoteGrabbedDict[resourceType] = false;
            wasResourceRemoteGrabbedDict[resourceType] = false;
            lastRemoteValueDict[resourceType] = 0.0f;
            grabbedAmplitudeChecker[resourceType] = null;
            lastUnitsToBuyDict[resourceType] = 0.0f;
        }

        StringBuilder sb = new();
        sb.AppendLine($"NetworkedPitStopStation.Awake() {StationName} resources:");

        if (resourceModules != null)
        {
            foreach (var resourceModule in resourceModules)
            {
                var grabHandlers = resourceModule.GetComponentsInChildren<GrabHandlerHingeJoint>();
                foreach (var grab in grabHandlers)
                {
                    if (grab != null)
                    {
                        //Delegates for handlers
                        void GrabbedHandler() => LeverGrabbed(resourceModule);
                        void UnGrabbedHandler() => LeverUnGrabbed(resourceModule);

                        //Subscribe
                        grab.Grabbed += GrabbedHandler;
                        grab.UnGrabbed += UnGrabbedHandler;

                        //Store delegates
                        grabberLookup[grab] = (resourceModule, GrabbedHandler, UnGrabbedHandler);
                        grabbedHandlerLookup[resourceModule.resourceType] = grab;

                        sb.AppendLine($"\t{resourceModule.resourceType}, Grab Handler found: {grab != null}, Name: {grab.name}");
                    }
                }

                var plug = resourceModule.resourceHose;
                if (plug != null)
                {
                    var netPlug = plug.GetOrAddComponent<NetworkedPluggableObject>();
                    resourceToPluggableObject[resourceModule.resourceType] = netPlug;
                    netPlug.InitPitStop(this);
                }
            }
        }
        else
        {
            sb.AppendLine($"ERROR Station is Null {Station == null}, resource modules: {Station?.locoResourceModules}");
        }

        Multiplayer.LogDebug(() => sb.ToString());
    }

    private IEnumerator WaitForLoad(ClientboundPitStopBulkUpdatePacket packet)
    {
        float time = Time.time;

        yield return new WaitUntil(() =>
                (Station?.pitstop?.carList != null && packet.CarCount == Station.pitstop.carList.Count)
                || (Time.time - time) > LOADING_TIMEOUT
            );

        if ((Time.time - time) < LOADING_TIMEOUT)
        {
            ProcessBulkUpdate(packet);
        }
        else
            Multiplayer.LogWarning($"NetworkedPitStopStation.WaitForLoad() Station {StationName} failed to process bulk update");
    }

    private void SetUnits(LocoResourceModule rm, float units)
    {
        if (rm == null)
            return;

        float clamped = Mathf.Clamp(units, rm.AbsoluteMinValue, rm.AbsoluteMaxValue);
        rm.SetUnitsToBuy(clamped);
    }

    /// <summary>
    /// Initialises data elements for each car in each resource module
    /// </summary>
    private void InitialiseData()
    {
        foreach (var resourceModule in Station.locoResourceModules.resourceModules)
        {
            if (resourceModule == null)
                continue;

            // Make sure resourceData has enough entries for all cars
            while (resourceModule.resourceData.Count < Station.pitstop.carList.Count)
            {
                if (resourceModule.resourceData.Count > 0)
                    resourceModule.resourceData.Add(new CashRegisterModuleData(resourceModule.resourceData[0]));
                else
                    resourceModule.resourceData.Add(new CashRegisterModuleData());
            }
        }
    }

    /// <summary>
    /// Sets the rotation of the faucet handle to the specified percentage
    /// </summary>
    public void SetFaucetRotation(float percentage)
    {
        if (faucetPositioner == null)
            return;

        float targetAngle = faucetPositioner.angleOffset + (percentage * faucetPositioner.angleRange);

        Vector3 axis = faucetPositioner.joint.axis;

        // Create a rotation around that axis by the target angle
        Quaternion rotationDelta = Quaternion.AngleAxis(targetAngle, axis);

        // Calculate the final target rotation
        Quaternion targetRotation = Quaternion.Inverse(faucetPositioner.startRotationInverse) * rotationDelta;

        faucetPositioner.transform.localRotation = targetRotation;
    }

    /// <summary>
    /// Set the car selection index
    /// </summary>
    public void SetCarSelection(int selection)
    {
        if (selection >= 0 && selection < Station.pitstop.carList.Count)
        {
            Station.pitstop.currentCarIndex = selection;
            Station.pitstop.OnCarSelectionChanged();
        }
        else
        {
            Multiplayer.LogWarning($"Pit Stop car selection change out of bounds! Selected: {selection}, current car count: {Station.pitstop.carList.Count}");
        }
    }
    #endregion


    #region Client
    /// <summary>
    /// Handles grab interactions for the car selector knob.
    /// </summary>
    private void CarSelectorGrabbed()
    {
        if (NetworkLifecycle.Instance.IsProcessingPacket || (NetworkLifecycle.Instance.IsHost() && processingAsHost))
            return;

        //Prevent new players/players entering the area from sending packets until initalised
        if (!Refreshed)
            return;

        Multiplayer.LogDebug(() => $"CarSelectorGrabbed() {StationName}");
        NetworkLifecycle.Instance?.Client.SendPitStopInteractionPacket(NetId, PitStopStationInteractionType.CarSelectorGrab, null, 0);
    }

    /// <summary>
    /// Handles end of grab (release) interactions for the car selector knob.
    /// </summary>
    private void CarSelectorUnGrabbed()
    {
        if (NetworkLifecycle.Instance.IsProcessingPacket || (NetworkLifecycle.Instance.IsHost() && processingAsHost))
            return;

        //Prevent new players/players entering the area from sending packets until initalised
        if (!Refreshed)
            return;

        Multiplayer.LogDebug(() => $"CarSelectorUnGrabbed() {StationName}");
        NetworkLifecycle.Instance?.Client.SendPitStopInteractionPacket(NetId, PitStopStationInteractionType.CarSelectorUngrab, null, Station.pitstop.SelectedIndex);
    }

    /// <summary>
    /// Handles change of selected car events.
    /// </summary>
    private void CarSelected()
    {
        if (NetworkLifecycle.Instance.IsProcessingPacket || (NetworkLifecycle.Instance.IsHost() && processingAsHost))
            return;

        //Prevent new players/players entering the area from sending packets until initalised
        if (!Refreshed)
            return;

        Multiplayer.LogDebug(() => $"CarSelected() selected: {Station.pitstop.SelectedIndex}");

        NetworkLifecycle.Instance?.Client.SendPitStopInteractionPacket(NetId, PitStopStationInteractionType.CarSelection, null, Station.pitstop.SelectedIndex);
    }

    /// <summary>
    /// Handles grab interactions for resource module levers.
    /// </summary>
    /// <param name="module">The resource module being grabbed.</param>
    private void LeverGrabbed(LocoResourceModule module)
    {
        if (NetworkLifecycle.Instance.IsProcessingPacket || (NetworkLifecycle.Instance.IsHost() && processingAsHost))
            return;

        //Prevent new players/players entering the area from sending packets until initalised
        if (!Refreshed)
            return;

        Multiplayer.LogDebug(() => $"LeverGrabbed() {StationName}, module: {module.resourceType}");

        isResourceGrabbedDict[module.resourceType] = true;
        wasResourceGrabbedDict[module.resourceType] = true;
        grabbedAmplitudeChecker[module.resourceType] = module.GetComponentInChildren<RotaryAmplitudeChecker>();
        lastUnitsToBuyDict[module.resourceType] = module.Data.unitsToBuy;

        NetworkLifecycle.Instance?.Client.SendPitStopInteractionPacket(NetId, PitStopStationInteractionType.ResourceGrab, module.resourceType, lastUnitsToBuyDict[module.resourceType]);
    }

    /// <summary>
    /// Handles end of grab (release) interactions for resource module levers.
    /// </summary>
    /// <param name="module">The resource module being grabbed.</param>
    private void LeverUnGrabbed(LocoResourceModule module)
    {
        if (NetworkLifecycle.Instance.IsProcessingPacket)
            return;

        //Prevent new players/players entering the area from sending packets until initalised
        if (!Refreshed)
            return;

        Multiplayer.LogDebug(() => $"LeverUnGrabbed() {StationName}, module: {module.resourceType}");
        isResourceGrabbedDict[module.resourceType] = false;
        wasResourceGrabbedDict[module.resourceType] = true;
    }

    /// <summary>
    /// Handles grab interactions for the faucet positioning handle (water towers).
    /// </summary>
    private void FaucetCrankGrabbed()
    {
        if (NetworkLifecycle.Instance.IsProcessingPacket || (NetworkLifecycle.Instance.IsHost() && processingAsHost))
            return;

        //Prevent new players/players entering the area from sending packets until initalised
        if (!Refreshed)
            return;

        Multiplayer.LogDebug(() => $"FaucetCrankGrabbed() {StationName}");
        isFaucetGrabbed = true;
        NetworkLifecycle.Instance?.Client.SendPitStopInteractionPacket(NetId, PitStopStationInteractionType.FaucetGrab, null, 0);
    }

    /// <summary>
    /// Handles end of grab (release) interactions for the car selector knob.
    /// </summary>
    private void FaucetCrankUnGrabbed()
    {
        if (NetworkLifecycle.Instance.IsProcessingPacket || (NetworkLifecycle.Instance.IsHost() && processingAsHost))
            return;

        //Prevent new players/players entering the area from sending packets until initalised
        if (!Refreshed)
            return;

        Multiplayer.LogDebug(() => $"FaucetCrankUnGrabbed() {StationName}, percentage: {faucetPositioner.Percentage}");
        isFaucetGrabbed = false;
        lastFaucetSent = faucetPositioner.Percentage;
        NetworkLifecycle.Instance?.Client.SendPitStopInteractionPacket(NetId, PitStopStationInteractionType.FaucetUngrab, null, lastFaucetSent);
    }

    public void ProcessBulkUpdate(ClientboundPitStopBulkUpdatePacket packet)
    {
        if (Station?.pitstop?.carList == null || Station.pitstop.carList.Count < packet.CarCount)
        {
            // Allow cars a chance to load in the pitstop
            Multiplayer.LogDebug(() => $"PitStop bulk data count mismatch, waiting for load: {packet.CarCount} != {Station.pitstop.carList.Count}");
            CoroutineManager.Instance.StartCoroutine(WaitForLoad(packet));
            return;
        }

        // Make sure the data elements exist prior to attempting to load them
        InitialiseData();

        Multiplayer.LogDebug(() => $"PitStop bulk data car count matches");

        // Load the data for each car and resource module
        foreach (var resource in packet.ResourceData)
        {
            var module = Station.locoResourceModules.resourceModules.FirstOrDefault(lm => lm.resourceType == resource.ResourceType);

            if (module)
                if (module.resourceData.Count == resource.Values.Count())
                    for (int i = 0; i < module.resourceData.Count; i++)
                        module.resourceData[i].unitsToBuy = resource.Values[i];
                else
                    Multiplayer.LogWarning($"PitStop bulk data count mismatch post-force: {module.resourceData.Count} != {resource.Values.Count()}");
            else
                Multiplayer.LogWarning($"PitStop module not found for resource type: {resource.ResourceType}");
        }

        //sync plugs
        foreach (var plug in packet.PlugData)
        {
            //todo: set plug states
        }

        // Mark data as refreshed to allow player interactions
        Refreshed = true;
    }

    /// <summary>
    /// Processes incoming network packets for pit stop interactions.
    /// </summary>
    /// <param name="packet">The packet containing interaction data.</param>
    public void ProcessInteractionPacketAsClient(CommonPitStopInteractionPacket packet)
    {
        if (!Enum.IsDefined(typeof(PitStopStationInteractionType), packet.InteractionType))
        {
            Multiplayer.LogWarning($"Invalid interaction type: {packet.InteractionType} in ProcessInteractionPacketAsClient()");
            return;
        }

        PitStopStationInteractionType interactionType = (PitStopStationInteractionType)packet.InteractionType;

        bool resourceValid = Enum.IsDefined(typeof(ResourceType), packet.ResourceType);

        ResourceType resourceType = resourceValid ? (ResourceType)packet.ResourceType : ResourceType.Fuel;

        GrabHandlerHingeJoint grab = null;
        LocoResourceModule resourceModule = null;

        Multiplayer.LogDebug(() => $"NetworkedPitStopStation.ProcessPacket() [{StationName}, {NetId}] {interactionType}, resource type: {resourceType}, state: {packet.Value}");

        if (resourceValid)
        {
            if (!grabbedHandlerLookup.TryGetValue((ResourceType)resourceType, out grab))
                Multiplayer.LogError($"Could not find ResourceType in grabbedHandlerLookup for Pit Stop station {StationName}, resource type: {resourceType}");
            else
                if (!grabberLookup.TryGetValue(grab, out var tup))
                Multiplayer.LogError($"Could not find GrabHandler in grabberLookup for Pit Stop station {StationName}, resource type: {resourceType}");
            else
                (resourceModule, _, _) = tup;

            if (packet.Value < resourceModule.AbsoluteMinValue || packet.Value > resourceModule.AbsoluteMaxValue)
            {
                Multiplayer.LogError($"Invalid Pit Stop state value: {packet.Value} for resource {resourceModule.resourceType}");
                return;
            }
        }

        switch (interactionType)
        {
            case PitStopStationInteractionType.Reject:
                //todo: implement rejection
                break;


            case PitStopStationInteractionType.ResourceGrab:
                //block interaction
                grab?.SetMovingDisabled(false);

                //set direction
                if (resourceValid && resourceModule != null)
                {
                    lastRemoteValueDict[resourceType] = packet.Value;
                    isResourceRemoteGrabbedDict[resourceType] = true;
                    wasResourceRemoteGrabbedDict[resourceType] = true;
                }

                break;

            case PitStopStationInteractionType.ResourceUngrab:
                //allow interaction
                grab?.SetMovingDisabled(true);

                if (isResourceRemoteGrabbedDict[resourceType] || wasResourceRemoteGrabbedDict[resourceType])
                {
                    lastRemoteValueDict[resourceType] = packet.Value;
                    SetUnits(resourceModule, lastRemoteValueDict[resourceType]);
                    isResourceRemoteGrabbedDict[resourceType] = false;
                }
                break;

            case PitStopStationInteractionType.ResourceUpdate:

                if (resourceValid && resourceModule != null)
                {
                    if (isResourceRemoteGrabbedDict[resourceType] || wasResourceRemoteGrabbedDict[resourceType])
                    {
                        lastRemoteValueDict[resourceType] = packet.Value;
                        SetUnits(resourceModule, lastRemoteValueDict[resourceType]);
                    }
                }
                break;


            case PitStopStationInteractionType.CarSelectorGrab:
                //block interaction
                carSelectorGrab?.SetMovingDisabled(false);
                break;

            case PitStopStationInteractionType.CarSelectorUngrab:
                //allow interaction
                carSelectorGrab?.SetMovingDisabled(true);
                SetCarSelection((int)packet.Value);
                break;

            case PitStopStationInteractionType.CarSelection:
                SetCarSelection((int)packet.Value);
                break;

            case PitStopStationInteractionType.FaucetGrab:
                //block interaction
                faucetPositionerGrab?.SetMovingDisabled(false);
                break;

            case PitStopStationInteractionType.FaucetUngrab:
                //allow interaction
                faucetPositionerGrab?.SetMovingDisabled(true);

                if (packet.Value >= -1 && packet.Value <= 1)
                {
                    if (faucetPositioner.Percentage != packet.Value)
                    {
                        faucetTargetPercentage = packet.Value;
                        faucetTargetReached = false;
                    }
                }
                break;

            case PitStopStationInteractionType.FaucetPosition:
                if (packet.Value >= -1 && packet.Value <= 1)
                {
                    if (faucetPositioner.Percentage != packet.Value)
                    {
                        faucetTargetPercentage = packet.Value;
                        faucetTargetReached = false;
                    }
                }
                break;


            case PitStopStationInteractionType.PayOrder:
                break;
            case PitStopStationInteractionType.CancelOrder:
                break;
            case PitStopStationInteractionType.ProcessOrder:
                break;
        }
    }
    #endregion
}
