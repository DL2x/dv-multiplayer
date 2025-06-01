using MPAPI.Interfaces.Packets;
using UnityEngine;

namespace MultiplayerAPITest.Packets
{
    internal class SimpleModPacket : IPacket
    {
        //Public properties are automatically serialised
        //acceptable types are:
        //    Primitives (bool, byte, sbyte, short, ushort, int, uint, long, ulong, float, double, string, char, IPEndPoint, Guid)
        //    Arrays of primitives
        //    Enums derived from primitives e.g. `enum MyEnum : byte`
        //    UnityEngine: Vector2, Vector3, Quarternion

        //Be mindful of the amount of data per packet.
        //  Avoid sending long strings or large structures
        //  Consider using an numeric Id system to represent objects
        //  Use the MP API to get the NetId (ushort) for TrainCars, rather than the car's string Id
        public string CarId { get; set; }   //It's better to use ushort and call `MultiplayerAPI.GetTrainCarNetId(TrainCar)`
                                            //See SimplePacketWithNetId for an example
        public Vector3 Position {  get; set; }
     
    }
}
