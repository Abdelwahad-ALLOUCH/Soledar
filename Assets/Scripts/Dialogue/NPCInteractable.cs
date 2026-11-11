using UnityEngine;

/// <summary>
/// NPC that starts a conversation when the player interacts with it.
/// Phase 2: The Old Cartographer placeholder. Quest hooks attach in Phase 4.
///
/// Wiring (Phase 2 setup tool):
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

    [Header("Wiring")]
    [SerializeField] private DialogueUI dialogueUI;

    public bool CanInteract
    {
        get { return dialogue != null && dialogueUI != null && !dialogueUI.IsOpen; }
    }

    public string GetPrompt()
    {
        return dialogue != null ? $"Talk to {dialogue.speakerName}" : "Talk";
    }

    public void Interact(GameObject interactor)
    {
        if (!CanInteract) return;
        dialogueUI.Show(dialogue);
    }

    void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }
}
