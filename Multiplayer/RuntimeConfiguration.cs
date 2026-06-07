using DV;
using DV.Platform.Steam;
using System;
using System.Linq;
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

    public static bool DedicatedRequested =>
        Application.isBatchMode
        || BuildDestination.Contains("dedicated")
        || HasCommandLineSwitch("-dvmp-dedicated")
        || HasCommandLineSwitch("--dvmp-dedicated")
        || HasCommandLineSwitch("+dvmp-dedicated");

    private static bool HasCommandLineSwitch(string name)
    {
        try
        {
            return Environment.GetCommandLineArgs().Any(arg => string.Equals(arg, name, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }

    public static MultiplayerRuntimeType RuntimeType
    {
        get
        {
            if (DedicatedRequested)
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

    // A dedicated server is headless regardless of Unity's -batchmode. The supported Linux
    // deployment runs the Windows build under Wine + Xvfb (a real GfxDevice, NOT -batchmode, so
    // Application.isBatchMode is false). It must still skip all player/UI/render work and mark the
    // loopback host invisible, so headless detection keys off the dedicated runtime type alone.
    public static bool IsHeadlessDedicated => RuntimeType == MultiplayerRuntimeType.Dedicated;

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

    public static bool CanJoinSteamLobbies => CanUseSteamServices;
}
