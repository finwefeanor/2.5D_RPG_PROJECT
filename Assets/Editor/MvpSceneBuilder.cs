// ============================================================
//  MVPSceneBuilder.cs  —  Assets/Editor/
//
//  Adds "RPG Scene Builder > Build MVP Scene" to the menu bar.
//  Instantiates the core prefabs into whatever scene is open.
//
//  Because every cross-object reference now resolves at runtime
//  through GameManager / GameEvents / ShopUIRefs, this script
//  only has to SPAWN things. If you ever find yourself adding
//  wiring below, that's a hint the script being wired should
//  resolve itself instead.
// ============================================================
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MVPSceneBuilder
{
    // Every core prefab lives here.
    private const string CorePath = "Assets/Prefabs/Core/";

    private static readonly string[] CorePrefabs =
    {
        "GameManager",
        "Player",
        "Merchant",
        "Enemy_0_with_sword",
        "ShopCanvas",
        "PlayerHealthBarCanvas",
        "PauseMenuCanvas",
        "HUDCanvas",
        "GlobalVolume",
    };

    [MenuItem("RPG Scene Builder/Build MVP Scene", false, 0)]
    public static void BuildMVPScene()
    {
        if (!EditorUtility.DisplayDialog(
                "Build MVP Scene",
                "This will add the core RPG prefabs to the currently open scene.\n\nContinue?",
                "Build", "Cancel"))
            return;

        Undo.SetCurrentGroupName("Build MVP Scene");
        int undoGroup = Undo.GetCurrentGroup();

        EnsureGround();
        EnsureLight();
        EnsureEventSystem();

        foreach (string prefabName in CorePrefabs)
            SpawnPrefab(prefabName);

        EnsureCamera();

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("[MVPSceneBuilder] Done. Press Play to test the core loop.");
    }

    // ── Prefab spawning ───────────────────────────────────────

    private static GameObject SpawnPrefab(string prefabName)
    {
        GameObject existing = GameObject.Find(prefabName);
        if (existing != null)
        {
            Debug.Log($"[MVPSceneBuilder] '{prefabName}' already in scene — skipped.");
            return existing;
        }

        string path = CorePath + prefabName + ".prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        if (prefab == null)
        {
            Debug.LogError($"[MVPSceneBuilder] Prefab not found at '{path}'. " +
                           $"Create it and place it in {CorePath}.");
            return null;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = prefabName;
        Undo.RegisterCreatedObjectUndo(instance, "Spawn " + prefabName);
        return instance;
    }

    // ── Scene scaffolding (generic — built in code, not prefabs)

    private static void EnsureGround()
    {
        if (GameObject.Find("Ground") != null) return;

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(5f, 1f, 5f); // 50x50 units
        Undo.RegisterCreatedObjectUndo(ground, "Create Ground");
    }

    private static void EnsureLight()
    {
        if (Object.FindAnyObjectByType<Light>() != null) return;

        GameObject lightGO = new GameObject("Directional Light");
        Light light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.shadows = LightShadows.Soft;
        light.intensity = 1.1f;
        light.color = new Color(1f, 0.96f, 0.84f);
        lightGO.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
        Undo.RegisterCreatedObjectUndo(lightGO, "Create Light");
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
    }

    private static void EnsureCamera()
    {
        Camera cam = Camera.main;

        if (cam == null)
        {
            GameObject camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            cam = camGO.AddComponent<Camera>();
            camGO.AddComponent<AudioListener>();
            Undo.RegisterCreatedObjectUndo(camGO, "Create Camera");
        }

        cam.transform.position = new Vector3(0f, 22f, -14f);
        cam.transform.rotation = Quaternion.Euler(58f, 0f, 0f);
        cam.fieldOfView = 50f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 200f;

        // The camera finds the Player itself at runtime via GameManager,
        // so all we do is make sure the controller is present.
        if (cam.GetComponent<TopDownCameraController>() == null)
            Undo.AddComponent<TopDownCameraController>(cam.gameObject);
    }
}