using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 5;
    public float invincibilityTime = 0.6f;
    public float stunTime = 0.2f;

    [Header("Knockback")]
    public float knockbackForce = 8f;

    private int currentHealth;
    private bool isInvincible;
    private bool isStunned;

    private Rigidbody2D rb;

    public bool IsStunned => isStunned; // 👈 read-only access
    public int CurrentHealth => currentHealth;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage, Vector2 knockbackDir)
    {
        if (isInvincible) return;

        currentHealth -= damage;

        // HARD reset velocity so force is visible
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(knockbackDir.normalized * knockbackForce, ForceMode2D.Impulse);

        isInvincible = true;
        isStunned = true;

        Invoke(nameof(ResetInvincibility), invincibilityTime);
        Invoke(nameof(ResetStun), stunTime);

        if (currentHealth <= 0)
            Die();
    }

    void ResetInvincibility()
    {
        isInvincible = false;
    }

    void ResetStun()
    {
        isStunned = false;
    }

    void Die()
    {
        Debug.Log("Player Died");
        Destroy(gameObject);
    }
}
