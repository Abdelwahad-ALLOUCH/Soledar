using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{

    /* ===================== MOVEMENT ===================== */
    [Header("Movement")]
    public float moveSpeed = 6f;

    /* ===================== JUMP ===================== */
    [Header("Jump")]
    public float jumpForce = 12f;
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.15f;

    /* ===================== WALL ===================== */
    [Header("Wall")]
    public float wallSlideSpeed = 2f;
    public Vector2 wallJumpForce = new Vector2(8f, 12f);
    public float wallJumpLockTime = 0.2f;

    /* ===================== DASH ===================== */
    [Header("Dash")]
    public float dashSpeed = 16f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 0.5f;

    /* ===================== ATTACK ===================== */
    [Header("Attack")]
    public Transform attackPoint;
    public float attackRange = 0.5f;
    public LayerMask enemyLayer;
    public float attackCooldown = 0.3f;

    /* ===================== CHECKS ===================== */
    [Header("Checks")]
    public Transform groundCheck;
    public Transform wallCheck;
    public float checkDistance = 0.1f;
    public LayerMask groundLayer;

    /* ===================== DASH VFX ===================== */
    [Header("Dash VFX")]
    public GameObject dashGhostPrefab;
    public float ghostInterval = 0.03f;

    /* ===================== PRIVATE ===================== */
    private Rigidbody2D rb;
    private float moveInput;
    private PlayerHealth playerHealth;

    private float coyoteCounter;
    private float jumpBufferCounter;

    private bool isWallSliding;
    private float wallJumpLockCounter;

    private bool isDashing;
    private float dashTime;
    private float dashCooldownTimer;
    private float originalGravity;

    private float ghostTimer;
    private float attackCooldownTimer;

    private int facingDirection = 1; // 1 = right, -1 = left

    /* ===================== UNITY ===================== */

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        originalGravity = rb.gravityScale;
        playerHealth = GetComponent<PlayerHealth>();

    }

    void Update()
    {
        /* -------- INPUT -------- */
        moveInput = Input.GetAxisRaw("Horizontal");

        if (moveInput > 0) facingDirection = 1;
        else if (moveInput < 0) facingDirection = -1;

        UpdateAttackPointPosition();

        /* -------- TIMERS -------- */
        coyoteCounter -= Time.deltaTime;
        jumpBufferCounter -= Time.deltaTime;
        dashCooldownTimer -= Time.deltaTime;
        wallJumpLockCounter -= Time.deltaTime;
        attackCooldownTimer -= Time.deltaTime;

        /* -------- GROUND CHECK -------- */
        if (IsGrounded())
            coyoteCounter = coyoteTime;

        /* -------- JUMP INPUT -------- */
        if (Input.GetKeyDown(KeyCode.Space))
            jumpBufferCounter = jumpBufferTime;

        /* -------- DASH INPUT -------- */
        if (Input.GetKeyDown(KeyCode.LeftShift) && dashCooldownTimer <= 0 && !isDashing)
            StartDash();

        /* -------- ATTACK INPUT -------- */
        if (Input.GetKeyDown(KeyCode.J) && attackCooldownTimer <= 0)
        {
            Attack();
            attackCooldownTimer = attackCooldown;
        }

        HandleWallSlide();
        HandleJump();
        HandleDash();
    }

    void FixedUpdate()
{
    if (!isDashing && wallJumpLockCounter <= 0 && !playerHealth.IsStunned)
    {
        rb.linearVelocity = new Vector2(
            moveInput * moveSpeed,
            rb.linearVelocity.y
        );
    }
}

    /* ===================== JUMP ===================== */

    void HandleJump()
    {
        if (jumpBufferCounter > 0 && coyoteCounter > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferCounter = 0;
            coyoteCounter = 0;
        }

        if (jumpBufferCounter > 0 && isWallSliding)
        {
            rb.linearVelocity = new Vector2(
                -facingDirection * wallJumpForce.x,
                wallJumpForce.y
            );
            wallJumpLockCounter = wallJumpLockTime;
            jumpBufferCounter = 0;
        }
    }

    /* ===================== WALL ===================== */

    void HandleWallSlide()
    {
        if (IsTouchingWall() && !IsGrounded() && moveInput == facingDirection)
        {
            isWallSliding = true;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
        }
        else
        {
            isWallSliding = false;
        }
    }

    /* ===================== DASH ===================== */

    void StartDash()
    {
        isDashing = true;
        dashTime = dashDuration;
        dashCooldownTimer = dashCooldown;
        rb.gravityScale = 0;
        ghostTimer = 0;
    }

    void HandleDash()
    {
        if (!isDashing) return;

        if (dashTime > 0)
        {
            rb.linearVelocity = new Vector2(facingDirection * dashSpeed, 0);

            ghostTimer -= Time.deltaTime;
            if (ghostTimer <= 0)
            {
                CreateDashGhost();
                ghostTimer = ghostInterval;
            }

            dashTime -= Time.deltaTime;
        }
        else
        {
            isDashing = false;
            rb.gravityScale = originalGravity;
        }
    }

    void CreateDashGhost()
    {
        GameObject ghost = Instantiate(
            dashGhostPrefab,
            transform.position,
            transform.rotation
        );

        SpriteRenderer ghostSR = ghost.GetComponent<SpriteRenderer>();
        SpriteRenderer playerSR = GetComponent<SpriteRenderer>();

        ghostSR.sprite = playerSR.sprite;
        ghostSR.flipX = playerSR.flipX;

        Destroy(ghost, 0.25f);
    }

    /* ===================== ATTACK ===================== */

    void UpdateAttackPointPosition()
    {
        if (attackPoint == null) return;

        Vector3 pos = attackPoint.localPosition;
        pos.x = Mathf.Abs(pos.x) * facingDirection;
        attackPoint.localPosition = pos;
    }

    void Attack()
{
    Collider2D[] enemiesHit = Physics2D.OverlapCircleAll(
        attackPoint.position,
        attackRange,
        enemyLayer
    );

    foreach (Collider2D enemy in enemiesHit)
    {
        EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            Vector2 knockbackDir = Vector2.right * facingDirection;
            enemyHealth.TakeDamage(1, knockbackDir);
        }
    }
}


    /* ===================== CHECKS ===================== */

    bool IsGrounded()
    {
        return Physics2D.Raycast(
            groundCheck.position,
            Vector2.down,
            checkDistance,
            groundLayer
        );
    }

    bool IsTouchingWall()
    {
        return Physics2D.Raycast(
            wallCheck.position,
            Vector2.right * facingDirection,
            checkDistance,
            groundLayer
        );
    }

    /* ===================== DEBUG ===================== */

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(
                groundCheck.position,
                groundCheck.position + Vector3.down * checkDistance
            );
        }

        if (wallCheck != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(
                wallCheck.position,
                wallCheck.position + Vector3.right * facingDirection * checkDistance
            );
        }

        if (attackPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}
