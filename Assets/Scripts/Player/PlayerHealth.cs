using System;
using UnityEngine;

/// <summary>
/// Player health, damage feedback, invincibility frames and death signalling.
///
/// Raises C# events consumed by HealthUI (mask display) and PlayerRespawn
/// (death sequence). The GameObject is NEVER destroyed on death — the respawn
/// system teleports the player back to the active checkpoint instead, so scene
/// references (camera, UI) remain valid.
///
/// Serialized field names (maxHealth, invincibilityTime, stunTime, knockbackForce)
/// are preserved from the original prototype so existing scene values keep working.
///
/// Lives on the Player GameObject.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[DisallowMultipleComponent]
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private float invincibilityTime = 0.6f;
    [SerializeField] private float stunTime = 0.2f;

    [Header("Knockback")]
    [SerializeField] private float knockbackForce = 8f;

    [Header("Hurt Feedback")]
    [Tooltip("Sprite tint applied for a moment when taking damage.")]
    [SerializeField] private Color hurtFlashColor = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField] private float hurtFlashDuration = 0.15f;

    /// <summary>(currentHealth, maxHealth). Raised on damage and on respawn reset.</summary>
    public event Action<int, int> OnHealthChanged;

    /// <summary>Raised every time damage gets through the invincibility guard.</summary>
    public event Action OnDamaged;

    /// <summary>Raised once when health reaches zero. PlayerRespawn listens to this.</summary>
    public event Action OnDied;

    public int CurrentHealth { get; private set; }
    public int MaxHealth { get { return maxHealth; } }
    public bool IsInvincible { get; private set; }
    public bool IsStunned { get; private set; }
    public bool IsDead { get; private set; }

    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private Color spriteBaseColor = Color.white;
    private bool spriteBaseColorCached;

    private float invincibilityTimer;
    private float stunTimer;
    private float flashTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();

        if (sprite != null)
        {
            spriteBaseColor = sprite.color;
            spriteBaseColorCached = true;
        }

        CurrentHealth = maxHealth;
        IsDead = false;
    }

    void Update()
    {
        if (IsDead) return;

        if (invincibilityTimer > 0f)
        {
            invincibilityTimer -= Time.deltaTime;
            if (invincibilityTimer <= 0f)
            {
                invincibilityTimer = 0f;
                IsInvincible = false;
            }
        }

        if (stunTimer > 0f)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f)
            {
                stunTimer = 0f;
                IsStunned = false;
            }
        }

        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f && sprite != null)
            {
                sprite.color = spriteBaseColor;
                flashTimer = 0f;
            }
        }
    }

    public void TakeDamage(int damage, Vector2 knockbackDir)
    {
        if (IsDead || IsInvincible || damage <= 0) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);

        // HARD reset velocity so the knockback impulse is clearly visible
        // (behaviour preserved from the original prototype).
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(knockbackDir.normalized * knockbackForce, ForceMode2D.Impulse);

        IsInvincible = true;
        IsStunned = true;
        invincibilityTimer = invincibilityTime;
        stunTimer = stunTime;

        if (sprite != null)
        {
            sprite.color = hurtFlashColor;
            flashTimer = hurtFlashDuration;
        }

        if (OnDamaged != null) OnDamaged();
        if (OnHealthChanged != null) OnHealthChanged(CurrentHealth, maxHealth);

        if (CurrentHealth <= 0)
            Die();
    }

    /// <summary>
    /// Restores full health and clears every damage state. Called by
    /// PlayerRespawn right after teleporting the player back to the checkpoint.
    /// </summary>
    public void ResetHealth()
    {
        CurrentHealth = maxHealth;
        IsDead = false;
        IsInvincible = false;
        IsStunned = false;
        invincibilityTimer = 0f;
        stunTimer = 0f;
        flashTimer = 0f;

        if (sprite != null && spriteBaseColorCached)
            sprite.color = spriteBaseColor;

        if (OnHealthChanged != null) OnHealthChanged(CurrentHealth, maxHealth);
    }

    /// <summary>
    /// Sets current health directly (clamped to 0..maxHealth). Used by the
    /// save system to restore a saved health value on load.
    /// </summary>
    public void SetHealth(int value)
    {
        CurrentHealth = Mathf.Clamp(value, 0, maxHealth);
        if (OnHealthChanged != null) OnHealthChanged(CurrentHealth, maxHealth);
    }

    /// <summary>
    /// Kills the player, ignoring invincibility frames — used by death pits
    /// and other inescapable hazards (falling must always be lethal).
    /// </summary>
    public void Kill()
    {
        if (IsDead) return;

        CurrentHealth = 0;
        IsInvincible = false;
        IsStunned = false;
        invincibilityTimer = 0f;
        stunTimer = 0f;
        flashTimer = 0f;

        if (rb != null) rb.linearVelocity = Vector2.zero;

        if (sprite != null && spriteBaseColorCached)
            sprite.color = spriteBaseColor;

        if (OnHealthChanged != null) OnHealthChanged(CurrentHealth, maxHealth);

        Die();
    }

    private void Die()
    {
        if (IsDead) return;

        IsDead = true;
        IsStunned = false;
        Debug.Log("[PlayerHealth] Player died — respawn sequence starting (Player GameObject stays alive).", this);

        if (OnDied != null) OnDied();
    }
}
