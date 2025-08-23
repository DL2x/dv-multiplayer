using DV.Logic.Job;
using MPAPI.Interfaces;
using MPAPI.Types;
using Multiplayer.Components.Networking;
using Multiplayer.Components.Networking.Train;
using Multiplayer.Networking.Data;
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

    public uint RegisterPaintTheme(string assetName)
    {
        if (string.IsNullOrEmpty(assetName))
        {
            Multiplayer.LogWarning("APIProvider.RegisterPaintTheme() called with empty assetName");
            return 0;
        }

        if (!NetworkLifecycle.Instance.IsServerRunning || !NetworkLifecycle.Instance.IsClientRunning)
        {
            Multiplayer.LogWarning("APIProvider.RegisterPaintTheme() called when server or client is not running");
            return 0;
        }

        return PaintThemeLookup.Instance.RegisterTheme(assetName);
    }

    public void UnregisterPaintTheme(uint themeId)
    {
        if (themeId == 0)
        {
            Multiplayer.LogWarning("APIProvider.UnregisterPaintTheme() called with themeId 0");
            return;
        }

        if (!NetworkLifecycle.Instance.IsServerRunning || !NetworkLifecycle.Instance.IsClientRunning)
        {
            Multiplayer.LogWarning("APIProvider.UnregisterPaintTheme() called when server or client is not running");
            return;
        }
    }

    public bool RegisterTaskType<TGameTask>(TaskType taskType, Func<TGameTask, TaskNetworkData> converter, Func<TaskType, TaskNetworkData> emptyCreator) where TGameTask : Task
    {
        return TaskNetworkDataFactory.RegisterTaskType<TGameTask>(taskType, converter, emptyCreator);
    }

    public bool UnRegisterTaskType<TGameTask>(TaskType taskType) where TGameTask : Task
    {
        return TaskNetworkDataFactory.UnRegisterTaskType<TGameTask>(taskType);
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
