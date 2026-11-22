using UnityEngine;

/// <summary>Presentation-only controller for Ziri. Gameplay remains owned by
/// PlayerMovement; this component mirrors facing, drives the Idle/Walk/Run
/// animator from horizontal speed, and falls back to a subtle bob on the old
/// static sprite if no animator is assigned.
///
/// IMPORTANT: this component lives on the "ZiriVisual" child of the Player,
/// not on the Player root. The root is driven by a Rigidbody2D, so writing a
/// bob offset to its Transform every frame would fight the physics body and
/// can stop the player from moving. Keeping the renderer on a child lets the
/// idle bob move the art without touching the collider, ground check, or
/// attack point.</summary>
[RequireComponent(typeof(SpriteRenderer))]
[DisallowMultipleComponent]
public class ZiriVisual : MonoBehaviour
{
    [SerializeField] private float idleBobAmount = 0.03f;
    [SerializeField] private float idleBobSpeed = 2f;
    [SerializeField] private Animator animator;

    private static readonly int SpeedParameter = Animator.StringToHash("Speed");

    private PlayerMovement movement;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D body;
    private Vector3 baseLocalPosition;
    private bool initialized;

    void Awake()
    {
        movement = GetComponentInParent<PlayerMovement>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        body = GetComponentInParent<Rigidbody2D>();
        baseLocalPosition = transform.localPosition;
        initialized = true;

        if (GetComponent<Rigidbody2D>() != null)
        {
            Debug.LogWarning(
                "[ZiriVisual] This component is on a physics body. Keep it on the " +
                "'ZiriVisual' child of the Player so the idle bob cannot move the collider.",
                this);
        }
    }

    void OnEnable()
    {
        if (initialized) transform.localPosition = baseLocalPosition;
    }

    void LateUpdate()
    {
        if (spriteRenderer == null) return;

        if (movement != null)
            spriteRenderer.flipX = movement.FacingDirection < 0;

        // Frame animation owns the vertical motion; the bob would fight it.
        if (animator != null)
        {
            float speed = body != null ? Mathf.Abs(body.linearVelocity.x) : 0f;
            animator.SetFloat(SpeedParameter, speed);
            return;
        }

        // Only bob when this Transform is not the playing physics body.
        if (GetComponent<Rigidbody2D>() != null) return;

        float bob = Mathf.Sin(Time.time * idleBobSpeed) * idleBobAmount;
        transform.localPosition = baseLocalPosition + Vector3.up * bob;
    }
}
