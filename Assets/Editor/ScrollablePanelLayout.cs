// ============================================================
//  ScrollablePanelLayout.cs  -  Assets/Editor/
//
//  Makes the Shop and Inventory panels taller and puts their
//  item lists inside a vertical ScrollRect, so any number of
//  items fits.
//
//  Menu: RPG Scene Builder -> Make Shop + Inventory Scrollable
//  Edits Prefabs/Core/ShopCanvas directly (scene instances pick
//  it up). Safe to run twice - an already-converted panel is
//  skipped. ShopUIBuilder also calls Apply() after a rebuild.
//
//  Nothing at runtime changes: ShopUIRefs still points at the
//  same ItemButtonContainer, it just lives inside the scroll
//  view now and grows to fit its buttons.
// ============================================================
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class ScrollablePanelLayout
{
    private const string PrefabPath  = "Assets/Prefabs/Core/ShopCanvas.prefab";
    private const float  PanelWidth  = 420f;
    private const float  PanelHeight = 720f;   // reference resolution is 1920x1080
    private const float  ListWidth   = 380f;

    [MenuItem("RPG Scene Builder/Make Shop + Inventory Scrollable", false, 22)]
    public static void UpgradePrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (root == null)
        {
            Debug.LogError($"[ScrollablePanelLayout] No prefab at {PrefabPath}.");
            return;
        }

        int changed = 0;
        foreach (string panelName in new[] { "ShopPanel", "InventoryPanel" })
        {
            Transform panel = root.transform.Find(panelName);
            if (panel == null) { Debug.LogWarning($"[ScrollablePanelLayout] {panelName} not found."); continue; }
            if (Apply(panel.gameObject)) changed++;
        }

        if (changed > 0) PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        PrefabUtility.UnloadPrefabContents(root);

        Debug.Log($"[ScrollablePanelLayout] Converted {changed} panel(s) in {PrefabPath}.");
    }

    // Re-lays out one panel: taller, header at the top, Close at the bottom,
    // ItemButtonContainer wrapped in a vertical scroll view in between.
    // Returns false if the panel was already converted or has no container.
    public static bool Apply(GameObject panel)
    {
        Transform container = panel.transform.Find("ItemButtonContainer");
        if (container == null)
        {
            if (panel.transform.Find("ItemScrollView") != null)
                Debug.Log($"[ScrollablePanelLayout] {panel.name} is already scrollable - skipped.");
            else
                Debug.LogWarning($"[ScrollablePanelLayout] {panel.name} has no ItemButtonContainer.");
            return false;
        }

        float top = PanelHeight / 2f;

        // -- Panel size and fixed elements ----------------------
        ((RectTransform)panel.transform).sizeDelta = new Vector2(PanelWidth, PanelHeight);

        SetY(panel, "TitleText",   top - 40f);
        SetY(panel, "GoldText",    top - 80f);    // shop only; inventory has none
        SetY(panel, "CloseButton", -top + 40f);

        // Two dividers: the upper one under the header, the lower one above Close.
        List<RectTransform> dividers = panel.transform.Cast<Transform>()
            .Where(t => t.name == "Divider")
            .Select(t => (RectTransform)t)
            .OrderByDescending(rt => rt.anchoredPosition.y)
            .ToList();
        if (dividers.Count > 0) SetY(dividers[0], top - 102f);
        if (dividers.Count > 1) SetY(dividers[dividers.Count - 1], -top + 82f);

        // -- Scroll view between the two dividers ---------------
        float listTop    = top - 110f;
        float listBottom = -top + 90f;

        var scrollGO = new GameObject("ItemScrollView",
            typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        scrollGO.transform.SetParent(panel.transform, false);
        scrollGO.transform.SetSiblingIndex(container.GetSiblingIndex());

        var scrollRt = (RectTransform)scrollGO.transform;
        scrollRt.anchoredPosition = new Vector2(0f, (listTop + listBottom) / 2f);
        scrollRt.sizeDelta        = new Vector2(ListWidth, listTop - listBottom);

        // Invisible but raycast-able, so dragging in the gaps between buttons scrolls too.
        var bg = scrollGO.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0f);
        bg.raycastTarget = true;

        // -- Content: the existing container, pinned to the top, growing downwards --
        container.SetParent(scrollGO.transform, false);
        var contentRt = (RectTransform)container;
        contentRt.anchorMin        = new Vector2(0f, 1f);
        contentRt.anchorMax        = new Vector2(1f, 1f);
        contentRt.pivot            = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta        = Vector2.zero;   // width = viewport, height from the fitter

        var fitter = container.GetComponent<ContentSizeFitter>();
        if (fitter == null) fitter = container.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = scrollGO.GetComponent<ScrollRect>();
        scroll.content           = contentRt;
        scroll.viewport          = scrollRt;
        scroll.horizontal        = false;
        scroll.vertical          = true;
        scroll.movementType      = ScrollRect.MovementType.Clamped;
        scroll.inertia           = true;
        scroll.scrollSensitivity = 30f;   // mouse wheel speed

        return true;
    }

    private static void SetY(GameObject panel, string childName, float y)
    {
        Transform t = panel.transform.Find(childName);
        if (t != null) SetY((RectTransform)t, y);
    }

    private static void SetY(RectTransform rt, float y)
    {
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
    }
}