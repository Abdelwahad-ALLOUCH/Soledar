using UnityEngine;

/// <summary>
/// Walk-over key pickup for gate progression. Touching it unlocks the target
/// Door. Collection is recorded in SaveManager (Phase 4) so keys stay collected
/// across sessions — Awake destroys the key if the save already has it.
///
/// Wiring (Phase 3 setup tool):
///   - Lives on the key GameObject with a small trigger collider.
///   - targetDoor: the Door this key opens.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class CollectibleKey : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string keyName = "Medina Gate Key";

    [Header("Wiring")]
    [SerializeField] private Door targetDoor;

    [Header("Feedback")]
    [SerializeField] private float bobAmplitude = 0.12f;
    [SerializeField] private float bobSpeed = 2.5f;

    public bool Collected { get; private set; }

    private Vector3 basePosition;
    private float bobTimer;
    private bool baseCached;

    void Awake()
    {
        // Persistence (Phase 4): already collected in the save file → gone.
        if (SaveManager.EnsureInstance().IsKeyCollected(keyName))
        {
            Destroy(gameObject);
            return;
        }

        basePosition = transform.position;
        baseCached = true;
        bobTimer = Random.value * Mathf.PI * 2f; // desync multiple keys
    }

    void Update()
    {
        if (Collected || !baseCached) return;

        bobTimer += Time.deltaTime * bobSpeed;
        transform.position = basePosition + Vector3.up * (Mathf.Sin(bobTimer) * bobAmplitude);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (Collected) return;

        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
        if (player == null || player.IsDead) return;

        Collected = true;

        // Persistence (Phase 4): record + auto-save, then unlock the target door
        // (which records + saves its own open state).
        SaveManager.EnsureInstance().MarkKeyCollected(keyName);

        if (targetDoor != null)
            targetDoor.Unlock(keyName);

        Debug.Log($"[CollectibleKey] '{keyName}' collected.", this);
        Destroy(gameObject);
    }

    void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }
}
