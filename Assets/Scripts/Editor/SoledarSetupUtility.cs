#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Cinemachine;

/// <summary>
/// One-click scene configuration for SOLEDAR Phase 1.
///
/// HOW TO USE:
///   1. Open Assets/TestScene.unity (and make sure no other scene is loaded).
///   2. Run the menu item:  Tools > Soledar > Setup Phase 1 Scene
///   3. Enter Play mode and follow the test checklist in Docs/DevLog.md.
///
/// WHAT IT DOES (idempotent — safe to run again):
///   - Renames 'DashGhost .prefab'/'Heart .prefab' (removes trailing spaces).
///   - Trims trailing spaces from layer names ('Ground ', 'Enemy ').
///   - Adds PlayerInputReader to the Player and assigns the input actions asset.
///   - Adds PlayerAttack and wires attackPoint + enemyLayer (moved out of PlayerMovement).
///   - Adds PlayerRespawn + creates a SpawnPoint + a demo Checkpoint.
///   - Adds a Cinemachine rig: CinemachineBrain on Main Camera + PlayerFollowCamera.
///   - Saves Assets/Prefabs/Player/Player.prefab from the wired Player.
///   - Sets TestScene as build scene 0.
///   - Deletes Assets/Scenes/SampleScene.unity (user-approved: it is broken and
///     incompatible with the current scripts).
///   - Saves the scene.
/// </summary>
public static class SoledarSetupUtility
{
    private const string TestScenePath = "Assets/TestScene.unity";
    private const string PlayerPrefabPath = "Assets/Prefabs/Player/Player.prefab";
    private const string InputAssetPath = "Assets/InputSystem_Actions.inputactions";

    [MenuItem("Tools/Soledar/Setup Phase 1 Scene")]
    public static void SetupPhase1Scene()
    {
        Scene scene = EditorSceneManager.GetActiveScene();

        /* ---------------- Guards ---------------- */
        if (scene.path != TestScenePath)
        {
            Debug.LogError($"[SoledarSetup] Active scene is '{scene.path}'. Open '{TestScenePath}' first, then run this command again.");
            return;
        }

        foreach (Scene loaded in EditorSceneManager.GetLoadedScenes())
        {
            if (loaded.path == "Assets/Scenes/SampleScene.unity")
            {
                Debug.LogError("[SoledarSetup] SampleScene is also loaded in the hierarchy. Close it (keep only TestScene), then run again.");
                return;
            }
        }

        /* ---------------- 1. Asset hygiene ---------------- */
        RenameAsset("Assets/Prefabs/DashGhost .prefab", "DashGhost");
        RenameAsset("Assets/Prefabs/Heart .prefab", "Heart");
        FixLayerNames();

        /* ---------------- 2. Player components ---------------- */
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogError("[SoledarSetup] No GameObject named 'Player' in the open scene.");
            return;
        }

        // Rename check children to clean names (references are by object, not name).
        RenameChildGameObject(player.transform, "AttackPoint ", "AttackPoint");
        RenameChildGameObject(player.transform, "GroundCheck ", "GroundCheck");
        RenameChildGameObject(player.transform, "WallCheck ", "WallCheck");

        // PlayerInputReader + input actions asset
        PlayerInputReader reader = player.GetComponent<PlayerInputReader>();
        if (reader == null)
            reader = Undo.AddComponent<PlayerInputReader>(player);
        InputActionAsset inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
        if (inputAsset == null)
        {
            Debug.LogError($"[SoledarSetup] Input actions asset not found at '{InputAssetPath}'.");
            return;
        }
        SetObjectRef(reader, "inputActions", inputAsset);

        // PlayerAttack (fields moved out of PlayerMovement; scene values were
        // attackRange 0.5, attackCooldown 0.3, enemyLayer = layer 'Enemy')
        PlayerAttack attack = player.GetComponent<PlayerAttack>();
        if (attack == null)
            attack = Undo.AddComponent<PlayerAttack>(player);
        Transform attackPoint = player.transform.Find("AttackPoint");
        if (attackPoint == null)
            attackPoint = FindChildByPrefix(player.transform, "AttackPoint");
        SetObjectRef(attack, "attackPoint", attackPoint);
        SetFloat(attack, "attackRange", 0.5f);
        SetFloat(attack, "attackCooldown", 0.3f);
        int enemyLayerIndex = LayerMask.NameToLayer("Enemy");
        if (enemyLayerIndex < 0) enemyLayerIndex = 6;
        SetInt(attack, "enemyLayer", 1 << enemyLayerIndex);

        // PlayerMovement: re-wire check references in case they were lost
        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        if (movement != null)
        {
            Transform groundCheck = player.transform.Find("GroundCheck");
            Transform wallCheck = player.transform.Find("WallCheck");
            if (groundCheck == null) groundCheck = FindChildByPrefix(player.transform, "GroundCheck");
            if (wallCheck == null) wallCheck = FindChildByPrefix(player.transform, "WallCheck");
            SetObjectRef(movement, "groundCheck", groundCheck);
            SetObjectRef(movement, "wallCheck", wallCheck);
            int groundLayerIndex = LayerMask.NameToLayer("Ground");
            if (groundLayerIndex < 0) groundLayerIndex = 3;
            SetInt(movement, "groundLayer", 1 << groundLayerIndex);
        }

        /* ---------------- 3. Respawn system ---------------- */
        PlayerRespawn respawn = player.GetComponent<PlayerRespawn>();
        if (respawn == null)
            respawn = Undo.AddComponent<PlayerRespawn>(player);

        GameObject spawn = GameObject.Find("SpawnPoint");
        if (spawn == null)
        {
            spawn = new GameObject("SpawnPoint");
            spawn.transform.position = player.transform.position;
            Undo.RegisterCreatedObjectUndo(spawn, "Create SpawnPoint");
            Debug.Log("[SoledarSetup] Created SpawnPoint at the player's current position.");
        }
        SetObjectRef(respawn, "respawnPoint", spawn.transform);
        SetFloat(respawn, "respawnDelay", 0.5f);
        SetFloat(respawn, "fadeDuration", 0.4f);

        // Demo checkpoint (overlaps the spawn point; Phase 3 places real benches)
        GameObject checkpoint = GameObject.Find("Checkpoint");
        if (checkpoint == null)
        {
            checkpoint = new GameObject("Checkpoint");
            checkpoint.transform.position = spawn.transform.position;
            BoxCollider2D box = checkpoint.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1.5f, 3f);
            checkpoint.AddComponent<Checkpoint>();
            Undo.RegisterCreatedObjectUndo(checkpoint, "Create Checkpoint");
            Debug.Log("[SoledarSetup] Created demo Checkpoint (trigger) at the spawn point.");
        }

        /* ---------------- 4. Camera rig (Cinemachine 3) ---------------- */
        // Interpolate the player's rigidbody so the damped camera doesn't jitter.
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb != null && playerRb.interpolate == RigidbodyInterpolation2D.None)
        {
            Undo.RecordObject(playerRb, "Enable rigidbody interpolation");
            playerRb.interpolate = RigidbodyInterpolation2D.Interpolate;
            Debug.Log("[SoledarSetup] Enabled Rigidbody2D interpolation on the Player (camera smoothness).");
        }

        GameObject mainCam = GameObject.Find("Main Camera");
        if (mainCam == null)
        {
            Debug.LogError("[SoledarSetup] No 'Main Camera' in the scene.");
            return;
        }

        if (mainCam.GetComponent<CinemachineBrain>() == null)
        {
            Undo.AddComponent<CinemachineBrain>(mainCam);
            Debug.Log("[SoledarSetup] Added CinemachineBrain to Main Camera.");
        }

        GameObject vcamGO = GameObject.Find("PlayerFollowCamera");
        if (vcamGO == null)
        {
            vcamGO = new GameObject("PlayerFollowCamera");
            Undo.RegisterCreatedObjectUndo(vcamGO, "Create PlayerFollowCamera");
        }

        CinemachineCamera vcam = vcamGO.GetComponent<CinemachineCamera>();
        if (vcam == null)
            vcam = Undo.AddComponent<CinemachineCamera>(vcamGO);

        SetObjectRef(vcam, "m_Follow", player.transform);
        SetFloat(vcam, "m_Lens.m_OrthographicSize", 5f); // matches the previous static camera

        CinemachinePositionComposer composer = vcamGO.GetComponent<CinemachinePositionComposer>();
        if (composer == null)
            composer = Undo.AddComponent<CinemachinePositionComposer>(vcamGO);

        // Make sure the composer is registered in the camera's pipeline list.
        RegisterPipelineComponent(vcam, composer);

        // Defaults are acceptable if these fields ever move; we log instead of failing.
        SetVector3(composer, "m_Damping", new Vector3(0.5f, 0.5f, 0f));
        SetFloat(composer, "m_LookaheadTime", 0.1f);

        /* ---------------- 5. Player prefab ---------------- */
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Player"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Player");
        PrefabUtility.SaveAsPrefabAssetAndConnect(player, PlayerPrefabPath, InteractionMode.AutomatedAction);
        Debug.Log($"[SoledarSetup] Saved player prefab to '{PlayerPrefabPath}'.");

        /* ---------------- 6. Build settings ---------------- */
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(TestScenePath, true) };
        Debug.Log($"[SoledarSetup] Build scene 0 set to '{TestScenePath}'.");

        /* ---------------- 7. Delete broken SampleScene (user-approved) ---------------- */
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/SampleScene.unity") != null)
        {
            if (AssetDatabase.DeleteAsset("Assets/Scenes/SampleScene.unity"))
                Debug.Log("[SoledarSetup] Deleted broken SampleScene.unity (user-approved).");
            else
                Debug.LogWarning("[SoledarSetup] Could not delete SampleScene.unity — remove it manually in the Project window.");
        }

        if (AssetDatabase.IsValidFolder("Assets/Scenes 1"))
            AssetDatabase.DeleteAsset("Assets/Scenes 1");

        /* ---------------- 8. Save ---------------- */
        EditorSceneManager.MarkSceneDirty(scene);
        if (EditorSceneManager.SaveScene(scene))
            Debug.Log("[SoledarSetup] Phase 1 scene setup COMPLETE. Enter Play mode and follow the test checklist in Docs/DevLog.md.");
        else
            Debug.LogError("[SoledarSetup] Scene save failed — save manually (Ctrl+S).");
    }

    /* ===================== helpers ===================== */

    private static void RenameAsset(string path, string newName)
    {
        Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
        if (asset == null) return; // already renamed or missing

        string error = AssetDatabase.RenameAsset(asset, newName);
        if (string.IsNullOrEmpty(error))
            Debug.Log($"[SoledarSetup] Renamed '{path}' -> '{newName}'.");
        else
            Debug.LogWarning($"[SoledarSetup] Rename failed for '{path}': {error}");
    }

    private static void FixLayerNames()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0)
        {
            Debug.LogWarning("[SoledarSetup] TagManager.asset not found — layer names not trimmed.");
            return;
        }

        SerializedObject tagManager = new SerializedObject(assets[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        if (layers == null) return;

        int fixedCount = 0;
        for (int i = 0; i < layers.arraySize; i++)
        {
            string layerName = layers.GetArrayElementAtIndex(i).stringValue;
            if (string.IsNullOrEmpty(layerName)) continue;

            string trimmed = layerName.Trim();
            if (trimmed != layerName)
            {
                layers.GetArrayElementAtIndex(i).stringValue = trimmed;
                fixedCount++;
            }
        }

        if (fixedCount > 0)
        {
            tagManager.ApplyModifiedProperties();
            Debug.Log($"[SoledarSetup] Trimmed trailing spaces from {fixedCount} layer name(s) ('Ground ', 'Enemy ').");
        }
    }

    private static void RenameChildGameObject(Transform parent, string currentName, string newName)
    {
        Transform child = parent.Find(currentName);
        if (child == null || child.name == newName) return;

        Undo.RecordObject(child.gameObject, "Rename child");
        child.name = newName;
        EditorUtility.SetDirty(child.gameObject);
    }

    private static Transform FindChildByPrefix(Transform parent, string prefix)
    {
        foreach (Transform child in parent)
            if (child.name.StartsWith(prefix)) return child;
        return null;
    }

    private static void SetObjectRef(Object target, string propertyPath, Object value)
    {
        if (value == null)
        {
            Debug.LogWarning($"[SoledarSetup] Tried to assign null to '{propertyPath}' on '{target.name}' — skipped.");
            return;
        }

        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(propertyPath);
        if (prop == null)
        {
            Debug.LogWarning($"[SoledarSetup] Property '{propertyPath}' not found on {target.GetType().Name} — skipped.");
            return;
        }

        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(Object target, string propertyPath, float value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(propertyPath);
        if (prop == null)
        {
            Debug.LogWarning($"[SoledarSetup] Property '{propertyPath}' not found on {target.GetType().Name} — skipped (defaults kept).");
            return;
        }

        prop.floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetInt(Object target, string propertyPath, int value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(propertyPath);
        if (prop == null)
        {
            Debug.LogWarning($"[SoledarSetup] Property '{propertyPath}' not found on {target.GetType().Name} — skipped (defaults kept).");
            return;
        }

        prop.intValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetVector3(Object target, string propertyPath, Vector3 value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(propertyPath);
        if (prop == null)
        {
            Debug.LogWarning($"[SoledarSetup] Property '{propertyPath}' not found on {target.GetType().Name} — skipped (defaults kept).");
            return;
        }

        prop.vector3Value = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void RegisterPipelineComponent(CinemachineCamera vcam, CinemachineComponentBase component)
    {
        if (vcam == null || component == null) return;

        SerializedObject so = new SerializedObject(vcam);
        SerializedProperty comps = so.FindProperty("m_Components");
        if (comps == null)
        {
            Debug.LogWarning("[SoledarSetup] 'm_Components' not found on CinemachineCamera — composer may not be registered.");
            return;
        }

        for (int i = 0; i < comps.arraySize; i++)
            if (comps.GetArrayElementAtIndex(i).objectReferenceValue == component)
                return; // already registered

        comps.InsertArrayElementAtIndex(comps.arraySize);
        comps.GetArrayElementAtIndex(comps.arraySize - 1).objectReferenceValue = component;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
