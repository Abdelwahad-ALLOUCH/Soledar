using UnityEngine;

/// <summary>
/// Scene-configured progression gate that opens after its named ability is
/// unlocked (Phase 5 groundwork, live in Phase 6).
///
/// Used for the Silent Palace's Shadow Passages (requiredAbility
/// "shadowPassage") and for optional Wind of the Atlas secrets
/// (requiredAbility "windOfAtlas"). While locked the blocker on the "Ground"
/// layer blocks the player, enemies and their probes; the moment the ability is
/// purchased/unlocked the blocker (and the placeholder visual) disappear.
///
/// Wiring (Phase 6 setup tool):
///   - Lives on the barrier GameObject, layer "Ground".
///   - blocker: the barrier's BoxCollider2D.
///   - visual: the barrier sprite GameObject (optional; defaults to itself).
/// </summary>
[DisallowMultipleComponent]
public class AbilityGate : MonoBehaviour
{
    [Header("Requirement")]
    [SerializeField] private string requiredAbility = "shadowPassage";

    [Header("Wiring")]
    [SerializeField] private Collider2D blocker;
    [SerializeField] private GameObject visual;

    public bool IsUnlocked { get { return SaveManager.EnsureInstance().HasAbility(requiredAbility); } }

    void Awake()
    {
        if (blocker == null) blocker = GetComponent<Collider2D>();
        if (visual == null) visual = gameObject;
        Refresh();
    }

    void OnEnable()
    {
        SaveManager.AbilityUnlocked += HandleAbilityUnlocked;
        Refresh();
    }

    void OnDisable()
    {
        SaveManager.AbilityUnlocked -= HandleAbilityUnlocked;
    }

    /// <summary>Re-applies the locked/unlocked state (also usable from the editor tool).</summary>
    public void Refresh()
    {
        bool unlocked = Application.isPlaying && IsUnlocked;

        // In edit mode the scene should always show the authored (locked) state.
        if (blocker != null) blocker.enabled = !unlocked;
        if (visual != null && visual != gameObject) visual.SetActive(!unlocked);
    }

    private void HandleAbilityUnlocked(string abilityName)
    {
        if (abilityName != requiredAbility) return;

        Debug.Log($"[AbilityGate] '{requiredAbility}' unlocked — '{name}' opens.", this);
        Refresh();
    }
}
