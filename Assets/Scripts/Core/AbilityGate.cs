using UnityEngine;

/// <summary>Scene-configured progression gate that opens after its named ability is unlocked.</summary>
[DisallowMultipleComponent]
public class AbilityGate : MonoBehaviour
{
    [SerializeField] private string requiredAbility = "windOfAtlas";
    [SerializeField] private Collider2D blocker;
    [SerializeField] private GameObject visual;
    void Awake() { if (blocker == null) blocker = GetComponent<Collider2D>(); Refresh(); }
    public bool IsUnlocked { get { return SaveManager.EnsureInstance().HasAbility(requiredAbility); } }
    public void Refresh()
    {
        if (blocker != null) blocker.enabled = !IsUnlocked;
        if (visual != null) visual.SetActive(!IsUnlocked);
    }
}
