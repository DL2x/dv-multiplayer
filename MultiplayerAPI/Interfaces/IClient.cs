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

    //public abstract void SendPacketToServer<T>(T packet, DeliveryMethod deliveryMethod) where T : class, new();

    //public abstract void SendNetSerializablePacketToServer<T>(T packet, DeliveryMethod deliveryMethod) where T : INetSerializable, new();


}
