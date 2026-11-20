using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ScrollStatType
{
    Brutality, // Đỏ: +25% Melee & Crit Damage
    Tactics,   // Tím: -20% Skill/Dash Cooldown, +25% Skill Damage
    Survival   // Xanh lá: +2 Max HP, +1 Flask Charge & Hồi máu
}

/// <summary>
/// Giao diện chọn Cuộn Sức Mạnh (Power Scroll) kiểu Dead Cells — Chọn 1 trong 3 nhánh:
/// Brutality (Đỏ) / Tactics (Tím) / Survival (Xanh lá).
/// Tự tạo Canvas runtime, hỗ trợ cả bấm chuột lẫn phím 1 / 2 / 3.
/// </summary>
public class ScrollUpgradeUI : MonoBehaviour
{
    private static ScrollUpgradeUI instance;

    public static int BrutalityLevel { get; private set; }
    public static int TacticsLevel { get; private set; }
    public static int SurvivalLevel { get; private set; }

    public static float BrutalityDamageMultiplier => 1f + BrutalityLevel * 0.25f;
    public static float TacticsSkillDamageMultiplier => 1f + TacticsLevel * 0.25f;
    public static float TacticsCooldownMultiplier => Mathf.Pow(0.82f, TacticsLevel);
    public static float SurvivalBonusHealth => SurvivalLevel * 2f;

    public static bool IsOpen => instance != null && instance.panelRoot != null && instance.panelRoot.activeSelf;

    public static event Action OnStatsChanged;

    private GameObject panelRoot;
    private float previousTimeScale = 1f;

    public static void ResetRunStats()
    {
        BrutalityLevel = 0;
        TacticsLevel = 0;
        SurvivalLevel = 0;
        OnStatsChanged?.Invoke();
    }

    public static ScrollUpgradeUI Get()
    {
        if (instance == null)
        {
            var go = new GameObject("ScrollUpgradeUI");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<ScrollUpgradeUI>();
            instance.BuildUI();
        }
        return instance;
    }

    public static void ShowScrollChoice(string title = "POWER SCROLL FOUND!")
    {
        var ui = Get();
        ui.Open(title);
    }

    private TextMeshProUGUI titleText;
    private TextMeshProUGUI brutalityLvlTxt;
    private TextMeshProUGUI tacticsLvlTxt;
    private TextMeshProUGUI survivalLvlTxt;

    private void BuildUI()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        gameObject.AddComponent<GraphicRaycaster>();

        panelRoot = new GameObject("PanelRoot");
        panelRoot.transform.SetParent(transform, false);
        var bg = panelRoot.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.04f, 0.1f, 0.86f);
        var bgRect = bg.rectTransform;
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Tiêu đề
        titleText = CreateText(panelRoot.transform, "POWER SCROLL FOUND!", 38, new Color(1f, 0.86f, 0.28f), new Vector2(0, 225), new Vector2(900, 60));
        CreateText(panelRoot.transform, "Chọn 1 nhánh sức mạnh (Click hoặc nhấn phím [1] [2] [3])", 18, new Color(0.8f, 0.82f, 0.92f), new Vector2(0, 175), new Vector2(900, 35));

        // 3 Thẻ Scroll
        brutalityLvlTxt = CreateScrollCard(panelRoot.transform, new Vector2(-310, -15), new Color(0.75f, 0.14f, 0.18f),
            "[1] BRUTALITY", "+25% Sát thương Cận chiến\n+25% Sát thương Chí mạng", () => Pick(ScrollStatType.Brutality));

        tacticsLvlTxt = CreateScrollCard(panelRoot.transform, new Vector2(0, -15), new Color(0.48f, 0.18f, 0.78f),
            "[2] TACTICS", "-18% Hồi chiêu Skill & Dash\n+25% Sát thương Skill [Q/E]", () => Pick(ScrollStatType.Tactics));

        survivalLvlTxt = CreateScrollCard(panelRoot.transform, new Vector2(310, -15), new Color(0.14f, 0.62f, 0.34f),
            "[3] SURVIVAL", "+2 Máu Tối Đa vĩnh viễn (Run)\n+1 Bình Máu [H] & Hồi 100% HP", () => Pick(ScrollStatType.Survival));

        panelRoot.SetActive(false);
    }

    private TextMeshProUGUI CreateScrollCard(Transform parent, Vector2 anchoredPos, Color themeColor, string header, string desc, Action onClick)
    {
        var card = new GameObject("Card_" + header);
        card.transform.SetParent(parent, false);
        var img = card.AddComponent<Image>();
        img.color = new Color(themeColor.r * 0.35f, themeColor.g * 0.35f, themeColor.b * 0.35f, 0.96f);

        var rect = img.rectTransform;
        rect.sizeDelta = new Vector2(270, 310);
        rect.anchoredPosition = anchoredPos;

        var outline = card.AddComponent<Outline>();
        outline.effectColor = themeColor;
        outline.effectDistance = new Vector2(3, -3);

        var btn = card.AddComponent<Button>();
        var cb = btn.colors;
        cb.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
        cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        btn.colors = cb;
        btn.onClick.AddListener(() => onClick());

        // Thanh banner màu phía trên thẻ
        var bar = new GameObject("Banner");
        bar.transform.SetParent(card.transform, false);
        var barImg = bar.AddComponent<Image>();
        barImg.color = themeColor;
        var barRect = barImg.rectTransform;
        barRect.anchorMin = new Vector2(0, 1);
        barRect.anchorMax = new Vector2(1, 1);
        barRect.pivot = new Vector2(0.5f, 1);
        barRect.sizeDelta = new Vector2(0, 56);
        barRect.anchoredPosition = Vector2.zero;

        CreateText(bar.transform, header, 24, Color.white, new Vector2(0, -28), new Vector2(260, 48));
        var lvlTxt = CreateText(card.transform, "Cấp hiện tại: 0", 17, new Color(1f, 0.92f, 0.55f), new Vector2(0, 50), new Vector2(240, 32));
        CreateText(card.transform, desc, 18, new Color(0.94f, 0.95f, 0.98f), new Vector2(0, -35), new Vector2(240, 130));
        CreateText(card.transform, "CHỌN NGAY", 18, themeColor * 1.4f, new Vector2(0, -120), new Vector2(220, 34));

        return lvlTxt;
    }

    private TextMeshProUGUI CreateText(Transform parent, string content, float size, Color color, Vector2 pos, Vector2 dims)
    {
        var go = new GameObject("Txt");
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = size;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        var rect = tmp.rectTransform;
        rect.sizeDelta = dims;
        rect.anchoredPosition = pos;
        return tmp;
    }

    private void Open(string title)
    {
        if (panelRoot.activeSelf) return;
        previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;

        if (titleText != null) titleText.text = title;
        if (brutalityLvlTxt != null) brutalityLvlTxt.text = $"Cấp: {BrutalityLevel}  ->  {BrutalityLevel + 1}";
        if (tacticsLvlTxt != null) tacticsLvlTxt.text = $"Cấp: {TacticsLevel}  ->  {TacticsLevel + 1}";
        if (survivalLvlTxt != null) survivalLvlTxt.text = $"Cấp: {SurvivalLevel}  ->  {SurvivalLevel + 1}";

        panelRoot.SetActive(true);
    }

    private void Update()
    {
        if (panelRoot == null || !panelRoot.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) Pick(ScrollStatType.Brutality);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) Pick(ScrollStatType.Tactics);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) Pick(ScrollStatType.Survival);
    }

    private void Pick(ScrollStatType stat)
    {
        var player = FindAnyObjectByType<Player>();
        Color col = Color.white;
        string label = "";

        switch (stat)
        {
            case ScrollStatType.Brutality:
                BrutalityLevel++;
                col = new Color(1f, 0.3f, 0.3f);
                label = "BRUTALITY +1!";
                break;
            case ScrollStatType.Tactics:
                TacticsLevel++;
                col = new Color(0.75f, 0.45f, 1f);
                label = "TACTICS +1!";
                break;
            case ScrollStatType.Survival:
                SurvivalLevel++;
                col = new Color(0.3f, 1f, 0.5f);
                label = "SURVIVAL +1!";
                if (player != null)
                {
                    player.IncreaseMaxHealth(2f, fullHeal: true);
                    var flask = player.GetComponent<RallyAndFlask>();
                    if (flask != null) flask.AddFlaskCharge(1, increaseMax: true);
                }
                break;
        }

        panelRoot.SetActive(false);
        Time.timeScale = previousTimeScale;

        if (player != null)
        {
            DamagePopup.SpawnText(player.transform.position + Vector3.up * 1.1f, label, col, 7.5f);
            CombatVFX.SpawnShockwave(player.transform.position, col, 0.3f, 2.8f, 0.35f);
        }
        if (AudioManager.Instance != null) AudioManager.Instance.SFX_Win();

        KillStreakManager.Get().RegisterScrollCollected();
        OnStatsChanged?.Invoke();
    }
}

/// <summary>
/// Vật phẩm Cuộn Sức Mạnh (Power Scroll) đặt trong màn chơi: đứng gần và nhấn [F] để mở bảng chọn 1 trong 3 nhánh.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PowerScrollPickup : MonoBehaviour
{
    private bool playerNear;
    private GameObject promptObj;
    private Vector3 basePos;

    private void Start()
    {
        basePos = transform.position;
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        var sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = CombatVFX.GetCircleSprite();
            sr.color = new Color(1f, 0.85f, 0.25f, 1f);
            sr.sortingOrder = 25;
            transform.localScale = Vector3.one * 0.65f;
        }

        promptObj = new GameObject("ScrollPrompt");
        promptObj.transform.SetParent(transform);
        promptObj.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        var tmp = promptObj.AddComponent<TextMeshPro>();
        tmp.text = "[F] SCROLL";
        tmp.fontSize = 4.5f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.9f, 0.3f);
        tmp.sortingOrder = 110;
        promptObj.SetActive(false);
    }

    private void Update()
    {
        transform.position = basePos + Vector3.up * Mathf.Sin(Time.time * 3f) * 0.14f;

        if (playerNear && Input.GetKeyDown(KeyCode.F) && !ScrollUpgradeUI.IsOpen)
        {
            ScrollUpgradeUI.ShowScrollChoice("POWER SCROLL FOUND!");
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<Player>() != null)
        {
            playerNear = true;
            if (promptObj != null) promptObj.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<Player>() != null)
        {
            playerNear = false;
            if (promptObj != null) promptObj.SetActive(false);
        }
    }
}
