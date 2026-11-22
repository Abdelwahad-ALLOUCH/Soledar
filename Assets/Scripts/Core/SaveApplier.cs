using UnityEngine;

/// <summary>
/// Applies the loaded save to the player at scene start (Phase 4).
///
/// Phase 6 adds the scene-transition hand-off: when SceneLoader just requested
/// a spawn point, the player is teleported to that SceneSpawnPoint (and it
/// becomes the respawn point for this scene) instead of the saved coordinates.
///
/// Lives on the Player GameObject; added by the Phase 4 setup tool.
/// With no save file present this component does nothing.
/// </summary>
[DisallowMultipleComponent]
public class SaveApplier : MonoBehaviour
{
    void Start()
    {
        SaveManager manager = SaveManager.EnsureInstance();
        PlayerHealth health = GetComponent<PlayerHealth>();
        PlayerRespawn respawn = GetComponent<PlayerRespawn>();

        /* -------- Scene transition arrival (Phase 6) -------- */
        string pendingSpawnId = SceneFlow.ConsumePendingSpawn(gameObject.scene.name);
        if (!string.IsNullOrEmpty(pendingSpawnId) && respawn != null)
        {
            SceneSpawnPoint spawn = SceneSpawnPoint.Find(pendingSpawnId);
            if (spawn != null)
            {
                respawn.TeleportTo(spawn.transform.position);
                respawn.SetRespawnPoint(spawn.transform);

                // Keep the health the player had when they walked through the door.
                if (manager.HasSave && health != null)
                    health.SetHealth(manager.SavedHealth);

                Debug.Log($"[SaveApplier] Arrived in '{gameObject.scene.name}' at spawn '{pendingSpawnId}'.", this);
                return;
            }

            Debug.LogWarning(
                $"[SaveApplier] Scene '{gameObject.scene.name}' has no SceneSpawnPoint '{pendingSpawnId}' — " +
                "leaving the player at the authored position.", this);
        }

        if (!manager.HasSave) return;

        manager.ApplyToScene(health, respawn);
    }
}
