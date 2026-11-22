using UnityEngine;

/// <summary>
/// Named arrival anchor for scene transitions (Phase 6).
///
/// Each playable scene places one per entrance (e.g. "palace_entry" in the
/// Silent Palace, "medina_return" in the Medina). SaveApplier looks the point
/// up by id when a transition or a save file asks for it.
///
/// Lives on an empty GameObject (no collider, no renderer).
/// </summary>
[DisallowMultipleComponent]
public class SceneSpawnPoint : MonoBehaviour
{
    [SerializeField] private string spawnId = "default";

    public string SpawnId { get { return spawnId; } }

    /// <summary>Finds the active spawn point with this id, or null.</summary>
    public static SceneSpawnPoint Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        SceneSpawnPoint[] points = FindObjectsByType<SceneSpawnPoint>(FindObjectsSortMode.None);
        foreach (SceneSpawnPoint point in points)
        {
            if (point != null && point.SpawnId == id) return point;
        }

        return null;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.4f, 0.9f, 1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, 0.35f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1.2f);
    }
}
