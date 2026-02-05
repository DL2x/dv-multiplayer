using System;
using DV;
using DV.Platform.Steam;

namespace Multiplayer.Utils;

public static class GameVersionDetector
{
    public static GameVersion Current { get; } = Detect();

    private static GameVersion Detect()
    {
        string dest = GetBuildDestinationString();

        if (dest.IndexOf("steam", StringComparison.OrdinalIgnoreCase) >= 0)
            return DVSteamworks.Success ? GameVersion.Steam : GameVersion.Cracked;

        if (dest.IndexOf("oculus", StringComparison.OrdinalIgnoreCase) >= 0 ||
            dest.IndexOf("meta", StringComparison.OrdinalIgnoreCase) >= 0 ||
            dest.IndexOf("quest", StringComparison.OrdinalIgnoreCase) >= 0)
            return GameVersion.Oculus;

        // unknown build destination -> safest: treat as cracked / unsupported
        return GameVersion.Cracked;
    }

    private static string GetBuildDestinationString()
    {
        try
        {
            object v = BuildInfo.BUILD_DESTINATION;
            return v != null ? v.ToString() : "";
        }
        catch
        {
            return "";
        }
    }

    public static bool IsSteam => Current == GameVersion.Steam;
    public static bool IsOculus => Current == GameVersion.Oculus;
    public static bool IsCracked => Current == GameVersion.Cracked;
}
