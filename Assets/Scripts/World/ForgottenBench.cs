using UnityEngine;

/// <summary>
/// Forgotten Bench — SOLEDAR's save point (Hollow Knight bench equivalent).
/// Resting restores full health and Echo Energy, moves the respawn point here
/// and writes the save file (Phase 4 persistence).
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
    private PlayerEnergy cachedEnergy;

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
        if (cachedEnergy == null) cachedEnergy = interactor.GetComponent<PlayerEnergy>();

        if (cachedRespawn != null)
            cachedRespawn.SetRespawnPoint(transform);

        if (cachedHealth != null)
            cachedHealth.ResetHealth();

        // A rest refills the Lantern too: benches are where a run resets.
        if (cachedEnergy != null)
            cachedEnergy.Refill();

        // Phase 4: bench rest = save. Snapshot health (now full) + respawn
        // position (now this bench) + keys/doors into the save file.
        SaveManager.EnsureInstance().SaveGame($"rest at '{benchName}'");

        Debug.Log($"[ForgottenBench] Rested at '{benchName}' — health and Echo Energy restored, respawn point set, game saved.", this);
    }

    void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }
}
