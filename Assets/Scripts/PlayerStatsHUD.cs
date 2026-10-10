// ============================================================
//  PlayerStatsHUD.cs  -  Assets/Scripts/
//  Sits on the PlayerHealthBarCanvas prefab root.
//
//  Shows the class name and gold directly under the health bar.
//  Builds its own Text in code, positioned from the "Slider"
//  child, so there is nothing to wire. Event-driven:
//    GameEvents.OnPlayerClassChanged  -> class line
//    GameEvents.OnGoldChanged         -> gold line
//  Later (Phase B) the level goes here too.
// ============================================================
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatsHUD : MonoBehaviour
{
    [Tooltip("Gap in pixels between the bottom of the health bar and the text.")]
    public float gapBelowBar = 4f;
    public int fontSize = 18;

    private Text label;
    private string className = "";
    private int gold;

    void Awake()  => BuildLabel();

    void OnEnable()
    {
        GameEvents.OnGoldChanged        += HandleGold;
        GameEvents.OnPlayerClassChanged += HandleClass;
    }

    void OnDisable()
    {
        GameEvents.OnGoldChanged        -= HandleGold;
        GameEvents.OnPlayerClassChanged -= HandleClass;
    }

    void Start()
    {
        // Initial pull, in case the events fired before we subscribed.
        PlayerRefs player = GameManager.Instance != null ? GameManager.Instance.Player : null;
        if (player != null)
        {
            if (player.Inventory != null) gold = player.Inventory.gold;
            if (player.Class != null && player.Class.Definition != null)
                className = player.Class.Definition.className;
        }
        Redraw();
    }

    private void HandleGold(int newGold) { gold = newGold; Redraw(); }

    private void HandleClass(CharacterClassDefinition c)
    {
        className = c != null ? c.className : "";
        Redraw();
    }

    private void Redraw()
    {
        if (label == null) return;
        label.text = $"{className}\n<color=#F2D14D>Gold: {gold}</color>";
    }

    private void BuildLabel()
    {
        var go = new GameObject("StatsText", typeof(RectTransform), typeof(Text), typeof(Shadow));
        go.transform.SetParent(transform, false);
        var rt = (RectTransform)go.transform;

        // Top-left anchored, just under the health bar's left edge.
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(300f, fontSize * 2.6f);

        var bar = transform.Find("Slider") as RectTransform;
        if (bar != null)
        {
            float left   = bar.anchoredPosition.x - bar.sizeDelta.x * bar.pivot.x;
            float bottom = bar.anchoredPosition.y - bar.sizeDelta.y * bar.pivot.y;
            rt.anchoredPosition = new Vector2(left, bottom - gapBelowBar);
        }
        else
        {
            rt.anchoredPosition = new Vector2(10f, -30f);
        }

        label = go.GetComponent<Text>();
        label.font               = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize           = fontSize;
        label.fontStyle          = FontStyle.Bold;
        label.color              = Color.white;
        label.alignment          = TextAnchor.UpperLeft;
        label.supportRichText    = true;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow   = VerticalWrapMode.Overflow;
        label.raycastTarget      = false;

        go.GetComponent<Shadow>().effectDistance = new Vector2(1.5f, -1.5f);
    }
}
