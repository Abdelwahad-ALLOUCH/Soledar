using UnityEngine;

/// <summary>Marks a named map room discovered when the player enters its trigger.</summary>
[RequireComponent(typeof(Collider2D))]
public class RoomDiscovery : MonoBehaviour
{
    [SerializeField] private string roomId = "medina_start";
    public string RoomId { get { return roomId; } }
    void Reset() { GetComponent<Collider2D>().isTrigger = true; }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerHealth>() != null) SaveManager.EnsureInstance().DiscoverRoom(roomId);
    }
}
