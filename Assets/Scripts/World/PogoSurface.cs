using System.Collections;
using UnityEngine;

/// <summary>
/// A surface the player can strike from above with the Descent of Ash
/// down-attack (Phase 5) to bounce — the bounce pad under the first death gap
/// today, breakable secret floors later.
///
/// Behaviour:
///   - PlayerAttack's downward strike calls Struck() when it overlaps.
///   - The player always bounces off a healthy surface (that is the point).
///   - If breakable, Struck() also counts down hitsToBreak; at zero the
///     surface disables its collider, fades out and destroys itself.
///
/// Wiring (Phase 5 setup tool): small trigger collider + tinted placeholder.
/// Lives on the surface GameObject.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class PogoSurface : MonoBehaviour
{
    [Header("Feedback")]
    [SerializeField] private float pulseScale = 1.25f;
    [SerializeField] private float pulseTime = 0.15f;

    [Header("Breakable (optional)")]
    [SerializeField] private bool breakable = false;
    [SerializeField] private int hitsToBreak = 1;

    public bool IsBroken { get; private set; }

    private SpriteRenderer sprite;
    private Collider2D col;
    private Vector3 baseScale = Vector3.one;
    private float pulseTimer;

    void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        baseScale = transform.localScale;
    }

    void Update()
    {
        if (pulseTimer <= 0f) return;

        pulseTimer -= Time.deltaTime;
        float t = Mathf.Clamp01(pulseTimer / pulseTime);
        transform.localScale = baseScale * Mathf.Lerp(1f, pulseScale, t);

        if (pulseTimer <= 0f)
            transform.localScale = baseScale;
    }

    /// <summary>Called by the down-strike. Returns true when the player should bounce.</summary>
    public bool Struck()
    {
        if (IsBroken) return false;

        pulseTimer = pulseTime;

        if (breakable)
        {
            hitsToBreak--;
            if (hitsToBreak <= 0)
                Break();
        }

        return true;
    }

    private void Break()
    {
        IsBroken = true;
        Debug.Log($"[PogoSurface] '{name}' broke.", this);

        if (col != null) col.enabled = false;
        StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        if (sprite != null)
        {
            Color start = sprite.color;
            Color end = new Color(start.r, start.g, start.b, 0f);
            float t = 0f;
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                sprite.color = Color.Lerp(start, end, Mathf.Clamp01(t / 0.4f));
                yield return null;
            }
        }

        Destroy(gameObject);
    }
}
