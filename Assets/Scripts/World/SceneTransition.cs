using UnityEngine;

/// <summary>
/// Walk-through doorway between scenes (Phase 6).
///
/// Place it just past a gate/arch. When the player enters the trigger it asks
/// SceneLoader to load the destination scene and place the player at the named
/// SceneSpawnPoint there.
///
/// The cooldown covers the moments right after the scene starts, so a spawn
/// point standing next to a return doorway can never bounce the player
/// straight back out.
///
/// Wiring (Phase 6 setup tool): lives on a trigger collider on any layer; the
/// collider is set to isTrigger automatically.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class SceneTransition : MonoBehaviour
{
    [Header("Destination")]
    [Tooltip("Scene name as it appears in Build Settings (e.g. \"SilentPalace\").")]
    [SerializeField] private string targetScene = "SilentPalace";
    [Tooltip("SceneSpawnPoint id in the destination scene.")]
    [SerializeField] private string targetSpawnId = "palace_entry";

    [Header("Safety")]
    [Tooltip("Seconds after scene start / previous use before this trigger can fire again.")]
    [SerializeField] private float reentryCooldown = 1.5f;

    private float readyTime;

    void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        readyTime = Time.time + reentryCooldown;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (Time.time < readyTime) return;

        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
        if (player == null || player.IsDead) return;

        // One request per pass-through, even if several colliders enter at once.
        readyTime = Time.time + reentryCooldown;
        SceneLoader.BeginTransition(targetScene, targetSpawnId);
    }

    void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }
}
