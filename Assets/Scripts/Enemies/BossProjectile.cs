using UnityEngine;

/// <summary>
/// The Gatekeeper's lobbed ash-bomb (Phase 5 volley). Arcs under gravity,
/// damages the player once on touch, then pops; also pops when it lands on
/// solid ground, or despawns after its lifetime.
///
/// Placeholder visual: a tinted placeholder square, spawned inline (no prefab
/// needed) via Create(), or from a projectilePrefab when one is assigned.
/// </summary>
[DisallowMultipleComponent]
public class BossProjectile : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float lifetime = 4f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;

    /// <summary>Builds a placeholder shot at runtime (setup tool assigns the sprite).</summary>
    public static BossProjectile Create(Vector3 position, Sprite sprite)
    {
        GameObject go = new GameObject("GatekeeperShot");

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        if (sprite != null) sr.sprite = sprite;
        sr.sortingOrder = 3;

        Rigidbody2D body = go.AddComponent<Rigidbody2D>();
        body.gravityScale = 3f;
        body.freezeRotation = true;

        CircleCollider2D circle = go.AddComponent<CircleCollider2D>();
        circle.isTrigger = true;
        circle.radius = 0.3f;

        BossProjectile shot = go.AddComponent<BossProjectile>();
        if (sprite != null)
            shot.GetComponent<SpriteRenderer>().color = new Color(0.85f, 0.4f, 0.2f, 1f);
        return shot;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Launch(Vector2 velocity)
    {
        if (rb != null) rb.linearVelocity = velocity;
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
        if (player != null && !player.IsDead)
        {
            Vector2 direction = (player.transform.position - transform.position).normalized;
            player.TakeDamage(damage, direction);
            Destroy(gameObject);
            return;
        }

        if (other.GetComponentInParent<PogoSurface>() != null)
            return; // sail through bounce pads

        if (((1 << other.gameObject.layer) & groundLayer) != 0)
            Destroy(gameObject); // landed on solid ground → pop
    }
}
