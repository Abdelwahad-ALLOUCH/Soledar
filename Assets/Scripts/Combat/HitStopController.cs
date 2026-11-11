using UnityEngine;

/// <summary>
/// Micro freeze-frame on powerful hits (hit-stop). Pauses the whole game for
/// a few milliseconds so impacts feel weighty — a core action-game feel trick.
///
/// Usage: HitStopController.Freeze(0.05f);
/// Stores the previous timeScale so it stays compatible with a future pause
/// menu (Phase 4). A hidden driver object restores the time scale using
/// unscaled time.
/// </summary>
public static class HitStopController
{
    private static HitStopDriver driver;
    private static float untilRealTime;
    private static bool active;
    private static float previousTimeScale = 1f;

    /// <summary>Freezes Time.timeScale for `duration` seconds (real time).</summary>
    public static void Freeze(float duration)
    {
        if (duration <= 0f) return;

        EnsureDriver();

        float now = Time.unscaledTime;
        untilRealTime = Mathf.Max(untilRealTime, now + duration);

        if (!active)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            active = true;
        }
    }

    private static void EnsureDriver()
    {
        if (driver != null) return;

        GameObject go = new GameObject("[HitStopDriver]");
        driver = go.AddComponent<HitStopDriver>();
        Object.DontDestroyOnLoad(go);
    }

    private static void Restore()
    {
        active = false;
        Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
    }

    /// <summary>Hidden helper that releases the freeze when time is up.</summary>
    private sealed class HitStopDriver : MonoBehaviour
    {
        void Update()
        {
            if (active && Time.unscaledTime >= untilRealTime)
                Restore();
        }

        void OnDestroy()
        {
            // Never leave the game stuck at timeScale 0 (e.g. on app quit).
            if (active)
                Restore();
        }
    }
}
