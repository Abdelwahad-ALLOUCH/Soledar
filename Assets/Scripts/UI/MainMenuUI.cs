using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Title-screen controller (Phase 7).
///
/// Continue reads the save file: it loads the recorded scene and places the
/// player at the recorded spawn point (SceneLoader + SceneSpawnPoint). New Game
/// deletes the save and starts in the opening region with everything reset.
///
/// Hierarchy: the action the player most likely wants is styled as the single
/// gold primary button (Continue when a save exists, otherwise New Game); the
/// other is a quiet secondary. The primary is also the initial keyboard/gamepad
/// focus so a controller can start the game without touching the mouse.
///
/// Wiring (Tools > Soledar > Setup Main Menu):
///   - Lives on the menu Canvas.
///   - newGameButton / continueButton: the two buttons (each has a Label child
///     and a FocusBar child).
///   - statusText: small line under the buttons.
///   - newGameScene: opening scene name (must be in Build Settings).
/// </summary>
[DisallowMultipleComponent]
public class MainMenuUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;

    [Header("Wiring")]
    [SerializeField] private Text statusText;

    [Header("Scenes")]
    [SerializeField] private string newGameScene = "SunkenMedina";

    void Start()
    {
        // Never inherit a frozen clock from a paused session.
        Time.timeScale = 1f;

        if (newGameButton != null) newGameButton.onClick.AddListener(NewGame);
        if (continueButton != null) continueButton.onClick.AddListener(ContinueGame);

        Refresh();
    }

    /// <summary>
    /// Enables Continue only when a loadable save exists, applies the
    /// primary/secondary action styling and updates the status line.
    /// </summary>
    public void Refresh()
    {
        SaveManager save = SaveManager.EnsureInstance();
        string scene = string.IsNullOrEmpty(save.CurrentScene) ? newGameScene : save.CurrentScene;
        bool canContinue = save.HasSave && Application.CanStreamedLevelBeLoaded(scene);

        if (continueButton != null)
            continueButton.interactable = canContinue;

        // Content decides the hierarchy: a returning player's first action is
        // Continue; a first-time player's is New Game.
        Button primary = canContinue ? continueButton : newGameButton;
        Button secondary = canContinue ? newGameButton : continueButton;

        ApplyActionStyle(primary, true, true);
        ApplyActionStyle(secondary, false, secondary != null && secondary.interactable);

        if (statusText != null)
        {
            statusText.text = save.HasSave && !canContinue
                ? $"Saved journey in '{scene}' is unavailable"
                : canContinue
                    ? $"Continue — {SceneLabel(scene)}"
                    : "No journey yet";
        }

        // Give keyboard/gamepad players a visible starting point.
        if (EventSystem.current != null
            && EventSystem.current.currentSelectedGameObject == null
            && primary != null)
        {
            EventSystem.current.SetSelectedGameObject(primary.gameObject);
        }
    }

    /// <summary>Deletes the save and starts a fresh run in the opening region.</summary>
    public void NewGame()
    {
        SaveManager.EnsureInstance().DeleteSave();

        if (!Application.CanStreamedLevelBeLoaded(newGameScene))
        {
            Debug.LogError($"[MainMenu] '{newGameScene}' is not in Build Settings — New Game cannot start.", this);
            return;
        }

        Debug.Log($"[MainMenu] New game → '{newGameScene}'.", this);
        SceneManager.LoadScene(newGameScene);
    }

    /// <summary>Loads the saved scene and spawn point; restores health via SaveApplier.</summary>
    public void ContinueGame()
    {
        SaveManager save = SaveManager.EnsureInstance();
        if (!save.HasSave)
        {
            Debug.LogWarning("[MainMenu] Continue pressed with no save file — refreshing the menu.", this);
            Refresh();
            return;
        }

        string scene = string.IsNullOrEmpty(save.CurrentScene) ? newGameScene : save.CurrentScene;
        string spawnPointId = save.SpawnPointId;

        if (!Application.CanStreamedLevelBeLoaded(scene))
        {
            // The recorded region was removed/renamed: start at the opening
            // region rather than leaving the player on a dead button.
            Debug.LogWarning($"[MainMenu] Saved scene '{scene}' is not loadable — falling back to '{newGameScene}'.", this);
            scene = newGameScene;
            spawnPointId = "";
        }

        Debug.Log($"[MainMenu] Continue → '{scene}' (spawn '{spawnPointId}').", this);
        SceneLoader.BeginTransition(scene, spawnPointId);
    }

    /// <summary>
    /// Primary: gold fill, ink bold label (10:1 contrast). Secondary: raised
    /// panel with a gold label; disabled secondary dims to the muted tone.
    /// </summary>
    private static void ApplyActionStyle(Button button, bool primary, bool interactable)
    {
        if (button == null) return;

        Image background = button.GetComponent<Image>();
        Text label = button.GetComponentInChildren<Text>(true);

        // A disabled secondary keeps its plate and label legible (≈5:1 on the
        // raised panel) while gold alone signals "available action".
        Color idleLabel = primary
            ? SoledarUITheme.Ink
            : interactable ? SoledarUITheme.Gold : SoledarUITheme.WithAlpha(SoledarUITheme.Muted, 0.8f);

        if (background != null)
        {
            background.color = primary
                ? SoledarUITheme.Gold
                : interactable ? SoledarUITheme.PanelRaised : SoledarUITheme.WithAlpha(SoledarUITheme.PanelRaised, 0.85f);
        }

        if (label != null)
        {
            label.color = idleLabel;
            label.fontStyle = primary ? FontStyle.Bold : FontStyle.Normal;
        }

        SelectableFocusVisual focus = button.GetComponent<SelectableFocusVisual>();
        if (focus != null)
        {
            // The focus rule sits in the dark page margin, so it is gold for
            // both roles (an ink rule on ink would be invisible).
            focus.SetPalette(
                idleLabel,
                primary ? SoledarUITheme.Ink : SoledarUITheme.Ivory,
                SoledarUITheme.Gold);
        }
    }

    private static string SceneLabel(string sceneName)
    {
        switch (sceneName)
        {
            case "SunkenMedina": return "The Sunken Medina";
            case "SilentPalace": return "The Silent Palace";
            case "TestScene": return "Test Scene";
            default: return string.IsNullOrEmpty(sceneName) ? "The Sunken Medina" : sceneName;
        }
    }
}
