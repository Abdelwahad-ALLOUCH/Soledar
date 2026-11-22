using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The pause menu's Abilities page: owned abilities with a one-line description,
/// built from AbilityCatalog each time it opens.
///
/// Rows are generated at runtime so a newly unlocked ability needs no scene
/// work; only the empty page shell is authored. Focus starts on Back and stays
/// there (the main pause buttons are covered by this page), so a controller can
/// never wander into a hidden control.
///
/// Wiring (Tools > Soledar > Apply HUD Design):
///   - Lives on "PausePanel/AbilitiesPanel" (full-screen ink overlay, inactive).
///   - listRoot:  "AbilitiesList" (VerticalLayoutGroup container).
///   - emptyText: "AbilitiesEmpty" (shown only when nothing is owned).
///   - backButton:"AbilitiesBackButton".
/// </summary>
[DisallowMultipleComponent]
public class AbilitiesPanel : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private RectTransform listRoot;
    [SerializeField] private Text emptyText;
    [SerializeField] private Button backButton;

    [Header("Rows")]
    [SerializeField] private float rowHeight = 74f;

    /// <summary>Raised after the page closes, so the pause menu can restore focus.</summary>
    public event Action Closed;

    public bool IsOpen { get; private set; }

    void Awake()
    {
        if (backButton != null)
            backButton.onClick.AddListener(Close);

        if (emptyText != null)
            emptyText.gameObject.SetActive(false);
    }

    /// <summary>Shows the page and rebuilds the list from the current save.</summary>
    public void Open()
    {
        gameObject.SetActive(true);
        Rebuild();
        IsOpen = true;

        if (EventSystem.current != null && backButton != null)
            EventSystem.current.SetSelectedGameObject(backButton.gameObject);
    }

    public void Close()
    {
        if (!IsOpen) return;

        IsOpen = false;
        gameObject.SetActive(false);

        if (Closed != null) Closed();
    }

    private void Rebuild()
    {
        if (listRoot == null)
        {
            Debug.LogError("[AbilitiesPanel] listRoot is not wired — the ability list cannot be shown.", this);
            return;
        }

        // Destroy is deferred to the end of the frame, so detach first: reopening
        // the page twice in one frame must not draw the old rows underneath.
        for (int i = listRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = listRoot.GetChild(i);
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }

        List<AbilityCatalog.Entry> owned =
            AbilityCatalog.GetOwned(SaveManager.EnsureInstance());

        if (emptyText != null)
            emptyText.gameObject.SetActive(owned.Count == 0);

        for (int i = 0; i < owned.Count; i++)
            CreateRow(owned[i]);
    }

    private void CreateRow(AbilityCatalog.Entry entry)
    {
        GameObject row = new GameObject(entry.Name, typeof(RectTransform), typeof(LayoutElement));
        row.transform.SetParent(listRoot, false);

        LayoutElement layout = row.GetComponent<LayoutElement>();
        layout.preferredHeight = rowHeight;
        layout.flexibleHeight = 0f;

        Text name = CreateText(row.transform, "Name", entry.Name,
            SoledarUITheme.HeadingSize, SoledarUITheme.Ivory, FontStyle.Bold, TextAnchor.UpperLeft);
        Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, 0f), new Vector2(0f, 28f), new Vector2(0.5f, 1f));

        Text description = CreateText(row.transform, "Description", entry.Description,
            SoledarUITheme.SmallSize, SoledarUITheme.Muted, FontStyle.Normal, TextAnchor.UpperLeft);
        Place(description.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -30f), new Vector2(0f, 36f), new Vector2(0.5f, 1f));

        // Thin rule between entries: separates rows without a card or shadow.
        GameObject divider = new GameObject("Divider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        divider.transform.SetParent(row.transform, false);

        Image line = divider.GetComponent<Image>();
        line.color = SoledarUITheme.WithAlpha(SoledarUITheme.Rule, 0.8f);
        line.raycastTarget = false;

        Place((RectTransform)divider.transform, new Vector2(0f, 0f), new Vector2(1f, 0f),
            Vector2.zero, new Vector2(0f, 1f), new Vector2(0.5f, 0f));
    }

    /// <summary>Creates a legacy Text child (no TMP in this project).</summary>
    private static Text CreateText(Transform parent, string name, string content, int size,
        Color color, FontStyle style, TextAnchor alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);

        Text text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = content;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    private static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 position, Vector2 size, Vector2 pivot)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
