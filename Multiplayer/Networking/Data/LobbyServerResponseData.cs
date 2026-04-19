using Newtonsoft.Json;

namespace Multiplayer.Networking.Data
{
    public class LobbyServerResponseData
    {
        [JsonProperty("game_server_id")]
        public string game_server_id { get; set; }

        [JsonProperty("private_key")]
        public string private_key { get; set; }

        public LobbyServerResponseData() { }

        public LobbyServerResponseData(string gameServerId, string privateKey)
        {
            game_server_id = gameServerId;
            private_key = privateKey;
        }
    }
}
