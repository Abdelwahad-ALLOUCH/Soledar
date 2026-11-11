using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Detects nearby IInteractable objects, shows the HUD prompt and triggers
/// interactions with the Interact input (E / gamepad Y).
/// While a dialogue is open it also freezes the player's movement and attack
/// components so conversations pause gameplay — Hollow Knight style.
///
/// Wiring (Phase 2 setup tool):
///   - Lives on the Player GameObject.
///   - promptText: the "InteractionPrompt" Text on the scene Canvas.
/// </summary>
[DisallowMultipleComponent]
public class PlayerInteractor : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float interactRadius = 1.3f;
    [SerializeField] private LayerMask interactableMask;

    [Header("Wiring")]
    [SerializeField] private Text promptText;

    private PlayerInputReader input;
    private PlayerMovement movement;
    private PlayerAttack attack;
    private PlayerHealth health;
    private DialogueUI dialogue;
    private IInteractable current;

    void Awake()
    {
        input = GetComponent<PlayerInputReader>();
        movement = GetComponent<PlayerMovement>();
        attack = GetComponent<PlayerAttack>();
        health = GetComponent<PlayerHealth>();

        if (input == null)
            Debug.LogError("[PlayerInteractor] PlayerInputReader missing — interactions disabled. Run the Phase 1 setup tool.", this);

        dialogue = FindFirstObjectByType<DialogueUI>(FindObjectsInactive.Include);
        if (dialogue != null)
            dialogue.OnDialogueClosed += HandleDialogueClosed;

        if (health != null)
            health.OnDied += HandlePlayerDied;
    }

    void OnDestroy()
    {
        if (dialogue != null)
            dialogue.OnDialogueClosed -= HandleDialogueClosed;
        if (health != null)
            health.OnDied -= HandlePlayerDied;
    }

    void Update()
    {
        if (input == null) return;

        if (dialogue != null && dialogue.IsOpen)
        {
            SetPrompt(string.Empty);
            current = null;
            return;
        }

        /* -------- find the nearest interactable -------- */
        current = null;
        float best = float.MaxValue;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactRadius, interactableMask);
        foreach (Collider2D hit in hits)
        {
            IInteractable interactable = hit.GetComponentInParent<IInteractable>();
            if (interactable == null || !interactable.CanInteract) continue;

            float dist = Vector2.Distance(transform.position, hit.transform.position);
            if (dist < best)
            {
                best = dist;
                current = interactable;
            }
        }

        if (health != null && health.IsDead)
            current = null;

        SetPrompt(current != null ? current.GetPrompt() : string.Empty);

        // Small guard: don't re-trigger immediately after closing a dialogue.
        bool debounce = dialogue != null && Time.time - dialogue.LastClosedTime < 0.2f;

        if (current != null && input.InteractWasPressed && !debounce)
        {
            SetPlayerControls(false);
            current.Interact(gameObject);
        }
    }

    /// <summary>Freezes/unfreezes movement + attack while a dialogue plays.</summary>
    private void SetPlayerControls(bool enabled)
    {
        if (movement != null)
        {
            if (!enabled && movement.enabled)
                movement.ResetState();
            movement.enabled = enabled;
        }
        if (attack != null)
            attack.enabled = enabled;
    }

    private void HandleDialogueClosed()
    {
        // If the player died mid-dialogue, the death sequence owns the
        // controls (respawn unfreezes them when it finishes).
        if (health != null && health.IsDead) return;

        SetPlayerControls(true);
    }

    private void HandlePlayerDied()
    {
        // A dying player must never be locked inside an open dialogue.
        if (dialogue != null && dialogue.IsOpen)
            dialogue.Close();
    }

    private void SetPrompt(string message)
    {
        if (promptText == null) return;
        promptText.text = string.IsNullOrEmpty(message) ? string.Empty : $"E  {message}";
        promptText.gameObject.SetActive(!string.IsNullOrEmpty(message));
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
