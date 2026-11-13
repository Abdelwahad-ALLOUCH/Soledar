using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Enemy health, hurt reaction (flash + knockback + brief AI stun) and death.
/// Reads EnemyData when assigned, otherwise falls back to the Inspector values
/// below (which match the original prototype's serialized scene data).
///
/// Raised events: OnDamaged / OnDied / OnHealthChanged — Phase 2+ systems
/// (audio, VFX, kill rewards) hook in here.
///
/// Lives on every enemy GameObject.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[DisallowMultipleComponent]
public class EnemyHealth : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("When assigned, maxHealth/knockbackForce come from EnemyData.")]
    [SerializeField] private EnemyData data;

    [Header("Fallback (used when no EnemyData assigned)")]
    public int maxHealth = 3;
    public float knockbackForce = 6f;

    [Header("Hurt Reaction")]
    [SerializeField] private float hurtStunTime = 0.25f;     // AI pauses while stunned
    [SerializeField] private float hurtFlashDuration = 0.12f;

    [Header("Death")]
    [SerializeField] private float deathFadeDuration = 0.4f;

    public event Action<int, int> OnHealthChanged;
    public event Action OnDamaged;
    public event Action OnDied;

    public int CurrentHealth { get; private set; }
    public int MaxHealth { get { return data != null ? data.maxHealth : maxHealth; } }
    public bool IsDead { get; private set; }
    public bool IsStunned { get; private set; }

    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private Color baseColor = Color.white;
    private bool baseColorCached;

    private float stunTimer;
    private float flashTimer;
    private bool dying;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();

        if (sprite != null)
        {
            baseColor = sprite.color;
            baseColorCached = true;
        }

        CurrentHealth = MaxHealth;
        IsDead = false;
    }

    void Update()
    {
        if (IsDead) return;

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
            if (flashTimer <= 0f && sprite != null && baseColorCached)
            {
                sprite.color = baseColor;
                flashTimer = 0f;
            }
        }
    }

    /// <summary>
    /// Applies damage, knockback (scaled by knockbackMultiplier and reduced by
    /// the enemy's knockbackResist), hurt stun and flash. Signature keeps
    /// compatibility with PlayerAttack (extra parameters are optional).
    /// </summary>
    public void TakeDamage(int damage, Vector2 knockbackDir, float knockbackMultiplier = 1f)
    {
        if (IsDead || dying || damage <= 0) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            if (knockbackDir.sqrMagnitude > 0.0001f)
            {
                float resist = data != null ? data.knockbackResist : 0f;
                float force = (data != null ? data.knockbackForce : knockbackForce) * knockbackMultiplier;
                rb.AddForce(knockbackDir.normalized * force * (1f - resist), ForceMode2D.Impulse);
            }
        }

        IsStunned = true;
        stunTimer = hurtStunTime;

        if (sprite != null)
        {
            sprite.color = Color.white;   // flash white, restore below
            flashTimer = hurtFlashDuration;
        }

        if (OnDamaged != null) OnDamaged();
        if (OnHealthChanged != null) OnHealthChanged(CurrentHealth, MaxHealth);

        if (CurrentHealth <= 0)
            Die();
    }

    /// <summary>
    /// Restores full health and clears hurt state without re-running Awake.
    /// Used by the boss arena reset when the player dies mid-fight (Phase 5).
    /// </summary>
    public void ResetHealth()
    {
        if (dying) return;

        CurrentHealth = MaxHealth;
        IsStunned = false;
        stunTimer = 0f;
        flashTimer = 0f;
        if (sprite != null && baseColorCached)
            sprite.color = baseColor;

        if (OnHealthChanged != null) OnHealthChanged(CurrentHealth, MaxHealth);
    }

    private void Die()
    {
        if (IsDead || dying) return;

        IsDead = true;
        IsStunned = false;
        Debug.Log($"[EnemyHealth] '{name}' died.", this);
        if (OnDied != null) OnDied();

        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        dying = true;

        // Freeze the body and disable the behaviours that act on it.
        if (rb != null) rb.simulated = false;
        foreach (Collider2D col in GetComponents<Collider2D>())
            col.enabled = false;

        foreach (MonoBehaviour behaviour in GetComponents<MonoBehaviour>())
            if (behaviour != this && behaviour != null)
                behaviour.enabled = false;

        // Placeholder death animation: fade out (real animation in Phase 6).
        if (sprite != null && deathFadeDuration > 0f)
        {
            Color start = sprite.color;
            Color end = new Color(start.r, start.g, start.b, 0f);
            float t = 0f;
            while (t < deathFadeDuration)
            {
                t += Time.deltaTime;
                sprite.color = Color.Lerp(start, end, Mathf.Clamp01(t / deathFadeDuration));
                yield return null;
            }
        }

        Destroy(gameObject);
    }
}
