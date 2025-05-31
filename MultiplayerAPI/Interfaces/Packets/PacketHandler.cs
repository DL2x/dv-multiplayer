namespace MPAPI.Interfaces.Packets;

/// <summary>
/// Delegate for handling received packets
/// </summary>
/// <typeparam name="T">Packet type</typeparam>
/// <param name="packet">The received packet</param>
/// <param name="sender">The player who sent the packet (null if from server)</param>
public delegate void PacketHandler<T>(T packet, IPlayer sender) where T : class;
