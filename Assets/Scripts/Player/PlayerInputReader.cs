using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Central input gateway for the player. Reads the shared Input System asset
/// (Assets/InputSystem_Actions.inputactions) and exposes polling-style
/// properties so gameplay systems never call the Input System directly.
///
/// Only the "Player" action map is enabled/disabled here. The "UI" map belongs
/// to the EventSystem's InputSystemUIInputModule and must remain untouched.
///
/// Actions used (bound for keyboard AND gamepad):
///   Move    WASD / arrow keys / left stick
///   Jump    Space / gamepad A (buttonSouth)
///   Dash    LeftShift / gamepad B (buttonEast) / RB (rightShoulder)
///   Attack  J / Enter / mouse left / gamepad X (buttonWest)
///   Interact E / gamepad Y (buttonNorth)
///   Pause   Escape / gamepad Start (added in Phase 4)
///
/// Lives on the Player GameObject. Configured by Tools > Soledar > Setup Phase 1 Scene.
/// </summary>
[DisallowMultipleComponent]
public class PlayerInputReader : MonoBehaviour
{
    [Header("Asset Reference")]
    [Tooltip("Must reference Assets/InputSystem_Actions.inputactions")]
    [SerializeField] private InputActionAsset inputActions;

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction dashAction;
    private InputAction attackAction;
    private InputAction interactAction;
    private InputAction pauseAction;
    private bool initialized;

    /// <summary>Current movement vector (x = horizontal). Zero when input is disabled.</summary>
    public Vector2 MoveInput
    {
        get { return IsReady() ? moveAction.ReadValue<Vector2>() : Vector2.zero; }
    }

    public bool JumpWasPressed { get { return IsReady() && jumpAction.WasPressedThisFrame(); } }
    public bool DashWasPressed { get { return IsReady() && dashAction.WasPressedThisFrame(); } }
    public bool AttackWasPressed { get { return IsReady() && attackAction.WasPressedThisFrame(); } }
    public bool InteractWasPressed { get { return IsReady() && interactAction.WasPressedThisFrame(); } }

    /// <summary>
    /// Pause/Escape press. Consumed by PauseMenu (Phase 4). Unlike the other
    /// presses this works even while the Player map holds gameplay input —
    /// pausing must always be available.
    /// </summary>
    public bool PauseWasPressed { get { return IsReady() && pauseAction.WasPressedThisFrame(); } }

    /// <summary>True while the Player action map is enabled. Frozen during the death sequence.</summary>
    public bool PlayerMapEnabled { get; private set; }

    void Awake()
    {
        Initialize();
    }

    /// <summary>Resolves the required actions from the assigned asset. Safe to call repeatedly.</summary>
    public void Initialize()
    {
        if (initialized) return;

        if (inputActions == null)
        {
            Debug.LogError(
                "[PlayerInputReader] No InputActionAsset assigned. " +
                "Run 'Tools > Soledar > Setup Phase 1 Scene' with the TestScene open, " +
                "or assign Assets/InputSystem_Actions.inputactions in the Inspector.",
                this);
            enabled = false;
            return;
        }

        moveAction = inputActions.FindAction("Player/Move", throwIfNotFound: false);
        jumpAction = inputActions.FindAction("Player/Jump", throwIfNotFound: false);
        dashAction = inputActions.FindAction("Player/Dash", throwIfNotFound: false);
        attackAction = inputActions.FindAction("Player/Attack", throwIfNotFound: false);
        interactAction = inputActions.FindAction("Player/Interact", throwIfNotFound: false);
        pauseAction = inputActions.FindAction("Player/Pause", throwIfNotFound: false);

        if (moveAction == null || jumpAction == null || dashAction == null ||
            attackAction == null || interactAction == null || pauseAction == null)
        {
            Debug.LogError(
                "[PlayerInputReader] Missing required action(s) in the assigned asset: " +
                "Player/Move, Player/Jump, Player/Dash, Player/Attack, Player/Interact, Player/Pause. " +
                "Check Assets/InputSystem_Actions.inputactions.",
                this);
            enabled = false;
            return;
        }

        initialized = true;
    }

    void OnEnable()
    {
        SetPlayerMapEnabled(true);
    }

    void OnDisable()
    {
        SetPlayerMapEnabled(false);
    }

    /// <summary>
    /// Enables/disables ONLY the Player action map. Used by PlayerRespawn to
    /// freeze input during the death sequence without touching the UI map.
    /// </summary>
    public void SetPlayerMapEnabled(bool enable)
    {
        if (!initialized) Initialize();
        if (!initialized) return;

        var map = inputActions.FindActionMap("Player", throwIfNotFound: false);
        if (map == null)
        {
            Debug.LogError("[PlayerInputReader] 'Player' action map not found in the assigned asset.", this);
            return;
        }

        if (enable && !map.enabled)
            map.Enable();
        else if (!enable && map.enabled)
            map.Disable();

        PlayerMapEnabled = map.enabled;
    }

    private bool IsReady()
    {
        if (!initialized) Initialize();
        return initialized && PlayerMapEnabled;
    }
}
