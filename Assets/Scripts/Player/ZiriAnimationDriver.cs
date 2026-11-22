using UnityEngine;

[RequireComponent(typeof(Animator))]
[DisallowMultipleComponent]
public class ZiriAnimationDriver : MonoBehaviour
{
    [SerializeField] private float walkThreshold = 0.05f;
    [SerializeField] private float speedDamping = 0.08f;
    private Animator animator;
    private Rigidbody2D body;

    void Awake()
    {
        animator = GetComponent<Animator>();
        body = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (animator == null) return;
        float speed = body != null ? Mathf.Abs(body.linearVelocity.x) : 0f;
        animator.SetFloat("Speed", speed > walkThreshold ? speed : 0f, speedDamping, Time.deltaTime);
    }
}