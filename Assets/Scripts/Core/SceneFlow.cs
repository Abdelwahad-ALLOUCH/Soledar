using UnityEngine;

/// <summary>
/// Hand-off between a scene transition and the scene being loaded (Phase 6).
///
/// A static field survives <see cref="UnityEngine.SceneManagement.SceneManager.LoadScene"/>
/// (the C# domain is not reloaded on a scene load), so the destination scene's
/// SaveApplier can read the requested spawn point during its Start().
///
/// The request is keyed by scene name: a spawn id is only handed out in the
/// scene it was meant for, so a failed/redirected load can never teleport the
/// player to coordinates from another region.
///
/// Cleared on play start so stale values never leak between Play sessions when
/// "Enter Play Mode Options" disables domain reload.
/// </summary>
public static class SceneFlow
{
    private static string pendingSceneName;
    private static string pendingSpawnId;

    /// <summary>Spawn point requested by the transition that is currently loading.</summary>
    public static string PendingSpawnId { get { return pendingSpawnId; } }

    /// <summary>Called by SceneLoader right before the destination scene loads.</summary>
    public static void RequestSpawn(string sceneName, string spawnPointId)
    {
        pendingSceneName = sceneName;
        pendingSpawnId = spawnPointId;
    }

    /// <summary>
    /// Returns the pending spawn id when <paramref name="sceneName"/> is the
    /// scene that was requested, then forgets the request. Returns null in any
    /// other scene.
    /// </summary>
    public static string ConsumePendingSpawn(string sceneName)
    {
        if (string.IsNullOrEmpty(pendingSpawnId)) return null;
        if (pendingSceneName != sceneName) return null;

        string id = pendingSpawnId;
        pendingSceneName = null;
        pendingSpawnId = null;
        return id;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        pendingSceneName = null;
        pendingSpawnId = null;
    }
}
