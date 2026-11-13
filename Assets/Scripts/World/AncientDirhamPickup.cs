using UnityEngine;

/// <summary>Persistent walk-over currency pickup for the vertical slice.</summary>
[RequireComponent(typeof(Collider2D))]
public class AncientDirhamPickup : MonoBehaviour
{
    [SerializeField] private string itemId = "dirham_001";
    [SerializeField] private int amount = 5;
    void Awake() { if (SaveManager.EnsureInstance().IsItemCollected(itemId)) Destroy(gameObject); }
    void Reset() { GetComponent<Collider2D>().isTrigger = true; }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerHealth>() == null) return;
        SaveManager save = SaveManager.EnsureInstance();
        if (save.IsItemCollected(itemId)) return;
        save.MarkItemCollected(itemId);
        save.AddDirhams(amount, itemId);
        Destroy(gameObject);
    }
}
