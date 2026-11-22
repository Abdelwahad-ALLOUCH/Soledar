#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Phase 7 — the title screen, styled with the SOLEDAR UI tokens.
///
/// "Tools > Soledar > Setup Main Menu" creates Assets/Scenes/MainMenu.unity,
/// builds the title block (title, gold rule, subtitle, premise, actions, status)
/// and makes the menu build scene 0. Re-running rebuilds the block so the scene
/// always matches this tool.
///
/// Layout intent: a left-aligned reading column, one gold primary action and one
/// quiet secondary action. Hierarchy comes from size, weight and colour; no
/// gradients, glass or decorative badges.
/// </summary>
public static class MainMenuBootstrap
{
    private const string MenuPath = "Assets/Scenes/MainMenu.unity";
    private const string OpeningScene = "SunkenMedina";

    private const float BlockWidth = 760f;
    private const float BlockHeight = 520f;

    [MenuItem("Tools/Soledar/Setup Main Menu")]
    public static void CreateOrUpdate()
    {
        Scene scene;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuPath) == null)
        {
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Debug.Log("[MainMenu] Creating Assets/Scenes/MainMenu.unity.");
        }
        else
        {
            scene = EditorSceneManager.OpenScene(MenuPath, OpenSceneMode.Single);
        }

        EnsureCamera();
        EnsureEventSystem();

        GameObject canvasGO = EnsureCanvas();
        RebuildContent(canvasGO.transform);

        EnsureMenuIsFirstBuildScene();

        EditorSceneManager.MarkSceneDirty(scene);
        if (EditorSceneManager.SaveScene(scene, MenuPath))
            Debug.Log("[MainMenu] Main menu COMPLETE — it is build scene 0.");
        else
            Debug.LogError("[MainMenu] Scene save failed — save manually (Ctrl+S).");
    }

    /* ================================================================= */

    private static void EnsureCamera()
    {
        if (Camera.main != null)
        {
            Camera.main.backgroundColor = SoledarUITheme.Ink;
            return;
        }

        GameObject go = new GameObject("Main Camera");
        Undo.RegisterCreatedObjectUndo(go, "Create Main Camera");
        go.tag = "MainCamera";
        go.transform.position = new Vector3(0f, 0f, -10f);

        Camera camera = go.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = SoledarUITheme.Ink;
        camera.depth = -1;

        go.AddComponent<AudioListener>();
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;

        GameObject go = new GameObject("EventSystem");
        Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }

    private static GameObject EnsureCanvas()
    {
        GameObject go = GameObject.Find("MenuCanvas");
        if (go == null)
        {
            go = new GameObject("MenuCanvas", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create MenuCanvas");
        }

        Canvas canvas = go.GetComponent<Canvas>();
        if (canvas == null) canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f; // balanced for desktop and narrow screens

        if (go.GetComponent<GraphicRaycaster>() == null)
            go.AddComponent<GraphicRaycaster>();

        return go;
    }

    /// <summary>Rebuilds the title block. The canvas is tool-owned, so this is safe to repeat.</summary>
    private static void RebuildContent(Transform canvas)
    {
        for (int i = canvas.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(canvas.GetChild(i).gameObject);

        // Full-bleed ink backdrop.
        GameObject background = CreateElement("MenuBackground", canvas,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = SoledarUITheme.Ink;
        backgroundImage.raycastTarget = false;

        // Left-aligned reading column.
        GameObject block = CreateElement("MenuContent", canvas,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(160f, 0f), new Vector2(BlockWidth, BlockHeight));

        // A single gold rule marks the brand margin; it is the only decoration.
        GameObject margin = CreateElement("MarginRule", block.transform,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(-32f, 0f), new Vector2(2f, 420f));
        Image marginImage = margin.AddComponent<Image>();
        marginImage.color = SoledarUITheme.WithAlpha(SoledarUITheme.Gold, 0.55f);
        marginImage.raycastTarget = false;

        CreateText("Title", block.transform, "SOLEDAR",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(700f, 96f),
            SoledarUITheme.DisplaySize, FontStyle.Bold, SoledarUITheme.Ivory, TextAnchor.MiddleLeft, false);

        GameObject rule = CreateElement("TitleRule", block.transform,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, -96f), new Vector2(96f, 3f));
        Image ruleImage = rule.AddComponent<Image>();
        ruleImage.color = SoledarUITheme.Gold;
        ruleImage.raycastTarget = false;

        CreateText("Subtitle", block.transform, "Echoes of a Forgotten Kingdom",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(700f, 30f),
            20, FontStyle.Normal, SoledarUITheme.Muted, TextAnchor.MiddleLeft, false)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -124f);

        Text premise = CreateText("Premise", block.transform,
            "The kingdom is not simply abandoned.\nSomething happened to it.",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(520f, 60f),
            18, FontStyle.Normal, SoledarUITheme.WithAlpha(SoledarUITheme.Ivory, 0.82f), TextAnchor.UpperLeft, true);
        premise.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -178f);
        premise.lineSpacing = 1.1f;

        GameObject divider = CreateElement("SectionRule", block.transform,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, -254f), new Vector2(520f, 1f));
        Image dividerImage = divider.AddComponent<Image>();
        dividerImage.color = SoledarUITheme.WithAlpha(SoledarUITheme.Rule, 0.9f);
        dividerImage.raycastTarget = false;

        Button newGame = CreateActionButton("NewGameButton", block.transform, "New Game",
            new Vector2(0f, -292f), primary: true);
        Button continueButton = CreateActionButton("ContinueButton", block.transform, "Continue",
            new Vector2(0f, -364f), primary: false);

        Text status = CreateText("StatusText", block.transform, "No journey yet",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(600f, 24f),
            SoledarUITheme.SmallSize, FontStyle.Normal, SoledarUITheme.Muted, TextAnchor.MiddleLeft, false);
        status.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -438f);

        WireMenu(canvas.gameObject, newGame, continueButton, status);
    }

    private static void WireMenu(GameObject canvasGO, Button newGame, Button continueButton, Text status)
    {
        MainMenuUI menu = canvasGO.GetComponent<MainMenuUI>();
        if (menu == null) menu = Undo.AddComponent<MainMenuUI>(canvasGO);

        SerializedObject serialized = new SerializedObject(menu);
        SetProperty(serialized, "newGameButton", newGame);
        SetProperty(serialized, "continueButton", continueButton);
        SetProperty(serialized, "statusText", status);
        SetProperty(serialized, "newGameScene", OpeningScene);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(menu);
    }

    /* ---------------- element helpers ---------------- */

    private static GameObject CreateElement(string name, Transform parent, Vector2 anchorMin,
        Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        return go;
    }

    private static Text CreateText(string name, Transform parent, string content, Vector2 anchorMin,
        Vector2 anchorMax, Vector2 pivot, Vector2 size, int fontSize, FontStyle style, Color color,
        TextAnchor alignment, bool multiline)
    {
        GameObject go = CreateElement(name, parent, anchorMin, anchorMax, pivot, Vector2.zero, size);

        Text text = go.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.supportRichText = true;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = multiline ? VerticalWrapMode.Truncate : VerticalWrapMode.Overflow;

        return text;
    }

    private static Button CreateActionButton(string name, Transform parent, string label, Vector2 position, bool primary)
    {
        GameObject go = CreateElement(name, parent,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            position, new Vector2(380f, 56f));

        Image background = go.AddComponent<Image>();
        background.color = primary ? SoledarUITheme.Gold : SoledarUITheme.PanelRaised;

        Button button = go.AddComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = primary
            ? new Color(1f, 0.97f, 0.88f, 1f)
            : new Color(1f, 0.96f, 0.86f, 1f);
        colors.pressedColor = primary
            ? new Color(0.86f, 0.80f, 0.66f, 1f)
            : new Color(0.82f, 0.77f, 0.65f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.55f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        // Focus rule sits in the margin, matching the block's left rule.
        GameObject focusBar = CreateElement("FocusBar", go.transform,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-8f, 0f), new Vector2(3f, 26f));
        focusBar.AddComponent<Image>().color = SoledarUITheme.Gold;

        Text text = CreateText("Label", go.transform, label,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero,
            SoledarUITheme.HeadingSize, primary ? FontStyle.Bold : FontStyle.Normal,
            primary ? SoledarUITheme.Ink : SoledarUITheme.Gold, TextAnchor.MiddleCenter, false);
        text.rectTransform.offsetMin = new Vector2(20f, 0f);
        text.rectTransform.offsetMax = new Vector2(-20f, 0f);

        SelectableFocusVisual focus = go.AddComponent<SelectableFocusVisual>();
        SerializedObject focusSO = new SerializedObject(focus);
        SetProperty(focusSO, "focusBar", focusBar);
        SetProperty(focusSO, "label", text);
        focusSO.ApplyModifiedPropertiesWithoutUndo();

        focusBar.SetActive(false);
        return button;
    }

    private static void SetProperty(SerializedObject serialized, string propertyName, object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogWarning($"[MainMenu] Serialized field '{propertyName}' not found.");
            return;
        }

        if (value is string s) property.stringValue = s;
        else if (value is Object o) property.objectReferenceValue = o;
    }

    private static void EnsureMenuIsFirstBuildScene()
    {
        var scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(MenuPath, true)
        };

        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            if (scene.path != MenuPath) scenes.Add(scene);

        EditorBuildSettings.scenes = scenes.ToArray();

        // Note: for a scene created in this same editor session Unity stores the
        // entry's GUID as zero until the project settings are reloaded. Scenes
        // resolve by path, so the entry is usable immediately and the GUID fills
        // in on the next launch.
        Debug.Log($"[MainMenu] MainMenu is build scene 0 ({SceneUtility.GetBuildIndexByScenePath(MenuPath)}).");
    }
}
#endif
