using UnityEngine;

/// <summary>
/// Forgotten Bench — SOLEDAR's save point (Hollow Knight bench equivalent).
/// Resting restores full health and moves the respawn point here.
///
/// In later phases this becomes the real save trigger (world state, map,
/// abilities — Phase 4). For now it is a checkpoint you choose to use.
///
/// Wiring (Phase 3 setup tool):
///   - Lives on the bench GameObject (layer "Interactable", trigger collider).
/// </summary>
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class ForgottenBench : MonoBehaviour, IInteractable
{
    [Header("Identity")]
    [SerializeField] private string benchName = "Forgotten Bench";

    private PlayerRespawn cachedRespawn;
    private PlayerHealth cachedHealth;

    public bool CanInteract
    {
        get { return true; }
    }

    public string GetPrompt()
    {
        return $"Rest at {benchName}";
    }

    public void Interact(GameObject interactor)
    {
        if (cachedRespawn == null) cachedRespawn = interactor.GetComponent<PlayerRespawn>();
        if (cachedHealth == null) cachedHealth = interactor.GetComponent<PlayerHealth>();

        if (cachedRespawn != null)
            cachedRespawn.SetRespawnPoint(transform);

        if (cachedHealth != null)
            cachedHealth.ResetHealth();

        Debug.Log($"[ForgottenBench] Rested at '{benchName}' — health restored, respawn point set.", this);
    }

    void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }
}
