using UnityEngine;

/// <summary>
/// Deals contact damage to the player when the enemy physically touches them.
/// Has its own cooldown so standing inside the enemy doesn't machine-gun hits
/// (the player's own i-frames remain the second layer of protection).
///
/// Replaces the contact damage that used to live inside EnemyHealth.
/// Lives on every enemy GameObject next to EnemyHealth.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class ContactDamage : MonoBehaviour
{
    [SerializeField] private EnemyData data;

    [Header("Fallback (used when no EnemyData assigned)")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float cooldown = 0.8f;

    private EnemyHealth health;
    private float cooldownTimer;

    void Awake()
    {
        health = GetComponent<EnemyHealth>();
    }

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        TryDamage(collision.gameObject, collision.transform);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamage(collision.gameObject, collision.transform);
    }

    private void TryDamage(GameObject other, Transform otherTransform)
    {
        if (health != null && health.IsDead) return;
        if (cooldownTimer > 0f) return;

        PlayerHealth player = otherTransform.GetComponentInParent<PlayerHealth>();
        if (player == null || player.IsDead) return;

        // Push the player away from the enemy (behaviour preserved from prototype).
        Vector2 direction = (otherTransform.position - transform.position).normalized;
        int dmg = data != null ? data.contactDamage : damage;
        float cd = data != null ? data.contactCooldown : cooldown;

        player.TakeDamage(dmg, direction);
        cooldownTimer = cd;
    }
}
