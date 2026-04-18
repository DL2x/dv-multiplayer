using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Multiplayer.Networking.TransportLayers;

public class CompositeTransport : ITransport
{
    private readonly List<ITransport> transports;

    public CompositeTransport(params ITransport[] transports)
    {
        this.transports = transports?.Where(transport => transport != null).ToList() ?? [];

        foreach (var transport in this.transports)
        {
            transport.OnConnectionRequest += HandleConnectionRequest;
            transport.OnPeerConnected += HandlePeerConnected;
            transport.OnPeerDisconnected += HandlePeerDisconnected;
            transport.OnNetworkReceive += HandleNetworkReceive;
            transport.OnNetworkError += HandleNetworkError;
            transport.OnNetworkLatencyUpdate += HandleNetworkLatencyUpdate;
        }
    }

    public NetStatistics Statistics => transports.FirstOrDefault(transport => transport.IsRunning)?.Statistics ?? new NetStatistics();
    public bool IsRunning => transports.Any(transport => transport.IsRunning);

    public event Action<NetDataReader, IConnectionRequest> OnConnectionRequest;
    public event Action<ITransportPeer> OnPeerConnected;
    public event Action<ITransportPeer, DisconnectReason> OnPeerDisconnected;
    public event Action<ITransportPeer, NetDataReader, byte, DeliveryMethod> OnNetworkReceive;
    public event Action<IPEndPoint, SocketError> OnNetworkError;
    public event Action<ITransportPeer, int> OnNetworkLatencyUpdate;

    public bool Start()
    {
        bool started = false;
        foreach (var transport in transports)
            started |= transport.Start();
        return started;
    }

    public bool Start(int port)
    {
        bool started = false;
        foreach (var transport in transports)
            started |= transport.Start(port);
        return started;
    }

    public bool Start(IPAddress ipv4, IPAddress ipv6, int port)
    {
        bool started = false;
        foreach (var transport in transports)
            started |= transport.Start(ipv4, ipv6, port);
        return started;
    }

    public void Stop(bool sendDisconnectPackets)
    {
        foreach (var transport in transports)
            transport.Stop(sendDisconnectPackets);
    }

    public void PollEvents()
    {
        foreach (var transport in transports)
            transport.PollEvents();
    }

    public void UpdateSettings(Settings settings)
    {
        foreach (var transport in transports)
            transport.UpdateSettings(settings);
    }

    public ITransportPeer Connect(string address, int port, NetDataWriter data)
    {
        if (port < 0)
        {
            var steamTransport = transports.OfType<SteamWorksTransport>().FirstOrDefault();
            return steamTransport?.Connect(address, port, data);
        }

        var directTransport = transports.OfType<LiteNetLibTransport>().FirstOrDefault();
        if (directTransport != null)
            return directTransport.Connect(address, port, data);

        return transports.FirstOrDefault()?.Connect(address, port, data);
    }

    public void Send(ITransportPeer peer, NetDataWriter writer, DeliveryMethod deliveryMethod)
    {
        peer?.Send(writer, deliveryMethod);
    }

    private void HandleConnectionRequest(NetDataReader reader, IConnectionRequest request)
    {
        OnConnectionRequest?.Invoke(reader, request);
    }

    private void HandlePeerConnected(ITransportPeer peer)
    {
        OnPeerConnected?.Invoke(peer);
    }

    private void HandlePeerDisconnected(ITransportPeer peer, DisconnectReason reason)
    {
        OnPeerDisconnected?.Invoke(peer, reason);
    }

    private void HandleNetworkReceive(ITransportPeer peer, NetDataReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        OnNetworkReceive?.Invoke(peer, reader, channel, deliveryMethod);
    }

    private void HandleNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        OnNetworkError?.Invoke(endPoint, socketError);
    }

    private void HandleNetworkLatencyUpdate(ITransportPeer peer, int latency)
    {
        OnNetworkLatencyUpdate?.Invoke(peer, latency);
    }
}
