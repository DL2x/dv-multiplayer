using LiteNetLib.Utils;
using Multiplayer.Components.MainMenu;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Multiplayer.Networking.Data
{
    public class LobbyServerData : IServerBrowserGameDetails
    {
        [JsonProperty("game_server_id")]
        public string id { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }

        public string ipv4 { get; set; }
        public string ipv6 { get; set; }
        public int port { get; set; }

        [JsonIgnore]
        public string LocalIPv4 { get; set; }
        [JsonIgnore]
        public string LocalIPv6 { get; set; }

        [JsonProperty("server_name")]
        public string Name { get; set; }

        [JsonProperty("password_protected")]
        public bool HasPassword { get; set; }

        [JsonProperty("game_mode")]
        public int GameMode { get; set; }

        [JsonProperty("difficulty")]
        public int Difficulty { get; set; }

        [JsonProperty("time_passed")]
        public string TimePassed { get; set; }

        [JsonProperty("current_players")]
        public int CurrentPlayers { get; set; }

        [JsonProperty("max_players")]
        public int MaxPlayers { get; set; }

        [JsonProperty("required_mods")]
        public ModInfo[] RequiredMods { get; set; }

        [JsonProperty("game_version")]
        public string GameVersion { get; set; }

        [JsonProperty("multiplayer_version")]
        public string MultiplayerVersion { get; set; }

        [JsonProperty("server_info")]
        public string ServerDetails { get; set; }

        [JsonProperty("hosting_type")]
        public string HostingType { get; set; }

        [JsonProperty("start_time")]
        public string StartTime { get; set; }

        [JsonProperty("ready")]
        public bool Ready { get; set; }

        [JsonProperty("online_players")]
        public List<string> OnlinePlayers { get; set; } = new List<string>();

        public NetworkTransportMode TransportMode { get; set; } = RuntimeConfiguration.GetDefaultHostTransportMode();
        public MultiplayerRuntimeType RuntimeType { get; set; } = RuntimeConfiguration.RuntimeType;

        [JsonIgnore]
        public int Ping { get; set; } = -1;
        [JsonIgnore]
        public ServerVisibility Visibility { get; set; } = ServerVisibility.Public;
        [JsonIgnore]
        public int LastSeen { get; set; } = int.MaxValue;

        public void Dispose() { }

        public void EnsureApiDefaults()
        {
            HostingType ??= RuntimeConfiguration.GetApiHostingType(RuntimeType, TransportMode);
            Address ??= BuildAddress(ipv4, ipv6, port);
            TimePassed ??= "00d 00h 00m 00s";
            OnlinePlayers ??= new List<string>();
            CurrentPlayers = OnlinePlayers.Count;
            NormalizeAfterDeserialization();
        }

        public static string BuildAddress(string ipv4, string ipv6, int port)
        {
            if (!string.IsNullOrWhiteSpace(ipv4) && port > 0)
                return ipv4.Trim() + ":" + port;

            if (!string.IsNullOrWhiteSpace(ipv6) && port > 0)
                return "[" + ipv6.Trim() + "]:" + port;

            if (!string.IsNullOrWhiteSpace(ipv4))
                return ipv4.Trim();

            if (!string.IsNullOrWhiteSpace(ipv6))
                return ipv6.Trim();

            return null;
        }

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            NormalizeAfterDeserialization();
        }

        public void NormalizeAfterDeserialization()
        {
            OnlinePlayers ??= new List<string>();
            CurrentPlayers = OnlinePlayers.Count;

            if (TryParseAddress(Address, out string host, out int parsedPort, out bool isIpv6))
            {
                if (port <= 0 && parsedPort > 0)
                    port = parsedPort;

                if (string.IsNullOrWhiteSpace(ipv4) && string.IsNullOrWhiteSpace(ipv6))
                {
                    if (isIpv6)
                        ipv6 = host;
                    else
                        ipv4 = host;
                }
            }

            TransportMode = HostingTypeToTransportMode(HostingType, TransportMode);
        }

        public static NetworkTransportMode HostingTypeToTransportMode(string hostingType, NetworkTransportMode fallback)
        {
            switch ((hostingType ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "steam":
                    return NetworkTransportMode.Steam;
                case "both":
                    return NetworkTransportMode.Both;
                case "dedicated":
                case "ip":
                    return NetworkTransportMode.Direct;
                default:
                    return fallback;
            }
        }

        public static bool TryParseAddress(string address, out string host, out int port, out bool isIpv6)
        {
            host = null;
            port = 0;
            isIpv6 = false;

            if (string.IsNullOrWhiteSpace(address))
                return false;

            string trimmed = address.Trim();

            if (trimmed.StartsWith("[") && trimmed.Contains("]"))
            {
                int endBracket = trimmed.IndexOf(']');
                host = trimmed.Substring(1, endBracket - 1);
                isIpv6 = true;

                if (endBracket + 1 < trimmed.Length && trimmed[endBracket + 1] == ':')
                {
                    string portString = trimmed.Substring(endBracket + 2);
                    int.TryParse(portString, out port);
                }

                return !string.IsNullOrWhiteSpace(host);
            }

            int colonCount = 0;
            foreach (char ch in trimmed)
            {
                if (ch == ':')
                    colonCount++;
            }

            if (colonCount == 1)
            {
                int lastColon = trimmed.LastIndexOf(':');
                host = trimmed.Substring(0, lastColon);
                string portString = trimmed.Substring(lastColon + 1);
                int.TryParse(portString, out port);
                return !string.IsNullOrWhiteSpace(host);
            }

            host = trimmed;
            isIpv6 = colonCount > 1;
            return true;
        }

        public static int GetDifficultyFromString(string difficulty)
        {
            int diff = 0;

            switch (difficulty)
            {
                case "Standard":
                    diff = 0;
                    break;
                case "Comfort":
                    diff = 1;
                    break;
                case "Realistic":
                    diff = 2;
                    break;
                default:
                    diff = 3;
                    break;
            }
            return diff;
        }

        public static string GetDifficultyFromInt(int difficulty)
        {
            string diff = "Standard";

            switch (difficulty)
            {
                case 0:
                    diff = "Standard";
                    break;
                case 1:
                    diff = "Comfort";
                    break;
                case 2:
                    diff = "Realistic";
                    break;
                default:
                    diff = "Custom";
                    break;
            }
            return diff;
        }

        public static int GetGameModeFromString(string difficulty)
        {
            int diff = 0;

            switch (difficulty)
            {
                case "Career":
                    diff = 0;
                    break;
                case "Sandbox":
                    diff = 1;
                    break;
                case "Scenario":
                    diff = 2;
                    break;
            }
            return diff;
        }

        public static string GetGameModeFromInt(int difficulty)
        {
            string diff = "Career";

            switch (difficulty)
            {
                case 0:
                    diff = "Career";
                    break;
                case 1:
                    diff = "Sandbox";
                    break;
                case 2:
                    diff = "Scenario";
                    break;
            }
            return diff;
        }

        public bool ShouldSerializeipv4() => false;
        public bool ShouldSerializeipv6() => false;
        public bool ShouldSerializeCurrentPlayers() => false;
        public bool ShouldSerializeTransportMode() => false;
        public bool ShouldSerializeRuntimeType() => false;

        public static void Serialize(NetDataWriter writer, LobbyServerData data)
        {
            writer.Put(data != null);

            if (data != null)
                writer.Put(new NetSerializer().Serialize(data));
        }

        public static LobbyServerData Deserialize(NetDataReader reader)
        {
            if (reader.GetBool())
                return new NetSerializer().Deserialize<LobbyServerData>(reader);
            else
                return null;
        }
    }
}
