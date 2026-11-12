using UnityEngine;

/// <summary>
/// NPC that starts a conversation when the player interacts with it.
///
/// Phase 4 adds quest groundwork:
///   - rewardDoor: when the conversation closes, the NPC grants this door
///     (the Cartographer hands over the "Cartographer's Seal" that opens the
///     Sealed Passage). The door's open state IS the persistence — the grant
///     never repeats once the door is open.
///   - repeatDialogue: optional shorter conversation used for later talks
///     after the reward has been granted.
///
/// Wiring (Phase 2 setup tool; reward wiring in Phase 4):
///   - Lives on the NPC GameObject (layer "Interactable", trigger collider).
///   - dialogue: DialogueData asset for this NPC.
///   - dialogueUI: the scene Canvas' DialogueUI component.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class NPCInteractable : MonoBehaviour, IInteractable
{
    [Header("Data")]
    [SerializeField] private DialogueData dialogue;
    [Tooltip("Optional: conversation used for later talks once the reward is granted.")]
    [SerializeField] private DialogueData repeatDialogue;

    [Header("Quest Reward")]
    [Tooltip("Optional: door unlocked when this NPC's conversation first completes.")]
    [SerializeField] private Door rewardDoor;
    [Tooltip("Name logged/reported for the reward (e.g. \"Cartographer's Seal\").")]
    [SerializeField] private string rewardName = "Cartographer's Seal";

    [Header("Wiring")]
    [SerializeField] private DialogueUI dialogueUI;

    private bool pendingRewardCheck;

    public bool CanInteract
    {
        get { return dialogue != null && dialogueUI != null && !dialogueUI.IsOpen; }
    }

    public string GetPrompt()
    {
        DialogueData current = UseRepeatDialogue() ? repeatDialogue : dialogue;
        return current != null ? $"Talk to {current.speakerName}" : "Talk";
    }

    public void Interact(GameObject interactor)
    {
        if (!CanInteract) return;

        DialogueData toShow = UseRepeatDialogue() ? repeatDialogue : dialogue;
        if (toShow == null || toShow.lines == null || toShow.lines.Length == 0) return;

        dialogueUI.Show(toShow);
        pendingRewardCheck = true; // granted on close (HandleDialogueClosed)
    }

    void OnEnable()
    {
        if (dialogueUI != null)
            dialogueUI.OnDialogueClosed += HandleDialogueClosed;
    }

    void OnDisable()
    {
        if (dialogueUI != null)
            dialogueUI.OnDialogueClosed -= HandleDialogueClosed;
    }

    void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    /// <summary>The reward is granted exactly once — door open state IS the persistence.</summary>
    private bool UseRepeatDialogue()
    {
        return rewardDoor != null && rewardDoor.IsOpen && repeatDialogue != null;
    }

    private void HandleDialogueClosed()
    {
        if (!pendingRewardCheck) return;
        pendingRewardCheck = false;

        if (rewardDoor == null || rewardDoor.IsOpen) return;

        rewardDoor.Unlock(rewardName);
        Debug.Log($"[NPCInteractable] Reward granted: {rewardName} → '{rewardDoor.DoorName}'.", this);
    }
}
