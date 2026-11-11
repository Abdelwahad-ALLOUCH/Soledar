using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 3;
    public float knockbackForce = 6f;

    private int currentHealth;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage, Vector2 knockbackDir)
    {
        currentHealth -= damage;

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(knockbackDir * knockbackForce, ForceMode2D.Impulse);

        if (currentHealth <= 0)
        {
            Die();
        }
    }
    void OnCollisionEnter2D(Collision2D collision)
{
    PlayerHealth player = collision.gameObject.GetComponent<PlayerHealth>();
    if (player != null)
    {
        Vector2 knockbackDir = (collision.transform.position - transform.position).normalized;
        player.TakeDamage(1, knockbackDir);
    }
}


    void Die()
    {
        Destroy(gameObject);
    }
}
