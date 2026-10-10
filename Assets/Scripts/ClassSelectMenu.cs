// ============================================================
//  ClassSelectMenu.cs  -  Assets/Scripts/
//  Sits on the GameManager prefab.
//
//  Simple "Choose your class" screen: one card per class with
//  its name, description and stats. The game is paused until a
//  class is picked. Builds its own UI in code, so there is
//  nothing to wire - just fill the Classes list.
//
//  Testing: press the reopen key (default C) to switch class
//  mid-game. Set it to None before release.
//
//  Later this becomes a proper menu scene; PlayerClass.SetClass
//  is the only thing it needs, so the swap is cheap.
// ============================================================
using UnityEngine;
using UnityEngine.UI;

public class ClassSelectMenu : MonoBehaviour
{
    [Tooltip("Classes offered, left to right.")]
    public CharacterClassDefinition[] classes;

    [Tooltip("Show the picker when the scene starts. The game is paused until a class is chosen.")]
    public bool showOnStart = true;

    [Tooltip("Key that opens/closes the picker while playing (testing). None = disabled.")]
    public KeyCode reopenKey = KeyCode.C;

    private GameObject panel;
    private bool isOpen;
    private float timeScaleBefore = 1f;

    void Start()
    {
        if (classes == null || classes.Length == 0)
        {
            Debug.LogWarning("ClassSelectMenu: no classes assigned - picker disabled.", this);
            enabled = false;
            return;
        }

        BuildUI();
        if (showOnStart) Open();
        else panel.SetActive(false);
    }

    void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (reopenKey == KeyCode.None || !Input.GetKeyDown(reopenKey)) return;
        if (isOpen) Close();
        else Open();
#endif
    }

    public void Open()
    {
        timeScaleBefore = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        panel.SetActive(true);
        isOpen = true;
    }

    private void Close()
    {
        panel.SetActive(false);
        Time.timeScale = timeScaleBefore;
        isOpen = false;
    }

    private void Choose(CharacterClassDefinition chosen)
    {
        PlayerRefs player = GameManager.Instance != null ? GameManager.Instance.Player : null;
        if (player != null && player.Class != null)
            player.Class.SetClass(chosen);
        else
            Debug.LogWarning("ClassSelectMenu: no Player with PlayerClass registered.", this);

        Close();
    }

    // ---------------------------------------------------------
    //  UI built in code
    // ---------------------------------------------------------

    private void BuildUI()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var canvasGO = new GameObject("ClassSelectCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);

        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;   // above HUD, shop and health bar

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        // Full-screen dim background - also blocks clicks on the game behind it.
        panel = MakeImage(canvasGO.transform, "ClassSelectPanel", new Color(0f, 0f, 0f, 0.8f));
        var panelRt = (RectTransform)panel.transform;
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        MakeText(panel.transform, "Title", "Choose your class", font, 52, FontStyle.Bold,
            new Color(1f, 0.85f, 0.35f), new Vector2(0f, 300f), new Vector2(900f, 80f), TextAnchor.MiddleCenter);

        const float cardWidth = 300f, cardHeight = 400f, gap = 40f;
        float startX = -(classes.Length - 1) * (cardWidth + gap) / 2f;

        for (int i = 0; i < classes.Length; i++)
        {
            CharacterClassDefinition c = classes[i];
            if (c == null) continue;

            GameObject card = MakeImage(panel.transform, "Card_" + c.className, new Color(0.13f, 0.12f, 0.11f, 0.97f));
            var cardRt = (RectTransform)card.transform;
            cardRt.anchoredPosition = new Vector2(startX + i * (cardWidth + gap), -40f);
            cardRt.sizeDelta        = new Vector2(cardWidth, cardHeight);

            var button = card.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(1f, 0.9f, 0.6f);
            colors.pressedColor     = new Color(0.8f, 0.7f, 0.4f);
            button.colors = colors;
            button.onClick.AddListener(() => Choose(c));

            MakeText(card.transform, "Name", c.className, font, 36, FontStyle.Bold,
                new Color(1f, 0.85f, 0.35f), new Vector2(0f, 150f), new Vector2(cardWidth - 20f, 50f), TextAnchor.MiddleCenter);

            MakeText(card.transform, "Description", c.description, font, 20, FontStyle.Italic,
                new Color(0.85f, 0.85f, 0.85f), new Vector2(0f, 55f), new Vector2(cardWidth - 40f, 110f), TextAnchor.UpperCenter);

            string stats =
                $"Health        {c.maxHealth}\n" +
                $"Damage        {c.baseAttackDamage}\n" +
                $"Attack speed  {c.attackSpeed:0.##}\n" +
                $"Move speed    {c.moveSpeed:0.#}";
            MakeText(card.transform, "Stats", stats, font, 22, FontStyle.Normal,
                Color.white, new Vector2(0f, -100f), new Vector2(cardWidth - 60f, 140f), TextAnchor.MiddleLeft);
        }
    }

    private static GameObject MakeImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    private static void MakeText(Transform parent, string name, string content, Font font, int size,
        FontStyle style, Color color, Vector2 pos, Vector2 box, TextAnchor align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchoredPosition = pos;
        rt.sizeDelta        = box;

        var t = go.GetComponent<Text>();
        t.font               = font;
        t.text               = content;
        t.fontSize           = size;
        t.fontStyle          = style;
        t.color              = color;
        t.alignment          = align;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow   = VerticalWrapMode.Overflow;
        t.raycastTarget      = false;   // clicks go to the card's Button
    }
}
