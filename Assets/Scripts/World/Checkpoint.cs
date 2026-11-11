using UnityEngine;

/// <summary>
/// Respawn checkpoint trigger. When Soledar walks into it, PlayerRespawn
/// updates its respawn location to this object's position.
///
/// Phase 3 will evolve this into the full Forgotten Bench save points
/// (save progress, restore health, update respawn, review abilities).
///
/// Wiring:
///   - Needs a Collider2D with Is Trigger enabled (the setup tool creates one).
///   - Give it a visible sprite so players can recognize it.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class Checkpoint : MonoBehaviour
{
    [Header("Behaviour")]
    [Tooltip("If true, the checkpoint stops updating the respawn point after the first use this session.")]
    [SerializeField] private bool deactivateAfterUse = false;

    private bool usedThisSession;

    void Reset()
    {
        // Editor convenience: a newly added Checkpoint defaults to a trigger.
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerRespawn respawn = other.GetComponentInParent<PlayerRespawn>();
        if (respawn == null) return;

        if (deactivateAfterUse && usedThisSession) return;
        usedThisSession = true;

        respawn.SetRespawnPoint(transform);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.35f);

        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
            Gizmos.DrawWireCube(transform.TransformPoint(box.offset), box.size);
    }
}
