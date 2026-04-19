using Newtonsoft.Json;
using System.Collections.Generic;

namespace Multiplayer.Networking.Data
{
    public class LobbyServerUpdateData
    {
        [JsonProperty("game_server_id")]
        public string game_server_id { get; set; }

        [JsonProperty("private_key")]
        public string private_key { get; set; }

        [JsonProperty("time_passed")]
        public string TimePassed { get; set; }

        [JsonProperty("current_players")]
        public int CurrentPlayers { get; set; }

        [JsonProperty("ready")]
        public bool? Ready { get; set; }

        [JsonProperty("online_players")]
        public List<string> OnlinePlayers { get; set; }

        public LobbyServerUpdateData(string gameServerId, string privateKey, string timePassed, int currentPlayers, bool? ready = null, List<string> onlinePlayers = null)
        {
            game_server_id = gameServerId;
            private_key = privateKey;
            TimePassed = timePassed;
            OnlinePlayers = onlinePlayers ?? new List<string>();
            CurrentPlayers = OnlinePlayers.Count;
            Ready = ready;
        }

        public bool ShouldSerializeCurrentPlayers() => false;
    }
}
