using Multiplayer.Components.MainMenu;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.IO;
using UnityEngine;
using UnityModManagerNet;

namespace Multiplayer.Components.DedicatedServer;

/// <summary>
/// Small file-based handoff for headless server launch scripts.
/// The shell/batch launchers write dedicated-server.json, the mod reads it once on startup,
/// applies it to the normal Multiplayer settings object and saves the regular mod config.
/// </summary>
public class DedicatedServerConfig
{
    private const string CONFIG_FILE_NAME = "dedicated-server.json";
    private const int DEFAULT_PORT = 7777;
    private const int MIN_PORT = 1024;
    private const int MAX_PORT = 49151;
    private const int MIN_PLAYERS = 1;

    public string ServerName { get; set; }
    public string Password { get; set; }
    public string Details { get; set; }
    public int? Port { get; set; }
    public int? MaxPlayers { get; set; }
    public ServerVisibility? Visibility { get; set; }
    public bool? PublicGame { get; set; }
    public NetworkTransportMode? HostTransportMode { get; set; }

    [JsonIgnore]
    public static string ConfigPath => Path.Combine(Multiplayer.ModEntry?.Path ?? string.Empty, CONFIG_FILE_NAME);

    public static void ApplyIfNeeded(UnityModManager.ModEntry modEntry, Settings settings)
    {
        if (RuntimeConfiguration.RuntimeType != MultiplayerRuntimeType.Dedicated)
            return;

        string path = Path.Combine(modEntry.Path, CONFIG_FILE_NAME);
        if (!File.Exists(path))
        {
            Multiplayer.Log($"Dedicated server config not found at '{path}'. Using existing mod settings.");
            return;
        }

        try
        {
            JsonSerializerSettings jsonSettings = new()
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
            };
            jsonSettings.Converters.Add(new StringEnumConverter());

            DedicatedServerConfig config = JsonConvert.DeserializeObject<DedicatedServerConfig>(File.ReadAllText(path), jsonSettings) ?? new DedicatedServerConfig();
            config.ApplyTo(settings);
            settings.Save(modEntry);
            Multiplayer.Log($"Applied dedicated server config from '{path}' and saved it to the Multiplayer mod settings.");
        }
        catch (Exception ex)
        {
            Multiplayer.LogException($"Failed to apply dedicated server config from '{path}':", ex);
        }
    }

    private void ApplyTo(Settings settings)
    {
        if (!string.IsNullOrWhiteSpace(ServerName))
            settings.ServerName = ServerName.Trim();

        if (Password != null)
            settings.Password = Password.Trim();

        if (Details != null)
            settings.Details = Details.Trim();

        if (Port.HasValue)
            settings.Port = SanitizePort(Port.Value);

        if (MaxPlayers.HasValue)
            settings.MaxPlayers = Mathf.Clamp(MaxPlayers.Value, MIN_PLAYERS, byte.MaxValue);

        if (Visibility.HasValue)
            settings.Visibility = Visibility.Value;

        if (PublicGame.HasValue)
            settings.PublicGame = PublicGame.Value;

        NetworkTransportMode requestedTransport = HostTransportMode ?? NetworkTransportMode.Direct;
        settings.HostTransportMode = RuntimeConfiguration.SanitizeHostTransportMode(requestedTransport);

        // Dedicated servers are intended to be reachable via IP hosting first.
        if (settings.HostTransportMode == NetworkTransportMode.Steam)
            settings.HostTransportMode = NetworkTransportMode.Direct;
    }

    private static int SanitizePort(int port)
    {
        return port >= MIN_PORT && port <= MAX_PORT ? port : DEFAULT_PORT;
    }
}
