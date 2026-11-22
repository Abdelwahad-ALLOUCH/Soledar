using UnityEngine;

/// <summary>
/// Player movement controller for Soledar.
/// Run, variable-height jump with coyote time + jump buffering, wall slide,
/// wall jump, and dash with a ghost trail — all fed by PlayerInputReader
/// (Unity Input System, keyboard + gamepad).
///
/// IMPORTANT: public field names below are serialized in the scene/prefab.
/// Do NOT rename them (moveSpeed, jumpForce, coyoteTime, ...) without checking
/// what exists in Assets/Scenes/TestScene.unity and Assets/Prefabs/Player.
///
/// Tunables live in the Inspector. Movement values are preserved from the
/// original prototype so the current feel is unchanged.
///
/// Lives on the Player GameObject.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[DisallowMultipleComponent]
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

    [Header("Unlockable Jump")]
    [Tooltip("SaveManager ability name that enables the second mid-air jump.")]
    public string doubleJumpAbilityName = "windOfAtlas";
    public float doubleJumpForce = 11f;

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
    private PlayerInputReader input;
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
    private bool doubleJumpAvailable;

    /* ===================== PUBLIC STATE ===================== */
    /// <summary>1 = facing right, -1 = facing left. Drives attack direction and sprite flips.</summary>
    public int FacingDirection { get; private set; } = 1;

    public bool IsWallSliding { get { return isWallSliding; } }
    public bool IsDashing { get { return isDashing; } }

    /// <summary>True when the player's ground ray hits the ground layer right now.</summary>
    public bool IsGroundedNow { get { return IsGrounded(); } }

    /// <summary>
    /// True while another system owns the player's feet — currently the
    /// Lantern focus, which must root the player while a mask is restored.
    /// </summary>
    public bool MovementLocked { get; private set; }

    /// <summary>Cached PlayerHealth reference (may be null — guarded everywhere).</summary>
    public PlayerHealth Health { get { return playerHealth; } }

    /* ===================== UNITY ===================== */

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        originalGravity = rb.gravityScale;
        playerHealth = GetComponent<PlayerHealth>();
        doubleJumpAvailable = true;

        input = GetComponent<PlayerInputReader>();
        if (input == null)
        {
            Debug.LogError(
                "[PlayerMovement] PlayerInputReader is missing on the Player GameObject. " +
                "Run 'Tools > Soledar > Setup Phase 1 Scene' to configure the player, " +
                "or add a PlayerInputReader component manually.",
                this);
            enabled = false;
            return;
        }

        if (playerHealth == null)
            Debug.LogWarning("[PlayerMovement] No PlayerHealth on this GameObject — knockback stun handling is disabled.", this);

        if (groundCheck == null || wallCheck == null)
            Debug.LogError(
                "[PlayerMovement] groundCheck or wallCheck is not assigned. " +
                "Run 'Tools > Soledar > Setup Phase 1 Scene' with the TestScene open.",
                this);
    }

    void Update()
    {
        /* -------- TIMERS -------- */
        coyoteCounter -= Time.deltaTime;
        jumpBufferCounter -= Time.deltaTime;
        dashCooldownTimer -= Time.deltaTime;
        wallJumpLockCounter -= Time.deltaTime;

        if (playerHealth != null && playerHealth.IsDead)
            return; // frozen during the respawn sequence

        /* -------- INPUT (Unity Input System via PlayerInputReader) -------- */
        float moveInput = MovementLocked ? 0f : input.MoveInput.x;

        if (moveInput > 0) FacingDirection = 1;
        else if (moveInput < 0) FacingDirection = -1;

        /* -------- GROUND CHECK -------- */
        if (IsGrounded())
        {
            coyoteCounter = coyoteTime;
            doubleJumpAvailable = true;
        }

        /* -------- JUMP INPUT (buffered) -------- */
        if (!MovementLocked && input.JumpWasPressed)
            jumpBufferCounter = jumpBufferTime;

        /* -------- DASH INPUT -------- */
        if (!MovementLocked && input.DashWasPressed && dashCooldownTimer <= 0 && !isDashing)
            StartDash();

        HandleWallSlide(moveInput);
        HandleJump();
        HandleDash();
    }

    void FixedUpdate()
    {
        if (playerHealth != null && playerHealth.IsDead)
            return;

        if (!isDashing && wallJumpLockCounter <= 0 && (playerHealth == null || !playerHealth.IsStunned))
        {
            float moveInput = MovementLocked ? 0f : input.MoveInput.x;
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
                -FacingDirection * wallJumpForce.x,
                wallJumpForce.y
            );
            wallJumpLockCounter = wallJumpLockTime;
            jumpBufferCounter = 0;
        }

        if (jumpBufferCounter > 0 && coyoteCounter <= 0f && !isWallSliding &&
            doubleJumpAvailable && SaveManager.EnsureInstance().HasAbility(doubleJumpAbilityName))
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, doubleJumpForce);
            doubleJumpAvailable = false;
            jumpBufferCounter = 0f;
        }
    }

    /* ===================== WALL ===================== */

    void HandleWallSlide(float moveInput)
    {
        if (IsTouchingWall() && !IsGrounded() && moveInput == FacingDirection)
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
            rb.linearVelocity = new Vector2(FacingDirection * dashSpeed, 0);

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
        if (dashGhostPrefab == null)
        {
            Debug.LogWarning("[PlayerMovement] dashGhostPrefab is not assigned — dash trail disabled. " +
                             "Run 'Tools > Soledar > Setup Phase 1 Scene' to wire it.", this);
            return;
        }

        // Ziri's renderer lives on the "ZiriVisual" child, not on this physics root.
        SpriteRenderer playerSR = GetComponentInChildren<SpriteRenderer>();
        if (playerSR == null) return;

        GameObject ghost = Instantiate(dashGhostPrefab, transform.position, transform.rotation);

        SpriteRenderer ghostSR = ghost.GetComponent<SpriteRenderer>();
        if (ghostSR != null)
        {
            ghostSR.sprite = playerSR.sprite;
            ghostSR.flipX = playerSR.flipX;
        }

        Destroy(ghost, 0.25f);
    }

    /* ===================== STATE RESET ===================== */

    /// <summary>
    /// Descent of Ash pogo bounce (Phase 5): cancels any dash/wall state,
    /// restores normal gravity and launches the player upward. Called by
    /// PlayerAttack when a downward strike connects with something pogo-able.
    /// </summary>
    public void PogoBounce(float upwardForce)
    {
        isDashing = false;
        isWallSliding = false;
        doubleJumpAvailable = true;
        wallJumpLockCounter = 0f;
        coyoteCounter = 0f;
        ghostTimer = 0f;

        if (rb != null)
        {
            rb.gravityScale = originalGravity;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, upwardForce);
        }
    }

    /// <summary>
    /// Roots (or releases) the player. Used by PlayerHealing while the Lantern
    /// focus is charging; a death cancels it via ResetState.
    /// </summary>
    public void SetMovementLocked(bool locked)
    {
        MovementLocked = locked;
    }

    /// <summary>
    /// Clears all transient movement state (active dash, buffers, timers) and
    /// restores gravity. Called by PlayerRespawn after teleporting the player,
    /// so a death mid-dash cannot leave gravityScale stuck at zero.
    /// </summary>
    public void ResetState()
    {
        MovementLocked = false;
        isDashing = false;
        dashTime = 0f;
        dashCooldownTimer = 0f;
        coyoteCounter = 0f;
        jumpBufferCounter = 0f;
        wallJumpLockCounter = 0f;
        ghostTimer = 0f;
        isWallSliding = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = originalGravity;
        }
    }

    /* ===================== CHECKS ===================== */

    bool IsGrounded()
    {
        if (groundCheck == null) return false;
        // A single centre ray misses whenever the player stands exactly on a
        // tilemap cell seam (the spawn point is one), because the ray grazes
        // the seam between composite polygons. A short box across the feet
        // overlaps the ground regardless of seams.
        return Physics2D.OverlapBox(
            groundCheck.position,
            new Vector2(0.8f, checkDistance * 2f),
            0f,
            groundLayer
        ) != null;
    }

    bool IsTouchingWall()
    {
        if (wallCheck == null) return false;
        return Physics2D.Raycast(
            wallCheck.position,
            Vector2.right * FacingDirection,
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
                wallCheck.position + Vector3.right * FacingDirection * checkDistance
            );
        }
    }
}
