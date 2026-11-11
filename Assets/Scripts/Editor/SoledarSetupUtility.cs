#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Cinemachine;

/// <summary>
/// One-click scene configuration for SOLEDAR (Phase 1 + Phase 2).
///
/// MENU ITEM 1 — Tools > Soledar > Setup Phase 1 Scene
///
/// HOW TO USE:
///   1. Open Assets/TestScene.unity (and make sure no other scene is loaded).
///   2. Run the menu item:  Tools > Soledar > Setup Phase 1 Scene
///   3. Run the menu item:  Tools > Soledar > Setup Phase 2 Combat & NPCs
///   4. Enter Play mode and follow the test checklists in Docs/DevLog.md.
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
        SetFloat(attack, "fallbackRange", 0.5f);
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

    /* ===================== Phase 2 ===================== */

    private const string PlaceholderSpritePath = "Assets/Art/Placeholder/PlaceholderSquare.png";
    private const string ComboAssetPath = "Assets/ScriptableObjects/Combat/KhoumyaCombo.asset";
    private const string DustCrawlerDataPath = "Assets/ScriptableObjects/Enemies/DustCrawler.asset";
    private const string MedinaGuardDataPath = "Assets/ScriptableObjects/Enemies/MedinaGuard.asset";
    private const string CartographerDialoguePath = "Assets/ScriptableObjects/Dialogue/CartographerIntro.asset";
    private const string DustCrawlerPrefabPath = "Assets/Prefabs/Enemies/DustCrawler.prefab";
    private const string MedinaGuardPrefabPath = "Assets/Prefabs/Enemies/MedinaGuard.prefab";

    /// <summary>
    /// One-click combat &amp; interaction wiring (Phase 2). Requires Phase 1
    /// setup to have run on the open TestScene. Idempotent — safe to run again
    /// (note: data asset values are reset to the defaults below on each run).
    /// </summary>
    [MenuItem("Tools/Soledar/Setup Phase 2 Combat & NPCs")]
    public static void SetupPhase2()
    {
        Scene scene = EditorSceneManager.GetActiveScene();

        /* ---------------- Guards ---------------- */
        if (scene.path != TestScenePath)
        {
            Debug.LogError($"[SoledarSetup] Active scene is '{scene.path}'. Open '{TestScenePath}' first, then run this command again.");
            return;
        }

        GameObject player = GameObject.Find("Player");
        if (player == null || player.GetComponent<PlayerRespawn>() == null || player.GetComponent<PlayerInputReader>() == null)
        {
            Debug.LogError("[SoledarSetup] Phase 1 setup not detected (Player needs PlayerRespawn + PlayerInputReader). Run 'Tools > Soledar > Setup Phase 1 Scene' first.");
            return;
        }

        int enemyLayerIndex = LayerMask.NameToLayer("Enemy");
        if (enemyLayerIndex < 0) enemyLayerIndex = 6;
        int groundLayerIndex = LayerMask.NameToLayer("Ground");
        if (groundLayerIndex < 0) groundLayerIndex = 3;

        // Enemies pass through each other (prevents pile-ups when two chase
        // the player). This is a project-level setting and persists.
        Physics2D.IgnoreLayerCollision(enemyLayerIndex, enemyLayerIndex, true);

        /* ---------------- 1. Layer, placeholder art, folders ---------------- */
        int interactableLayerIndex = EnsureLayer("Interactable");
        if (interactableLayerIndex < 0)
        {
            Debug.LogError("[SoledarSetup] Could not ensure the 'Interactable' layer — Phase 2 setup aborted.");
            return;
        }

        Sprite placeholder = EnsurePlaceholderSprite();
        if (placeholder == null)
        {
            Debug.LogError("[SoledarSetup] Placeholder sprite could not be created — Phase 2 setup aborted.");
            return;
        }

        EnsureFolder("Assets/ScriptableObjects/Combat");
        EnsureFolder("Assets/ScriptableObjects/Enemies");
        EnsureFolder("Assets/ScriptableObjects/Dialogue");
        EnsureFolder("Assets/Prefabs/Enemies");

        /* ---------------- 2. Data assets ---------------- */
        AttackComboData combo = EnsureComboAsset();
        EnemyData dustData = EnsureEnemyData(
            DustCrawlerDataPath, "Dust Crawler",
            maxHealth: 3, knockbackResist: 0f, patrolSpeed: 1.5f, chaseSpeed: 2.5f,
            detectionRange: 5f, attackRange: 1.3f, lungeSpeed: 6f,
            tint: new Color(0.78f, 0.45f, 0.28f, 1f), groundLayerIndex);
        EnemyData guardData = EnsureEnemyData(
            MedinaGuardDataPath, "Medina Guard",
            maxHealth: 4, knockbackResist: 0.3f, patrolSpeed: 2f, chaseSpeed: 3.5f,
            detectionRange: 7f, attackRange: 1.4f, lungeSpeed: 7f,
            tint: new Color(0.38f, 0.44f, 0.62f, 1f), groundLayerIndex);
        DialogueData cartographer = EnsureCartographerDialogue();

        /* ---------------- 3. Player: combo, shake, interactor ---------------- */
        PlayerAttack attack = player.GetComponent<PlayerAttack>();
        if (attack == null)
            attack = Undo.AddComponent<PlayerAttack>(player);
        Transform attackPoint = player.transform.Find("AttackPoint");
        if (attackPoint == null)
            attackPoint = FindChildByPrefix(player.transform, "AttackPoint");
        SetObjectRef(attack, "attackPoint", attackPoint);
        SetInt(attack, "enemyLayer", 1 << enemyLayerIndex);
        SetObjectRef(attack, "comboData", combo);

        // Camera shake: impulse source on the player, listener on the vcam.
        if (player.GetComponent<CinemachineImpulseSource>() == null)
            Undo.AddComponent<CinemachineImpulseSource>(player);
        if (player.GetComponent<CameraShake>() == null)
            Undo.AddComponent<CameraShake>(player);

        GameObject vcamGO = GameObject.Find("PlayerFollowCamera");
        if (vcamGO != null)
        {
            if (vcamGO.GetComponent<CinemachineImpulseListener>() == null)
            {
                Undo.AddComponent<CinemachineImpulseListener>(vcamGO);
                Debug.Log("[SoledarSetup] Added CinemachineImpulseListener to PlayerFollowCamera (receives shake impulses).");
            }
        }
        else
        {
            Debug.LogWarning("[SoledarSetup] PlayerFollowCamera not found — camera shake will not work. Run Phase 1 setup first.");
        }

        /* ---------------- 4. Dialogue UI + interaction prompt ---------------- */
        GameObject canvasGO = GameObject.Find("UI");
        if (canvasGO == null)
        {
            Debug.LogError("[SoledarSetup] No Canvas GameObject named 'UI' in the scene.");
            return;
        }

        DialogueUI dialogueUI = canvasGO.GetComponent<DialogueUI>();
        if (dialogueUI == null)
            dialogueUI = Undo.AddComponent<DialogueUI>(canvasGO);

        // Dialogue panel: dark box at the bottom of the screen (hidden by default).
        Transform panelT = FindChildByPrefix(canvasGO.transform, "DialoguePanel");
        GameObject panelGO = panelT != null ? panelT.gameObject
            : CreateUIElement("DialoguePanel", canvasGO.transform,
                new Vector2(0.02f, 0f), new Vector2(0.98f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 250f), new Vector2(0f, 45f));
        if (panelGO.GetComponent<Image>() == null)
        {
            Image background = panelGO.AddComponent<Image>();
            background.color = new Color(0.05f, 0.04f, 0.08f, 0.92f);
            background.raycastTarget = false;
        }

        Text speakerText = GetOrCreateUIText("SpeakerText", panelGO.transform,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -62f), new Vector2(-28f, -14f),
            26, TextAnchor.MiddleLeft, new Color(0.95f, 0.82f, 0.5f, 1f));
        Text bodyText = GetOrCreateUIText("BodyText", panelGO.transform,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(28f, 46f), new Vector2(-28f, -70f),
            24, TextAnchor.UpperLeft, Color.white);
        Text hintText = GetOrCreateUIText("HintText", panelGO.transform,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-260f, 12f), new Vector2(-16f, 44f),
            20, TextAnchor.MiddleRight, new Color(1f, 1f, 1f, 0.7f));
        hintText.text = "E - continue";

        SetObjectRef(dialogueUI, "panel", panelGO);
        SetObjectRef(dialogueUI, "speakerText", speakerText);
        SetObjectRef(dialogueUI, "bodyText", bodyText);
        SetObjectRef(dialogueUI, "hintText", hintText);

        // Interaction prompt: bottom-center, hidden by default.
        Text promptText = GetOrCreateUIText("InteractionPrompt", canvasGO.transform,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-400f, 300f), new Vector2(400f, 344f),
            22, TextAnchor.LowerCenter, new Color(1f, 0.95f, 0.85f, 0.95f));
        promptText.text = string.Empty;
        promptText.gameObject.SetActive(false);

        PlayerInteractor interactor = player.GetComponent<PlayerInteractor>();
        if (interactor == null)
            interactor = Undo.AddComponent<PlayerInteractor>(player);
        SetFloat(interactor, "interactRadius", 1.3f);
        SetInt(interactor, "interactableMask", 1 << interactableLayerIndex);
        SetObjectRef(interactor, "promptText", promptText);

        /* ---------------- 5. Enemies ---------------- */
        // Existing enemy: 'Enemy ' (trailing space in the original scene).
        GameObject dust = GameObject.Find("Enemy ");
        if (dust == null) dust = GameObject.Find("DustCrawler");
        if (dust == null)
        {
            dust = new GameObject("DustCrawler");
            dust.transform.position = player.transform.position + new Vector3(5f, 1f, 0f);
            Debug.LogWarning("[SoledarSetup] No existing enemy found — created a new DustCrawler near the player.");
        }
        else if (dust.name != "DustCrawler")
        {
            Undo.RecordObject(dust, "Rename enemy");
            dust.name = "DustCrawler";
            EditorUtility.SetDirty(dust);
        }
        WireEnemy(dust, dustData, placeholder, enemyLayerIndex);

        GameObject guard = GameObject.Find("MedinaGuard");
        if (guard == null)
        {
            guard = new GameObject("MedinaGuard");
            Vector3 basePos = dust != null ? dust.transform.position : player.transform.position;
            guard.transform.position = basePos + new Vector3(3f, 0f, 0f);
            Undo.RegisterCreatedObjectUndo(guard, "Create MedinaGuard");
        }
        WireEnemy(guard, guardData, placeholder, enemyLayerIndex);

        /* ---------------- 6. Save enemy prefabs ---------------- */
        PrefabUtility.SaveAsPrefabAssetAndConnect(dust, DustCrawlerPrefabPath, InteractionMode.AutomatedAction);
        PrefabUtility.SaveAsPrefabAssetAndConnect(guard, MedinaGuardPrefabPath, InteractionMode.AutomatedAction);
        Debug.Log($"[SoledarSetup] Saved enemy prefabs to '{DustCrawlerPrefabPath}' and '{MedinaGuardPrefabPath}'.");

        /* ---------------- 7. NPC: The Old Cartographer ---------------- */
        GameObject spawn = GameObject.Find("SpawnPoint");
        GameObject npc = GameObject.Find("OldCartographer");
        if (npc == null)
        {
            npc = new GameObject("OldCartographer");
            Vector3 npcPos = spawn != null ? spawn.transform.position : player.transform.position;
            npc.transform.position = npcPos + new Vector3(2.5f, 0f, 0f);
            Undo.RegisterCreatedObjectUndo(npc, "Create OldCartographer");
        }
        npc.layer = interactableLayerIndex;

        SpriteRenderer npcSprite = npc.GetComponent<SpriteRenderer>();
        if (npcSprite == null)
            npcSprite = Undo.AddComponent<SpriteRenderer>(npc);
        npcSprite.sprite = placeholder;
        npcSprite.color = new Color(0.85f, 0.68f, 0.35f, 1f); // warm amber
        npcSprite.sortingOrder = 2;

        BoxCollider2D npcBox = npc.GetComponent<BoxCollider2D>();
        if (npcBox == null)
            npcBox = Undo.AddComponent<BoxCollider2D>(npc);
        npcBox.isTrigger = true;
        npcBox.size = new Vector2(1.2f, 2f);

        NPCInteractable npcInteractable = npc.GetComponent<NPCInteractable>();
        if (npcInteractable == null)
            npcInteractable = Undo.AddComponent<NPCInteractable>(npc);
        SetObjectRef(npcInteractable, "dialogue", cartographer);
        SetObjectRef(npcInteractable, "dialogueUI", dialogueUI);

        /* ---------------- 8. Save ---------------- */
        EditorSceneManager.MarkSceneDirty(scene);
        if (EditorSceneManager.SaveScene(scene))
            Debug.Log("[SoledarSetup] Phase 2 setup COMPLETE. Enter Play mode and follow the Phase 2 checklist in Docs/DevLog.md.");
        else
            Debug.LogError("[SoledarSetup] Scene save failed — save manually (Ctrl+S).");
    }

    /* ===================== Phase 2 helpers ===================== */

    /// <summary>
    /// Adds/wires every component an enemy needs. Component order contract:
    /// EnemyHealth -> ContactDamage -> EnemyAI -> EnemyAttack, so EnemyAttack's
    /// FixedUpdate (lunge velocity) runs after EnemyAI's (state movement).
    /// </summary>
    private static void WireEnemy(GameObject enemy, EnemyData data, Sprite placeholder, int enemyLayerIndex)
    {
        enemy.layer = enemyLayerIndex;

        Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
        if (rb == null) rb = Undo.AddComponent<Rigidbody2D>(enemy);
        rb.gravityScale = 3f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolate = RigidbodyInterpolation2D.Interpolate;

        BoxCollider2D box = enemy.GetComponent<BoxCollider2D>();
        if (box == null) box = Undo.AddComponent<BoxCollider2D>(enemy);
        if (box.size.sqrMagnitude < 0.01f) box.size = new Vector2(1f, 1f);

        SpriteRenderer sprite = enemy.GetComponent<SpriteRenderer>();
        if (sprite == null) sprite = Undo.AddComponent<SpriteRenderer>(enemy);
        sprite.sprite = placeholder;
        sprite.color = data.placeholderColor;
        sprite.sortingOrder = 1;

        EnemyHealth health = enemy.GetComponent<EnemyHealth>();
        if (health == null) health = Undo.AddComponent<EnemyHealth>(enemy);
        SetObjectRef(health, "data", data);

        ContactDamage contact = enemy.GetComponent<ContactDamage>();
        if (contact == null) contact = Undo.AddComponent<ContactDamage>(enemy);
        SetObjectRef(contact, "data", data);

        EnemyAI ai = enemy.GetComponent<EnemyAI>();
        if (ai == null) ai = Undo.AddComponent<EnemyAI>(enemy);
        SetObjectRef(ai, "data", data);

        EnemyAttack lunge = enemy.GetComponent<EnemyAttack>();
        if (lunge == null) lunge = Undo.AddComponent<EnemyAttack>(enemy);
        SetObjectRef(lunge, "data", data);
    }

    private static int EnsureLayer(string layerName)
    {
        int existing = LayerMask.NameToLayer(layerName);
        if (existing >= 0) return existing;

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0)
        {
            Debug.LogWarning("[SoledarSetup] TagManager.asset not found — layer not added.");
            return -1;
        }

        SerializedObject tagManager = new SerializedObject(assets[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        if (layers == null)
        {
            Debug.LogWarning("[SoledarSetup] 'layers' not found in TagManager — layer not added.");
            return -1;
        }

        // User layers start at index 8 (0-7 are built-in).
        for (int i = 8; i < layers.arraySize; i++)
        {
            if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
            {
                layers.GetArrayElementAtIndex(i).stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                Debug.Log($"[SoledarSetup] Added layer '{layerName}' at index {i}.");
                return i;
            }
        }

        Debug.LogWarning($"[SoledarSetup] No empty layer slot for '{layerName}' — all user layers are in use.");
        return -1;
    }

    private static Sprite EnsurePlaceholderSprite()
    {
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
        if (existing != null) return existing;

        EnsureFolder("Assets/Art/Placeholder");

        Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[64 * 64];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(255, 255, 255, 255);
        texture.SetPixels32(pixels);
        texture.Apply();

        File.WriteAllBytes(PlaceholderSpritePath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(PlaceholderSpritePath);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(PlaceholderSpritePath);
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }

        Debug.Log($"[SoledarSetup] Created placeholder sprite '{PlaceholderSpritePath}'.");
        return AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
    }

    private static void EnsureFolder(string fullPath)
    {
        string[] segments = fullPath.Split('/');
        string current = segments[0]; // "Assets"
        for (int i = 1; i < segments.Length; i++)
        {
            string next = current + "/" + segments[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, segments[i]);
            current = next;
        }
    }

    private static T CreateAssetIfMissing<T>(string path) where T : ScriptableObject
    {
        T existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null) return existing;

        EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
        T asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        Debug.Log($"[SoledarSetup] Created asset '{path}'.");
        return asset;
    }

    private static AttackComboData EnsureComboAsset()
    {
        AttackComboData combo = CreateAssetIfMissing<AttackComboData>(ComboAssetPath);

        combo.steps = new[]
        {
            // Step 1: quick horizontal slash
            new AttackComboData.AttackStep { damage = 1, range = 0.5f, recoveryTime = 0.22f, comboWindow = 0.3f, knockbackMultiplier = 1f, hitStopDuration = 0.03f, shakeForce = 0.25f },
            // Step 2: diagonal slash
            new AttackComboData.AttackStep { damage = 1, range = 0.55f, recoveryTime = 0.22f, comboWindow = 0.3f, knockbackMultiplier = 1.1f, hitStopDuration = 0.035f, shakeForce = 0.3f },
            // Step 3: finisher
            new AttackComboData.AttackStep { damage = 2, range = 0.6f, recoveryTime = 0.4f, comboWindow = 0.3f, knockbackMultiplier = 1.6f, hitStopDuration = 0.07f, shakeForce = 0.6f },
        };

        EditorUtility.SetDirty(combo);
        return combo;
    }

    private static EnemyData EnsureEnemyData(string path, string enemyName,
        int maxHealth, float knockbackResist, float patrolSpeed, float chaseSpeed,
        float detectionRange, float attackRange, float lungeSpeed, Color tint, int groundLayerIndex)
    {
        EnemyData data = CreateAssetIfMissing<EnemyData>(path);

        data.enemyName = enemyName;
        data.maxHealth = maxHealth;
        data.knockbackResist = knockbackResist;
        data.knockbackForce = 6f;
        data.contactDamage = 1;
        data.contactCooldown = 0.8f;
        data.patrolSpeed = patrolSpeed;
        data.chaseSpeed = chaseSpeed;
        data.detectionRange = detectionRange;
        data.attackRange = attackRange;
        data.attackCooldown = 1.5f;
        data.attackWindup = 0.35f;
        data.lungeSpeed = lungeSpeed;
        data.lungeDuration = 0.25f;
        data.recoveryTime = 0.35f;
        data.edgeProbeDistance = 0.6f;
        data.edgeProbeDepth = 0.6f;
        data.groundMask = 1 << groundLayerIndex;
        data.placeholderColor = tint;

        EditorUtility.SetDirty(data);
        return data;
    }

    private static DialogueData EnsureCartographerDialogue()
    {
        DialogueData dialogue = CreateAssetIfMissing<DialogueData>(CartographerDialoguePath);

        dialogue.speakerName = "The Old Cartographer";
        dialogue.lines = new[]
        {
            "Ah! A living traveler... the desert has not taken you yet.",
            "I mapped these streets when the medina still breathed. Now I chart only what the ash leaves behind.",
            "Old routes, hidden doors - all of it lives in my head, and my head is failing me.",
            "Bring me what the ruins whisper, and I will draw you a path through SOLEDAR.",
        };

        EditorUtility.SetDirty(dialogue);
        return dialogue;
    }

    private static GameObject CreateUIElement(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.sizeDelta = sizeDelta;
        rect.anchoredPosition = anchoredPosition;
        rect.localScale = Vector3.one;
        return go;
    }

    private static Text CreateUIText(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
        int fontSize, TextAnchor alignment, Color color)
    {
        GameObject go = CreateUIElement(name, parent, anchorMin, anchorMax,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        Text text = go.AddComponent<Text>();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            Debug.LogWarning("[SoledarSetup] Built-in font 'LegacyRuntime.ttf' not found — assign a font to the new Text objects.");
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    /// <summary>Creates the Text if it doesn't exist yet (idempotent UI wiring).</summary>
    private static Text GetOrCreateUIText(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
        int fontSize, TextAnchor alignment, Color color)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            Text existingText = existing.GetComponent<Text>();
            if (existingText != null) return existingText;
        }

        return CreateUIText(name, parent, anchorMin, anchorMax, offsetMin, offsetMax, fontSize, alignment, color);
    }

    /* ===================== Phase 3 ===================== */

    private const string GroundTilePath = "Assets/GroundTile.asset";
    private const string WallTileAssetPath = "Assets/Art/Placeholder/WallTile.asset";
    private const string GroundSpritePath = "Assets/Art/Placeholder/PlaceholderGround.png";
    private const string WallSpritePath = "Assets/Art/Placeholder/PlaceholderWall.png";
    private const string CameraBoundsGOName = "CameraBounds";

    private const int RegionWidth = 96;
    private const int RegionHeight = 22;

    /// <summary>
    /// Builds the first region — "Medina Outskirts" — as a real tilemap level:
    ///   * Left-to-right exploration: plaza → pit → elevated street (Medina
    ///     Guard) → key alcove → locked Medina Gate → inner gate street →
    ///     descending steps → dash gaps (death pits) → climb → city wall
    ///     plateau with a bench and the sealed passage (Phase 4+ exit).
    ///   * All jumps respect the player's real numbers: apex ~2.45 tiles
    ///     (jumpForce 12, gravity scale 3), dash ~3.2 units.
    ///   * Camera confined to the region (CinemachineConfiner2D).
    /// Requires Phase 1 + 2 setup. Idempotent.
    /// </summary>
    [MenuItem("Tools/Soledar/Setup Phase 3 Region (Medina Outskirts)")]
    public static void SetupPhase3()
    {
        Scene scene = EditorSceneManager.GetActiveScene();

        /* ---------------- Guards ---------------- */
        if (scene.path != TestScenePath)
        {
            Debug.LogError($"[SoledarSetup] Active scene is '{scene.path}'. Open '{TestScenePath}' first, then run this command again.");
            return;
        }

        GameObject player = GameObject.Find("Player");
        if (player == null || player.GetComponent<PlayerRespawn>() == null || player.GetComponent<PlayerInputReader>() == null)
        {
            Debug.LogError("[SoledarSetup] Phase 1 setup not detected. Run 'Tools > Soledar > Setup Phase 1 Scene' first.");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<AttackComboData>(ComboAssetPath) == null)
        {
            Debug.LogError("[SoledarSetup] Phase 2 setup not detected (KhoumyaCombo asset missing). Run 'Tools > Soledar > Setup Phase 2 Combat & NPCs' first.");
            return;
        }

        GameObject dustPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DustCrawlerPrefabPath);
        GameObject guardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MedinaGuardPrefabPath);
        if (dustPrefab == null || guardPrefab == null)
        {
            Debug.LogError("[SoledarSetup] Enemy prefabs missing (Assets/Prefabs/Enemies). Run 'Tools > Soledar > Setup Phase 2 Combat & NPCs' first.");
            return;
        }

        /* ---------------- 1. Placeholder tiles ---------------- */
        EnsureFolder("Assets/Art/Placeholder");

        Sprite groundSprite = EnsurePlaceholderSpriteVariant(GroundSpritePath,
            new Color32(140, 122, 95, 255), new Color32(176, 154, 118, 255), new Color32(104, 90, 70, 255));
        Sprite wallSprite = EnsurePlaceholderSpriteVariant(WallSpritePath,
            new Color32(96, 86, 76, 255), new Color32(112, 102, 90, 255), new Color32(78, 70, 62, 255));
        if (groundSprite == null || wallSprite == null)
        {
            Debug.LogError("[SoledarSetup] Placeholder ground/wall sprites could not be created — Phase 3 setup aborted.");
            return;
        }

        RuleTile groundTile = EnsureGroundTile(groundSprite);
        RuleTile wallTile = EnsureWallTile(wallSprite);
        if (groundTile == null || wallTile == null)
        {
            Debug.LogError("[SoledarSetup] Tile assets could not be prepared — Phase 3 setup aborted.");
            return;
        }

        /* ---------------- 2. Level grid + tilemaps ---------------- */
        GameObject levelGO = GameObject.Find("Level");
        if (levelGO == null)
        {
            levelGO = new GameObject("Level");
            Undo.RegisterCreatedObjectUndo(levelGO, "Create Level");
        }
        if (levelGO.GetComponent<Grid>() == null)
            levelGO.AddComponent<Grid>();

        Tilemap background = GetOrCreateTilemap(levelGO.transform, "BackgroundTiles", wallTile,
            new Color(0.55f, 0.52f, 0.5f, 1f), sortingOrder: -10, groundLayer: false);
        Tilemap ground = GetOrCreateTilemap(levelGO.transform, "GroundTiles", groundTile,
            Color.white, sortingOrder: -5, groundLayer: true);

        PaintRegion(ground, background, groundTile, wallTile);

        /* ---------------- 3. Retire superseded prototype objects ---------------- */
        SetGameObjectActive(GameObject.Find("Ground"), false, "old prototype ground box (replaced by the tilemap region)");
        SetGameObjectActive(GameObject.Find("Checkpoint"), false, "demo checkpoint (replaced by Forgotten Benches)");

        /* ---------------- 4. Camera confiner ---------------- */
        Collider2D boundsShape = EnsureCameraBounds();

        GameObject vcamGO = GameObject.Find("PlayerFollowCamera");
        if (vcamGO == null)
        {
            Debug.LogWarning("[SoledarSetup] PlayerFollowCamera not found — camera will not be confined. Run Phase 1 setup first.");
        }
        else
        {
            CinemachineConfiner2D confiner = vcamGO.GetComponent<CinemachineConfiner2D>();
            if (confiner == null)
            {
                confiner = Undo.AddComponent<CinemachineConfiner2D>(vcamGO);
                Debug.Log("[SoledarSetup] Added CinemachineConfiner2D to PlayerFollowCamera.");
            }
            SetObjectRef(confiner, "m_BoundingShape2D", boundsShape);
            SetFloat(confiner, "m_Damping", 0.5f);
        }

        /* ---------------- 5. Region actors ---------------- */
        // Player + spawn point at the plaza.
        Vector3 regionSpawn = new Vector3(5f, 3.6f, 0f);
        Undo.RecordObject(player.transform, "Move player to region spawn");
        player.transform.position = regionSpawn;

        GameObject spawn = GameObject.Find("SpawnPoint");
        if (spawn == null)
        {
            spawn = new GameObject("SpawnPoint");
            Undo.RegisterCreatedObjectUndo(spawn, "Create SpawnPoint");
        }
        Undo.RecordObject(spawn.transform, "Move SpawnPoint to region spawn");
        spawn.transform.position = regionSpawn;

        GameObject npc = GameObject.Find("OldCartographer");
        if (npc != null)
        {
            Undo.RecordObject(npc.transform, "Move NPC into region");
            npc.transform.position = new Vector3(9f, 3.6f, 0f);
        }
        else
        {
            Debug.LogWarning("[SoledarSetup] OldCartographer not found — run Phase 2 setup to place the NPC.");
        }

        // Forgotten Benches (E to rest: heal + set respawn).
        PlaceBench("Bench_Plaza", new Vector3(12f, 3.35f, 0f));
        PlaceBench("Bench_Street", new Vector3(29f, 4.35f, 0f));
        PlaceBench("Bench_CityWall", new Vector3(83.5f, 10.35f, 0f));

        // Key + gate progression loop.
        Door gate = PlaceDoor("MedinaGate", new Vector3(40.5f, 8f, 0f),
            new Vector2(2f, 6f), new Color(0.45f, 0.2f, 0.16f, 1f), locked: true);
        PlaceKey("MedinaGateKey", new Vector3(36f, 8.6f, 0f), gate);

        // Sealed passage on the plateau — region exit teaser (Phase 4+).
        PlaceDoor("SealedPassage", new Vector3(86.5f, 12f, 0f),
            new Vector2(2f, 4f), new Color(0.3f, 0.3f, 0.38f, 1f), locked: true);

        // Death pit under the whole region (catches the two dash gaps).
        PlaceDeathPit(new Vector2(47.5f, -2.5f), new Vector2(96f, 2f));

        // Enemies: reuse the Phase 2 scene instances for the first two spots,
        // instantiate the rest from the saved prefabs.
        EnsureEnemyAt(dustPrefab, new[] { "DustCrawler", "DustCrawler_Plaza" }, "DustCrawler_Plaza", new Vector3(22f, 3.5f, 0f));
        EnsureEnemyAt(guardPrefab, new[] { "MedinaGuard", "MedinaGuard_Street" }, "MedinaGuard_Street", new Vector3(32f, 5.5f, 0f));
        EnsureEnemyAt(dustPrefab, new[] { "DustCrawler_InnerGate" }, "DustCrawler_InnerGate", new Vector3(46f, 5.5f, 0f));
        EnsureEnemyAt(dustPrefab, new[] { "DustCrawler_LowerStreet" }, "DustCrawler_LowerStreet", new Vector3(66f, 3.5f, 0f));

        /* ---------------- 6. Save ---------------- */
        EditorSceneManager.MarkSceneDirty(scene);
        if (EditorSceneManager.SaveScene(scene))
            Debug.Log("[SoledarSetup] Phase 3 setup COMPLETE — the Medina Outskirts region is playable. Follow the Phase 3 checklist in Docs/DevLog.md.");
        else
            Debug.LogError("[SoledarSetup] Scene save failed — save manually (Ctrl+S).");
    }

    /* ===================== Phase 3 helpers ===================== */

    private static void FillRect(bool[,] grid, int x0, int x1, int y0, int y1)
    {
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                if (x >= 0 && x < RegionWidth && y >= 0 && y < RegionHeight)
                    grid[x, y] = true;
    }

    private static void ClearRect(bool[,] grid, int x0, int x1, int y0, int y1)
    {
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                if (x >= 0 && x < RegionWidth && y >= 0 && y < RegionHeight)
                    grid[x, y] = false;
    }

    private static void PaintRegion(Tilemap ground, Tilemap background, RuleTile groundTile, RuleTile wallTile)
    {
        bool[,] solid = new bool[RegionWidth, RegionHeight];

        /* Base street + side walls */
        FillRect(solid, 1, 93, 0, 2);      // base ground, top surface y=3
        FillRect(solid, 0, 1, 0, 21);      // left wall
        FillRect(solid, 94, 95, 0, 21);    // right wall

        /* Teaching pit (2 deep, climbable out) */
        ClearRect(solid, 15, 18, 0, 2);
        FillRect(solid, 15, 18, 0, 0);

        /* Step up + elevated street (surfaces y=4, then y=5) */
        FillRect(solid, 25, 30, 3, 3);
        FillRect(solid, 31, 47, 3, 4);

        /* Key alcove above the elevated street + stepping stone.
           Clearance below the alcove is 2 tiles (player fits under);
           reaching it: street y5 → stone top y7 → alcove top y8. */
        FillRect(solid, 35, 37, 7, 7);
        FillRect(solid, 38, 38, 6, 6);

        /* Descend after the gate: y5 → y4 → y3 */
        FillRect(solid, 48, 50, 3, 3);

        /* Dash gaps (fall = death pit below) */
        ClearRect(solid, 56, 57, 0, 2);
        ClearRect(solid, 71, 72, 0, 2);

        /* Climb to the city wall (each hop ≤ 2 tiles up) */
        FillRect(solid, 73, 74, 4, 4);
        FillRect(solid, 76, 77, 6, 6);
        FillRect(solid, 79, 80, 8, 8);

        /* City-wall plateau, top surface y=10 */
        FillRect(solid, 82, 93, 0, 9);

        ground.ClearAllTiles();
        background.ClearAllTiles();

        int painted = 0;
        for (int y = 0; y < RegionHeight; y++)
        {
            for (int x = 0; x < RegionWidth; x++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                background.SetTile(pos, wallTile);
                if (solid[x, y])
                {
                    ground.SetTile(pos, groundTile);
                    painted++;
                }
            }
        }

        Debug.Log($"[SoledarSetup] Painted '{ground.gameObject.name}': {painted} solid tiles; '{background.gameObject.name}': {RegionWidth * RegionHeight} background tiles.");
    }

    private static Tilemap GetOrCreateTilemap(Transform parent, string name, RuleTile tile,
        Color color, int sortingOrder, bool groundLayer)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name);
        if (existing == null)
        {
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
        }

        go.layer = groundLayer ? LayerMask.NameToLayer("Ground") : 0;

        Tilemap tilemap = go.GetComponent<Tilemap>();
        if (tilemap == null) tilemap = go.AddComponent<Tilemap>();
        tilemap.color = color;

        TilemapRenderer renderer = go.GetComponent<TilemapRenderer>();
        if (renderer == null) renderer = go.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = sortingOrder;

        if (groundLayer)
        {
            // One merged collider for the whole ground: better physics queries
            // and a single edge for the camera confiner / enemy probes.
            TilemapCollider2D tileCollider = go.GetComponent<TilemapCollider2D>();
            if (tileCollider == null) tileCollider = Undo.AddComponent<TilemapCollider2D>(go);
            tileCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

            if (go.GetComponent<CompositeCollider2D>() == null)
                Undo.AddComponent<CompositeCollider2D>(go); // adds a static Rigidbody2D automatically
        }

        return tilemap;
    }

    private static RuleTile EnsureGroundTile(Sprite groundSprite)
    {
        RuleTile tile = AssetDatabase.LoadAssetAtPath<RuleTile>(GroundTilePath);
        if (tile == null)
        {
            Debug.LogError($"[SoledarSetup] RuleTile not found at '{GroundTilePath}'.");
            return null;
        }

        SerializedObject so = new SerializedObject(tile);
        SerializedProperty spriteProp = so.FindProperty("m_DefaultSprite");
        if (spriteProp != null && spriteProp.objectReferenceValue == null)
        {
            spriteProp.objectReferenceValue = groundSprite;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tile);
            Debug.Log("[SoledarSetup] Repaired GroundTile's broken default sprite reference.");
        }
        return tile;
    }

    private static RuleTile EnsureWallTile(Sprite wallSprite)
    {
        RuleTile tile = AssetDatabase.LoadAssetAtPath<RuleTile>(WallTileAssetPath);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<RuleTile>();
            AssetDatabase.CreateAsset(tile, WallTileAssetPath);
            Debug.Log($"[SoledarSetup] Created '{WallTileAssetPath}'.");
        }

        SerializedObject so = new SerializedObject(tile);
        SerializedProperty spriteProp = so.FindProperty("m_DefaultSprite");
        if (spriteProp != null && spriteProp.objectReferenceValue == null)
            spriteProp.objectReferenceValue = wallSprite;
        SerializedProperty colliderProp = so.FindProperty("m_DefaultColliderType");
        if (colliderProp != null)
            colliderProp.intValue = 0; // background tiles never collide
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(tile);
        return tile;
    }

    /// <summary>Placeholder sprite with a base color, a lighter top band and darker strata.</summary>
    private static Sprite EnsurePlaceholderSpriteVariant(string path, Color32 baseColor, Color32 topColor, Color32 edgeColor)
    {
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color32 c = baseColor;
                if (y >= size - 10) c = topColor;        // surface highlight
                else if (y < 6) c = edgeColor;           // bottom shade
                else if (y % 16 == 7) c = edgeColor;     // strata line
                pixels[y * size + x] = c;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(path);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }

        Debug.Log($"[SoledarSetup] Created placeholder sprite '{path}'.");
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Collider2D EnsureCameraBounds()
    {
        GameObject boundsGO = GameObject.Find(CameraBoundsGOName);
        if (boundsGO == null)
        {
            boundsGO = new GameObject(CameraBoundsGOName);
            Undo.RegisterCreatedObjectUndo(boundsGO, "Create CameraBounds");
        }

        PolygonCollider2D poly = boundsGO.GetComponent<PolygonCollider2D>();
        if (poly == null)
            poly = boundsGO.AddComponent<PolygonCollider2D>();

        // Trigger-only: the shape must never physically block the player
        // (otherwise falls into pits would land on the polygon's bottom edge).
        poly.isTrigger = true;

        Vector2[] points = new Vector2[]
        {
            new Vector2(1f, 0f),
            new Vector2(95f, 0f),
            new Vector2(95f, 20f),
            new Vector2(1f, 20f),
        };
        bool changed = poly.points.Length != points.Length;
        if (!changed)
        {
            for (int i = 0; i < points.Length; i++)
                if (poly.points[i] != points[i]) { changed = true; break; }
        }
        if (changed)
        {
            Undo.RecordObject(poly, "Set CameraBounds polygon");
            poly.points = points;
            EditorUtility.SetDirty(poly);
        }

        return poly;
    }

    private static void SetGameObjectActive(GameObject go, bool active, string reason)
    {
        if (go == null || go.activeSelf == active) return;
        Undo.RecordObject(go, (active ? "Enable " : "Disable ") + go.name);
        go.SetActive(active);
        EditorUtility.SetDirty(go);
        Debug.Log($"[SoledarSetup] {(active ? "Enabled" : "Disabled")} '{go.name}' — {reason}.");
    }

    private static void PlaceBench(string name, Vector3 position)
    {
        GameObject bench = GameObject.Find(name);
        if (bench == null)
        {
            bench = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(bench, "Create " + name);

            bench.layer = LayerMask.NameToLayer("Interactable");
            SpriteRenderer sprite = bench.AddComponent<SpriteRenderer>();
            sprite.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
            sprite.color = new Color(0.85f, 0.68f, 0.35f, 1f); // warm amber
            sprite.sortingOrder = 2;
            bench.transform.localScale = new Vector3(1.6f, 0.7f, 1f);

            BoxCollider2D trigger = bench.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;

            bench.AddComponent<ForgottenBench>();
        }

        Undo.RecordObject(bench.transform, "Place " + name);
        bench.transform.position = position;
    }

    private static Door PlaceDoor(string name, Vector3 position, Vector2 size, Color color, bool locked)
    {
        GameObject doorGO = GameObject.Find(name);
        if (doorGO == null)
        {
            doorGO = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(doorGO, "Create " + name);

            doorGO.layer = LayerMask.NameToLayer("Ground"); // blocks player + enemies + probes
            SpriteRenderer sprite = doorGO.AddComponent<SpriteRenderer>();
            sprite.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
            sprite.color = color;
            sprite.sortingOrder = 1;
            doorGO.transform.localScale = new Vector3(size.x, size.y, 1f);

            BoxCollider2D box = doorGO.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1f, 1f); // scaled with the object

            doorGO.AddComponent<Door>();
        }

        Undo.RecordObject(doorGO.transform, "Place " + name);
        doorGO.transform.position = position;
        return doorGO.GetComponent<Door>();
    }

    private static void PlaceKey(string name, Vector3 position, Door targetDoor)
    {
        GameObject key = GameObject.Find(name);
        if (key == null)
        {
            key = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(key, "Create " + name);

            SpriteRenderer sprite = key.AddComponent<SpriteRenderer>();
            sprite.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
            sprite.color = new Color(0.95f, 0.8f, 0.3f, 1f); // gold
            sprite.sortingOrder = 3;
            key.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

            BoxCollider2D trigger = key.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;

            CollectibleKey keyComponent = key.AddComponent<CollectibleKey>();
            SetObjectRef(keyComponent, "targetDoor", targetDoor);
        }

        Undo.RecordObject(key.transform, "Place " + name);
        key.transform.position = position;
    }

    private static void PlaceDeathPit(Vector2 center, Vector2 size)
    {
        GameObject pit = GameObject.Find("DeathPit");
        if (pit == null)
        {
            pit = new GameObject("DeathPit");
            Undo.RegisterCreatedObjectUndo(pit, "Create DeathPit");
            pit.AddComponent<DeathPit>();
        }

        BoxCollider2D box = pit.GetComponent<BoxCollider2D>();
        if (box == null)
            box = Undo.AddComponent<BoxCollider2D>(pit);
        box.isTrigger = true;
        box.size = size;

        Undo.RecordObject(pit.transform, "Place DeathPit");
        pit.transform.position = new Vector3(center.x, center.y, 0f);
    }

    /// <summary>Reuses an existing scene enemy (Phase 2 instances) or spawns the prefab.</summary>
    private static void EnsureEnemyAt(GameObject prefab, string[] possibleNames, string finalName, Vector3 position)
    {
        GameObject enemy = null;
        foreach (string n in possibleNames)
        {
            enemy = GameObject.Find(n);
            if (enemy != null) break;
        }

        if (enemy == null)
        {
            enemy = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(enemy, "Place " + finalName);
        }

        Undo.RecordObject(enemy, "Rename " + finalName);
        Undo.RecordObject(enemy.transform, "Move " + finalName);
        enemy.name = finalName;
        enemy.transform.position = position;
    }
}
#endif
