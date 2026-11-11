using System.Collections;
using UnityEngine;

/// <summary>
/// Death and respawn orchestration for Soledar.
///
/// Listens to PlayerHealth.OnDied, then: freezes the player (input map off,
/// components off, physics off), fades the sprite out, teleports back to the
/// active checkpoint, restores full health and fades in again.
///
/// The Player GameObject is never destroyed, so scene references (HealthUI,
/// Cinemachine camera, checkpoints) stay valid — this replaces the old
/// Destroy(gameObject) death which broke every reference.
///
/// Wiring (configured by Tools > Soledar > Setup Phase 1 Scene):
///   - Lives on the Player GameObject.
///   - respawnPoint: scene object "SpawnPoint" (updated by Checkpoint triggers).
/// </summary>
[DisallowMultipleComponent]
public class PlayerRespawn : MonoBehaviour
{
    [Header("Respawn")]
    [Tooltip("Where the player respawns. Updated by Checkpoint triggers.")]
    [SerializeField] private Transform respawnPoint;
    [Tooltip("Seconds to wait between fade-out and teleport.")]
    [SerializeField] private float respawnDelay = 0.5f;
    [Tooltip("Duration of the fade-out/fade-in.")]
    [SerializeField] private float fadeDuration = 0.4f;

    private PlayerHealth health;
    private PlayerMovement movement;
    private PlayerAttack attack;
    private PlayerInputReader input;
    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private Vector3 fallbackSpawn;
    private bool respawning;

    void Awake()
    {
        health = GetComponent<PlayerHealth>();
        movement = GetComponent<PlayerMovement>();
        attack = GetComponent<PlayerAttack>();
        input = GetComponent<PlayerInputReader>();
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();

        if (health == null)
        {
            Debug.LogError("[PlayerRespawn] PlayerHealth missing on this GameObject — respawn system disabled.", this);
            enabled = false;
            return;
        }

        if (respawnPoint == null)
            Debug.LogWarning(
                "[PlayerRespawn] respawnPoint is not assigned — falling back to the player's start position. " +
                "Run 'Tools > Soledar > Setup Phase 1 Scene' to wire a SpawnPoint object.",
                this);

        fallbackSpawn = transform.position;
        health.OnDied += HandleDied;
    }

    void OnDestroy()
    {
        if (health != null)
            health.OnDied -= HandleDied;
    }

    /// <summary>Called by Checkpoint triggers to move the respawn location.</summary>
    public void SetRespawnPoint(Transform point)
    {
        if (point == null) return;
        respawnPoint = point;
        Debug.Log($"[PlayerRespawn] Respawn point updated to '{point.name}'.", this);
    }

    private void HandleDied()
    {
        if (respawning) return;
        respawning = true;
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        /* -------- 1. Freeze everything -------- */
        if (movement != null) movement.enabled = false;
        if (attack != null) attack.enabled = false;
        if (input != null) input.SetPlayerMapEnabled(false);

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false; // freezes the body completely
        }

        /* -------- 2. Fade out -------- */
        yield return FadeAlpha(fadeDuration, 0f);

        if (respawnDelay > 0f)
            yield return new WaitForSeconds(respawnDelay);

        /* -------- 3. Teleport + restore -------- */
        transform.position = respawnPoint != null ? respawnPoint.position : fallbackSpawn;

        if (movement != null) movement.ResetState();
        if (rb != null) rb.simulated = true;
        health.ResetHealth();

        /* -------- 4. Fade in -------- */
        yield return FadeAlpha(fadeDuration, 1f);

        /* -------- 5. Unfreeze -------- */
        if (movement != null) movement.enabled = true;
        if (attack != null) attack.enabled = true;
        if (input != null) input.SetPlayerMapEnabled(true);

        respawning = false;
        Debug.Log("[PlayerRespawn] Respawn complete.", this);
    }

    private IEnumerator FadeAlpha(float duration, float targetAlpha)
    {
        if (sprite == null) yield break;

        if (duration <= 0f)
        {
            Color instant = sprite.color;
            instant.a = targetAlpha;
            sprite.color = instant;
            yield break;
        }

        Color start = sprite.color;
        Color end = new Color(start.r, start.g, start.b, targetAlpha);

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // keeps working even if Time.timeScale changes later
            sprite.color = Color.Lerp(start, end, Mathf.Clamp01(t / duration));
            yield return null;
        }

        sprite.color = end;
    }
}
