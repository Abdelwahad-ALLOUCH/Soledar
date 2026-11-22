using UnityEngine;
using UnityEngine.EventSystems;
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
///   - panel:          "PausePanel" (full-screen dark overlay, inactive by default)
///   - resumeButton:   "PausePanel/ResumeButton"
///   - abilitiesButton:"PausePanel/AbilitiesButton" (opens the owned-abilities page)
///   - abilitiesPanel: "PausePanel/AbilitiesPanel" (AbilitiesPanel)
///   - mainMenuButton: "PausePanel/MainMenuButton" (returns to the title screen)
///   - quitButton:     "PausePanel/QuitButton"
/// </summary>
[DisallowMultipleComponent]
public class PauseMenu : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button abilitiesButton;
    [SerializeField] private AbilitiesPanel abilitiesPanel;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;
    [Tooltip("Optional: keyboard/gamepad focus target when the menu opens.")]
    [SerializeField] private GameObject defaultSelection;

    [Header("Scenes")]
    [Tooltip("Title screen loaded by 'Main Menu'. Must be in Build Settings.")]
    [SerializeField] private string mainMenuScene = "MainMenu";

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

        if (abilitiesButton != null)
            abilitiesButton.onClick.AddListener(OpenAbilities);

        if (abilitiesPanel != null)
            abilitiesPanel.Closed += RestorePauseFocus;

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);

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

        // Always open on the action list, never on the page left behind by the
        // previous session.
        if (abilitiesPanel != null) abilitiesPanel.Close();

        IsPaused = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        if (panel != null) panel.SetActive(true);

        if (EventSystem.current != null && defaultSelection != null)
            EventSystem.current.SetSelectedGameObject(defaultSelection);

        Debug.Log("[PauseMenu] Paused.", this);
    }

    public void Resume()
    {
        if (!IsPaused) return;

        if (abilitiesPanel != null) abilitiesPanel.Close();

        IsPaused = false;
        Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;

        if (panel != null) panel.SetActive(false);

        // Return focus to gameplay so the next Interact press is not eaten by
        // the hidden UI selection.
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        Debug.Log("[PauseMenu] Resumed.", this);
    }

    /// <summary>
    /// Leaves the run and shows the title screen. The save file is untouched, so
    /// Continue resumes from the last checkpoint.
    /// </summary>
    public void ReturnToMainMenu()
    {
        if (!Application.CanStreamedLevelBeLoaded(mainMenuScene))
        {
            Debug.LogError($"[PauseMenu] '{mainMenuScene}' is not in Build Settings — cannot return to the menu.", this);
            return;
        }

        if (abilitiesPanel != null) abilitiesPanel.Close();

        IsPaused = false;

        if (panel != null) panel.SetActive(false);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        Debug.Log($"[PauseMenu] Returning to '{mainMenuScene}'.", this);

        // Fade to ink, load, fade back in — no hard cut from gameplay to the
        // title. The clock stays frozen while the ink closes (the fader runs on
        // unscaled time); the title screen restores it in MainMenuUI.Start.
        if (!ScreenFader.FadeToScene(mainMenuScene))
        {
            // Never leave the run frozen with a hidden panel.
            Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;
            return;
        }
    }

    /// <summary>Shows the owned-abilities page on top of the pause overlay.</summary>
    public void OpenAbilities()
    {
        if (abilitiesPanel == null)
        {
            Debug.LogWarning("[PauseMenu] No AbilitiesPanel wired — the page cannot open.", this);
            return;
        }

        abilitiesPanel.Open();
    }

    /// <summary>Returns keyboard/gamepad focus to the button that opened the page.</summary>
    private void RestorePauseFocus()
    {
        if (EventSystem.current != null && abilitiesButton != null)
            EventSystem.current.SetSelectedGameObject(abilitiesButton.gameObject);
    }

    private void Quit()
    {
        Debug.Log("[PauseMenu] Quit requested. (In the editor, stop Play mode manually.)", this);
        Application.Quit();
    }
}
