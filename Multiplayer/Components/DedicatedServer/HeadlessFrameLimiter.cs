using System.Threading;
using UnityEngine;

namespace Multiplayer.Components.DedicatedServer;

/// <summary>
/// Manual frame-rate limiter for the headless dedicated server.
///
/// Application.targetFrameRate is a no-op under Wine, so the loop runs uncapped (e.g. ~120 FPS on a
/// GPU host) and fluctuates. The network tick is one-per-frame paced to TICK_INTERVAL, so a
/// fluctuating frame rate makes tick spacing uneven -> clients (which interpolate train positions
/// assuming even spacing) see constant jitter. We hold a steady frame time ourselves with an OS
/// sleep (+ a short spin for precision), which works regardless of targetFrameRate support.
///
/// Runs in LateUpdate, which fires every frame even with rendering disabled (unlike
/// WaitForEndOfFrame, which depends on the render loop). A steady FPS that is a multiple of
/// TICK_RATE makes every tick land on the same number of frames -> even snapshots -> smooth trains.
/// </summary>
public class HeadlessFrameLimiter : MonoBehaviour
{
    private readonly System.Diagnostics.Stopwatch frameTimer = System.Diagnostics.Stopwatch.StartNew();

    private void LateUpdate()
    {
        int fps = HeadlessServerOptimizations.DesiredTargetFrameRate;
        if (fps <= 0)
        {
            frameTimer.Restart();
            return;
        }

        double targetMs = 1000.0 / fps;

        // Sleep most of the remaining budget (frees the CPU), then spin the last ~1 ms for precise,
        // even pacing. If the frame already overran the target (low-end host below the cap), don't
        // sleep at all.
        double remaining = targetMs - frameTimer.Elapsed.TotalMilliseconds;
        if (remaining > 2.0)
            Thread.Sleep((int)(remaining - 1.0));
        while (frameTimer.Elapsed.TotalMilliseconds < targetMs)
        { /* spin for sub-millisecond precision */ }

        frameTimer.Restart();
    }
}
