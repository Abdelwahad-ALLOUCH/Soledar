#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Installs the generated Ziri source sprite as the Player's visual
/// without touching movement, combat, physics, or input components.
///
/// The renderer lives on a child named "ZiriVisual" so ZiriVisual's idle bob
/// can move the art without writing to the Rigidbody2D-driven Player root.</summary>
public static class ZiriCharacterSetup
{
    private const string SpritePath = "Assets/Art/Characters/Ziri/Ziri_Idle_Source_v1.png";
    private const string PlayerPrefabPath = "Assets/Prefabs/Player/Player.prefab";
    private const string TestScenePath = "Assets/TestScene.unity";
    private const string VisualChildName = "ZiriVisual";

    // Source art is 1145x1374. The figure occupies x 94..966 and y 70..1303
    // measured from the bottom, so the pivot is placed at the character's
    // horizontal centre with the feet resting on the collider's bottom edge.
    // 680 pixels per unit makes Ziri about 1.8 world units tall.
    private const float PixelsPerUnit = 680f;
    private static readonly Vector2 SpritePivot = new Vector2(530f / 1145f, 0.2984f);

    [MenuItem("Tools/Soledar/Install Ziri As Playable Character")]
    public static void Install()
    {
        ConfigureImporter();
        Sprite ziri = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        if (ziri == null)
        {
            Debug.LogError("[Ziri] Sprite import failed. Select the source PNG and reimport it, then run this command again.");
            return;
        }

        InstallOnPrefab(ziri);
        InstallOnScene(ziri);

        AssetDatabase.SaveAssets();
        Debug.Log("[Ziri] Installed as the playable character visual. Test movement, collisions, and facing in Play mode.");
    }

    private static void InstallOnPrefab(Sprite sprite)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath) == null)
        {
            Debug.LogError($"[Ziri] Player prefab missing at '{PlayerPrefabPath}'.");
            return;
        }

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            ApplyVisual(prefabRoot, sprite);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(prefabRoot); }
    }

    private static void InstallOnScene(Sprite sprite)
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.path != TestScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            scene = EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Single);
        }

        // The Player is a prefab instance; it inherits the visual from the
        // prefab, but a scene-local Player still gets wired up here.
        PlayerMovement player = Object.FindFirstObjectByType<PlayerMovement>();
        if (player == null)
        {
            Debug.LogWarning("[Ziri] No Player with PlayerMovement found in TestScene.");
            return;
        }

        if (ApplyVisual(player.gameObject, sprite))
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }

    private static void ConfigureImporter()
    {
        TextureImporter importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"[Ziri] No texture importer found at '{SpritePath}'.");
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = 2048;

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = SpritePivot;
        importer.SetTextureSettings(settings);

        importer.SaveAndReimport();
    }

    /// <summary>Returns true when anything on the player had to change.</summary>
    private static bool ApplyVisual(GameObject player, Sprite sprite)
    {
        bool changed = false;

        Transform visualTransform = player.transform.Find(VisualChildName);
        if (visualTransform == null)
        {
            GameObject visual = new GameObject(VisualChildName);
            visual.layer = player.layer;
            visualTransform = visual.transform;
            visualTransform.SetParent(player.transform, false);
            changed = true;
        }

        SpriteRenderer renderer = visualTransform.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = visualTransform.gameObject.AddComponent<SpriteRenderer>();
            changed = true;
        }

        if (renderer.sprite != sprite) { renderer.sprite = sprite; changed = true; }
        if (renderer.color != Color.white) { renderer.color = Color.white; changed = true; }
        if (renderer.sortingOrder != 2) { renderer.sortingOrder = 2; changed = true; }

        if (visualTransform.GetComponent<ZiriVisual>() == null)
        {
            visualTransform.gameObject.AddComponent<ZiriVisual>();
            changed = true;
        }

        // The Player root previously carried the placeholder square. PlayerHealth
        // and PlayerRespawn now tint the visual child, so the root renderer would
        // only draw a stray square behind Ziri.
        SpriteRenderer rootRenderer = player.GetComponent<SpriteRenderer>();
        if (rootRenderer != null && rootRenderer != renderer)
        {
            Object.DestroyImmediate(rootRenderer);
            changed = true;
        }

        return changed;
    }
}
#endif
