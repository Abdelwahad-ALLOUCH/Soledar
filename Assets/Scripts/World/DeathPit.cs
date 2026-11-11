using UnityEngine;

/// <summary>
/// Kill volume for pits and chasms. Bypasses invincibility frames on purpose:
/// falling into a pit must always be lethal, even right after taking damage.
///
/// Wiring (Phase 3 setup tool):
///   - Invisible trigger box stretched across the region bottom.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class DeathPit : MonoBehaviour
{
    void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
        if (player == null || player.IsDead) return;

        Debug.Log("[DeathPit] Player fell into a pit.", this);
        player.Kill();
    }
}
