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
    float AnyPlayerSqrMag(GameObject item);

    float AnyPlayerSqrMag(Vector3 anchor);
    #endregion 
}
