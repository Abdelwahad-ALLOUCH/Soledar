using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The single entry point for changing scenes (Phase 6).
///
/// Before loading it records the destination scene + spawn point in the save
/// file (so death, quit and restart all resume in the right place), then hands
/// the spawn id to <see cref="SceneFlow"/> for the destination scene's
/// SaveApplier to consume.
/// </summary>
public static class SceneLoader
{
    /// <summary>True while a transition is in flight (guards double triggers).</summary>
    public static bool IsLoading { get; private set; }

    /// <summary>
    /// Loads <paramref name="sceneName"/> and places the player at the
    /// SceneSpawnPoint named <paramref name="spawnPointId"/>.
    /// Returns false (and changes nothing) when the scene is not in Build Settings.
    /// </summary>
    public static bool BeginTransition(string sceneName, string spawnPointId)
    {
        if (IsLoading) return false;

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneLoader] Transition requested with no scene name.");
            return false;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                $"[SceneLoader] Scene '{sceneName}' is not in Build Settings — add it via " +
                "'Tools > Soledar > Setup Phase 6 Silent Palace' and try again.");
            return false;
        }

        IsLoading = true;

        // Persist the destination first: if the game is closed mid-transition,
        // the save still points at the new scene and spawn point.
        SaveManager.EnsureInstance().SetSceneCheckpoint(sceneName, spawnPointId);
        SceneFlow.RequestSpawn(sceneName, spawnPointId);

        // Clear the in-flight flag once the destination has actually loaded.
        // Without this, only the first transition of a session would ever run.
        SceneManager.sceneLoaded += HandleSceneLoaded;

        Debug.Log($"[SceneLoader] Loading '{sceneName}' (spawn '{spawnPointId}').");
        SceneManager.LoadScene(sceneName);
        return true;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        IsLoading = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        IsLoading = false;
    }
}
