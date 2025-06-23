using Multiplayer.Networking.Data;
namespace Multiplayer.Networking.Packets.Clientbound.Jobs;

public class ServerboundWarehouseMachineControllerRequestPacket
{
    public string WarehouseMachineID { get; set; }
    public WarehouseAction warehouseAction { get; set; }
}
