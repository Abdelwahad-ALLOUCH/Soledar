using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pause menu (Phase 4). Toggled by the Player/Pause action (Escape / Start).
///
/// Pausing sets Time.timeScale to 0 so every scaled-time system (movement,
/// physics, enemy AI, tweens) freezes; the Input System and unscaled-time
/// systems (dialogue typing) keep running, so Escape also unpauses.
///
/// Hit-stop interplay: HitStopController restores its own stored timeScale on
/// connect. If a hit-stop happens to end while the game is paused, the menu's
/// stored previous value (0) wins — the practical timing window makes this
/// unnoticeable.
///
/// Wiring (Phase 4 setup tool) — lives on the scene Canvas:
///   - panel:       "PausePanel" (full-screen dark overlay, inactive by default)
///   - resumeButton:"PausePanel/ResumeButton"
///   - quitButton:  "PausePanel/QuitButton"
/// </summary>
[DisallowMultipleComponent]
public class PauseMenu : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button quitButton;

    public bool IsPaused { get; private set; }

    private PlayerInputReader input;
    private float previousTimeScale = 1f;

    void Awake()
    {
        input = FindFirstObjectByType<PlayerInputReader>(FindObjectsInactive.Include);

        if (panel != null)
            panel.SetActive(false);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(Resume);

        if (quitButton != null)
            quitButton.onClick.AddListener(Quit);
    }

    void OnDestroy()
    {
        // Never leave the game frozen if the menu is destroyed while paused.
        if (IsPaused)
            Time.timeScale = previousTimeScale;
    }

    void Update()
    {
        if (input == null || !input.PauseWasPressed) return;

        if (IsPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (IsPaused) return;

        IsPaused = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        if (panel != null) panel.SetActive(true);
        Debug.Log("[PauseMenu] Paused.", this);
    }

    public void Resume()
    {
        if (!IsPaused) return;

        IsPaused = false;
        Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;

        if (panel != null) panel.SetActive(false);
        Debug.Log("[PauseMenu] Resumed.", this);
    }

    private void Quit()
    {
        Debug.Log("[PauseMenu] Quit requested. (In the editor, stop Play mode manually.)", this);
        Application.Quit();
    }
}
