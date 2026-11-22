#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Applies the SOLEDAR HUD design to the active scene's gameplay Canvas.
///
/// What changes:
///   - Health masks: ivory when filled, a visible dim slot when spent (the old
///     UI simply hid the icon, so maximum health was unreadable).
///   - Dialogue: clear speaker/body/hint hierarchy, brand panel, gold hairline.
///   - Interaction prompt: dark backing plus a gold key-cap so it stays legible
///     over bright sandstone.
///   - Pause: one gold primary (Resume), one quiet secondary (Quit), visible
///     focus rules and explicit keyboard/gamepad navigation.
///   - Boss bar: smaller bold title, crisper bar, danger-red fill.
///   - Transient banners: a dark outline so they stay readable over the world.
///
/// Idempotent: run it on any gameplay scene (SunkenMedina, SilentPalace).
/// </summary>
public static class SoledarUIPolish
{
    private const string HeartPrefabPath = "Assets/Prefabs/Heart.prefab";

    [MenuItem("Tools/Soledar/Apply HUD Design (active scene)")]
    public static void Apply()
    {
        GameObject canvasGO = GameObject.Find("UI");
        if (canvasGO == null)
        {
            Debug.LogError($"[UIPolish] No Canvas named 'UI' in '{EditorSceneManager.GetActiveScene().path}'.");
            return;
        }

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        if (scaler != null) scaler.matchWidthOrHeight = 0.5f;

        StyleHearts(canvasGO.transform);
        StyleEnergy(canvasGO.transform);
        StyleCurrency(canvasGO.transform);
        StyleDialogue(canvasGO.transform);
        StyleInteractionPrompt(canvasGO.transform);
        StylePause(canvasGO.transform);
        StyleBossBar(canvasGO.transform);
        StyleBanners(canvasGO.transform);

        if (canvas != null) EditorUtility.SetDirty(canvas);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log($"[UIPolish] HUD design applied to '{EditorSceneManager.GetActiveScene().name}'.");
    }

    /* ================= echo energy ================= */

    /// <summary>
    /// Thin Echo Energy track under the masks. A full bar is the signal that a
    /// Lantern heal is affordable, so no icon or label is needed.
    /// </summary>
    private static void StyleEnergy(Transform canvas)
    {
        Transform existing = FindDeep(canvas, "EnergyBar");
        GameObject barGO;

        if (existing == null)
        {
            barGO = new GameObject("EnergyBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(barGO, "Create Energy Bar");
            barGO.transform.SetParent(canvas, false);
        }
        else
        {
            barGO = existing.gameObject;
        }

        RectTransform bar = (RectTransform)barGO.transform;
        bar.anchorMin = new Vector2(0f, 1f);
        bar.anchorMax = new Vector2(0f, 1f);
        bar.pivot = new Vector2(0f, 1f);
        bar.anchoredPosition = new Vector2(32f, -70f);
        bar.sizeDelta = new Vector2(140f, 8f);

        Image track = barGO.GetComponent<Image>();
        if (track == null) track = barGO.AddComponent<Image>();
        track.color = new Color(SoledarUITheme.Ink.r, SoledarUITheme.Ink.g, SoledarUITheme.Ink.b, 0.75f);
        track.raycastTarget = false;

        Transform fillT = bar.Find("EnergyFill");
        if (fillT == null)
        {
            GameObject fillGO = new GameObject("EnergyFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(fillGO, "Create Energy Fill");
            fillGO.transform.SetParent(bar, false);
            fillT = fillGO.transform;
        }

        RectTransform fillRect = (RectTransform)fillT;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(1f, 1f);
        fillRect.offsetMax = new Vector2(-1f, -1f);

        Image fill = fillT.GetComponent<Image>();
        fill.color = SoledarUITheme.Gold;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 0f;
        fill.raycastTarget = false;

        EnergyUI meter = barGO.GetComponent<EnergyUI>();
        if (meter == null) meter = barGO.AddComponent<EnergyUI>();

        SerializedObject serialized = new SerializedObject(meter);
        serialized.FindProperty("fill").objectReferenceValue = fill;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(barGO);
    }

    /* ================= currency ================= */

    /// <summary>
    /// Dirham readout: a gold rhombus (zellige geometry, and safe in the legacy
    /// font) plus the balance. Sits in the left column with the masks and the
    /// Echo Energy bar, so the whole survival readout is one stack.
    /// </summary>
    private static void StyleCurrency(Transform canvas)
    {
        Transform existing = FindDeep(canvas, "CurrencyRow");
        GameObject rowGO;

        if (existing == null)
        {
            rowGO = new GameObject("CurrencyRow", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(rowGO, "Create Currency Row");
            rowGO.transform.SetParent(canvas, false);
        }
        else
        {
            rowGO = existing.gameObject;
        }

        // Left column, directly under the masks: keeps the top-right clear for
        // the boss bar, which is wider than the game view at small resolutions.
        RectTransform row = (RectTransform)rowGO.transform;
        row.anchorMin = new Vector2(0f, 1f);
        row.anchorMax = new Vector2(0f, 1f);
        row.pivot = new Vector2(0f, 1f);
        row.anchoredPosition = new Vector2(32f, -86f);
        row.sizeDelta = new Vector2(180f, 32f);

        HorizontalLayoutGroup layout = rowGO.GetComponent<HorizontalLayoutGroup>();
        if (layout == null) layout = rowGO.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.spacing = 8f;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        Transform markT = row.Find("DirhamMark");
        if (markT == null)
        {
            GameObject markGO = new GameObject("DirhamMark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(markGO, "Create Dirham Mark");
            markGO.transform.SetParent(row, false);
            markT = markGO.transform;
        }

        RectTransform markRect = (RectTransform)markT;
        markRect.sizeDelta = new Vector2(12f, 12f);
        markRect.localRotation = Quaternion.Euler(0f, 0f, 45f);

        Image markImage = markT.GetComponent<Image>();
        markImage.color = SoledarUITheme.Gold;
        markImage.raycastTarget = false;

        Transform countT = row.Find("DirhamCount");
        if (countT == null)
        {
            GameObject countGO = new GameObject("DirhamCount", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            Undo.RegisterCreatedObjectUndo(countGO, "Create Dirham Count");
            countGO.transform.SetParent(row, false);
            countT = countGO.transform;
        }

        RectTransform countRect = (RectTransform)countT;
        countRect.sizeDelta = new Vector2(64f, 24f);

        Text count = countT.GetComponent<Text>();
        count.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        count.fontSize = SoledarUITheme.LabelSize;
        count.fontStyle = FontStyle.Normal;
        count.color = SoledarUITheme.Ivory;
        count.alignment = TextAnchor.MiddleLeft;
        count.supportRichText = false;
        count.raycastTarget = false;
        if (string.IsNullOrEmpty(count.text)) count.text = "0";

        // Same ink edge as the prompt: the sandstone behind it is bright.
        EnsureOutline(countT.gameObject, new Color(0f, 0f, 0f, 0.9f), new Vector2(1f, -1f));

        CurrencyUI currency = rowGO.GetComponent<CurrencyUI>();
        if (currency == null) currency = rowGO.AddComponent<CurrencyUI>();

        SerializedObject serialized = new SerializedObject(currency);
        serialized.FindProperty("countText").objectReferenceValue = count;
        serialized.FindProperty("pulseTarget").objectReferenceValue = markRect;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(rowGO);
    }

    /* ================= health ================= */

    private static void StyleHearts(Transform canvas)
    {
        Transform container = FindDeep(canvas, "HeartsContainer");
        if (container == null)
        {
            Debug.LogWarning("[UIPolish] HeartsContainer not found — health styling skipped.");
            return;
        }

        container.name = "HeartsContainer"; // legacy trailing space
        RectTransform rect = (RectTransform)container;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(32f, -24f);
        rect.sizeDelta = new Vector2(320f, 40f);

        HorizontalLayoutGroup layout = container.GetComponent<HorizontalLayoutGroup>();
        if (layout != null)
        {
            layout.spacing = 8f;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childAlignment = TextAnchor.MiddleLeft;
            // Let the masks keep their authored 34 px size; the layout group only
            // spaces them (control would collapse them to the sprite's preferred size).
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        // The mask icon: 34 px, ivory, never blocks pointer raycasts.
        GameObject heart = AssetDatabase.LoadAssetAtPath<GameObject>(HeartPrefabPath);
        if (heart != null)
        {
            RectTransform heartRect = heart.GetComponent<RectTransform>();
            if (heartRect != null) heartRect.sizeDelta = new Vector2(34f, 34f);

            Image heartImage = heart.GetComponent<Image>();
            if (heartImage != null)
            {
                heartImage.color = SoledarUITheme.Ivory;
                heartImage.raycastTarget = false;
            }

            // Ivory masks sit over bright tiles too; a hard ink edge keeps the
            // silhouette readable on both sandstone and shadow.
            Outline heartOutline = heart.GetComponent<Outline>();
            if (heartOutline == null) heartOutline = heart.AddComponent<Outline>();
            heartOutline.effectColor = SoledarUITheme.WithAlpha(SoledarUITheme.Ink, 0.9f);
            heartOutline.effectDistance = new Vector2(1f, -1f);
            heartOutline.useGraphicAlpha = true;

            EditorUtility.SetDirty(heart);
            AssetDatabase.SaveAssets();
        }

        HealthUI healthUI = canvas.GetComponent<HealthUI>();
        if (healthUI != null)
        {
            SerializedObject serialized = new SerializedObject(healthUI);
            SetColor(serialized, "filledColor", SoledarUITheme.Ivory);
            SetColor(serialized, "emptyColor", SoledarUITheme.WithAlpha(SoledarUITheme.Dim, 0.85f));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(healthUI);
        }
    }

    /* ================= dialogue ================= */

    private static void StyleDialogue(Transform canvas)
    {
        Transform panel = FindDeep(canvas, "DialoguePanel");
        if (panel == null)
        {
            Debug.LogWarning("[UIPolish] DialoguePanel not found — dialogue styling skipped.");
            return;
        }

        RectTransform panelRect = (RectTransform)panel;
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 24f);
        panelRect.sizeDelta = new Vector2(0f, 220f);

        // Solid, not translucent: over bright sandstone a 90%-alpha ink panel
        // composites to mid-grey and destroys the text contrast.
        Image panelImage = panel.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.color = SoledarUITheme.Panel;
            panelImage.raycastTarget = false;
        }

        // The gold hairline separates the panel from the world without a border.
        Transform rule = panel.Find("DialogueRule");
        if (rule == null)
        {
            GameObject ruleGO = new GameObject("DialogueRule", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(ruleGO, "Create DialogueRule");
            ruleGO.transform.SetParent(panel, false);
            rule = ruleGO.transform;
        }

        RectTransform ruleRect = (RectTransform)rule;
        ruleRect.anchorMin = new Vector2(0f, 1f);
        ruleRect.anchorMax = new Vector2(1f, 1f);
        ruleRect.pivot = new Vector2(0.5f, 1f);
        ruleRect.anchoredPosition = Vector2.zero;
        ruleRect.sizeDelta = new Vector2(0f, 2f);

        Image ruleImage = rule.GetComponent<Image>();
        if (ruleImage == null) ruleImage = rule.gameObject.AddComponent<Image>();
        ruleImage.color = SoledarUITheme.WithAlpha(SoledarUITheme.Gold, 0.55f);
        ruleImage.raycastTarget = false;

        Transform speaker = FindDeep(panel, "SpeakerText");
        if (speaker != null)
        {
            RectTransform rect = (RectTransform)speaker;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -20f);
            rect.sizeDelta = new Vector2(-56f, 32f);

            Text text = speaker.GetComponent<Text>();
            if (text != null)
            {
                text.fontSize = SoledarUITheme.HeadingSize;
                text.fontStyle = FontStyle.Bold;
                text.color = SoledarUITheme.Gold;
                text.alignment = TextAnchor.MiddleLeft;
                text.raycastTarget = false;
            }
        }

        Transform body = FindDeep(panel, "BodyText");
        if (body != null)
        {
            RectTransform rect = (RectTransform)body;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(28f, 44f);
            rect.offsetMax = new Vector2(-28f, -60f);

            Text text = body.GetComponent<Text>();
            if (text != null)
            {
                text.fontSize = SoledarUITheme.BodySize;
                text.color = SoledarUITheme.Ivory;
                text.alignment = TextAnchor.UpperLeft;
                text.lineSpacing = 1.2f;
                text.raycastTarget = false;
            }
        }

        Transform hint = FindDeep(panel, "HintText");
        if (hint != null)
        {
            RectTransform rect = (RectTransform)hint;
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-28f, 16f);
            rect.sizeDelta = new Vector2(280f, 22f);

            Text text = hint.GetComponent<Text>();
            if (text != null)
            {
                text.text = $"<color={SoledarUITheme.GoldHex}>E</color>   Continue";
                text.fontSize = SoledarUITheme.SmallSize;
                text.color = SoledarUITheme.Muted;
                text.alignment = TextAnchor.MiddleRight;
                text.supportRichText = true;
                text.raycastTarget = false;
            }
        }
    }

    /* ================= interaction prompt ================= */

    private static void StyleInteractionPrompt(Transform canvas)
    {
        Transform prompt = FindDeep(canvas, "InteractionPrompt");
        if (prompt == null)
        {
            Debug.LogWarning("[UIPolish] InteractionPrompt not found — prompt styling skipped.");
            return;
        }

        RectTransform rect = (RectTransform)prompt;
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 268f);
        rect.sizeDelta = new Vector2(720f, 40f);

        Text text = prompt.GetComponent<Text>();
        if (text != null)
        {
            text.fontSize = SoledarUITheme.LabelSize;
            text.color = SoledarUITheme.Ivory;
            text.alignment = TextAnchor.MiddleCenter;
            text.supportRichText = true;
            text.raycastTarget = false;
        }

        EnsureOutline(prompt.gameObject, new Color(0f, 0f, 0f, 0.9f), new Vector2(1f, -1f));

        // Backing panel for legibility over bright tiles (the prompt text itself
        // is off by default; PlayerInteractor toggles both).
        Transform backing = canvas.Find("InteractionPromptBacking");
        if (backing == null)
        {
            GameObject backingGO = new GameObject("InteractionPromptBacking", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(backingGO, "Create InteractionPromptBacking");
            backingGO.transform.SetParent(canvas, false);
            backing = backingGO.transform;
        }

        RectTransform backingRect = (RectTransform)backing;
        backingRect.anchorMin = new Vector2(0.5f, 0f);
        backingRect.anchorMax = new Vector2(0.5f, 0f);
        backingRect.pivot = new Vector2(0.5f, 0f);
        backingRect.anchoredPosition = new Vector2(0f, 260f);
        backingRect.sizeDelta = new Vector2(720f, 56f);

        Image backingImage = backing.GetComponent<Image>();
        if (backingImage == null) backingImage = backing.gameObject.AddComponent<Image>();
        backingImage.color = SoledarUITheme.Ink;
        backingImage.raycastTarget = false;

        // Backing renders behind the prompt. Only move it when it sits after the
        // text, otherwise a second run would swap them and hide the label.
        if (backing.GetSiblingIndex() > prompt.GetSiblingIndex())
            backing.SetSiblingIndex(prompt.GetSiblingIndex());
        if (!backing.gameObject.activeSelf) backing.gameObject.SetActive(false);

        GameObject player = GameObject.Find("Player");
        PlayerInteractor interactor = player != null ? player.GetComponent<PlayerInteractor>() : null;
        if (interactor != null)
        {
            SerializedObject serialized = new SerializedObject(interactor);
            SerializedProperty property = serialized.FindProperty("promptBacking");
            if (property != null)
            {
                property.objectReferenceValue = backing.gameObject;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(interactor);
            }
        }
    }

    /* ================= pause ================= */

    private static void StylePause(Transform canvas)
    {
        Transform panel = FindDeep(canvas, "PausePanel");
        if (panel == null)
        {
            Debug.LogWarning("[UIPolish] PausePanel not found — pause styling skipped.");
            return;
        }

        // The pause overlay must sit above the HUD (hearts, energy, currency) so
        // the Abilities page is one clean surface, not a page with a HUD on top.
        panel.SetAsLastSibling();

        Image panelImage = panel.GetComponent<Image>();
        if (panelImage != null)
            panelImage.color = SoledarUITheme.Ink;

        Transform title = FindDeep(panel, "PauseTitle");
        if (title != null)
        {
            RectTransform rect = (RectTransform)title;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 172f);
            rect.sizeDelta = new Vector2(600f, 60f);

            Text text = title.GetComponent<Text>();
            if (text != null)
            {
                text.text = "PAUSED";
                text.fontSize = SoledarUITheme.TitleSize;
                text.fontStyle = FontStyle.Bold;
                text.color = SoledarUITheme.Gold;
                text.alignment = TextAnchor.MiddleCenter;
                text.raycastTarget = false;
            }
        }

        Button resume = StylePauseButton(panel, "ResumeButton", "Resume",
            new Vector2(0f, 44f), new Vector2(320f, 56f), PauseTone.Primary);
        Button abilities = StylePauseButton(panel, "AbilitiesButton", "Abilities",
            new Vector2(0f, -24f), new Vector2(320f, 48f), PauseTone.Secondary);
        Button mainMenu = StylePauseButton(panel, "MainMenuButton", "Main Menu",
            new Vector2(0f, -96f), new Vector2(320f, 48f), PauseTone.Secondary);
        Button quit = StylePauseButton(panel, "QuitButton", "Quit",
            new Vector2(0f, -164f), new Vector2(320f, 48f), PauseTone.Quiet);

        // Keep reading order (Resume, Abilities, Main Menu, Quit) even when a
        // button was created later in the scene's life.
        if (abilities != null && mainMenu != null
            && abilities.transform.GetSiblingIndex() > mainMenu.transform.GetSiblingIndex())
            abilities.transform.SetSiblingIndex(mainMenu.transform.GetSiblingIndex());
        if (mainMenu != null && quit != null
            && mainMenu.transform.GetSiblingIndex() > quit.transform.GetSiblingIndex())
            mainMenu.transform.SetSiblingIndex(quit.transform.GetSiblingIndex());

        // One vertical chain, so up/down always moves exactly one step.
        SetNavigation(resume, null, abilities);
        SetNavigation(abilities, resume, mainMenu);
        SetNavigation(mainMenu, abilities, quit);
        SetNavigation(quit, mainMenu, null);

        AbilitiesPanel abilitiesPanel = EnsureAbilitiesPanel(panel);

        PauseMenu pause = canvas.GetComponent<PauseMenu>();
        if (pause != null)
        {
            SerializedObject serialized = new SerializedObject(pause);
            SetObject(serialized, "defaultSelection", resume != null ? resume.gameObject : null);
            SetObject(serialized, "abilitiesButton", abilities);
            SetObject(serialized, "abilitiesPanel", abilitiesPanel);
            SetObject(serialized, "mainMenuButton", mainMenu);
            SerializedProperty scene = serialized.FindProperty("mainMenuScene");
            if (scene != null) scene.stringValue = "MainMenu";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pause);
        }
    }

    private enum PauseTone { Primary, Secondary, Quiet }

    /// <summary>
    /// Builds/refreshes the owned-abilities page inside the pause overlay. The
    /// page is a full-screen ink cover with a generated list, so no pause button
    /// behind it can be clicked or focused while it is open.
    /// </summary>
    private static AbilitiesPanel EnsureAbilitiesPanel(Transform pausePanel)
    {
        Transform existing = FindDeep(pausePanel, "AbilitiesPanel");
        GameObject page;

        if (existing == null)
        {
            page = new GameObject("AbilitiesPanel", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(AbilitiesPanel));
            Undo.RegisterCreatedObjectUndo(page, "Create AbilitiesPanel");
            page.transform.SetParent(pausePanel, false);
        }
        else
        {
            page = existing.gameObject;
            if (page.GetComponent<AbilitiesPanel>() == null)
                Undo.AddComponent<AbilitiesPanel>(page);
        }

        page.transform.SetAsLastSibling(); // draw over the pause actions

        RectTransform pageRect = (RectTransform)page.transform;
        pageRect.anchorMin = Vector2.zero;
        pageRect.anchorMax = Vector2.one;
        pageRect.offsetMin = Vector2.zero;
        pageRect.offsetMax = Vector2.zero;

        Image background = page.GetComponent<Image>();
        if (background == null) background = page.AddComponent<Image>();
        background.color = SoledarUITheme.Ink;
        background.raycastTarget = true; // swallow clicks meant for the actions behind

        Transform titleT = FindDeep(page.transform, "AbilitiesTitle");
        if (titleT == null)
        {
            GameObject titleGO = new GameObject("AbilitiesTitle", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Text));
            Undo.RegisterCreatedObjectUndo(titleGO, "Create AbilitiesTitle");
            titleGO.transform.SetParent(page.transform, false);
            titleT = titleGO.transform;
        }

        RectTransform titleRect = (RectTransform)titleT;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -68f);
        titleRect.sizeDelta = new Vector2(720f, 52f);

        Text title = titleT.GetComponent<Text>();
        if (title == null) title = titleT.gameObject.AddComponent<Text>();
        title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        title.text = "ABILITIES";
        title.fontSize = SoledarUITheme.TitleSize;
        title.fontStyle = FontStyle.Bold;
        title.color = SoledarUITheme.Gold;
        title.alignment = TextAnchor.MiddleCenter;
        title.raycastTarget = false;

        Transform listT = FindDeep(page.transform, "AbilitiesList");
        if (listT == null)
        {
            GameObject listGO = new GameObject("AbilitiesList", typeof(RectTransform),
                typeof(VerticalLayoutGroup));
            Undo.RegisterCreatedObjectUndo(listGO, "Create AbilitiesList");
            listGO.transform.SetParent(page.transform, false);
            listT = listGO.transform;
        }

        RectTransform listRect = (RectTransform)listT;
        listRect.anchorMin = new Vector2(0.5f, 1f);
        listRect.anchorMax = new Vector2(0.5f, 1f);
        listRect.pivot = new Vector2(0.5f, 1f);
        listRect.anchoredPosition = new Vector2(0f, -146f);
        listRect.sizeDelta = new Vector2(760f, 620f);

        VerticalLayoutGroup listLayout = listT.GetComponent<VerticalLayoutGroup>();
        if (listLayout == null) listLayout = listT.gameObject.AddComponent<VerticalLayoutGroup>();
        listLayout.spacing = 12f;
        listLayout.padding = new RectOffset(0, 0, 0, 0);
        listLayout.childAlignment = TextAnchor.UpperLeft;
        listLayout.childControlWidth = true;
        listLayout.childControlHeight = true;
        listLayout.childForceExpandWidth = true;
        listLayout.childForceExpandHeight = false;

        Transform emptyT = FindDeep(page.transform, "AbilitiesEmpty");
        if (emptyT == null)
        {
            GameObject emptyGO = new GameObject("AbilitiesEmpty", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Text));
            Undo.RegisterCreatedObjectUndo(emptyGO, "Create AbilitiesEmpty");
            emptyGO.transform.SetParent(page.transform, false);
            emptyT = emptyGO.transform;
        }

        RectTransform emptyRect = (RectTransform)emptyT;
        emptyRect.anchorMin = new Vector2(0.5f, 0.5f);
        emptyRect.anchorMax = new Vector2(0.5f, 0.5f);
        emptyRect.pivot = new Vector2(0.5f, 0.5f);
        emptyRect.anchoredPosition = Vector2.zero;
        emptyRect.sizeDelta = new Vector2(760f, 60f);

        Text empty = emptyT.GetComponent<Text>();
        if (empty == null) empty = emptyT.gameObject.AddComponent<Text>();
        empty.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        empty.text = "No abilities yet — explore the Medina and the Palace.";
        empty.fontSize = SoledarUITheme.BodySize;
        empty.color = SoledarUITheme.Dim;
        empty.alignment = TextAnchor.MiddleCenter;
        empty.raycastTarget = false;
        empty.gameObject.SetActive(false);

        // Reuses the pause button styling (plate, label, focus bar) so Back
        // belongs to the same family.
        Button back = StylePauseButton(page.transform, "AbilitiesBackButton", "Back",
            Vector2.zero, new Vector2(320f, 48f), PauseTone.Secondary);

        RectTransform backRect = (RectTransform)back.transform;
        backRect.anchorMin = new Vector2(0.5f, 0f);
        backRect.anchorMax = new Vector2(0.5f, 0f);
        backRect.pivot = new Vector2(0.5f, 0f);
        backRect.anchoredPosition = new Vector2(0f, 64f);

        // Back is the only stop on this page; explicit None keeps navigation
        // from jumping to the pause actions hidden underneath.
        Navigation navigation = back.navigation;
        navigation.mode = Navigation.Mode.None;
        back.navigation = navigation;

        AbilitiesPanel panelComponent = page.GetComponent<AbilitiesPanel>();
        SerializedObject pageSerialized = new SerializedObject(panelComponent);
        SetObject(pageSerialized, "listRoot", listRect);
        SetObject(pageSerialized, "emptyText", empty);
        SetObject(pageSerialized, "backButton", back);
        pageSerialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(panelComponent);

        if (page.activeSelf)
        {
            Undo.RecordObject(page, "Hide AbilitiesPanel");
            page.SetActive(false);
        }

        return panelComponent;
    }

    /// <summary>Creates a pause button in scenes authored before it existed.</summary>
    private static Transform EnsurePauseButton(Transform panel, string name)
    {
        Transform existing = FindDeep(panel, name);
        if (existing != null) return existing;

        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
            typeof(Image), typeof(Button));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(panel, false);

        GameObject labelGO = new GameObject("Label", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(labelGO, "Create Label");
        labelGO.transform.SetParent(go.transform, false);

        RectTransform labelRect = (RectTransform)labelGO.transform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(20f, 0f);
        labelRect.offsetMax = new Vector2(-20f, 0f);

        Text text = labelGO.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.supportRichText = true;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;

        return go.transform;
    }

    private static Button StylePauseButton(Transform panel, string name, string label, Vector2 position,
        Vector2 size, PauseTone tone)
    {
        bool primary = tone == PauseTone.Primary;
        Transform existing = EnsurePauseButton(panel, name);

        RectTransform rect = (RectTransform)existing;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image background = existing.GetComponent<Image>();
        if (background != null)
        {
            background.color = primary ? SoledarUITheme.Gold : SoledarUITheme.PanelRaised;
            background.raycastTarget = true;
        }

        Button button = existing.GetComponent<Button>();
        if (button != null)
        {
            button.targetGraphic = background; // created buttons start unbound
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = primary ? new Color(1f, 0.97f, 0.88f, 1f) : new Color(1f, 0.96f, 0.86f, 1f);
            colors.pressedColor = primary ? new Color(0.86f, 0.80f, 0.66f, 1f) : new Color(0.82f, 0.77f, 0.65f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.55f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
        }

        Transform labelT = FindDeep(existing, "Label");
        Text text = labelT != null ? labelT.GetComponent<Text>() : null;
        if (text != null)
        {
            text.text = label;
            text.fontSize = primary ? SoledarUITheme.HeadingSize : SoledarUITheme.LabelSize;
            text.fontStyle = primary ? FontStyle.Bold : FontStyle.Normal;
            text.color = primary ? SoledarUITheme.Ink
                : tone == PauseTone.Secondary ? SoledarUITheme.Gold : SoledarUITheme.Muted;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
        }

        Transform barT = existing.Find("FocusBar");
        if (barT == null)
        {
            GameObject barGO = new GameObject("FocusBar", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(barGO, "Create FocusBar");
            barGO.transform.SetParent(existing, false);
            barT = barGO.transform;
        }

        RectTransform barRect = (RectTransform)barT;
        barRect.anchorMin = new Vector2(0f, 0.5f);
        barRect.anchorMax = new Vector2(0f, 0.5f);
        barRect.pivot = new Vector2(1f, 0.5f);
        barRect.anchoredPosition = new Vector2(-10f, 0f);
        barRect.sizeDelta = new Vector2(3f, 26f);

        Image barImage = barT.GetComponent<Image>();
        if (barImage == null) barImage = barT.gameObject.AddComponent<Image>();
        // Sits outside the plate, on the ink overlay: gold works for every role.
        barImage.color = SoledarUITheme.Gold;
        barImage.raycastTarget = false;

        SelectableFocusVisual focus = existing.GetComponent<SelectableFocusVisual>();
        if (focus == null) focus = existing.gameObject.AddComponent<SelectableFocusVisual>();

        SerializedObject serialized = new SerializedObject(focus);
        SetObject(serialized, "focusBar", barT.gameObject);
        SetObject(serialized, "label", text);
        SetColor(serialized, "labelIdleColor", primary ? SoledarUITheme.Ink
            : tone == PauseTone.Secondary ? SoledarUITheme.Gold : SoledarUITheme.Muted);
        SetColor(serialized, "labelFocusColor", primary ? SoledarUITheme.Ink : SoledarUITheme.Ivory);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        barT.gameObject.SetActive(false);
        return button;
    }

    /* ================= boss bar ================= */

    private static void StyleBossBar(Transform canvas)
    {
        Transform panel = FindDeep(canvas, "BossBar");
        if (panel == null) return;

        RectTransform panelRect = (RectTransform)panel;
        panelRect.sizeDelta = new Vector2(620f, 56f);

        Transform title = FindDeep(panel, "BossTitle");
        if (title != null)
        {
            RectTransform rect = (RectTransform)title;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -6f);
            rect.sizeDelta = new Vector2(0f, 22f);

            Text text = title.GetComponent<Text>();
            if (text != null)
            {
                text.fontSize = 18;
                text.fontStyle = FontStyle.Bold;
                text.color = SoledarUITheme.Gold;
                text.alignment = TextAnchor.MiddleCenter;
                text.raycastTarget = false;
            }
        }

        Transform background = FindDeep(panel, "BarBackground");
        if (background != null)
        {
            RectTransform rect = (RectTransform)background;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -34f);
            rect.sizeDelta = new Vector2(600f, 22f);

            // Opaque track so the drained portion does not show bright world art.
            Image image = background.GetComponent<Image>();
            if (image != null) image.color = SoledarUITheme.Ink;
        }

        Transform fill = FindDeep(panel, "BarFill");
        if (fill != null)
        {
            Image image = fill.GetComponent<Image>();
            if (image != null) image.color = SoledarUITheme.Danger;
        }
    }

    /* ================= banners ================= */

    private static void StyleBanners(Transform canvas)
    {
        StyleBanner(canvas, "RewardBanner", 24);
        StyleBanner(canvas, "DemoCompletePanel", 26);
    }

    private static void StyleBanner(Transform canvas, string name, int size)
    {
        Transform banner = FindDeep(canvas, name);
        if (banner == null) return;

        Text text = banner.GetComponent<Text>();
        if (text != null)
        {
            text.fontSize = size;
            text.color = SoledarUITheme.Gold;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
        }

        EnsureOutline(banner.gameObject, new Color(0f, 0f, 0f, 0.85f), new Vector2(1f, -1f));
    }

    /// <summary>Banners and the prompt sit over the world; a hard outline keeps them readable.</summary>
    private static void EnsureOutline(GameObject target, Color color, Vector2 distance)
    {
        Outline outline = target.GetComponent<Outline>();
        if (outline == null) outline = target.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = distance;
        outline.useGraphicAlpha = true;
    }

    /* ================= helpers ================= */

    private static void SetNavigation(Button button, Button up, Button down)
    {
        if (button == null) return;

        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.Explicit;
        navigation.selectOnUp = up;
        navigation.selectOnDown = down;
        button.navigation = navigation;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        // Phase 1 created 'HeartsContainer ' with a trailing space — match trimmed.
        if (root.name.Trim() == name.Trim()) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    private static void SetObject(SerializedObject serialized, string propertyName, Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null) property.objectReferenceValue = value;
    }

    private static void SetColor(SerializedObject serialized, string propertyName, Color value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null) property.colorValue = value;
    }
}
#endif
