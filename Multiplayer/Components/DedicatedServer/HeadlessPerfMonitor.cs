using Multiplayer.Components.Networking;
using UnityEngine;

namespace Multiplayer.Components.DedicatedServer;

/// <summary>
/// Lightweight headless-server health readout: every <see cref="IntervalSeconds"/> it logs the
/// achieved frame rate and the achieved network tick rate. On a software-rendered headless server
/// the network tick is frame-driven, so a tick rate well below <see cref="NetworkLifecycle.TICK_RATE"/>
/// is the direct cause of client-side rubber-banding. Logged at Info; set LogLevel to Warning to hide.
/// </summary>
public class HeadlessPerfMonitor : MonoBehaviour
{
    private const float IntervalSeconds = 10f;

    private float elapsed;
    private int frames;
    private uint lastTick;

    private void Start()
    {
        lastTick = NetworkLifecycle.Instance != null ? NetworkLifecycle.Instance.Tick : 0u;
    }

    private void Update()
    {
        frames++;
        elapsed += Time.unscaledDeltaTime;
        if (elapsed < IntervalSeconds)
            return;

        uint tick = NetworkLifecycle.Instance != null ? NetworkLifecycle.Instance.Tick : lastTick;
        float fps = frames / elapsed;
        float tps = (tick - lastTick) / elapsed;
        Multiplayer.Log($"[Perf] {fps:F1} FPS, {tps:F1} ticks/s (target {NetworkLifecycle.TICK_RATE}).");

        elapsed = 0f;
        frames = 0;
        lastTick = tick;
    }
}
