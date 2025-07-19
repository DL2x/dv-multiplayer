using MPAPI.Interfaces;
using MPAPI.Types;
using Multiplayer.Components.Networking;
using System;

namespace Multiplayer.API;

public class APIProvider : IMultiplayerAPI
{
    internal const string BUILT_AGAINST_API_VERSION = "0.1.0.0";

    public string SupportedApiVersion => BUILT_AGAINST_API_VERSION;

    public string MultiplayerVersion => Multiplayer.Ver;

    public bool IsMultiplayerLoaded => true;

    public bool IsConnected => NetworkLifecycle.Instance.IsClientRunning || NetworkLifecycle.Instance.IsServerRunning;

    public bool IsHost => NetworkLifecycle.Instance.IsHost();

    public bool IsDedicatedServer => false; //feature not implemented

    public bool IsSinglePlayer => NetworkLifecycle.Instance.IsServerRunning && (NetworkLifecycle.Instance?.Server.IsSinglePlayer ?? false);

    public event Action<uint> OnTick;
    public uint TICK_RATE => NetworkLifecycle.TICK_RATE;
    public uint CurrentTick => NetworkLifecycle.Instance.Tick;

    public bool TryGetNetId<T>(T obj, out ushort netId) where T : class
    {
        return NetIdProvider.Instance.TryGetNetId<T>(obj, out netId);
    }

    public bool TryGetObjectFromNetId<T>(ushort netId, out T obj) where T : class
    {
        return NetIdProvider.Instance.TryGetObject<T>(netId, out obj);
    }

    public void SetModCompatibility(string modId, MultiplayerCompatibility compatibility)
    {
        ModCompatibilityManager.Instance.RegisterCompatibility(modId, compatibility);
    }

    #region Class Helpers

    internal APIProvider()
    {
        NetworkLifecycle.Instance.OnTick += OnTickInternal;
    }

    internal void Dispose()
    {
        NetworkLifecycle.Instance.OnTick -= OnTickInternal;
    }

    internal void OnTickInternal(uint tick)
    {
        OnTick?.Invoke(tick);
    }

    #endregion
}
