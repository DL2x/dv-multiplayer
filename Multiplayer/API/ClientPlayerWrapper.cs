using MPAPI.Interfaces;
using Multiplayer.Components.Networking.Player;
using Multiplayer.Networking.Data;
using System;
using UnityEngine;

namespace Multiplayer.API;

public class ClientPlayerWrapper : IPlayer
{
    private readonly NetworkedPlayer _networkedPlayer;
    private readonly bool _isHost;

    public ClientPlayerWrapper(NetworkedPlayer networkedPlayer, bool isHost = false)
    {
        _networkedPlayer = networkedPlayer;
        _isHost = isHost;
    }

    public byte Id => _networkedPlayer.Id;
    public string Username
    {
        get => _networkedPlayer.Username;
        set => _networkedPlayer.Username = value;
    }
    public Guid Guid => Guid.Empty; // NetworkedPlayer doesn't store GUID
    public Vector3 Position => _networkedPlayer.transform.position;
    public float RotationY => _networkedPlayer.transform.rotation.eulerAngles.y;
    public bool IsLoaded => true; // If we have the object, it's loaded
    public bool IsHost => _isHost;
    public int Ping => _networkedPlayer.GetPing();
    public bool IsOnCar => _networkedPlayer.IsOnCar; // You'll need to add this logic
    public TrainCar OccupiedCar => _networkedPlayer.OccupiedCar; // You'll need to track this in NetworkedPlayer
}
