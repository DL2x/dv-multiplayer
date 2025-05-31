using LiteNetLib.Utils;
using MPAPI.Interfaces.Packets;
using System;
using System.Collections.Generic;
using System.IO;

namespace Multiplayer.API;

/// <summary>
/// Wrapper for external serializable packets to integrate with LiteNetLib
/// </summary>
/// <typeparam name="T">The packet type</typeparam>
public class ExternalSerializablePacketWrapper<T> : INetSerializable where T : class, ISerializablePacket, new()
{
    public T Packet { get; set; }

    public void Serialize(NetDataWriter writer)
    {
        using var memoryStream = new MemoryStream();
        using var binaryWriter = new BinaryWriter(memoryStream);

        Packet.Serialize(binaryWriter);

        var data = memoryStream.ToArray();
        writer.PutBytesWithLength(data);
    }

    public void Deserialize(NetDataReader reader)
    {
        var data = reader.GetBytesWithLength();

        using var memoryStream = new MemoryStream(data);
        using var binaryReader = new BinaryReader(memoryStream);

        Packet = new T();
        Packet.Deserialize(binaryReader);
    }
}
