using System;

namespace MPAPI.Interfaces;

/// <summary>
/// Main interface for interacting with the Multiplayer mod
/// </summary>
public interface IMultiplayerAPI
{
    /// <summary>
    /// Gets whether the multiplayer mod is currently loaded and active
    /// </summary>
    bool IsMultiplayerLoaded { get; }

    /// <summary>
    /// Returns true if either a host or client exist
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Gets whether this instance is host
    /// </summary>
    bool IsHost { get; }

    /// <summary>
    /// Gets whether this instance is a dedicated server
    /// </summary>
    bool IsDedicatedServer { get; }

    /// <summary>
    /// Gets whether this current session is single player
    /// </summary>
    bool IsSinglePlayer { get; }

}
