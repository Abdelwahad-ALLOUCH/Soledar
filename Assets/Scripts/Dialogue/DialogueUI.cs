using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dialogue box UI (Phase 2): speaker name, typed-out lines, advance with the
/// Interact input, close at the end. Raises OnDialogueClosed so gameplay
/// systems (PlayerInteractor) can unfreeze the player.
///
/// UI wiring (Phase 2 setup tool) — lives on the scene Canvas:
///   - panel:      "DialoguePanel" (hidden dark box, bottom of the screen)
///   - speakerText:"DialoguePanel/SpeakerText"
///   - bodyText:   "DialoguePanel/BodyText"
///   - hintText:   "DialoguePanel/HintText"
///
/// Typing effect uses unscaled time so it works during hit-stops.
/// </summary>
[DisallowMultipleComponent]
public class DialogueUI : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Text speakerText;
    [SerializeField] private Text bodyText;
    [SerializeField] private Text hintText;

    [Header("Typing")]
    [SerializeField] private float charactersPerSecond = 45f;
    [SerializeField] private float inputLockAfterOpen = 0.15f;

    /// <summary>Raised when the dialogue finishes and the panel closes.</summary>
    public event Action OnDialogueClosed;

    public bool IsOpen { get; private set; }
    public float LastClosedTime { get; private set; } = -999f;

    private DialogueData current;
    private int lineIndex;
    private float typeProgress;      // characters shown so far (float)
    private float inputLockTimer;
    private PlayerInputReader input;

    void Awake()
    {
        input = FindFirstObjectByType<PlayerInputReader>(FindObjectsInactive.Include);
        if (panel != null)
            panel.SetActive(false);
    }

    public void Show(DialogueData data)
    {
        if (data == null || data.lines == null || data.lines.Length == 0)
        {
            Debug.LogWarning("[DialogueUI] Show called with empty dialogue data.", this);
            return;
        }

        current = data;
        lineIndex = 0;
        typeProgress = 0f;
        inputLockTimer = inputLockAfterOpen;
        IsOpen = true;

        if (speakerText != null) speakerText.text = data.speakerName;
        if (panel != null) panel.SetActive(true);
        if (hintText != null) hintText.gameObject.SetActive(false);

        UpdateBodyText();
    }

    void Update()
    {
        if (!IsOpen || current == null) return;

        string fullLine = current.lines[lineIndex];

        if (typeProgress < fullLine.Length)
        {
            typeProgress += Time.unscaledDeltaTime * charactersPerSecond;
            UpdateBodyText();
        }
        else if (hintText != null)
        {
            hintText.gameObject.SetActive(true);
        }

        if (inputLockTimer > 0f)
        {
            inputLockTimer -= Time.unscaledDeltaTime;
            return; // ignore the key press that opened the dialogue
        }

        if (input == null || !input.InteractWasPressed) return;

        if (typeProgress < fullLine.Length)
        {
            typeProgress = fullLine.Length;   // skip typing
            UpdateBodyText();
        }
        else
        {
            lineIndex++;
            if (lineIndex >= current.lines.Length)
            {
                Close();
            }
            else
            {
                typeProgress = 0f;
                if (hintText != null) hintText.gameObject.SetActive(false);
                UpdateBodyText();
            }
        }
    }

    private void UpdateBodyText()
    {
        if (bodyText == null || current == null) return;

        string fullLine = current.lines[lineIndex];
        int count = Mathf.Min(fullLine.Length, Mathf.FloorToInt(typeProgress));
        bodyText.text = fullLine.Substring(0, count);
    }

    public void Close()
    {
        IsOpen = false;
        current = null;
        LastClosedTime = Time.time;

        if (panel != null) panel.SetActive(false);

        if (OnDialogueClosed != null) OnDialogueClosed();
    }
}
