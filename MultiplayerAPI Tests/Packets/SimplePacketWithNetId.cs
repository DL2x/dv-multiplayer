using MPAPI.Interfaces.Packets;


namespace MultiplayerAPITest.Packets
{
    
    //for dynamic info a hash or other numbering system could be used, rather than a static enum.
    public enum WheelArrangement : byte
    {
        Default = 0,
        American440  = 1,
        Atlantic442 = 2,
        Reading444 = 3
    }

    //example packet with netId and an enum.
    internal class SimplePacketWithNetId : IPacket
    {
        public ushort CarNetId { get; set; }
        public WheelArrangement WheelArrangement { get; set; }
    }
}
