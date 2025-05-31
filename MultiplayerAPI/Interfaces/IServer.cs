using System;
using System.Collections.Generic;
using UnityEngine;


namespace MPAPI.Interfaces;

public interface IServer
{
    event Action<IPlayer> OnPlayerConnected;
    event Action<IPlayer> OnPlayerDisconnected;

    int PlayerCount { get; }

    //public IReadOnlyCollection<IPlayer> Players { get; }

    float AnyPlayerSqrMag(GameObject item);

    float AnyPlayerSqrMag(Vector3 anchor);
}
