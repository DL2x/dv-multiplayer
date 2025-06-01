
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

    /// <summary>
    // Gets the NetId for an object
    // returns true if the object has a NetId
    /// </summary>
    bool TryGetNetId<T>(T obj, out ushort netId) where T : class;

    /// <summary>
    // Gets the object for an NetId
    // returns true if the object was found
    /// </summary>
    bool TryGetObjectFromNetId<T>(ushort netId, out T obj) where T : class;

}
