using MPAPI.Types;
using System;

namespace MPAPI.Interfaces;

/// <summary>
/// Main interface for interacting with the Multiplayer mod
/// </summary>
public interface IMultiplayerAPI
{
    /// <summary>
    /// Returns the version of the Multiplayer API if multiplayer is loaded, otherwise returns null
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Gets whether the multiplayer mod is currently loaded and active
    /// </summary>
    bool IsMultiplayerLoaded { get; }

    /// <summary>Sets the mod's compatibility requirements</summary>
    /// <param name="modId">String representing the your mod's Id (`ModEntry.Info.Id`)</param>
    /// <param name="compatibility">ModCompatibility flags representing installation host/client requirements</param>
    void SetModCompatibility(string modId, MultiplayerCompatibility compatibility);

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

    /// <summary>
    /// Event fired when a game/network tick occurs
    /// Ticks occur at a fixed interval (TICK_INTERVAL = 1/TICK_RATE) and are useful for synchronisation, batching, and processing changes.
    ///
    /// The tick parameter can be used to determine if non-reliable packets have been dropped and to sequence actions for rollbacks or preventing stale data from being processed.
    /// 
    /// Example: In Multiplayer's TrainCar simulation sync, small changes are cached when they occur but sent as a single packet per TrainCar when OnTick fires, reducing network overhead.
    /// </summary>
    /// <param name="tick">The current game tick number, incremented each tick cycle</param>
    event Action<uint> OnTick;

    /// <summary>
    /// The number of ticks per second (currently 24).
    /// Used to calculate the fixed tick interval: TICK_INTERVAL = 1.0f / TICK_RATE.
    /// </summary>
    uint TICK_RATE { get; }

    /// <summary>
    /// The current game tick.
    /// </summary>
    uint CurrentTick { get; }

    /// <summary>
    /// Gets the NetId for an object
    /// </summary>
    /// <param name="obj">The object you want the NetId for</param>
    /// <param name="netId">When this method returns, contains the NetId associated with the specified object, if found; otherwise, 0</param>
    /// <returns>True if a NetId for the object was found; otherwise, false</returns>
    bool TryGetNetId<T>(T obj, out ushort netId) where T : class;

    /// <summary>
    /// Gets the object for a NetId
    /// </summary>
    /// <param name="netId">The non-zero NetId for the object</param>
    /// <param name="obj">When this method returns, contains the object associated with the NetId, if found; otherwise null</param>
    /// <returns>True if the object was found; otherwise, false</returns>
    bool TryGetObjectFromNetId<T>(ushort netId, out T obj) where T : class;

}
