using System.IO;

namespace MPAPI.Interfaces.Packets;

/// <summary>
/// Base interface for packets using manual serialization
/// Implementing classes must handle their own serialization/deserialization.
/// </summary>
public interface ISerializablePacket
{
    /// <summary>
    /// Serialize the packet data to the provided writer
    /// </summary>
    /// <param name="writer">Writer to serialize data to</param>
    void Serialize(BinaryWriter writer);

    /// <summary>
    /// Deserialize the packet data from the provided reader
    /// </summary>
    /// <param name="reader">Reader to deserialize data from</param>
    void Deserialize(BinaryReader reader);
}
