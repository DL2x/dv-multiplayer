
using Multiplayer.Networking.Data;

namespace Multiplayer.Networking.Packets.Common;

public class CommonPitStopInteractionPacket
{
    public ushort NetId { get; set; }
    public PitStopStationInteractionType InteractionType { get; set; }
    public int ResourceType { get; set; }
    public float Value { get; set; }
}
