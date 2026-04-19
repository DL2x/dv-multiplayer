using DV.Platform.Steam;
using DV.WeatherSystem;
using LiteNetLib;
using LiteNetLib.Utils;
using Multiplayer.Components.Networking;
using Multiplayer.Components.MainMenu;
using Multiplayer.Networking.Data;
using Multiplayer.Networking.Packets.Unconnected;
using Newtonsoft.Json;
using Multiplayer.Utils;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

namespace Multiplayer.Networking.Managers.Server;
public class LobbyServerManager : MonoBehaviour
{
    private const string ENDPOINT_ADD_SERVER = "add";
    private const string ENDPOINT_UPDATE_SERVER = "update";
    private const string ENDPOINT_REMOVE_SERVER = "remove";

    private readonly Regex IPv4Match = new Regex(@"(\b25[0-5]|\b2[0-4][0-9]|\b[01]?[0-9][0-9]?)(\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)){3}");

    private const int REDIRECT_MAX = 5;
    private const int UPDATE_TIME_BUFFER = 10;
    private const int UPDATE_TIME = 120 - UPDATE_TIME_BUFFER;
    private const int PLAYER_CHANGE_TIME = 5;
    private const float REGISTER_RETRY_TIME = 5f;

    private NetworkServer server;
    private string server_id;
    private string private_key;

    public static readonly string[] EXCLUDE_PARAMS = { "id", "ipv4", "ipv6", "port", "LocalIPv4", "LocalIPv6", "Ping", "Visibility", "LastSeen", "CurrentPlayers", "MaxPlayers", "Address", "HostingType", "StartTime", "Ready", "OnlinePlayers" };
    private Lobby? lobby;

    private bool initialised;
    private bool sendUpdates;
    private bool registerInProgress;
    private bool pendingReady;
    private float timePassed;
    private float registerRetryTimer;

    private NetManager discoveryManager;
    private NetPacketProcessor packetProcessor;
    private EventBasedNetListener discoveryListener;
    private readonly NetDataWriter cachedWriter = new();
    public static int[] discoveryPorts = { 8888, 8889, 8890 };

    public string ServerId => server_id;
    public string PrivateKey => private_key;
    public bool IsRegistered => !string.IsNullOrEmpty(server_id) && !string.IsNullOrEmpty(private_key);

    public void Awake()
    {
        server = NetworkLifecycle.Instance.Server;

        if (server == null || server.ServerData == null)
        {
            Multiplayer.LogError("Failed to load LobbyServerManager");
            Destroy(this);
        }
    }

    public IEnumerator Start()
    {
        if (server == null || server.ServerData == null)
            yield break;

        if (RuntimeConfiguration.CanUseSteamServices && server.TransportMode != NetworkTransportMode.Direct)
            CreateSteamLobby();

        server.ServerData.ipv6 = GetStaticIPv6Address();
        server.ServerData.LocalIPv4 = GetLocalIPv4Address();
        PrepareServerData();

        server.Log("\r\nPublic IPv4: " + server.ServerData.ipv4 +
            "\r\nPublic IPv6: " + server.ServerData.ipv6 +
            "\r\nPrivate IPv4: " + server.ServerData.LocalIPv4 +
            "\r\nAPI Address: " + server.ServerData.Address +
            "\r\nAPI Hosting Type: " + server.ServerData.HostingType);

        TryRegisterServer();

        if (!string.IsNullOrEmpty(Multiplayer.Settings.Ipv4AddressCheck))
        {
            StartCoroutine(GetIPv4(Multiplayer.Settings.Ipv4AddressCheck));
        }
        else
        {
            server.LogWarning("Ipv4AddressCheck URL is null or empty, skipping IPv4 detection");
            initialised = true;
        }

        if (server.TransportMode != NetworkTransportMode.Steam)
            StartDiscoveryServer();

        yield break;
    }

    public void OnDestroy()
    {
        sendUpdates = false;
        StopAllCoroutines();

        if (IsRegistered)
            StartCoroutine(RemoveFromLobbyServer(GetEndpoint(ENDPOINT_REMOVE_SERVER)));
        else
            Multiplayer.Log("Skipping lobby remove because server was never registered");

        lobby?.SetJoinable(false);
        lobby?.Leave();
        discoveryManager?.Stop();
    }

    public void Update()
    {
        if (!sendUpdates)
        {
            server.ServerData.OnlinePlayers = BuildOnlinePlayerList();
            server.ServerData.CurrentPlayers = server.ServerData.OnlinePlayers.Count;
            if (!registerInProgress)
            {
                registerRetryTimer += Time.deltaTime;
                if (registerRetryTimer >= REGISTER_RETRY_TIME)
                {
                    registerRetryTimer = 0f;
                    TryRegisterServer();
                }
            }
        }
        else
        {
            timePassed += Time.deltaTime;
            int previousPlayers = server.ServerData.CurrentPlayers;
            server.ServerData.OnlinePlayers = BuildOnlinePlayerList();
            server.ServerData.CurrentPlayers = server.ServerData.OnlinePlayers.Count;
            server.ServerData.OnlinePlayers = BuildOnlinePlayerList();

            if (timePassed > UPDATE_TIME || (previousPlayers != server.PlayerCount && timePassed > PLAYER_CHANGE_TIME))
            {
                timePassed = 0f;
                StartCoroutine(UpdateLobbyServer(GetEndpoint(ENDPOINT_UPDATE_SERVER), null));

                if (lobby != null)
                    SteamworksUtils.SetLobbyData((Lobby)lobby, server.ServerData, EXCLUDE_PARAMS);
            }
        }

        discoveryManager?.PollEvents();
    }

    public void MarkReady()
    {
        pendingReady = true;
        server.ServerData.Ready = true;

        if (sendUpdates)
            StartCoroutine(UpdateLobbyServer(GetEndpoint(ENDPOINT_UPDATE_SERVER), true));
    }

    public void RemoveFromLobbyServer()
    {
        sendUpdates = false;
        StopAllCoroutines();

        if (!IsRegistered)
        {
            Multiplayer.Log("Skipping lobby remove because no game_server_id/private_key is available");
            return;
        }

        StartCoroutine(RemoveFromLobbyServer(GetEndpoint(ENDPOINT_REMOVE_SERVER)));
    }

    private void PrepareServerData()
    {
        server.ServerData.TransportMode = server.TransportMode;
        server.ServerData.RuntimeType = RuntimeConfiguration.RuntimeType;
        server.ServerData.OnlinePlayers = BuildOnlinePlayerList();
        server.ServerData.CurrentPlayers = server.ServerData.OnlinePlayers.Count;
        server.ServerData.Private = server.ServerData.Visibility == ServerVisibility.Private;
        server.ServerData.HostingType = RuntimeConfiguration.GetApiHostingType(server.ServerData.RuntimeType, server.ServerData.TransportMode);
        server.ServerData.TimePassed = server.ServerData.TimePassed ?? "00d 00h 00m 00s";
        server.ServerData.Ready = pendingReady;
        server.ServerData.Address = BuildBestAddress();
        server.ServerData.EnsureApiDefaults();
    }

    private string BuildBestAddress()
    {
        if (!string.IsNullOrWhiteSpace(server.ServerData.ipv4))
            return LobbyServerData.BuildAddress(server.ServerData.ipv4, null, server.ServerData.port);

        if (!string.IsNullOrWhiteSpace(server.ServerData.LocalIPv4))
            return LobbyServerData.BuildAddress(server.ServerData.LocalIPv4, null, server.ServerData.port);

        if (!string.IsNullOrWhiteSpace(server.ServerData.ipv6))
            return LobbyServerData.BuildAddress(null, server.ServerData.ipv6, server.ServerData.port);

        return LobbyServerData.BuildAddress(null, null, server.ServerData.port);
    }

    private List<string> BuildOnlinePlayerList()
    {
        return server.ServerPlayers
            .Select(player => player?.Username)
            .Where(username => !string.IsNullOrWhiteSpace(username))
            .Distinct()
            .ToList();
    }

    private string GetEndpoint(string endpoint)
    {
        string baseUri = (Multiplayer.Settings.LobbyServerAddress ?? string.Empty).TrimEnd('/');
        return baseUri + "/" + endpoint;
    }

    private void TryRegisterServer()
    {
        if (registerInProgress)
            return;

        PrepareServerData();
        registerRetryTimer = 0f;
        StartCoroutine(RegisterWithLobbyServer(GetEndpoint(ENDPOINT_ADD_SERVER)));
    }

    public async void CreateSteamLobby()
    {
        if (server == null || server.ServerData == null)
            return;

        var result = await SteamMatchmaking.CreateLobbyAsync(server.ServerData.MaxPlayers);

        if (result.HasValue)
        {
            lobby = result.Value;
            server.Log("Steam Lobby created successfully!");
            lobby?.SetData(SteamworksUtils.LOBBY_MP_MOD_KEY, SteamworksUtils.LOBBY_MP_MOD_KEY);
            lobby?.SetData(SteamworksUtils.LOBBY_NET_LOCATION_KEY, SteamNetworkingUtils.LocalPingLocation.ToString());
            SteamworksUtils.SetLobbyData((Lobby)lobby, server.ServerData, EXCLUDE_PARAMS);

            if (server.ServerData.Visibility == ServerVisibility.Private)
                lobby?.SetPrivate();
            else if (server.ServerData.Visibility == ServerVisibility.Friends)
                lobby?.SetFriendsOnly();
            else if (server.ServerData.Visibility == ServerVisibility.Public)
                lobby?.SetPublic();

            lobby?.SetJoinable(true);
        }
        else
        {
            server.LogError("Failed to create lobby.");
        }
    }

    private IEnumerator RegisterWithLobbyServer(string uri)
    {
        registerInProgress = true;
        JsonSerializerSettings jsonSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
        string json = JsonConvert.SerializeObject(server.ServerData, jsonSettings);
        Multiplayer.Log("Registering server at: " + uri);
        Multiplayer.LogDebug(() => "Register JsonRequest: " + json);

        yield return SendJsonRequest(
            uri,
            json,
            webRequest =>
            {
                registerInProgress = false;
                LobbyServerResponseData response = null;
                try
                {
                    response = JsonConvert.DeserializeObject<LobbyServerResponseData>(webRequest.downloadHandler.text);
                }
                catch (Exception ex)
                {
                    Multiplayer.LogException("Failed to parse register response", ex);
                }

                if (response != null)
                {
                    private_key = response.private_key;
                    server_id = response.game_server_id;
                    server.ServerData.id = server_id;
                    sendUpdates = IsRegistered;

                    Multiplayer.Log("Registered with lobby server. game_server_id=" + server_id);

                    if (pendingReady)
                        StartCoroutine(UpdateLobbyServer(GetEndpoint(ENDPOINT_UPDATE_SERVER), true));
                }
            },
            webRequest =>
            {
                registerInProgress = false;
                Multiplayer.LogError("Failed to register with lobby server");
            }
        );
    }

    private IEnumerator RemoveFromLobbyServer(string uri)
    {
        if (!IsRegistered)
            yield break;

        JsonSerializerSettings jsonSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
        string json = JsonConvert.SerializeObject(new LobbyServerResponseData(server_id, private_key), jsonSettings);
        Multiplayer.LogDebug(() => "Remove JsonRequest: " + json);

        yield return SendJsonRequest(
            uri,
            json,
            webRequest => Multiplayer.Log("Successfully removed from lobby server"),
            webRequest => Multiplayer.LogError("Failed to remove from lobby server")
        );
    }

    private IEnumerator UpdateLobbyServer(string uri, bool? ready)
    {
        if (!IsRegistered)
        {
            Multiplayer.Log("Skipping lobby update because server is not registered yet");
            yield break;
        }

        JsonSerializerSettings jsonSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

        DateTime start = AStartGameData.BaseTimeAndDate;
        DateTime current = WeatherDriver.Instance.manager.DateTime;
        TimeSpan inGame = current - start;

        PrepareServerData();
        server.ServerData.TimePassed = inGame.ToString(@"d\d\ hh\h\ mm\m\ ss\s");

        LobbyServerUpdateData reqData = new LobbyServerUpdateData(
            server_id,
            private_key,
            server.ServerData.TimePassed,
            server.ServerData.CurrentPlayers,
            ready,
            server.ServerData.OnlinePlayers
        );

        string json = JsonConvert.SerializeObject(reqData, jsonSettings);
        Multiplayer.LogDebug(() => "Update JsonRequest: " + json);

        yield return SendJsonRequest(
            uri,
            json,
            webRequest => Multiplayer.Log("Successfully updated lobby server"),
            webRequest =>
            {
                Multiplayer.LogError("Failed to update lobby server, attempting to re-register");
                sendUpdates = false;
                private_key = null;
                server_id = null;
                registerInProgress = false;
                TryRegisterServer();
            }
        );
    }

    private IEnumerator GetIPv4(string uri)
    {
        Multiplayer.Log("Preparing to get IPv4: " + uri);

        yield return SendWebRequestGET(
            uri,
            webRequest =>
            {
                Match match = IPv4Match.Match(webRequest.downloadHandler.text);
                if (match != null)
                {
                    Multiplayer.Log("IPv4 address extracted: " + match.Value);
                    bool addressChanged = server.ServerData.ipv4 != match.Value;
                    server.ServerData.ipv4 = match.Value;
                    PrepareServerData();
                    if (addressChanged && !sendUpdates)
                        TryRegisterServer();
                }
                else
                {
                    Multiplayer.LogError("Failed to find IPv4 address. Server will only be available via IPv6");
                }

                initialised = true;
            },
            webRequest =>
            {
                Multiplayer.LogError("Failed to find IPv4 address. Server will only be available via IPv6");
                initialised = true;
            }
        );
    }

    private IEnumerator SendJsonRequest(string uri, string json, Action<UnityWebRequest> onSuccess, Action<UnityWebRequest> onError, int depth = 0)
    {
        if (depth > REDIRECT_MAX)
        {
            Multiplayer.LogError("Reached maximum redirects: " + uri);
            yield break;
        }

        byte[] body = string.IsNullOrEmpty(json) ? null : Encoding.UTF8.GetBytes(json);
        using UnityWebRequest webRequest = new UnityWebRequest(uri, UnityWebRequest.kHttpVerbPOST);
        webRequest.redirectLimit = 0;
        webRequest.downloadHandler = new DownloadHandlerBuffer();
        webRequest.SetRequestHeader("Accept", "application/json");

        if (body != null)
        {
            webRequest.uploadHandler = new UploadHandlerRaw(body);
            webRequest.uploadHandler.contentType = "application/json";
            webRequest.SetRequestHeader("Content-Type", "application/json");
        }

        yield return webRequest.SendWebRequest();

        if (webRequest.responseCode >= 300 && webRequest.responseCode < 400)
        {
            string redirectUrl = webRequest.GetResponseHeader("Location");
            Multiplayer.LogWarning("Lobby Server redirected, check address is up to date: '" + redirectUrl + "'");

            if (redirectUrl != null && redirectUrl.StartsWith("https://") && redirectUrl.Replace("https://", "http://") == uri)
                yield return SendJsonRequest(redirectUrl, json, onSuccess, onError, depth + 1);
            yield break;
        }

        if (webRequest.isNetworkError || webRequest.isHttpError)
        {
            Multiplayer.LogError("SendJsonRequest(" + uri + ") responseCode: " + webRequest.responseCode + ", Error: " + webRequest.error + "\n" + webRequest.downloadHandler.text);
            onError?.Invoke(webRequest);
        }
        else
        {
            Multiplayer.Log("Received: " + webRequest.downloadHandler.text);
            onSuccess?.Invoke(webRequest);
        }
    }

    private IEnumerator SendWebRequestGET(string uri, Action<UnityWebRequest> onSuccess, Action<UnityWebRequest> onError, int depth = 0)
    {
        if (depth > REDIRECT_MAX)
        {
            Multiplayer.LogError("Reached maximum redirects: " + uri);
            yield break;
        }

        using UnityWebRequest webRequest = UnityWebRequest.Get(uri);
        webRequest.redirectLimit = 0;
        webRequest.downloadHandler = new DownloadHandlerBuffer();

        yield return webRequest.SendWebRequest();

        if (webRequest.responseCode >= 300 && webRequest.responseCode < 400)
        {
            string redirectUrl = webRequest.GetResponseHeader("Location");
            Multiplayer.LogWarning("Lobby Server redirected, check address is up to date: '" + redirectUrl + "'");

            if (redirectUrl != null && redirectUrl.StartsWith("https://") && redirectUrl.Replace("https://", "http://") == uri)
                yield return SendWebRequestGET(redirectUrl, onSuccess, onError, depth + 1);
            yield break;
        }

        if (webRequest.isNetworkError || webRequest.isHttpError)
        {
            Multiplayer.LogError("SendWebRequestGET(" + uri + ") responseCode: " + webRequest.responseCode + ", Error: " + webRequest.error + "\n" + webRequest.downloadHandler.text);
            onError?.Invoke(webRequest);
        }
        else
        {
            Multiplayer.Log("Received: " + webRequest.downloadHandler.text);
            onSuccess?.Invoke(webRequest);
        }
    }

    public static string GetStaticIPv6Address()
    {
        foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            bool flag = !networkInterface.Supports(NetworkInterfaceComponent.IPv6) || networkInterface.OperationalStatus != OperationalStatus.Up || networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback || networkInterface.NetworkInterfaceType == NetworkInterfaceType.Tunnel;
            if (!flag)
            {
                foreach (UnicastIPAddressInformation unicastIPAddressInformation in networkInterface.GetIPProperties().UnicastAddresses)
                {
                    bool flag2 = unicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetworkV6;
                    if (flag2)
                    {
                        bool flag3 = !unicastIPAddressInformation.Address.IsIPv6LinkLocal && !unicastIPAddressInformation.Address.IsIPv6SiteLocal && unicastIPAddressInformation.IsDnsEligible;
                        if (flag3)
                            return unicastIPAddressInformation.Address.ToString();
                    }
                }
            }
        }
        return null;
    }

    public static string GetLocalIPv4Address()
    {
        foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            bool flag = !networkInterface.Supports(NetworkInterfaceComponent.IPv4) || networkInterface.OperationalStatus != OperationalStatus.Up || networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback;
            if (!flag)
            {
                IPInterfaceProperties properties = networkInterface.GetIPProperties();
                if (properties.GatewayAddresses.Count == 0)
                    continue;

                foreach (UnicastIPAddressInformation unicastIPAddressInformation in properties.UnicastAddresses)
                {
                    bool flag2 = unicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork;
                    if (flag2)
                        return unicastIPAddressInformation.Address.ToString();
                }
            }
        }
        return null;
    }

    public void StartDiscoveryServer()
    {
        server.Log("StartDiscoveryServer()");
        discoveryListener = new EventBasedNetListener();
        discoveryManager = new NetManager(discoveryListener)
        {
            IPv6Enabled = true,
            UnconnectedMessagesEnabled = true,
            BroadcastReceiveEnabled = true,
        };
        packetProcessor = new NetPacketProcessor();

        discoveryListener.NetworkReceiveUnconnectedEvent += OnNetworkReceiveUnconnected;

        packetProcessor.RegisterNestedType(LobbyServerData.Serialize, LobbyServerData.Deserialize);
        packetProcessor.SubscribeReusable<UnconnectedDiscoveryPacket, IPEndPoint>(OnUnconnectedDiscoveryPacket);

        int successPort = discoveryPorts.FirstOrDefault(port => discoveryManager.Start(IPAddress.Any, IPAddress.IPv6Any, port));

        if (successPort != 0)
            server.Log("Discovery server started on port " + successPort);
        else
            server.LogError("Failed to start discovery server on any port");
    }

    protected NetDataWriter WritePacket<T>(T packet) where T : class, new()
    {
        cachedWriter.Reset();
        packetProcessor.Write(cachedWriter, packet);
        return cachedWriter;
    }

    protected void SendUnconnectedPacket<T>(T packet, string ipAddress, int port) where T : class, new()
    {
        discoveryManager.SendUnconnectedMessage(WritePacket(packet), ipAddress, port);
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        try
        {
            packetProcessor.ReadAllPackets(reader, remoteEndPoint);
        }
        catch (ParseException e)
        {
            server.LogWarning("LobbyServerManager.OnNetworkReceiveUnconnected() Failed to parse packet: " + e.Message);
        }
    }

    private void OnUnconnectedDiscoveryPacket(UnconnectedDiscoveryPacket packet, IPEndPoint endPoint)
    {
        if (!packet.IsResponse)
        {
            packet.IsResponse = true;
            packet.Data = server.ServerData;
        }

        SendUnconnectedPacket(packet, endPoint.Address.ToString(), endPoint.Port);
    }
}
