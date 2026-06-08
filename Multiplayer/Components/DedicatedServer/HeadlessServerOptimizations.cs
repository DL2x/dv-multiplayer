using Multiplayer.Components.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Multiplayer.Components.DedicatedServer;

/// <summary>
/// Frees CPU on a headless dedicated server by stopping the (GPU-less, software-rendered)
/// scene render and capping the otherwise near-empty frame loop.
///
/// We do NOT use Unity's -nographics: the GfxDevice stays alive, so render/compute-shader code
/// paths are simply never *driven* (cameras disabled) rather than crashing on a NullGfxDevice.
/// On a software renderer the per-frame scene render is the dominant CPU cost, and the network
/// tick coroutine is frame-driven, so cutting render cost directly raises the achievable tick rate.
/// </summary>
public static class HeadlessServerOptimizations
{
    private static bool applied;

    /// <summary>
    /// The frame-rate cap we want held, as a multiple of the network tick rate. -1 = no cap set.
    /// <see cref="HeadlessPerfMonitor"/> re-asserts this every frame because the game overwrites
    /// Application.targetFrameRate from its own FrameLimit preference after the world loads.
    /// </summary>
    public static int DesiredTargetFrameRate { get; private set; } = -1;

    public static void Apply()
    {
        if (applied)
            return;
        applied = true;

        // Health readout (FPS / tick rate) + manual frame limiter (the real FPS cap; Wine ignores
        // Application.targetFrameRate, so a steady frame rate is held via an OS sleep instead).
        GameObject monitor = new GameObject("MultiplayerHeadlessServer");
        Object.DontDestroyOnLoad(monitor);
        monitor.AddComponent<HeadlessPerfMonitor>();
        monitor.AddComponent<HeadlessFrameLimiter>();

        // Cap the frame rate to a MULTIPLE of the network tick rate. The tick is one-per-frame
        // paced to TICK_INTERVAL, so smooth (even) tick spacing -> smooth trains needs a steady FPS
        // that divides evenly into TICK_RATE. An off-multiple/fluctuating FPS (e.g. an uncapped GPU
        // at ~120, or a 60 cap = 2.5x 24) makes ticks land on a varying number of frames -> jitter.
        int tickRate = NetworkLifecycle.TICK_RATE;
        int requested = Mathf.Max(Multiplayer.Settings.HeadlessTargetFrameRate, tickRate * 2);
        DesiredTargetFrameRate = Mathf.RoundToInt(requested / (float)tickRate) * tickRate;
        EnforceFrameRateCap();
        // Marker so the log unambiguously shows this build's OS-sleep frame limiter is active
        // (distinct from the older, Wine-ineffective Application.targetFrameRate approach).
        Multiplayer.Log($"Headless dedicated mode: OS-sleep frame limiter active, capping to {DesiredTargetFrameRate} FPS.");

        if (!Multiplayer.Settings.HeadlessDisableRendering)
        {
            Multiplayer.Log($"Headless dedicated mode: rendering left enabled, targetFrameRate={DesiredTargetFrameRate}.");
            return;
        }

        // Disable cameras so Unity skips the per-frame scene render (and the image-effect/compute
        // passes that are pure waste here). Re-applied on scene loads to catch new cameras.
        DisableAllCameras();
        WorldStreamingInit.LoadingFinished += DisableAllCameras;
        SceneManager.sceneLoaded += OnSceneLoaded;

        Multiplayer.Log($"Headless dedicated mode: scene rendering disabled, targetFrameRate={DesiredTargetFrameRate}.");
    }

    /// <summary>
    /// Re-assert the FPS cap. The game sets Application.targetFrameRate from its FrameLimit pref
    /// (default 0 = unlimited) after load, overriding our cap, so this is called every frame.
    /// </summary>
    public static void EnforceFrameRateCap()
    {
        if (DesiredTargetFrameRate <= 0)
            return;
        if (QualitySettings.vSyncCount != 0)
            QualitySettings.vSyncCount = 0;
        if (Application.targetFrameRate != DesiredTargetFrameRate)
            Application.targetFrameRate = DesiredTargetFrameRate;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => DisableAllCameras();

    private static void DisableAllCameras()
    {
        try
        {
            Camera[] cameras = Camera.allCameras; // only currently-enabled cameras
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != null)
                    cameras[i].enabled = false;
            }
        }
        catch (System.Exception e)
        {
            Multiplayer.LogWarning($"HeadlessServerOptimizations.DisableAllCameras() failed: {e.Message}");
        }
    }
}
