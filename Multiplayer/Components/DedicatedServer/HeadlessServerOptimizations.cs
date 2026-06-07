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

    public static void Apply()
    {
        if (applied)
            return;
        applied = true;

        // Health readout (FPS / achieved tick rate) for headless monitoring.
        GameObject monitor = new GameObject("MultiplayerHeadlessPerfMonitor");
        Object.DontDestroyOnLoad(monitor);
        monitor.AddComponent<HeadlessPerfMonitor>();

        if (!Multiplayer.Settings.HeadlessDisableRendering)
        {
            Multiplayer.Log("Headless dedicated mode: rendering left enabled (HeadlessDisableRendering=false).");
            return;
        }

        // Keep the loop comfortably above the network tick rate, but stop it spinning a CPU core
        // on empty frames once rendering is disabled.
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = Mathf.Max(NetworkLifecycle.TICK_RATE * 2, Multiplayer.Settings.HeadlessTargetFrameRate);

        // Disable cameras so Unity skips the per-frame scene render (and the image-effect/compute
        // passes that are pure waste here). Re-applied on scene loads to catch new cameras.
        DisableAllCameras();
        WorldStreamingInit.LoadingFinished += DisableAllCameras;
        SceneManager.sceneLoaded += OnSceneLoaded;

        Multiplayer.Log($"Headless dedicated mode: scene rendering disabled, targetFrameRate={Application.targetFrameRate}.");
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
