#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Unity.Cinemachine;

/// <summary>
/// Phase 6 — the Silent Palace region and the scene transitions into it.
///
/// HOW TO USE (after scripts compile):
///   1. Run "Tools > Soledar > Setup Phase 6 Silent Palace".
///      Clones Assets/TestScene.unity → Assets/Scenes/SilentPalace.unity (once),
///      strips the Medina content, paints the palace and places its actors.
///   2. Open Assets/Scenes/SunkenMedina.unity and run
///      "Tools > Soledar > Setup Phase 6 Scene Flow (active scene)".
///      Adds the doorway behind the Palace Gate and the return spawn point.
///
/// Both commands are idempotent: re-running them repositions/repaints but never
/// duplicates objects. They never touch Assets/TestScene.unity itself.
/// </summary>
public static class SilentPalaceBootstrap
{
    private const string TestScenePath = "Assets/TestScene.unity";
    private const string MedinaScenePath = "Assets/Scenes/SunkenMedina.unity";
    private const string MedinaSceneName = "SunkenMedina";
    private const string PalaceSceneName = "SilentPalace";
    private const string PalacePath = "Assets/Scenes/SilentPalace.unity";

    private const string PlaceholderSpritePath = "Assets/Art/Placeholder/PlaceholderSquare.png";
    private const string GroundTilePath = "Assets/GroundTile.asset";
    private const string WallTilePath = "Assets/Art/Placeholder/WallTile.asset";
    private const string DustCrawlerPrefabPath = "Assets/Prefabs/Enemies/DustCrawler.prefab";
    private const string MedinaGuardPrefabPath = "Assets/Prefabs/Enemies/MedinaGuard.prefab";

    /* Palace layout (world tiles) */
    private const int RegionWidth = 76;
    private const int RegionHeight = 22;
    private const float SpawnX = 4f;
    private const float SpawnY = 3.6f;
    private const float FloorTop = 3f;   // top surface of the entry hall
    private const float HallTop = 9f;    // top surface of the upper hall

    /// <summary>Medina-only objects that must not exist inside the palace clone.</summary>
    private static readonly string[] MedinaOnlyObjects =
    {
        // old prototype leftovers / demo checkpoint
        "Ground", "Square", "Checkpoint",
        // Medina enemies + boss
        "DustCrawler_Plaza", "MedinaGuard_Street", "DustCrawler_InnerGate", "DustCrawler_LowerStreet",
        "DustCrawler_District2", "MedinaGuard_District2", "Gatekeeper", "GatekeeperArena",
        // Medina benches + gates + key
        "Bench_Plaza", "Bench_Street", "Bench_CityWall", "Bench_UpperMedina",
        "MedinaGate", "SealedPassage", "PalaceGate", "MedinaGateKey",
        // Medina NPCs + set pieces
        "OldCartographer", "Aisha", "PogoPad",
        // Medina map rooms
        "Room_Start", "Room_Street", "Room_Rooftops", "Room_Courtyard",
        // vertical-slice demo additions (absent in TestScene, harmless here)
        "SilentMerchant", "LostSoul", "MedinaWarden", "DemoCompletion", "DemoCompletePanel",
        "Dirham_Street_01", "Dirham_Rooftop_01", "Dirham_Courtyard_01",
    };

    [MenuItem("Tools/Soledar/Setup Phase 6 Silent Palace")]
    public static void CreateOrUpdatePalace()
    {
        /* ---------------- 1. Clone TestScene once ---------------- */
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(PalacePath) == null)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TestScenePath) == null)
            {
                Debug.LogError("[SilentPalace] TestScene is missing — nothing was changed.");
                return;
            }

            if (!AssetDatabase.CopyAsset(TestScenePath, PalacePath))
            {
                Debug.LogError("[SilentPalace] Could not clone TestScene into Assets/Scenes/SilentPalace.unity.");
                return;
            }

            Debug.Log("[SilentPalace] Cloned TestScene → Assets/Scenes/SilentPalace.unity.");
        }

        Scene scene = EditorSceneManager.OpenScene(PalacePath, OpenSceneMode.Single);

        /* ---------------- 2. Guards ---------------- */
        GameObject player = GameObject.Find("Player");
        if (player == null || player.GetComponent<PlayerRespawn>() == null || player.GetComponent<SaveApplier>() == null)
        {
            Debug.LogError("[SilentPalace] The palace scene has no wired Player (PlayerRespawn/SaveApplier). Open Assets/TestScene.unity, run the Phase 1 + 4 tools, then delete Assets/Scenes/SilentPalace.unity and re-run this command.");
            return;
        }

        GameObject canvasGO = GameObject.Find("UI");
        if (canvasGO == null || canvasGO.GetComponent<DialogueUI>() == null)
        {
            Debug.LogError("[SilentPalace] The palace scene has no UI canvas with DialogueUI. Rebuild it from TestScene via the earlier phase tools first.");
            return;
        }

        GameObject levelGO = GameObject.Find("Level");
        if (levelGO == null || levelGO.transform.Find("GroundTiles") == null || levelGO.transform.Find("BackgroundTiles") == null)
        {
            Debug.LogError("[SilentPalace] 'Level' with GroundTiles/BackgroundTiles is missing — run Phase 3 setup on TestScene first.");
            return;
        }

        Sprite placeholder = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
        RuleTile groundTile = AssetDatabase.LoadAssetAtPath<RuleTile>(GroundTilePath);
        RuleTile wallTile = AssetDatabase.LoadAssetAtPath<RuleTile>(WallTilePath);
        if (placeholder == null || groundTile == null || wallTile == null)
        {
            Debug.LogError("[SilentPalace] Placeholder sprite / RuleTiles are missing — run the Phase 3 setup tool first.");
            return;
        }

        int groundLayerIndex = LayerMask.NameToLayer("Ground");
        if (groundLayerIndex < 0) groundLayerIndex = 3;

        /* ---------------- 3. Remove Medina content ---------------- */
        // Root-object walk rather than GameObject.Find: the retired prototype
        // 'Ground' box and the demo 'Checkpoint' are INACTIVE, and Find skips
        // inactive objects — they used to survive into the palace scene.
        RemoveMedinaObjects(scene);

        /* ---------------- 4. Palace geometry ---------------- */
        Tilemap ground = levelGO.transform.Find("GroundTiles").GetComponent<Tilemap>();
        Tilemap background = levelGO.transform.Find("BackgroundTiles").GetComponent<Tilemap>();

        PaintPalace(ground, background, groundTile, wallTile);

        // Cool ivory stone instead of the Medina's sandstone; the wall tint goes
        // dark indigo so the two regions read differently at a glance.
        ground.color = new Color(0.82f, 0.84f, 0.96f, 1f);
        background.color = new Color(0.42f, 0.42f, 0.6f, 1f);

        // Regression guard (see Phase 5 defect #1): the tilemap must never simulate.
        Rigidbody2D groundRb = ground.GetComponent<Rigidbody2D>();
        if (groundRb != null)
        {
            groundRb.bodyType = RigidbodyType2D.Static;
            groundRb.gravityScale = 0f;
            groundRb.freezeRotation = true;
            EditorUtility.SetDirty(groundRb);
        }

        /* ---------------- 5. Camera + hazards ---------------- */
        EnsureCameraBounds(74f);

        GameObject vcamGO = GameObject.Find("PlayerFollowCamera");
        if (vcamGO != null)
        {
            CinemachineConfiner2D confiner = vcamGO.GetComponent<CinemachineConfiner2D>();
            if (confiner != null)
                SetField(confiner, "BoundingShape2D", GameObject.Find("CameraBounds").GetComponent<PolygonCollider2D>());
            else
                Debug.LogWarning("[SilentPalace] PlayerFollowCamera has no CinemachineConfiner2D — the camera is not confined.");
        }

        PlaceDeathPit(new Vector2(38f, -2.5f), new Vector2(78f, 2f));

        /* ---------------- 6. Player arrival + spawn points ---------------- */
        Vector3 arrival = new Vector3(SpawnX, SpawnY, 0f);

        GameObject spawnPoint = GameObject.Find("SpawnPoint");
        if (spawnPoint != null) spawnPoint.transform.position = arrival;

        player.transform.position = arrival;
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb != null) playerRb.position = arrival;
        Physics2D.SyncTransforms();

        EnsureSpawnPoint("PalaceEntry", "palace_entry", arrival);

        GameObject followGO = vcamGO;
        if (followGO != null) followGO.transform.position = new Vector3(arrival.x, arrival.y, -10f);
        GameObject mainCamera = GameObject.Find("Main Camera");
        if (mainCamera != null) mainCamera.transform.position = new Vector3(arrival.x, arrival.y, -10f);

        /* ---------------- 7. Palace actors ---------------- */
        // Return doorway: walk into the left wall to leave for the Medina.
        EnsureTransition("PalaceDoorway", new Vector3(2.2f, 5f, 0f), new Vector2(1.4f, 4f),
            MedinaSceneName, "medina_return");

        EnsureRoom("Room_PalaceEntry", "palace_entry_hall", new Vector2(10f, 6f), new Vector2(18f, 12f));
        EnsureRoom("Room_PalaceGallery", "palace_gallery", new Vector2(31f, 6f), new Vector2(16f, 12f));
        EnsureRoom("Room_PalaceHall", "palace_upper_hall", new Vector2(56f, 15f), new Vector2(34f, 12f));

        EnsureBench("Bench_PalaceEntry", new Vector3(9f, 3.35f, 0f), "Forgotten Bench (Palace Entry)");

        // Merchant chain: the keeper only sells once Wind of the Atlas is owned.
        EnsureLanternKeeper(new Vector3(16.5f, 3.6f, 0f), placeholder);

        // Currency: 3 × 15 in the entry hall pays for Shadow Passage (40).
        EnsureDirham("Dirham_Palace_01", "dirham_palace_01", new Vector2(6f, 3.6f), 15, placeholder);
        EnsureDirham("Dirham_Palace_02", "dirham_palace_02", new Vector2(12f, 3.6f), 15, placeholder);
        EnsureDirham("Dirham_Palace_03", "dirham_palace_03", new Vector2(14f, 3.6f), 15, placeholder);

        // Shadow Passage barrier: blocks the corridor at ground level.
        EnsureBarrier("ShadowBarrier_Entry", "shadowPassage",
            new Vector3(20.5f, FloorTop + 1.5f, 0f), new Vector2(1f, 3f),
            new Color(0.35f, 0.22f, 0.55f, 0.9f), placeholder, groundLayerIndex);

        EnsureEnemy(DustCrawlerPrefabPath, "DustCrawler_PalaceEntry", new Vector3(24f, 3.5f, 0f));

        // Upper hall: the optional wind-gated alcove teaches ability-gated traversal.
        EnsureBarrier("WindBarrier_Alcove", "windOfAtlas",
            new Vector3(49.5f, HallTop + 1.5f, 0f), new Vector2(1f, 3f),
            new Color(0.35f, 0.55f, 0.7f, 0.9f), placeholder, groundLayerIndex);

        EnsureDirham("Dirham_Palace_04", "dirham_palace_04", new Vector2(52f, HallTop + 0.6f), 20, placeholder);

        EnsureEnemy(MedinaGuardPrefabPath, "MedinaGuard_PalaceHall", new Vector3(44f, HallTop + 0.5f, 0f));
        EnsureBench("Bench_PalaceHall", new Vector3(57f, HallTop + 0.35f, 0f), "Forgotten Bench (Silent Hall)");
        EnsureEnemy(DustCrawlerPrefabPath, "DustCrawler_PalaceHall", new Vector3(63f, HallTop + 0.5f, 0f));

        // Future region exit — intentionally sealed (no key exists).
        EnsureSealedDoor("PalaceSeal", new Vector3(71.5f, HallTop + 2f, 0f), new Vector2(2f, 4f), placeholder);

        /* ---------------- 8. Build settings ---------------- */
        EnsureBuildScene(TestScenePath);
        EnsureBuildScene(MedinaScenePath);
        EnsureBuildScene(PalacePath);

        /* ---------------- 9. Save ---------------- */
        EditorSceneManager.MarkSceneDirty(scene);
        if (EditorSceneManager.SaveScene(scene))
            Debug.Log("[SilentPalace] Phase 6 palace COMPLETE — open it in Play mode via the Medina doorway, or press Play here to check the layout. Follow the Phase 6 checklist in Docs/DevLog.md.");
        else
            Debug.LogError("[SilentPalace] Scene save failed — save manually (Ctrl+S).");
    }

    /* ================================================================= */
    /*  Medina-side scene flow (run on SunkenMedina)                     */
    /* ================================================================= */

    [MenuItem("Tools/Soledar/Setup Phase 6 Scene Flow (active scene)")]
    public static void SetupSceneFlow()
    {
        Scene scene = EditorSceneManager.GetActiveScene();

        GameObject player = GameObject.Find("Player");
        GameObject palaceGate = GameObject.Find("PalaceGate");
        if (player == null || palaceGate == null)
        {
            Debug.LogError(
                $"[SceneFlow] Active scene is '{scene.path}'. Open Assets/Scenes/SunkenMedina.unity " +
                "(the Medina side must contain the Player and the PalaceGate) and run this command again.");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(PalacePath) == null)
        {
            Debug.LogError("[SceneFlow] Assets/Scenes/SilentPalace.unity does not exist — run 'Tools > Soledar > Setup Phase 6 Silent Palace' first.");
            return;
        }

        // The shrine ledge floor is top y9 (x122..131) in front of the Palace Gate.
        EnsureSpawnPoint("MedinaReturn", "medina_return", new Vector3(126f, 9.6f, 0f));

        // Behind the (boss-opened) Palace Gate: walking through enters the palace.
        EnsureTransition("PalaceDoorway", new Vector3(130.6f, 10.5f, 0f), new Vector2(1.6f, 3f),
            PalaceSceneName, "palace_entry");

        EnsureBuildScene(MedinaScenePath);
        EnsureBuildScene(PalacePath);

        EditorSceneManager.MarkSceneDirty(scene);
        if (EditorSceneManager.SaveScene(scene))
            Debug.Log("[SceneFlow] Medina doorway wired — the Palace Gate now leads to SilentPalace (return spawn 'medina_return').");
        else
            Debug.LogError("[SceneFlow] Scene save failed — save manually (Ctrl+S).");
    }

    /* ================================================================= */
    /*  Painting                                                         */
    /* ================================================================= */

    private static void RemoveMedinaObjects(Scene scene)
    {
        var targets = new System.Collections.Generic.HashSet<string>(MedinaOnlyObjects);

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root == null) continue;

            if (targets.Contains(root.name))
            {
                Debug.Log($"[SilentPalace] Removed Medina object '{root.name}'.");
                Object.DestroyImmediate(root);
                continue;
            }

            // Named children of kept objects (e.g. the demo completion panel
            // under the Canvas) are removed individually.
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child == root.transform) continue;
                if (child == null || !targets.Contains(child.name)) continue;

                Debug.Log($"[SilentPalace] Removed Medina object '{child.name}'.");
                Object.DestroyImmediate(child.gameObject);
            }
        }
    }

    private static void PaintPalace(Tilemap ground, Tilemap background, RuleTile groundTile, RuleTile wallTile)
    {
        bool[,] solid = new bool[RegionWidth, RegionHeight];

        /* Entry hall + corridor + upper hall base floor (top surface y3) */
        Fill(solid, 1, 72, 0, 2);

        /* Left / right boundary walls */
        Fill(solid, 0, 0, 0, 21);
        Fill(solid, 73, 75, 0, 21);

        /* Death gap in the corridor (2 wide, jumpable, dashable) */
        Clear(solid, 27, 28, 0, 2);

        /* Grand stair of platforms up to the upper hall (each hop 2 tiles) */
        Fill(solid, 31, 32, 3, 4);   // p1 — top y5
        Fill(solid, 34, 35, 3, 6);   // p2 — top y7
        Fill(solid, 37, 38, 3, 8);   // p3 — top y9

        /* Upper hall (top surface y9) */
        Fill(solid, 39, 72, 0, 8);

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

        CompositeCollider2D composite = ground.GetComponent<CompositeCollider2D>();
        if (composite != null) composite.GenerateGeometry();

        Debug.Log($"[SilentPalace] Painted the palace: {painted} solid tiles, {RegionWidth * RegionHeight} background tiles.");
    }

    private static void Fill(bool[,] grid, int x0, int x1, int y0, int y1)
    {
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                if (x >= 0 && x < RegionWidth && y >= 0 && y < RegionHeight)
                    grid[x, y] = true;
    }

    private static void Clear(bool[,] grid, int x0, int x1, int y0, int y1)
    {
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                if (x >= 0 && x < RegionWidth && y >= 0 && y < RegionHeight)
                    grid[x, y] = false;
    }

    /* ================================================================= */
    /*  Object helpers                                                   */
    /* ================================================================= */

    private static GameObject EnsureObject(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go != null) return go;

        go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        return go;
    }

    private static void EnsureSpawnPoint(string name, string spawnId, Vector3 position)
    {
        GameObject go = EnsureObject(name);
        go.transform.position = position;

        SceneSpawnPoint point = go.GetComponent<SceneSpawnPoint>();
        if (point == null) point = Undo.AddComponent<SceneSpawnPoint>(go);
        SetField(point, "spawnId", spawnId);
    }

    private static void EnsureTransition(string name, Vector3 position, Vector2 size,
        string targetScene, string targetSpawnId)
    {
        GameObject go = EnsureObject(name);
        go.transform.position = position;

        BoxCollider2D col = go.GetComponent<BoxCollider2D>();
        if (col == null) col = Undo.AddComponent<BoxCollider2D>(go);
        col.isTrigger = true;
        col.size = size;

        SceneTransition transition = go.GetComponent<SceneTransition>();
        if (transition == null) transition = Undo.AddComponent<SceneTransition>(go);
        SetField(transition, "targetScene", targetScene);
        SetField(transition, "targetSpawnId", targetSpawnId);
    }

    private static void EnsureBench(string name, Vector3 position, string benchName)
    {
        GameObject go = EnsureObject(name);
        go.transform.position = position;
        go.layer = LayerMask.NameToLayer("Interactable");

        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
        sr.color = new Color(0.85f, 0.68f, 0.35f, 1f);
        sr.sortingOrder = 2;
        go.transform.localScale = new Vector3(1.6f, 0.7f, 1f);

        BoxCollider2D col = go.GetComponent<BoxCollider2D>();
        if (col == null) col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        ForgottenBench bench = go.GetComponent<ForgottenBench>();
        if (bench == null) bench = go.AddComponent<ForgottenBench>();
        SetField(bench, "benchName", benchName);
    }

    private static void EnsureLanternKeeper(Vector3 position, Sprite placeholder)
    {
        GameObject go = EnsureObject("LanternKeeper");
        go.transform.position = position;
        go.layer = LayerMask.NameToLayer("Interactable");

        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = placeholder;
        sr.color = new Color(0.95f, 0.72f, 0.3f, 1f);
        sr.sortingOrder = 2;
        go.transform.localScale = new Vector3(1f, 2f, 1f);

        BoxCollider2D col = go.GetComponent<BoxCollider2D>();
        if (col == null) col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1f, 1f);

        MerchantInteractable merchant = go.GetComponent<MerchantInteractable>();
        if (merchant == null) merchant = go.AddComponent<MerchantInteractable>();
        SetField(merchant, "itemId", "merchant_shadow_passage");
        SetField(merchant, "itemName", "Shadow Passage");
        SetField(merchant, "price", 40);
        SetField(merchant, "grantedAbility", "shadowPassage");
        SetField(merchant, "requiresAbility", "windOfAtlas");
        SetField(merchant, "lockedPrompt", "The Lantern Keeper waits for a windblessed wanderer");
    }

    private static void EnsureDirham(string name, string itemId, Vector2 position, int amount, Sprite placeholder)
    {
        GameObject go = EnsureObject(name);
        go.transform.position = position;
        go.transform.localScale = new Vector3(0.45f, 0.45f, 1f);

        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = placeholder;
        sr.color = new Color(0.95f, 0.78f, 0.25f, 1f);
        sr.sortingOrder = 3;

        CircleCollider2D col = go.GetComponent<CircleCollider2D>();
        if (col == null) col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        AncientDirhamPickup pickup = go.GetComponent<AncientDirhamPickup>();
        if (pickup == null) pickup = go.AddComponent<AncientDirhamPickup>();
        SetField(pickup, "itemId", itemId);
        SetField(pickup, "amount", amount);
    }

    private static void EnsureBarrier(string name, string requiredAbility, Vector3 position, Vector2 size,
        Color color, Sprite placeholder, int groundLayerIndex)
    {
        GameObject go = EnsureObject(name);
        go.transform.position = position;
        go.layer = groundLayerIndex;

        BoxCollider2D col = go.GetComponent<BoxCollider2D>();
        if (col == null) col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = false;
        col.size = size;

        // Visual is a child so the blocker can stay on this object while the
        // veil disappears when the ability is unlocked.
        Transform veilT = go.transform.Find("Veil");
        GameObject veil;
        if (veilT == null)
        {
            veil = new GameObject("Veil");
            Undo.RegisterCreatedObjectUndo(veil, "Create Veil");
            veil.transform.SetParent(go.transform, false);
        }
        else
        {
            veil = veilT.gameObject;
        }

        veil.transform.localPosition = Vector3.zero;
        veil.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer sr = veil.GetComponent<SpriteRenderer>();
        if (sr == null) sr = veil.AddComponent<SpriteRenderer>();
        sr.sprite = placeholder;
        sr.color = color;
        sr.sortingOrder = 1;

        AbilityGate gate = go.GetComponent<AbilityGate>();
        if (gate == null) gate = go.AddComponent<AbilityGate>();
        SetField(gate, "requiredAbility", requiredAbility);
        SetField(gate, "blocker", col);
        SetField(gate, "visual", veil);
    }

    private static void EnsureEnemy(string prefabPath, string name, Vector3 position)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null)
        {
            existing.transform.position = position;
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[SilentPalace] Enemy prefab missing at '{prefabPath}' — skipped '{name}'.");
            return;
        }

        GameObject enemy = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(enemy, "Place " + name);
        enemy.name = name;
        enemy.transform.position = position;
    }

    private static void EnsureRoom(string name, string roomId, Vector2 center, Vector2 size)
    {
        GameObject go = EnsureObject(name);
        go.transform.position = center;
        go.layer = LayerMask.NameToLayer("Interactable");

        BoxCollider2D col = go.GetComponent<BoxCollider2D>();
        if (col == null) col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = size;

        RoomDiscovery room = go.GetComponent<RoomDiscovery>();
        if (room == null) room = go.AddComponent<RoomDiscovery>();
        SetField(room, "roomId", roomId);
    }

    private static void EnsureSealedDoor(string name, Vector3 position, Vector2 size, Sprite placeholder)
    {
        GameObject go = EnsureObject(name);
        go.transform.position = position;
        go.layer = LayerMask.NameToLayer("Ground");
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = placeholder;
        sr.color = new Color(0.3f, 0.28f, 0.42f, 1f);
        sr.sortingOrder = 1;

        BoxCollider2D col = go.GetComponent<BoxCollider2D>();
        if (col == null) col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;

        Door door = go.GetComponent<Door>();
        if (door == null) door = go.AddComponent<Door>();
        SetField(door, "doorName", name);
        SetField(door, "startLocked", true);
    }

    private static void EnsureCameraBounds(float maxX)
    {
        GameObject go = EnsureObject("CameraBounds");
        PolygonCollider2D poly = go.GetComponent<PolygonCollider2D>();
        if (poly == null) poly = go.AddComponent<PolygonCollider2D>();
        poly.isTrigger = true;
        poly.points = new Vector2[]
        {
            new Vector2(1f, 0f),
            new Vector2(maxX, 0f),
            new Vector2(maxX, 20f),
            new Vector2(1f, 20f),
        };
        EditorUtility.SetDirty(poly);
    }

    private static void PlaceDeathPit(Vector2 center, Vector2 size)
    {
        GameObject pit = EnsureObject("DeathPit");
        pit.transform.position = new Vector3(center.x, center.y, 0f);

        BoxCollider2D col = pit.GetComponent<BoxCollider2D>();
        if (col == null) col = pit.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = size;

        if (pit.GetComponent<DeathPit>() == null) pit.AddComponent<DeathPit>();
    }

    private static void EnsureBuildScene(string path)
    {
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            if (scene.path == path) return;

        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes)
        {
            new EditorBuildSettingsScene(path, true)
        };
        EditorBuildSettings.scenes = list.ToArray();
        Debug.Log($"[SilentPalace] Added '{path}' to Build Settings.");
    }

    private static void SetField(Object target, string propertyName, object value)
    {
        if (target == null) return;

        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty field = serialized.FindProperty(propertyName);
        if (field == null)
        {
            Debug.LogWarning($"[SilentPalace] '{target.name}' has no serialized field '{propertyName}'.");
            return;
        }

        if (value is string s) field.stringValue = s;
        else if (value is int i) field.intValue = i;
        else if (value is float f) field.floatValue = f;
        else if (value is bool b) field.boolValue = b;
        else if (value is Object o) field.objectReferenceValue = o;

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }
}
#endif
