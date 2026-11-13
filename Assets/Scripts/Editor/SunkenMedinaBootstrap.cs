#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Creates the vertical-slice scene from the proven prototype instead of
/// modifying it. Run Tools > Soledar > Create Sunken Medina Vertical Slice
/// after scripts compile. The command is safe to repeat: it only creates
/// missing demo additions in the cloned scene.
/// </summary>
public static class SunkenMedinaBootstrap
{
    private const string SourcePath = "Assets/TestScene.unity";
    private const string DemoPath = "Assets/Scenes/SunkenMedina.unity";
    private const int GroundLayer = 3;
    private const int EnemyLayer = 6;
    private const int InteractableLayer = 8;

    [MenuItem("Tools/Soledar/Create Sunken Medina Vertical Slice")]
    public static void CreateOrUpdate()
    {
        if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(SourcePath))
        {
            Debug.LogError("[SunkenMedina] TestScene is missing; nothing was changed.");
            return;
        }
        if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(DemoPath) && !AssetDatabase.CopyAsset(SourcePath, DemoPath))
        {
            Debug.LogError("[SunkenMedina] Could not clone TestScene.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(DemoPath, OpenSceneMode.Single);
        // BoxCollider2D sizes are centered on the transform, so these are
        // trigger CENTRES: Start x-2..8, Street x13..27, Rooftops x30..46
        // (y4..16), Courtyard x50..64 (y0..10).
        AddRoom("Room_Start", "medina_start", new Vector2(3, 0), new Vector2(10, 8));
        AddRoom("Room_Street", "medina_street", new Vector2(20, 0), new Vector2(14, 8));
        AddRoom("Room_Rooftops", "medina_rooftops", new Vector2(38, 10), new Vector2(16, 12));
        AddRoom("Room_Courtyard", "broken_riad", new Vector2(57, 5), new Vector2(14, 10));
        // Positions match the tilemap route: start ground top y3 (x4-16), the
        // shallow street trench y1 (x17-20), the rooftop climb (x27-43), the
        // y5 plateau (x44-49) and the broken-riad ground y3 (x53-57).
        AddCurrency("Dirham_Street_01", new Vector2(11, 3.5f), "dirham_street_01");
        AddCurrency("Dirham_Rooftop_01", new Vector2(27, 6.5f), "dirham_rooftop_01");
        AddCurrency("Dirham_Courtyard_01", new Vector2(45, 5.5f), "dirham_courtyard_01");
        AddMerchant();
        AddLostSoul();
        AddMiniBoss();
        AddCompletionTrigger();
        AddCompletionPanel();

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(SourcePath, true),
            new EditorBuildSettingsScene(DemoPath, true)
        };
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[SunkenMedina] Vertical-slice scene created/updated. Validate each route in Play mode before shipping.");
    }

    private static void AddRoom(string name, string id, Vector2 position, Vector2 size)
    {
        if (GameObject.Find(name) != null) return;
        GameObject go = new GameObject(name); go.layer = InteractableLayer; go.transform.position = position;
        BoxCollider2D col = go.AddComponent<BoxCollider2D>(); col.isTrigger = true; col.size = size;
        RoomDiscovery room = go.AddComponent<RoomDiscovery>(); Set(room, "roomId", id);
    }

    private static void AddCurrency(string name, Vector2 position, string id)
    {
        if (GameObject.Find(name) != null) return;
        GameObject go = CreateMarker(name, position, new Color(0.85f, 0.65f, 0.15f));
        CircleCollider2D col = go.AddComponent<CircleCollider2D>(); col.isTrigger = true; col.radius = .3f;
        AncientDirhamPickup pickup = go.AddComponent<AncientDirhamPickup>(); Set(pickup, "itemId", id); Set(pickup, "amount", 10);
    }

    private static void AddMerchant()
    {
        if (GameObject.Find("SilentMerchant") != null) return;
        GameObject go = CreateMarker("SilentMerchant", new Vector2(20, 1.5f), new Color(.2f, .65f, .55f)); go.layer = InteractableLayer;
        BoxCollider2D col = go.AddComponent<BoxCollider2D>(); col.isTrigger = true; col.size = new Vector2(1f, 2f);
        go.AddComponent<MerchantInteractable>();
    }

    private static void AddLostSoul()
    {
        if (GameObject.Find("LostSoul") != null) return;
        GameObject go = CreateMarker("LostSoul", new Vector2(34, 6), new Color(.45f, .7f, 1f)); go.layer = EnemyLayer;
        Rigidbody2D rb = go.AddComponent<Rigidbody2D>(); rb.gravityScale = 0f; rb.freezeRotation = true;
        CircleCollider2D col = go.AddComponent<CircleCollider2D>(); col.radius = .45f;
        EnemyHealth health = go.AddComponent<EnemyHealth>(); Set(health, "maxHealth", 3);
        go.AddComponent<LostSoulRanged>();
    }

    private static void AddMiniBoss()
    {
        if (GameObject.Find("MedinaWarden") != null) return;
        GameObject go = CreateMarker("MedinaWarden", new Vector2(55, 3.5f), new Color(.7f, .25f, .18f)); go.layer = EnemyLayer;
        go.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
        Rigidbody2D rb = go.AddComponent<Rigidbody2D>(); rb.freezeRotation = true;
        BoxCollider2D col = go.AddComponent<BoxCollider2D>(); col.size = Vector2.one;
        EnemyHealth health = go.AddComponent<EnemyHealth>(); Set(health, "maxHealth", 12); Set(health, "knockbackForce", 2f);
        // It is intentionally a tough guard, not a second boss framework; the Gatekeeper remains the major boss.
        go.AddComponent<ContactDamage>();
        go.AddComponent<MedinaWarden>();
    }

    private static void AddCompletionTrigger()
    {
        if (GameObject.Find("DemoCompletion") != null) return;
        GameObject go = new GameObject("DemoCompletion"); go.transform.position = new Vector2(65, 3);
        BoxCollider2D col = go.AddComponent<BoxCollider2D>(); col.isTrigger = true; col.size = new Vector2(2, 4);
        go.AddComponent<DemoEndTrigger>();
    }

    /// <summary>Creates (once) the banner DemoEndTrigger shows after the
    /// Gatekeeper's persistent defeat, and wires it into the trigger.</summary>
    private static void AddCompletionPanel()
    {
        GameObject panel = GameObject.Find("DemoCompletePanel");
        DemoEndTrigger trigger = null;
        GameObject completion = GameObject.Find("DemoCompletion");
        if (completion != null) trigger = completion.GetComponent<DemoEndTrigger>();
        if (panel == null)
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null) { Debug.LogWarning("[SunkenMedina] No Canvas found; completion panel skipped."); return; }
            panel = new GameObject("DemoCompletePanel", typeof(RectTransform), typeof(UnityEngine.UI.Text));
            panel.transform.SetParent(canvas.transform, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f); rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 120f); rect.sizeDelta = new Vector2(760f, 90f);
            UnityEngine.UI.Text text = panel.GetComponent<UnityEngine.UI.Text>();
            text.text = "The Sunken Medina is yours.\nVertical slice complete - thank you for playing.";
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 28;
            text.color = new Color(0.98f, 0.82f, 0.4f, 1f);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            panel.SetActive(false);
        }
        if (trigger != null) Set(trigger, "completionPanel", panel);
    }

    private static GameObject CreateMarker(string name, Vector2 position, Color color)
    {
        // Unity 6.3: AddComponent<SpriteRenderer>() returns null while the
        // quad's MeshRenderer is still present, so the quad mesh itself is the
        // placeholder visual and the tint goes through a material instance.
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad); go.name = name; go.transform.position = position;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
        if (meshRenderer != null) meshRenderer.material.color = color;
        return go;
    }

    private static void Set(Object target, string property, object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty field = serialized.FindProperty(property);
        if (field == null) return;
        if (value is string s) field.stringValue = s;
        else if (value is int i) field.intValue = i;
        else if (value is float f) field.floatValue = f;
        else if (value is Object o) field.objectReferenceValue = o;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
