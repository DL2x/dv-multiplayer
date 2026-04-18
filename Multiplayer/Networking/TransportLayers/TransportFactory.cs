namespace Multiplayer.Networking.TransportLayers;

public static class TransportFactory
{
    public static ITransport CreateServerTransport(NetworkTransportMode mode)
    {
        mode = RuntimeConfiguration.SanitizeHostTransportMode(mode);

        return mode switch
        {
            NetworkTransportMode.Direct => new LiteNetLibTransport(),
            NetworkTransportMode.Both => new CompositeTransport(new LiteNetLibTransport(), new SteamWorksTransport()),
            _ => new SteamWorksTransport(),
        };
    }

    public static ITransport CreateClientTransport(NetworkTransportMode mode)
    {
        return mode switch
        {
            NetworkTransportMode.Direct => new LiteNetLibTransport(),
            _ => new SteamWorksTransport(),
        };
    }
}
