using MPAPI.Interfaces;
using Multiplayer.Components.Networking;
using Multiplayer.Components.Networking.Train;
using Multiplayer.Networking.Data;
using System;
using UnityEngine;

namespace Multiplayer.API;

public class ServerPlayerWrapper : IPlayer
{
    private readonly ServerPlayer _serverPlayer;
    private readonly bool _isHost;

    public ServerPlayerWrapper(ServerPlayer serverPlayer)
    {
        _serverPlayer = serverPlayer;
        _isHost = NetworkLifecycle.Instance?.IsHost() ?? false;
    }

    public byte Id => _serverPlayer.Id;

    public string Username
    {
        get => _serverPlayer.Username;
        set => _serverPlayer.Username = value;
    }

    public Vector3 Position => _serverPlayer.WorldPosition;
    public float RotationY => _serverPlayer.WorldRotationY;
    public bool IsLoaded => _serverPlayer.IsLoaded;
    public bool IsHost => _isHost;
    public int Ping => 0; // Server doesn't track ping for players
    public bool IsOnCar => _serverPlayer.CarId != 0;
    public TrainCar OccupiedCar => GetOccupiedCar();

    internal TrainCar GetOccupiedCar()
    {
        NetworkedTrainCar.TryGet(_serverPlayer.CarId, out TrainCar trainCar);
        return trainCar;
    }
}
