
using LiteNetLib.Utils;
using Multiplayer.Networking.Data;
using Multiplayer.Networking.Serialization;
using UnityEngine;

namespace Multiplayer.Networking.Packets.Common;

public class CommonPitStopPlugInteractionPacket : INetSerializable
{
    public ushort NetId { get; set; }
    public PlugInteractionType InteractionType { get; set; }
    public byte PlayerId { get; set; }
    public ushort TrainCarNetId { get; set; }
    public bool IsLeftSide { get; set; }
    public Vector3? Position { get; set; }
    public Quaternion? Rotation { get; set; }

    public void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetUShort();
        InteractionType = (PlugInteractionType)reader.GetByte();

        switch (InteractionType)
        {
            case PlugInteractionType.Rejected:
                break;

            case PlugInteractionType.PickedUp:
                PlayerId = reader.GetByte();
                break;

            case PlugInteractionType.Dropped:
                Position = Vector3Serializer.Deserialize(reader);
                Rotation = QuaternionSerializer.Deserialize(reader);
                break;

            case PlugInteractionType.DockHome:
                break;

            case PlugInteractionType.DockSocket:
                TrainCarNetId = reader.GetUShort();
                IsLeftSide = reader.GetBool();
                break;
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.Put((byte)InteractionType);

        switch (InteractionType)
        {
            case PlugInteractionType.Rejected:
                break;

            case PlugInteractionType.PickedUp:
                writer.Put(PlayerId);
                break;

            case PlugInteractionType.Dropped:
                Vector3Serializer.Serialize(writer, Position ?? Vector3.zero);
                QuaternionSerializer.Serialize(writer, Rotation ?? Quaternion.identity);
                break;

            case PlugInteractionType.DockHome:
                break;

            case PlugInteractionType.DockSocket:
                writer.Put(TrainCarNetId);
                writer.Put(IsLeftSide);
                break;
        }
    }
}
