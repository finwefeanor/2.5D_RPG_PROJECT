// ============================================================
//  ShopUIBuilder.cs  —  place in Assets/Editor/
//
//  Menu:  RPG Scene Builder ▸ Build Shop UI
//                           ▸ Clear Shop UI
//
//  Generates a complete Canvas with:
//    - Shop panel (background)
//    - Title label
//    - Two item buttons (Hat + Shirt) with name and price
//    - Close button
//    - Player gold display
//  Then finds ShopUIController, ShopManager, ShopItem components
//  and wires all references automatically.
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
        // ── Canvas ────────────────────────────────────────────
        var canvasGO = new GameObject("ShopCanvas");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create ShopCanvas");

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Shop Panel ────────────────────────────────────────
        var panel = MakePanel(canvasGO,  "ShopPanel",
            new Vector2(0, 0), new Vector2(400, 460));
        SetColor(panel, new Color(0.12f, 0.10f, 0.08f, 0.96f));
        panel.SetActive(false); // hidden until shopkeeper triggers it

        // ── Title ─────────────────────────────────────────────
        var title = MakeText(panel, "TitleText", "SHOP",
            new Vector2(0, 180), new Vector2(360, 50), 28, FontStyle.Bold);
        title.GetComponent<Text>().color = new Color(1f, 0.85f, 0.4f);

        // ── Gold display ──────────────────────────────────────
        var goldLabel = MakeText(panel, "GoldText", "Gold: 100",
            new Vector2(0, 140), new Vector2(360, 35), 18, FontStyle.Normal);
        goldLabel.GetComponent<Text>().color = new Color(0.9f, 0.8f, 0.3f);

        // ── Divider ───────────────────────────────────────────
        MakeDivider(panel, new Vector2(0, 110));

        // ── Item buttons ──────────────────────────────────────
        // Hat
        var hatBtn = MakeItemButton(panel, "HatItemButton",
            "Hat", "Price: 50",
            new Vector2(0, 50),
            new Color(0.55f, 0.30f, 0.15f)); // brown = hat color

        // Shirt
        var shirtBtn = MakeItemButton(panel, "ShirtItemButton",
            "Shirt", "Price: 30",
            new Vector2(0, -30),
            new Color(0.20f, 0.55f, 0.75f)); // cyan = shirt color

        // ── Equip / Unequip buttons ───────────────────────────
        var equipBtn   = MakeButton(panel, "EquipButton",   "Equip",
            new Vector2(-70, -110), new Vector2(120, 36));
        var unequipBtn = MakeButton(panel, "UnequipButton", "Unequip",
            new Vector2( 70, -110), new Vector2(120, 36));
        SetButtonColor(equipBtn,   new Color(0.25f, 0.65f, 0.30f));
        SetButtonColor(unequipBtn, new Color(0.65f, 0.25f, 0.25f));
        // Hidden by default; PlayerInventory will show them after first purchase
        equipBtn.SetActive(false);
        unequipBtn.SetActive(false);

        // ── Divider ───────────────────────────────────────────
        MakeDivider(panel, new Vector2(0, -148));

        // ── Close button ─────────────────────────────────────
        var closeBtn = MakeButton(panel, "CloseButton", "Close Shop",
            new Vector2(0, -185), new Vector2(160, 40));
        SetButtonColor(closeBtn, new Color(0.5f, 0.15f, 0.15f));

        // ── Wire ShopUIController ─────────────────────────────
        var shopUIController = FindOrWarn<ShopUIController>("ShopUIController");
        if (shopUIController != null)
        {
            shopUIController.shopUI = panel;
            // Hook Close button → CloseShop
            closeBtn.GetComponent<Button>().onClick.AddListener(shopUIController.CloseShop);
            EditorUtility.SetDirty(shopUIController);
        }

        // ── Wire ShopManager gold text ────────────────────────
        var shopManager = FindOrWarn<ShopManager>("ShopManager");
        if (shopManager != null)
        {
            shopManager.playerGoldText = goldLabel.GetComponent<Text>();
            EditorUtility.SetDirty(shopManager);
        }

        // ── Wire ShopItem components ──────────────────────────
        var shopItems = Object.FindObjectsOfType<ShopItem>();
        foreach (var item in shopItems)
        {
            if (item.itemName == "Hat")
            {
                item.price = 50;
                item.outfitColor = new Color(0.55f, 0.30f, 0.15f);
                hatBtn.GetComponent<Button>().onClick.AddListener(item.BuyItem);
                EditorUtility.SetDirty(item);
            }
            else if (item.itemName == "Shirt")
            {
                item.price = 30;
                item.outfitColor = new Color(0.20f, 0.55f, 0.75f);
                shirtBtn.GetComponent<Button>().onClick.AddListener(item.BuyItem);
                EditorUtility.SetDirty(item);
            }
        }

        // ── Wire PlayerInventory equip/unequip buttons ────────
        var playerInventory = FindOrWarn<PlayerInventory>("PlayerInventory");
        if (playerInventory != null)
        {
            playerInventory.equipButton   = equipBtn.GetComponent<Button>();
            playerInventory.unequipButton = unequipBtn.GetComponent<Button>();
            EditorUtility.SetDirty(playerInventory);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[ShopUIBuilder] Shop UI built and wired successfully.");
    }

    [MenuItem("RPG Scene Builder/Clear Shop UI")]
    public static void ClearShopUI()
    {
        var existing = GameObject.Find("ShopCanvas");
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
            Debug.Log("[ShopUIBuilder] Shop UI cleared.");
        }
        else
        {
            Debug.Log("[ShopUIBuilder] No ShopCanvas found to clear.");
        }
    }

    // =============================================================
    //  UI factory helpers
    // =============================================================

    static GameObject MakePanel(GameObject parent, string name,
                                Vector2 anchoredPos, Vector2 size)
    {
        var go  = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent.transform, false);
        var rt  = go.GetComponent<RectTransform>();
        rt.sizeDelta        = size;
        rt.anchoredPosition = anchoredPos;
        return go;
    }

    static GameObject MakeText(GameObject parent, string name, string content,
                               Vector2 anchoredPos, Vector2 size,
                               int fontSize, FontStyle style)
    {
        var go   = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent.transform, false);
        var rt   = go.GetComponent<RectTransform>();
        rt.sizeDelta        = size;
        rt.anchoredPosition = anchoredPos;
        var txt  = go.GetComponent<Text>();
        txt.text      = content;
        txt.fontSize  = fontSize;
        txt.fontStyle = style;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color     = Color.white;
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return go;
    }

    static GameObject MakeButton(GameObject parent, string name, string label,
                                 Vector2 anchoredPos, Vector2 size)
    {
        var go  = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent.transform, false);
        var rt  = go.GetComponent<RectTransform>();
        rt.sizeDelta        = size;
        rt.anchoredPosition = anchoredPos;
        SetColor(go, new Color(0.25f, 0.25f, 0.25f, 1f));

        // Label child
        MakeText(go, "Label", label,
            Vector2.zero, size, 15, FontStyle.Bold);

        return go;
    }

    // Item button: colored swatch on left, name + price text on right
    static GameObject MakeItemButton(GameObject parent, string name,
                                     string itemName, string priceText,
                                     Vector2 anchoredPos, Color swatchColor)
    {
        var go  = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent.transform, false);
        var rt  = go.GetComponent<RectTransform>();
        rt.sizeDelta        = new Vector2(340, 60);
        rt.anchoredPosition = anchoredPos;
        SetColor(go, new Color(0.22f, 0.20f, 0.18f, 1f));

        // Color swatch
        var swatch = new GameObject("Swatch", typeof(RectTransform), typeof(Image));
        swatch.transform.SetParent(go.transform, false);
        var sr = swatch.GetComponent<RectTransform>();
        sr.sizeDelta        = new Vector2(40, 40);
        sr.anchoredPosition = new Vector2(-130, 0);
        SetColor(swatch, swatchColor);

        // Item name
        MakeText(go, "ItemName", itemName,
            new Vector2(20, 10), new Vector2(200, 25), 16, FontStyle.Bold);

        // Price
        var priceGO = MakeText(go, "PriceText", priceText,
            new Vector2(20, -12), new Vector2(200, 22), 13, FontStyle.Normal);
        priceGO.GetComponent<Text>().color = new Color(0.9f, 0.8f, 0.3f);

        return go;
    }

    static void MakeDivider(GameObject parent, Vector2 anchoredPos)
    {
        var go = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta        = new Vector2(360, 2);
        rt.anchoredPosition = anchoredPos;
        SetColor(go, new Color(1f, 1f, 1f, 0.12f));
    }

    static void SetColor(GameObject go, Color color)
    {
        var img = go.GetComponent<Image>();
        if (img != null) img.color = color;
    }

    static void SetButtonColor(GameObject go, Color color)
    {
        var img = go.GetComponent<Image>();
        if (img != null) img.color = color;
        var btn = go.GetComponent<Button>();
        if (btn != null)
        {
            var colors     = btn.colors;
            colors.normalColor      = color;
            colors.highlightedColor = color * 1.2f;
            colors.pressedColor     = color * 0.8f;
            btn.colors = colors;
        }
    }

    static T FindOrWarn<T>(string label) where T : Object
    {
        var result = Object.FindObjectOfType<T>();
        if (result == null)
            Debug.LogWarning($"[ShopUIBuilder] Could not find {label} in scene. " +
                             $"Wire it manually in the Inspector.");
        return result;
    }
}
