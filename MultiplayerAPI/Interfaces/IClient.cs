using MPAPI.Interfaces.Packets;
using System;
using System.Collections.Generic;

namespace MPAPI.Interfaces;

public interface IClient
{

    event Action<IPlayer> OnPlayerConnected;
    event Action<IPlayer> OnPlayerDisconnected;

    // Player access
    IReadOnlyCollection<IPlayer> Players { get; }
    IPlayer GetPlayer(byte id);

    // Client info
    bool IsConnected { get; }
    int Ping { get; }

    #region Packet API
    /// <summary>
    /// Register a packet type that uses automatic serialization
    /// </summary>
    /// <typeparam name="T">Packet type implementing IPacket</typeparam>
    /// <param name="handler">Handler to call when packet is received</param>
    void RegisterPacket<T>(ClientPacketHandler<T> handler) where T : class, IPacket, new();

    /// <summary>
    /// Register a packet type that uses manual serialization
    /// </summary>
    /// <typeparam name="T">Packet type implementing ISerializablePacket</typeparam>
    /// <param name="handler">Handler to call when packet is received</param>
    void RegisterSerializablePacket<T>(ClientPacketHandler<T> handler) where T : class, ISerializablePacket, new();


    /// <summary>
    /// Send a packet to the server
    /// </summary>
    /// <typeparam name="T">Packet type</typeparam>
    /// <param name="packet">Packet to send</param>
    /// <param name="reliable">Whether to send reliably</param>
    void SendPacketToServer<T>(T packet, bool reliable = true) where T : class, IPacket, new();

    /// <summary>
    /// Send a packet to all connected players
    /// </summary>
    /// <typeparam name="T">Packet type</typeparam>
    /// <param name="packet">Packet to send</param>
    /// <param name="reliable">Whether to send reliably</param>
    void SendSerializablePacketToServer<T>(T packet, bool reliable = true) where T : class, ISerializablePacket, new();

    #endregion
}
