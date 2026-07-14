// ============================================================
//  ShopUIBuilder.cs  —  Assets/Editor/
//  Phase 2 — replaces ALL previous versions
//  Menu: RPG Scene Builder > Build Shop UI
//        RPG Scene Builder > Clear Shop UI
// ============================================================
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.SceneManagement;

public static class ShopUIBuilder
{
    [MenuItem("RPG Scene Builder/Build Shop UI")]
    public static void BuildShopUI()
    {
        ClearShopUI();

        // ── Canvas ─────────────────────────────────────────────
        var canvasGO = new GameObject("ShopCanvas");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Build Shop UI");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Shop Panel ─────────────────────────────────────────
        var panel = MakeImage(canvasGO, "ShopPanel",
            Vector2.zero, new Vector2(420, 500),
            new Color(0.10f, 0.09f, 0.08f, 0.97f));
        panel.SetActive(false);

        // ── Title ──────────────────────────────────────────────
        MakeText(panel, "TitleText", "SHOP",
            new Vector2(0, 210), new Vector2(380, 50),
            28, FontStyle.Bold, new Color(1f, 0.85f, 0.35f));

        // ── Gold Text ──────────────────────────────────────────
        var goldGO = MakeText(panel, "GoldText", "Gold: 100",
            new Vector2(0, 170), new Vector2(380, 32),
            18, FontStyle.Normal, new Color(0.95f, 0.82f, 0.3f));

        // ── Divider ────────────────────────────────────────────
        MakeDivider(panel, new Vector2(0, 148));

        // ── Item Button Container ──────────────────────────────
        // ShopManager spawns one button per ItemData into this at runtime
        var container = MakeLayoutGroup(panel, "ItemButtonContainer",
            new Vector2(0, 30), new Vector2(380, 200));

        // ── Divider ────────────────────────────────────────────
        MakeDivider(panel, new Vector2(0, -80));

        // ── Close Button ───────────────────────────────────────
        var closeBtn = MakeButton(panel, "CloseButton", "Close",
            new Vector2(0, -180), new Vector2(150, 40),
            new Color(0.35f, 0.33f, 0.30f));

        // ── Item Button Prefab ─────────────────────────────────
        // Inactive — ShopManager clones this at runtime per item
        var prefab = MakeItemButtonPrefab(canvasGO);

        // ── Wire ShopUIController ──────────────────────────────
        var shopUI = Object.FindObjectOfType<ShopUIController>();
        if (shopUI != null)
        {
            shopUI.shopUI = panel;
            EditorUtility.SetDirty(shopUI);
            Debug.Log("[ShopUIBuilder] Wired ShopUIController.");
        }
        else
        {
            Debug.LogWarning("[ShopUIBuilder] ShopUIController not found — wire shopUI manually.");
        }

        // Wire Close button to ShopUIController
        if (shopUI != null)
        {
            var btn = closeBtn.GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            // Note: runtime wiring only; for editor we just set up the reference
        }

        // ── Wire ShopManager ───────────────────────────────────
        var shopManager = Object.FindObjectOfType<ShopManager>();
        if (shopManager != null)
        {
            shopManager.goldText            = goldGO.GetComponent<Text>();
            shopManager.itemButtonContainer = container.transform;
            shopManager.itemButtonPrefab    = prefab;
            EditorUtility.SetDirty(shopManager);
            Debug.Log("[ShopUIBuilder] Wired ShopManager — goldText, container, prefab all assigned.");
        }
        else
        {
            Debug.LogWarning("[ShopUIBuilder] ShopManager not found — add it to Merchant first.");
        }

        // ── Wire ShopkeeperInteraction close toggle ─────────────
        var shopkeeper = Object.FindObjectOfType<ShopKeeperInteraction>();
        if (shopkeeper != null)
        {
            shopkeeper.shopUIController = shopUI;
            EditorUtility.SetDirty(shopkeeper);
            Debug.Log("[ShopUIBuilder] Wired ShopKeeperInteraction.");
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[ShopUIBuilder] Done. Now wire CloseButton OnClick in Inspector: " +
                  "Merchant -> ShopKeeperInteraction -> CloseShop");
    }

    [MenuItem("RPG Scene Builder/Clear Shop UI")]
    public static void ClearShopUI()
    {
        var existing = GameObject.Find("ShopCanvas");
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
            Debug.Log("[ShopUIBuilder] Cleared ShopCanvas.");
        }
    }

    // =============================================================
    //  Item button prefab — cloned by ShopManager at runtime
    // =============================================================
    static GameObject MakeItemButtonPrefab(GameObject parent)
    {
        var go = new GameObject("ItemButtonPrefab",
            typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(360, 55);
        SetColor(go, new Color(0.20f, 0.18f, 0.16f));

        // Color swatch
        MakeImage(go, "Swatch",
            new Vector2(-140, 0), new Vector2(36, 36), Color.white);

        // Item name label
        MakeText(go, "ItemName", "Item Name",
            new Vector2(20, 10), new Vector2(220, 26),
            15, FontStyle.Bold, Color.white);

        // Price label
        MakeText(go, "PriceText", "Price: 0",
            new Vector2(20, -12), new Vector2(220, 22),
            13, FontStyle.Normal, new Color(0.95f, 0.82f, 0.3f));

        go.SetActive(false); // hidden — ShopManager instantiates copies
        return go;
    }

    // =============================================================
    //  Helpers
    // =============================================================
    static GameObject MakeImage(GameObject parent, string name,
        Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        SetColor(go, color);
        return go;
    }

    static GameObject MakeText(GameObject parent, string name, string content,
        Vector2 pos, Vector2 size, int fontSize, FontStyle style, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var txt = go.GetComponent<Text>();
        txt.text = content;
        txt.fontSize = fontSize;
        txt.fontStyle = style;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = color;
        // Arial works in all Unity versions including 2021
        txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return go;
    }

    static GameObject MakeButton(GameObject parent, string name, string label,
        Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        SetColor(go, color);
        var btn = go.GetComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = color * 1.25f;
        colors.pressedColor = color * 0.75f;
        btn.colors = colors;
        MakeText(go, "Label", label, Vector2.zero, size, 14, FontStyle.Bold, Color.white);
        return go;
    }

    static GameObject MakeLayoutGroup(GameObject parent, string name,
        Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
        go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var layout = go.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(8, 8, 4, 4);
        return go;
    }

    static void MakeDivider(GameObject parent, Vector2 pos)
    {
        var go = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(380, 2);
        SetColor(go, new Color(1f, 1f, 1f, 0.10f));
    }

    static void SetColor(GameObject go, Color color)
    {
        var img = go.GetComponent<Image>();
        if (img != null) img.color = color;
    }
}