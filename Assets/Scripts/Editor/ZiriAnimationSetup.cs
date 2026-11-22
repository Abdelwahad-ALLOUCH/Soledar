using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Imports the extracted Ziri frames as foot-pivoted sprites, builds the Idle /
/// Walk / Run clips and controller, and wires them onto the Player prefab.
///
/// Run Tools/Soledar/Extract Ziri Animation Frames first (and after replacing
/// the source sheet), then Tools/Soledar/Setup Ziri Animations.
/// </summary>
public static class ZiriAnimationSetup
{
    const string Folder = "Assets/Art/Characters/Ziri/Animations";
    const string ControllerPath = Folder + "/Ziri.controller";
    const string PrefabPath = "Assets/Prefabs/Player/Player.prefab";

    // 1.80 world units tall, matching the previous static Ziri sprite.
    const float PixelsPerUnit = 84f;
    const float PivotY = (float)ZiriFrameExtractor.FootAnchorY / ZiriFrameExtractor.CanvasHeight;

    [MenuItem("Tools/Soledar/Setup Ziri Animations")]
    static void SetupFromMenu()
    {
        if (SetupAll()) Debug.Log("[ZiriAnim] Animations set up.");
    }

    public static bool SetupAll()
    {
        string[] idle = FramePaths("Idle", 6);
        string[] walk = FramePaths("Walk", 8);
        string[] run = FramePaths("Run", 8);
        foreach (string p in idle) if (!System.IO.File.Exists(p)) { Debug.LogError("[ZiriAnim] Missing " + p + " — run the frame extractor first."); return false; }
        foreach (string p in walk) if (!System.IO.File.Exists(p)) { Debug.LogError("[ZiriAnim] Missing " + p); return false; }
        foreach (string p in run) if (!System.IO.File.Exists(p)) { Debug.LogError("[ZiriAnim] Missing " + p); return false; }

        ImportAll(idle); ImportAll(walk); ImportAll(run);
        AssetDatabase.Refresh();

        AnimationClip idleClip = BuildClip("Ziri_Idle", idle, 8f);
        AnimationClip walkClip = BuildClip("Ziri_Walk", walk, 12f);
        AnimationClip runClip = BuildClip("Ziri_Run", run, 14f);
        if (idleClip == null || walkClip == null || runClip == null) return false;

        AnimatorController controller = BuildController(idleClip, walkClip, runClip);
        if (controller == null) return false;

        return WirePrefab(AssetDatabase.LoadAssetAtPath<Sprite>(idle[0]), controller);
    }

    static string[] FramePaths(string label, int count)
    {
        string[] paths = new string[count];
        for (int i = 0; i < count; i++)
            paths[i] = Folder + "/Ziri_" + label + "_" + (i + 1).ToString("00") + ".png";
        return paths;
    }

    static void ImportAll(string[] paths)
    {
        foreach (string path in paths)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            importer.textureType = TextureImporterType.Sprite;

            // Pivot/alignment/mesh live on TextureImporterSettings in Unity 6.
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(0.5f, PivotY);
            settings.spritePixelsPerUnit = PixelsPerUnit;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);

            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }
    }

    static AnimationClip BuildClip(string name, string[] frames, float fps)
    {
        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frames.Length + 1];
        for (int i = 0; i < frames.Length; i++)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(frames[i]);
            if (sprite == null) { Debug.LogError("[ZiriAnim] Could not load sprite " + frames[i]); return null; }
            keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = sprite };
        }

        // Mecanim samples the last key before wrapping; repeating frame 1 at the
        // end of the cycle keeps the loop from holding the final pose for a beat.
        keys[frames.Length] = new ObjectReferenceKeyframe
        {
            time = frames.Length / fps,
            value = AssetDatabase.LoadAssetAtPath<Sprite>(frames[0])
        };

        AnimationClip clip = new AnimationClip { name = name, frameRate = fps };
        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = string.Empty,
            propertyName = "m_Sprite"
        };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        string path = Folder + "/" + name + ".anim";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(clip, path);
        return AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
    }

    static AnimatorController BuildController(AnimationClip idle, AnimationClip walk, AnimationClip run)
    {
        AssetDatabase.DeleteAsset(ControllerPath);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        if (controller == null) { Debug.LogError("[ZiriAnim] Could not create controller."); return null; }

        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        AnimatorStateMachine machine = controller.layers[0].stateMachine;

        AnimatorState idleState = machine.AddState("Idle", new Vector3(260f, 80f));
        AnimatorState walkState = machine.AddState("Walk", new Vector3(260f, 180f));
        AnimatorState runState = machine.AddState("Run", new Vector3(260f, 280f));
        idleState.motion = idle;
        walkState.motion = walk;
        runState.motion = run;
        machine.defaultState = idleState;

        AddTransition(idleState, walkState, AnimatorConditionMode.Greater, 0.3f);
        AddTransition(idleState, runState, AnimatorConditionMode.Greater, 9f);
        AddTransition(walkState, idleState, AnimatorConditionMode.Less, 0.3f);
        AddTransition(walkState, runState, AnimatorConditionMode.Greater, 9f);
        AddTransition(runState, walkState, AnimatorConditionMode.Less, 9f);
        AddTransition(runState, idleState, AnimatorConditionMode.Less, 0.3f);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    static void AddTransition(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, float threshold)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.05f;
        transition.AddCondition(mode, threshold, "Speed");
    }

    static bool WirePrefab(Sprite idleSprite, AnimatorController controller)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Transform visual = root.transform.Find("ZiriVisual");
            if (visual == null) { Debug.LogError("[ZiriAnim] Player prefab has no ZiriVisual child."); return false; }

            SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
            if (renderer == null) { Debug.LogError("[ZiriAnim] ZiriVisual has no SpriteRenderer."); return false; }
            renderer.sprite = idleSprite;

            Animator animator = visual.GetComponent<Animator>();
            if (animator == null) animator = visual.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            // The sprite pivot sits on the feet, so drop the visual to the
            // collider's bottom edge (the old sprite's pivot already did this).
            visual.localPosition = new Vector3(0f, -0.5f, 0f);

            ZiriVisual ziri = visual.GetComponent<ZiriVisual>();
            if (ziri != null)
            {
                SerializedObject so = new SerializedObject(ziri);
                so.FindProperty("animator").objectReferenceValue = animator;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log("[ZiriAnim] Player prefab wired with " + controller.name + ".");
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
