using MPAPI.Interfaces.Packets;
using System;
using System.Collections.Generic;
using UnityEngine;


namespace MPAPI.Interfaces;

public interface IServer
{
    event Action<IPlayer> OnPlayerConnected;
    event Action<IPlayer> OnPlayerDisconnected;

    #region Server Properties
    int PlayerCount { get; }

    public IReadOnlyCollection<IPlayer> Players { get; }

    #endregion

    #region Packet API
    /// <summary>
    /// Register a packet type that uses automatic serialization
    /// </summary>
    /// <typeparam name="T">Packet type implementing IPacket</typeparam>
    /// <param name="handler">Handler to call when packet is received</param>
    void RegisterPacket<T>(ServerPacketHandler<T> handler) where T : class, IPacket, new();

    /// <summary>
    /// Register a packet type that uses manual serialization
    /// </summary>
    /// <typeparam name="T">Packet type implementing ISerializablePacket</typeparam>
    /// <param name="handler">Handler to call when packet is received</param>
    void RegisterSerializablePacket<T>(ServerPacketHandler<T> handler) where T : class, ISerializablePacket, new();


    /// <summary>
    /// Send a packet to all connected players
    /// </summary>
    /// <typeparam name="T">Packet type</typeparam>
    /// <param name="packet">Packet to send</param>
    /// <param name="reliable">Whether to send reliably</param>
    /// <param name="excludePlayer">Exclude this player</param>
    void SendPacketToAll<T>(T packet, bool reliable = true, IPlayer excludePlayer = null) where T : class, IPacket, new();

    /// <summary>
    /// Send a packet to all connected players
    /// </summary>
    /// <typeparam name="T">Packet type</typeparam>
    /// <param name="packet">Packet to send</param>
    /// <param name="reliable">Whether to send reliably</param>
    /// <param name="excludePlayer">Exclude this player</param>
    void SendSerializablePacketToAll<T>(T packet, bool reliable = true, IPlayer excludePlayer = null) where T : class, ISerializablePacket, new();

    /// <summary>
    /// Send a packet to a specific player
    /// </summary>
    /// <typeparam name="T">Packet type</typeparam>
    /// <param name="packet">Packet to send</param>
    /// <param name="player">Target player</param>
    /// <param name="reliable">Whether to send reliably</param>
    void SendPacketToPlayer<T>(T packet, IPlayer player, bool reliable = true) where T : class, IPacket, new();

    /// <summary>
    /// Send a packet to a specific player
    /// </summary>
    /// <typeparam name="T">Packet type</typeparam>
    /// <param name="packet">Packet to send</param>
    /// <param name="player">Target player</param>
    /// <param name="reliable">Whether to send reliably</param>
    void SendSerializablePacketToPlayer<T>(T packet, IPlayer player, bool reliable = true) where T : class, ISerializablePacket, new();
    #endregion

    #region Server Util
    /// <summary>
    /// Returns the distance (Square Magnitude) of the closest player to a given GameObject
    /// </summary>
    /// <param name="gameObject">GameObject to compare players against</param>
    // <returns>Returns the distance (Square Magnitude) of the closest player, or float.MaxValue if no player is nearby</returns>
    float AnyPlayerSqrMag(GameObject gameObject);

    /// <summary>
    /// Returns the distance (Square Magnitude) of the closest player to a given point
    /// </summary>
    /// <param name="anchor">Anchor point to compare players against</param>
    /// <returns>Returns the distance (Square Magnitude) of the closest player, or float.MaxValue if no player is nearby</returns>
    float AnyPlayerSqrMag(Vector3 anchor);
    #endregion

    #region Chat
    /// <summary>
    /// Sends a server chat message
    /// </summary>
    /// <param name="message">Message to be sent</param>
    /// <param name="excludePlayer">Player to exclude. If null, message will go to all players</param>
    void SendServerChatMessage(string message, IPlayer excludePlayer = null);

    /// <summary>
    /// Registers a chat command e.g. `/server` and optional short command '/s'
    /// </summary>
    /// <param name="commandLong">Command to be filtered for, without a leading '/' e.g. 'server'</param>
    /// <param name="commandShort">Optional short command to be filtered for, without a leading '/' e.g. 's'</param>
    /// <param name="helpMessage">Optional callback for a help message e.g. "Send a message as the server (host only)\r\n\t\t/server <message>\r\n\t\t/s <message>" It is recommended to provide localisation/translation for this string</param>
    /// <param name="callback">Action to execute when the command is triggered. First parameter contains command arguments as string array, second parameter is the player who executed the command.</param>
    /// <returns>True if the command was successfully registered, false if registration failed (e.g. command already exists).</returns>
    bool RegisterChatCommand(string commandLong, string commandShort, Func<string> helpMessage, Action<string[], IPlayer> callback);


    /// <summary>
    /// Registers a chat filter that processes non-command messages in registration order.
    /// Filters form a chain where each can either allow the message to continue to the next filter or block further processing.
    /// If all filters return true, the message will be sent to all players (default action).
    /// </summary>
    /// <param name="callback">Filter function that processes the message. First parameter is the message content, second parameter is the player who sent the message. Return true to pass the message to the next filter/default action, false to block propagation.</param>
    void RegisterChatFilter(Func<string, IPlayer, bool> callback);

    #endregion
}
