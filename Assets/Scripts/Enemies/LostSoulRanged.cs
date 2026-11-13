using UnityEngine;

/// <summary>Floating ranged enemy using the shared health/damage contract.
/// Assign any SpriteRenderer sprite; projectiles are created at runtime.</summary>
[RequireComponent(typeof(EnemyHealth))]
public class LostSoulRanged : MonoBehaviour
{
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float fireInterval = 1.8f;
    [SerializeField] private float projectileSpeed = 6f;
    [SerializeField] private float hoverHeight = 0.2f;
    private Transform player;
    private EnemyHealth health;
    private float nextShot;
    private Vector3 basePosition;
    private Sprite sprite;
    void Awake()
    {
        health = GetComponent<EnemyHealth>();
        PlayerHealth found = FindFirstObjectByType<PlayerHealth>();
        if (found != null) player = found.transform;
        basePosition = transform.position;
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null) sprite = renderer.sprite;
    }
    void Update()
    {
        if (health.IsDead) return;
        transform.position = new Vector3(basePosition.x, basePosition.y + Mathf.Sin(Time.time * 2f) * hoverHeight, basePosition.z);
        if (player == null || Time.time < nextShot || Vector2.Distance(transform.position, player.position) > detectionRange) return;
        BossProjectile shot = BossProjectile.Create(transform.position, sprite);
        shot.Launch(((Vector2)(player.position - transform.position)).normalized * projectileSpeed);
        nextShot = Time.time + fireInterval;
    }
}
