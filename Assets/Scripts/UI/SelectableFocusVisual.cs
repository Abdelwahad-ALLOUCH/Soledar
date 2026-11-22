using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Visible keyboard / gamepad focus for UI buttons (Phase 7 polish).
///
/// Unity's default ColorTint is easy to miss on a dark panel, so focused or
/// hovered buttons show a short gold rule beside the label and brighten the
/// label itself. Disabled buttons never show the marker.
///
/// Wiring (Tools > Soledar > Apply HUD Design): lives beside the Button,
/// focusBar is a child Image (3 px rule), label the button's Text.
/// </summary>
[RequireComponent(typeof(Selectable))]
public class SelectableFocusVisual : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Wiring")]
    [SerializeField] private GameObject focusBar;
    [SerializeField] private Text label;

    [Header("Label Colours")]
    [SerializeField] private Color labelIdleColor = new Color(0.89f, 0.76f, 0.48f, 1f);
    [SerializeField] private Color labelFocusColor = new Color(0.98f, 0.92f, 0.78f, 1f);

    private Selectable selectable;
    private bool hovered;
    private bool selected;

    public bool IsFocused { get { return hovered || selected; } }

    /// <summary>
    /// Re-themes the label/rule for a button whose role changed at runtime
    /// (the main menu promotes Continue to primary once a save exists).
    /// </summary>
    public void SetPalette(Color idleLabel, Color focusLabel, Color barColor)
    {
        labelIdleColor = idleLabel;
        labelFocusColor = focusLabel;

        if (focusBar != null)
        {
            Image bar = focusBar.GetComponent<Image>();
            if (bar != null) bar.color = barColor;
        }

        Apply();
    }

    void Awake()
    {
        selectable = GetComponent<Selectable>();
    }

    void OnEnable()
    {
        Apply();
    }

    void OnDisable()
    {
        hovered = false;
        selected = false;
        Apply();
    }

    public void OnSelect(BaseEventData eventData)
    {
        selected = true;
        Apply();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        selected = false;
        Apply();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        Apply();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        Apply();
    }

    private void Apply()
    {
        bool available = selectable == null || selectable.IsInteractable();

        // Keyboard/gamepad selection always owns the rule. A hovered button only
        // shows one when nothing else is selected, so the menu never shows two
        // focus rules at once (default selection + mouse hover).
        bool otherSelected = !selected
            && EventSystem.current != null
            && EventSystem.current.currentSelectedGameObject != null
            && EventSystem.current.currentSelectedGameObject != gameObject;

        bool focused = available && (selected || (hovered && !otherSelected));

        if (focusBar != null) focusBar.SetActive(focused);
        if (label != null) label.color = focused ? labelFocusColor : labelIdleColor;
    }
}
