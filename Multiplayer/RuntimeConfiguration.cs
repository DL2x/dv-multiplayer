using DV;
using DV.Platform.Steam;
using UnityEngine;

namespace Multiplayer;

public enum MultiplayerRuntimeType
{
    Steam,
    Oculus,
    Cracked,
    Dedicated
}

public enum NetworkTransportMode
{
    Steam,
    Direct,
    Both
}

public static class RuntimeConfiguration
{
    public static string BuildDestination => (BuildInfo.BUILD_DESTINATION ?? string.Empty).Trim().ToLowerInvariant();

    public static MultiplayerRuntimeType RuntimeType
    {
        get
        {
            if (Application.isBatchMode || BuildDestination.Contains("dedicated"))
                return MultiplayerRuntimeType.Dedicated;

            if (BuildDestination.Contains("oculus"))
                return MultiplayerRuntimeType.Oculus;

            if (BuildDestination.Contains("steam"))
                return DVSteamworks.Success
                    ? MultiplayerRuntimeType.Steam
                    : MultiplayerRuntimeType.Cracked;

            return DVSteamworks.Success
                ? MultiplayerRuntimeType.Steam
                : MultiplayerRuntimeType.Oculus;
        }
    }

    public static bool CanUseSteamServices => RuntimeType == MultiplayerRuntimeType.Steam;
    public static bool CanUseDirectUdp => RuntimeType != MultiplayerRuntimeType.Cracked;
    public static bool ShouldPreserveSteamProtection => RuntimeType == MultiplayerRuntimeType.Cracked;

    public static NetworkTransportMode GetDefaultHostTransportMode()
    {
        return RuntimeType switch
        {
            MultiplayerRuntimeType.Steam => NetworkTransportMode.Steam,
            MultiplayerRuntimeType.Oculus => NetworkTransportMode.Direct,
            MultiplayerRuntimeType.Dedicated => NetworkTransportMode.Direct,
            _ => NetworkTransportMode.Direct,
        };
    }

    public static NetworkTransportMode SanitizeHostTransportMode(NetworkTransportMode requested)
    {
        return RuntimeType switch
        {
            MultiplayerRuntimeType.Steam => requested,
            MultiplayerRuntimeType.Oculus => NetworkTransportMode.Direct,
            MultiplayerRuntimeType.Dedicated => requested == NetworkTransportMode.Steam ? NetworkTransportMode.Direct : requested,
            MultiplayerRuntimeType.Cracked => NetworkTransportMode.Direct,
            _ => NetworkTransportMode.Direct,
        };
    }

    public static bool CanHostWith(NetworkTransportMode mode)
    {
        mode = SanitizeHostTransportMode(mode);

        return RuntimeType switch
        {
            MultiplayerRuntimeType.Steam => true,
            MultiplayerRuntimeType.Oculus => mode == NetworkTransportMode.Direct,
            MultiplayerRuntimeType.Dedicated => mode != NetworkTransportMode.Steam,
            MultiplayerRuntimeType.Cracked => false,
            _ => false,
        };
    }


    public static string GetApiHostingType(MultiplayerRuntimeType runtimeType, NetworkTransportMode transportMode)
    {
        if (runtimeType == MultiplayerRuntimeType.Dedicated)
            return "dedicated";

        return transportMode switch
        {
            NetworkTransportMode.Steam => "steam",
            NetworkTransportMode.Both => "both",
            _ => "ip",
        };
    }

    public static bool CanJoinSteamLobbies => CanUseSteamServices;
}
