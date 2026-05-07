// ============================================================
//  RPGSceneBuilder.cs  –  place in Assets/Editor/
//  Unity 2022 LTS +  (works with any URP / Built-in pipeline)
//
//  Menu:  RPG Scene Builder ▸ Build Scene
//                           ▸ Clear Scene
//                           ▸ Rebuild Scene  (clear + build)
// ============================================================
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public static class RPGSceneBuilder
{
    // ── Palette ──────────────────────────────────────────────
    static readonly Color COL_GROUND     = new Color(0.35f, 0.55f, 0.25f);
    static readonly Color COL_FLOOR      = new Color(0.75f, 0.70f, 0.60f);
    static readonly Color COL_WALL       = new Color(0.55f, 0.50f, 0.42f);
    static readonly Color COL_ROOF       = new Color(0.60f, 0.25f, 0.20f);
    static readonly Color COL_DOOR       = new Color(0.45f, 0.30f, 0.15f);
    static readonly Color COL_TREE_TRUNK = new Color(0.40f, 0.28f, 0.15f);
    static readonly Color COL_TREE_TOP   = new Color(0.20f, 0.50f, 0.18f);
    static readonly Color COL_PLAYER     = new Color(0.25f, 0.45f, 0.85f);
    static readonly Color COL_ENEMY      = new Color(0.85f, 0.25f, 0.25f);
    static readonly Color COL_NPC        = new Color(0.85f, 0.75f, 0.25f);
    static readonly Color COL_CHEST      = new Color(0.65f, 0.50f, 0.20f);
    static readonly Color COL_WATER      = new Color(0.20f, 0.45f, 0.75f);
    static readonly Color COL_PATH       = new Color(0.80f, 0.72f, 0.55f);
    static readonly Color COL_FENCE      = new Color(0.55f, 0.42f, 0.28f);
    static readonly Color COL_ROCK       = new Color(0.55f, 0.53f, 0.50f);

    // ── Material cache ────────────────────────────────────────
    static Dictionary<Color, Material> _matCache = new Dictionary<Color, Material>();

    // =============================================================
    //  Menu items
    // =============================================================
    [MenuItem("RPG Scene Builder/Build Scene")]
    public static void BuildScene()
    {
        _matCache.Clear();
        var root = GetOrCreateRoot();
        BuildAll(root);
        SetupCamera();
        SetupLighting();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[RPGSceneBuilder] Scene built successfully.");
    }

    [MenuItem("RPG Scene Builder/Clear Scene")]
    public static void ClearScene()
    {
        var root = GameObject.Find("_RPGScene");
        if (root != null) Undo.DestroyObjectImmediate(root);
        Debug.Log("[RPGSceneBuilder] Scene cleared.");
    }

    [MenuItem("RPG Scene Builder/Rebuild Scene")]
    public static void RebuildScene()
    {
        ClearScene();
        BuildScene();
    }

    // =============================================================
    //  Top-level build
    // =============================================================
    static void BuildAll(GameObject root)
    {
        // Ground & terrain
        BuildGround(root);

        // Layout objects  (edit these lists to match your prototype)
        BuildPaths(root);
        BuildWater(root);
        BuildBuildings(root);
        BuildTrees(root);
        BuildRocks(root);
        BuildFences(root);
        BuildChests(root);

        // Characters
        BuildPlayer(root);
        BuildEnemies(root);
        BuildNPCs(root);
    }

    // =============================================================
    //  Ground
    // =============================================================
    static void BuildGround(GameObject root)
    {
        // Main grass plane  (40 × 40 world units)
        var g = MakePrimitive(PrimitiveType.Plane, "Ground", COL_GROUND, root);
        g.transform.localScale = new Vector3(4, 1, 4);
        g.transform.position   = Vector3.zero;
    }

    // =============================================================
    //  Paths  – flat, slightly raised quads that act as dirt roads
    // =============================================================
    static void BuildPaths(GameObject root)
    {
        var folder = Folder("Paths", root);

        // Horizontal path through the middle
        MakePath(folder, new Vector3(0, 0.01f, 0), new Vector3(40, 1, 3));
        // Vertical path
        MakePath(folder, new Vector3(0, 0.01f, 0), new Vector3(3, 1, 40));
    }

    static void MakePath(GameObject folder, Vector3 pos, Vector3 scale)
    {
        var p = MakePrimitive(PrimitiveType.Plane, "Path", COL_PATH, folder);
        p.transform.position   = pos;
        p.transform.localScale = scale / 10f; // Plane is 10 units by default
    }

    // =============================================================
    //  Water
    // =============================================================
    static void BuildWater(GameObject root)
    {
        var folder = Folder("Water", root);
        var w = MakePrimitive(PrimitiveType.Plane, "Pond", COL_WATER, folder);
        w.transform.position   = new Vector3(-12, 0.02f, -12);
        w.transform.localScale = new Vector3(0.8f, 1, 0.8f);
        // Make slightly transparent
        var mat = GetOrCreateMaterial(COL_WATER);
        mat.SetFloat("_Mode", 3);
        Color c = COL_WATER; c.a = 0.75f;
        mat.color = c;
    }

    // =============================================================
    //  Buildings
    // =============================================================
    static void BuildBuildings(GameObject root)
    {
        var folder = Folder("Buildings", root);

        // Inn  (larger building, top-right)
        MakeBuilding(folder, "Inn",     new Vector3( 10, 0,  10), new Vector3(6, 3, 5), true);
        // Blacksmith (smaller, left)
        MakeBuilding(folder, "Smithy",  new Vector3(-10, 0,  10), new Vector3(4, 2.5f, 4), false);
        // Shop
        MakeBuilding(folder, "Shop",    new Vector3( 10, 0, -10), new Vector3(5, 3, 4), true);
        // Small house
        MakeBuilding(folder, "House1",  new Vector3(-14, 0,  -5), new Vector3(4, 2, 3), false);
        MakeBuilding(folder, "House2",  new Vector3(-14, 0,   5), new Vector3(4, 2, 3), false);
    }

    static void MakeBuilding(GameObject folder, string name,
                             Vector3 pos, Vector3 size, bool hasRoof)
    {
        var bldgRoot = Folder(name, folder);

        // Walls (one cube = whole building shell)
        var walls = MakePrimitive(PrimitiveType.Cube, "Walls", COL_WALL, bldgRoot);
        walls.transform.position   = pos + Vector3.up * (size.y / 2f);
        walls.transform.localScale = size;

        // Floor (thin slab so we see inside from top-down)
        var floor = MakePrimitive(PrimitiveType.Cube, "Floor", COL_FLOOR, bldgRoot);
        floor.transform.position   = pos + Vector3.up * 0.05f;
        floor.transform.localScale = new Vector3(size.x - 0.2f, 0.1f, size.z - 0.2f);

        // Roof
        if (hasRoof)
        {
            var roof = MakePrimitive(PrimitiveType.Cube, "Roof", COL_ROOF, bldgRoot);
            roof.transform.position   = pos + Vector3.up * (size.y + 0.3f);
            roof.transform.localScale = new Vector3(size.x + 0.4f, 0.4f, size.z + 0.4f);
        }

        // Door  (small dark rectangle on front face)
        var door = MakePrimitive(PrimitiveType.Cube, "Door", COL_DOOR, bldgRoot);
        door.transform.position   = pos + new Vector3(0, 0.9f, -(size.z / 2f) - 0.01f);
        door.transform.localScale = new Vector3(1f, 1.8f, 0.1f);
    }

    // =============================================================
    //  Trees
    // =============================================================
    static void BuildTrees(GameObject root)
    {
        var folder = Folder("Trees", root);

        var treePositions = new Vector3[]
        {
            new Vector3(-5,  0,  14), new Vector3( 5,  0,  14),
            new Vector3( 15, 0,   3), new Vector3( 15, 0,  -3),
            new Vector3(-15, 0,   3), new Vector3(-15, 0,  -3),
            new Vector3(-5,  0, -14), new Vector3( 5,  0, -14),
            new Vector3( 18, 0,  18), new Vector3(-18, 0,  18),
            new Vector3( 18, 0, -18), new Vector3(-18, 0, -18),
        };

        int i = 0;
        foreach (var pos in treePositions)
            MakeTree(folder, $"Tree_{i++}", pos);
    }

    static void MakeTree(GameObject folder, string name, Vector3 pos)
    {
        var t = Folder(name, folder);

        // Trunk
        var trunk = MakePrimitive(PrimitiveType.Cylinder, "Trunk", COL_TREE_TRUNK, t);
        trunk.transform.position   = pos + Vector3.up * 0.8f;
        trunk.transform.localScale = new Vector3(0.3f, 0.8f, 0.3f);

        // Foliage (sphere)
        var top = MakePrimitive(PrimitiveType.Sphere, "Foliage", COL_TREE_TOP, t);
        top.transform.position   = pos + Vector3.up * 2.2f;
        top.transform.localScale = new Vector3(1.4f, 1.4f, 1.4f);
    }

    // =============================================================
    //  Rocks
    // =============================================================
    static void BuildRocks(GameObject root)
    {
        var folder = Folder("Rocks", root);

        var rocks = new (Vector3 pos, Vector3 scale)[]
        {
            (new Vector3(-8,  0,  -8),  new Vector3(1.2f, 0.8f, 1.0f)),
            (new Vector3(-9,  0,  -7),  new Vector3(0.7f, 0.5f, 0.9f)),
            (new Vector3( 7,  0,   8),  new Vector3(1.4f, 0.9f, 1.1f)),
            (new Vector3(-17, 0,  12),  new Vector3(1.0f, 0.7f, 1.2f)),
        };

        int i = 0;
        foreach (var (pos, scale) in rocks)
        {
            var r = MakePrimitive(PrimitiveType.Sphere, $"Rock_{i++}", COL_ROCK, folder);
            r.transform.position   = pos + Vector3.up * (scale.y / 2f);
            r.transform.localScale = scale;
        }
    }

    // =============================================================
    //  Fences
    // =============================================================
    static void BuildFences(GameObject root)
    {
        var folder = Folder("Fences", root);

        // A simple fence row along the top edge of the map
        for (int i = -9; i <= 9; i++)
        {
            if (i == 0) continue; // gate gap
            MakeFencePost(folder, new Vector3(i * 2, 0, 19));
        }
        // Fence along bottom edge
        for (int i = -9; i <= 9; i++)
        {
            if (i == 0) continue;
            MakeFencePost(folder, new Vector3(i * 2, 0, -19));
        }
    }

    static void MakeFencePost(GameObject folder, Vector3 pos)
    {
        var post = MakePrimitive(PrimitiveType.Cube, "FencePost", COL_FENCE, folder);
        post.transform.position   = pos + Vector3.up * 0.6f;
        post.transform.localScale = new Vector3(0.15f, 1.2f, 0.15f);

        // Horizontal rail
        var rail = MakePrimitive(PrimitiveType.Cube, "Rail", COL_FENCE, folder);
        rail.transform.position   = pos + Vector3.up * 0.9f;
        rail.transform.localScale = new Vector3(2f, 0.1f, 0.1f);
    }

    // =============================================================
    //  Chests & interactables
    // =============================================================
    static void BuildChests(GameObject root)
    {
        var folder = Folder("Chests", root);

        var chestPositions = new Vector3[]
        {
            new Vector3(8, 0, 8), new Vector3(-8, 0, 9), new Vector3(2, 0, -8),
        };

        int i = 0;
        foreach (var pos in chestPositions)
        {
            var chest = MakePrimitive(PrimitiveType.Cube, $"Chest_{i++}", COL_CHEST, folder);
            chest.transform.position   = pos + Vector3.up * 0.3f;
            chest.transform.localScale = new Vector3(0.7f, 0.6f, 0.5f);
            // Tag it so your runtime scripts can find it
            chest.tag = "Untagged"; // Replace with "Interactable" if you have that tag
        }
    }

    // =============================================================
    //  Player
    // =============================================================
    static void BuildPlayer(GameObject root)
    {
        var folder = Folder("Characters", root);
        var player = Folder("Player", folder);

        // Body
        var body = MakePrimitive(PrimitiveType.Capsule, "Body", COL_PLAYER, player);
        body.transform.position   = new Vector3(0, 1, 0);
        body.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);

        // Direction indicator (small cube in front so you know which way they face)
        var dir = MakePrimitive(PrimitiveType.Cube, "FacingIndicator", Color.white, player);
        dir.transform.position   = new Vector3(0, 1.15f, 0.4f);
        dir.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);

        player.AddComponent<SceneBuilderNote>().Note =
            "PLAYER – Replace with your PlayerController. Capsule collider already attached.";
    }

    // =============================================================
    //  Enemies
    // =============================================================
    static void BuildEnemies(GameObject root)
    {
        var folder = GameObject.Find("_RPGScene/Characters") ??
                     Folder("Characters", root);

        var enemyPositions = new Vector3[]
        {
            new Vector3( 6, 0,  6),
            new Vector3(-6, 0,  7),
            new Vector3( 7, 0, -6),
            new Vector3(-7, 0, -7),
        };

        int i = 0;
        foreach (var pos in enemyPositions)
        {
            var enemy = Folder($"Enemy_{i}", folder);
            var body  = MakePrimitive(PrimitiveType.Capsule, "Body", COL_ENEMY, enemy);
            body.transform.position   = pos + Vector3.up * 0.6f;
            body.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);

            var dir = MakePrimitive(PrimitiveType.Cube, "FacingIndicator", Color.white, enemy);
            dir.transform.position   = pos + new Vector3(0, 1.1f, 0.35f);
            dir.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);

            enemy.AddComponent<SceneBuilderNote>().Note = $"ENEMY_{i} – Attach your EnemyAI here.";
            i++;
        }
    }

    // =============================================================
    //  NPCs
    // =============================================================
    static void BuildNPCs(GameObject root)
    {
        var folder = GameObject.Find("_RPGScene/Characters") ??
                     Folder("Characters", root);

        var npcs = new (Vector3 pos, string npcName)[]
        {
            (new Vector3( 9,  0, 9),  "Innkeeper"),
            (new Vector3(-9,  0, 9),  "Blacksmith"),
            (new Vector3( 9,  0, -9), "Merchant"),
        };

        foreach (var (pos, npcName) in npcs)
        {
            var npc  = Folder(npcName, folder);
            var body = MakePrimitive(PrimitiveType.Capsule, "Body", COL_NPC, npc);
            body.transform.position   = pos + Vector3.up * 0.6f;
            body.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);

            npc.AddComponent<SceneBuilderNote>().Note = $"NPC: {npcName} – Attach dialogue component here.";
        }
    }

    // =============================================================
    //  Camera
    // =============================================================
    static void SetupCamera()
    {
        var camGO = GameObject.Find("Main Camera");
        if (camGO == null)
        {
            camGO = new GameObject("Main Camera");
            camGO.AddComponent<Camera>();
            camGO.AddComponent<AudioListener>();
            camGO.tag = "MainCamera";
        }

        var cam = camGO.GetComponent<Camera>();
        // Top-down perspective, looking straight down at ~45° angle
        camGO.transform.position = new Vector3(0, 22, -14);
        camGO.transform.rotation = Quaternion.Euler(58, 0, 0);
        cam.fieldOfView          = 50f;
        cam.nearClipPlane        = 0.1f;
        cam.farClipPlane         = 200f;

        camGO.AddComponent<SceneBuilderNote>().Note =
            "Top-down RPG camera. Attach your CameraController script here for follow behaviour.";
    }

    // =============================================================
    //  Lighting
    // =============================================================
    static void SetupLighting()
    {
        // Directional light (sun)
        var lightGO = GameObject.Find("Directional Light");
        if (lightGO == null)
        {
            lightGO = new GameObject("Directional Light");
            var l = lightGO.AddComponent<Light>();
            l.type = LightType.Directional;
        }

        var light = lightGO.GetComponent<Light>();
        light.intensity             = 1.1f;
        light.color                 = new Color(1f, 0.96f, 0.84f);
        lightGO.transform.rotation  = Quaternion.Euler(55, -30, 0);

        // Ambient
        RenderSettings.ambientMode      = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight     = new Color(0.3f, 0.35f, 0.42f);
        RenderSettings.skybox           = null;
        RenderSettings.fogColor         = new Color(0.7f, 0.8f, 0.7f);
    }

    // =============================================================
    //  Helpers
    // =============================================================

    static GameObject GetOrCreateRoot()
    {
        var existing = GameObject.Find("_RPGScene");
        if (existing != null) return existing;
        var r = new GameObject("_RPGScene");
        Undo.RegisterCreatedObjectUndo(r, "Create RPG Scene Root");
        return r;
    }

    static GameObject Folder(string name, GameObject parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    static GameObject MakePrimitive(PrimitiveType type, string name,
                                    Color color, GameObject parent)
    {
        var go  = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent.transform, false);

        // Apply shared material
        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = GetOrCreateMaterial(color);

        // Remove colliders from pure-visual children (keep them on characters & walls)
        // Colliders are fine for now – remove the ones you don't need at runtime.
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        return go;
    }

    static Material GetOrCreateMaterial(Color color)
    {
        if (_matCache.TryGetValue(color, out var cached)) return cached;

        var mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        // Slightly rougher look
        mat.SetFloat("_Glossiness", 0.15f);
        mat.SetFloat("_Metallic",   0.0f);
        _matCache[color] = mat;
        return mat;
    }
}
