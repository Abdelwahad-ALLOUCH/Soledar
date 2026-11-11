using UnityEngine;

/// <summary>
/// Anything the player can interact with (NPCs now; benches, doors, levers,
/// pickups in later phases). Implementations live on the object's collider.
/// </summary>
public interface IInteractable
{
    /// <summary>Short label for the HUD prompt, e.g. "Talk to The Old Cartographer".</summary>
    string GetPrompt();

    /// <summary>False while the object cannot currently be interacted with.</summary>
    bool CanInteract { get; }

    /// <summary>Executes the interaction. `interactor` is the player GameObject.</summary>
    void Interact(GameObject interactor);
}
