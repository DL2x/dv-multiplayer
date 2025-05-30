using System;
using UnityEngine;


namespace MPAPI.Interfaces;

public interface IServer
{
    public event Action<bool> OnPlayerConnected;
    public event Action<bool> OnPlayerDisconnected;

    public abstract float AnyPlayerSqrMag(GameObject item);

    public abstract float AnyPlayerSqrMag(Vector3 anchor);
}
