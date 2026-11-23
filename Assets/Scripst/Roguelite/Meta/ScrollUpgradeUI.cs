using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ScrollStatType
{
    Brutality, // Đỏ: +25% Melee & Crit Damage
    Tactics,   // Tím: -18% Skill/Dash Cooldown, +25% Skill Damage
    Survival   // Xanh lá: +2 Max HP, +1 Flask Charge & Hồi máu
}

/// <summary>
/// Giao diện chọn Cuộn Sức Mạnh (Power Scroll) chuẩn phong cách thẻ Dead Cells gọn gàng:
/// - 3 dải ngang rõ ràng (Header + Badge | Stat Strip | Mô tả căn trái gọn trong khung + RectMask2D).
/// - Hủy HitStop & luôn khôi phục Time.timeScale = 1f khi đóng (không bao giờ bị lag/slow-mo sau khi chọn!).
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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitStaticOnPlay()
    {
        instance = null;
        BrutalityLevel = 0;
        TacticsLevel = 0;
        SurvivalLevel = 0;
        OnStatsChanged = null;
    }

    private GameObject panelRoot;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI brutalityStatTxt;
    private TextMeshProUGUI tacticsStatTxt;
    private TextMeshProUGUI survivalStatTxt;

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
        Get().Open(title);
    }

    private void BuildUI()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        panelRoot = new GameObject("PanelRoot");
        panelRoot.transform.SetParent(transform, false);
        var bg = panelRoot.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.04f, 0.09f, 0.88f);
        StretchFill(bg.rectTransform);

        titleText = CreateFittedText(panelRoot.transform, "POWER SCROLL FOUND!", 22, 34, new Color(1f, 0.88f, 0.3f), TextAlignmentOptions.Center);
        SetCenterBox(titleText.rectTransform, new Vector2(0, 210), new Vector2(920, 48));

        var sub = CreateFittedText(panelRoot.transform, "Chọn 1 nhánh sức mạnh  —  Click chuột hoặc nhấn phím [1]  [2]  [3]", 13, 17, new Color(0.78f, 0.82f, 0.92f), TextAlignmentOptions.Center);
        SetCenterBox(sub.rectTransform, new Vector2(0, 170), new Vector2(920, 28));

        // 3 Thẻ phong cách Dead Cells (Ảnh 2): 3 dải màu gọn gàng, căn trái chuẩn chỉnh, có RectMask2D chống tràn chữ!
        brutalityStatTxt = CreateDeadCellsCard(panelRoot.transform, new Vector2(-315, -18),
            new Color(0.88f, 0.22f, 0.28f), "1", "BRUTALITY",
            "Cấp 0 -> 1\n+25% Sát thương Cận chiến",
            "• Tăng mạnh sát thương đánh thường & đòn Chí mạng.\n• Lý tưởng cho lối chơi áp sát chém Combo 3 đòn & Backstab.",
            () => Pick(ScrollStatType.Brutality));

        tacticsStatTxt = CreateDeadCellsCard(panelRoot.transform, new Vector2(0, -18),
            new Color(0.62f, 0.32f, 0.95f), "2", "TACTICS",
            "Cấp 0 -> 1\n-18% Hồi chiêu Skill & Dash",
            "• Giảm thời gian hồi chiêu Bomb [Q], Wave [E], Parry & Dash.\n• Tăng +25% sát thương toàn bộ kỹ năng chủ động.",
            () => Pick(ScrollStatType.Tactics));

        survivalStatTxt = CreateDeadCellsCard(panelRoot.transform, new Vector2(315, -18),
            new Color(0.22f, 0.78f, 0.45f), "3", "SURVIVAL",
            "Cấp 0 -> 1\n+2 Máu Tối Đa & +1 Bình Máu",
            "• Tăng +2 Max HP và nạp thêm +1 lượt Bình Máu [H].\n• Hồi phục lập tức 100% máu hiện tại ngay khi chọn.",
            () => Pick(ScrollStatType.Survival));

        panelRoot.SetActive(false);
    }

    private TextMeshProUGUI CreateDeadCellsCard(Transform parent, Vector2 pos, Color accent, string keyTag, string title, string statSummary, string bodyDesc, Action onClick)
    {
        const float cardW = 292f;
        const float cardH = 290f;

        var card = new GameObject("ScrollCard_" + title);
        card.transform.SetParent(parent, false);
        var cardImg = card.AddComponent<Image>();
        cardImg.color = new Color(0.05f, 0.07f, 0.13f, 0.97f);
        SetCenterBox(cardImg.rectTransform, pos, new Vector2(cardW, cardH));

        // Viền mỏng sắc nét + Mask đảm bảo 100% chữ không bao giờ tràn ra ngoài thẻ!
        var outline = card.AddComponent<Outline>();
        outline.effectColor = accent;
        outline.effectDistance = new Vector2(2f, -2f);
        card.AddComponent<RectMask2D>();

        var btn = card.AddComponent<Button>();
        var cb = btn.colors;
        cb.highlightedColor = new Color(1.18f, 1.18f, 1.24f, 1f);
        cb.pressedColor = new Color(0.88f, 0.88f, 0.92f, 1f);
        btn.colors = cb;
        btn.onClick.AddListener(() => onClick());

        // --- DẢI 1: HEADER (54px trên cùng) ---
        var headerBand = CreateBand(card.transform, "HeaderBand", 0f, 54f, new Color(0.08f, 0.1f, 0.18f, 1f));
        // Ô vuông biểu tượng số phím góc trái trên (giống khung icon ảnh 2)
        var iconBox = new GameObject("KeyBadge");
        iconBox.transform.SetParent(headerBand.transform, false);
        var iconImg = iconBox.AddComponent<Image>();
        iconImg.color = new Color(accent.r * 0.35f, accent.g * 0.35f, accent.b * 0.35f, 1f);
        var ir = iconImg.rectTransform;
        ir.anchorMin = new Vector2(0, 0.5f);
        ir.anchorMax = new Vector2(0, 0.5f);
        ir.pivot = new Vector2(0, 0.5f);
        ir.anchoredPosition = new Vector2(10, 0);
        ir.sizeDelta = new Vector2(36, 36);
        var iOut = iconBox.AddComponent<Outline>();
        iOut.effectColor = accent;
        iOut.effectDistance = new Vector2(1.5f, -1.5f);

        var keyTxt = CreateFittedText(iconBox.transform, keyTag, 16, 22, Color.white, TextAlignmentOptions.Center);
        StretchFill(keyTxt.rectTransform, 2, 2, 2, 2);

        var titleTxt = CreateFittedText(headerBand.transform, title, 16, 22, Color.Lerp(accent, Color.white, 0.35f), TextAlignmentOptions.Left);
        StretchFill(titleTxt.rectTransform, 58, 12, 6, 6);

        // --- DẢI 2: STAT STRIP (56px màu nhấn giống dòng DPS/damage của ảnh 2) ---
        var statBand = CreateBand(card.transform, "StatBand", 54f, 58f, new Color(accent.r * 0.32f, accent.g * 0.28f, accent.b * 0.38f, 0.95f));
        var statTxt = CreateFittedText(statBand.transform, statSummary, 12, 16, Color.white, TextAlignmentOptions.Left);
        StretchFill(statTxt.rectTransform, 14, 14, 6, 6);

        // --- DẢI 3: MÔ TẢ CHI TIẾT (nền tối sâu, căn trái, có dòng kẻ mảnh ngăn cách) ---
        var bodyBand = CreateBand(card.transform, "BodyBand", 112f, 134f, new Color(0.04f, 0.06f, 0.11f, 1f));
        var descTxt = CreateFittedText(bodyBand.transform, bodyDesc, 11, 14.5f, new Color(0.86f, 0.9f, 0.96f), TextAlignmentOptions.TopLeft);
        StretchFill(descTxt.rectTransform, 14, 14, 10, 8);

        // Đường kẻ ngang mảnh ở đáy phần mô tả (giống ảnh 2)
        var divider = CreateBand(card.transform, "Divider", 246f, 2f, new Color(accent.r, accent.g, accent.b, 0.55f));

        // --- DẢI 4: FOOTER NÚT CHỌN (42px đáy) ---
        var footerBand = CreateBand(card.transform, "FooterBand", 248f, 42f, new Color(accent.r * 0.22f, accent.g * 0.22f, accent.b * 0.28f, 1f));
        var footTxt = CreateFittedText(footerBand.transform, $"NHẤN [{keyTag}] HOẶC CLICK ĐỂ CHỌN", 11, 13.5f, Color.Lerp(accent, Color.white, 0.45f), TextAlignmentOptions.Center);
        StretchFill(footTxt.rectTransform, 10, 10, 4, 4);

        return statTxt;
    }

    private static GameObject CreateBand(Transform parent, string name, float topOffset, float height, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var r = img.rectTransform;
        r.anchorMin = new Vector2(0, 1);
        r.anchorMax = new Vector2(1, 1);
        r.pivot = new Vector2(0.5f, 1);
        r.anchoredPosition = new Vector2(0, -topOffset);
        r.sizeDelta = new Vector2(0, height);
        return go;
    }

    private static TextMeshProUGUI CreateFittedText(Transform parent, string content, float minSize, float maxSize, Color color, TextAlignmentOptions align)
    {
        var go = new GameObject("Txt");
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = color;
        tmp.alignment = align;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = minSize;
        tmp.fontSizeMax = maxSize;
        tmp.lineSpacing = 4f;
        return tmp;
    }

    private static void StretchFill(RectTransform r, float left = 0, float right = 0, float top = 0, float bottom = 0)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(left, bottom);
        r.offsetMax = new Vector2(-right, -top);
    }

    private static void SetCenterBox(RectTransform r, Vector2 pos, Vector2 size)
    {
        r.anchorMin = new Vector2(0.5f, 0.5f);
        r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = size;
    }

    private void Open(string title)
    {
        if (panelRoot.activeSelf) return;

        // QUAN TRỌNG: Hủy mọi HitStop (0.05f) đang chạy trước khi pause để không bị kẹt slow-mo sau khi chọn!
        HitStop.Cancel();
        Time.timeScale = 0f;

        if (titleText != null) titleText.text = title;
        if (brutalityStatTxt != null)
            brutalityStatTxt.text = $"Cấp {BrutalityLevel} -> {BrutalityLevel + 1}\n+25% Sát thương Cận chiến (Tổng: x{BrutalityDamageMultiplier + 0.25f:0.00})";
        if (tacticsStatTxt != null)
            tacticsStatTxt.text = $"Cấp {TacticsLevel} -> {TacticsLevel + 1}\n-18% Hồi chiêu Skill & +25% Sát thương Skill";
        if (survivalStatTxt != null)
            survivalStatTxt.text = $"Cấp {SurvivalLevel} -> {SurvivalLevel + 1}\n+2 Máu Tối Đa, +1 Bình Máu & Hồi 100% HP";

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

        // Luôn khôi phục tốc độ game chuẩn 1.0f ngay lập tức — triệt tiêu hoàn toàn lỗi lag/chậm nhiều giây!
        HitStop.Cancel();
        Time.timeScale = 1f;

        if (player != null)
        {
            DamagePopup.SpawnText(player.transform.position + Vector3.up * 1.1f, label, col, 7.2f);
            CombatVFX.SpawnShockwave(player.transform.position, col, 0.3f, 2.8f, 0.35f);
        }
        if (AudioManager.Instance != null) AudioManager.Instance.SFX_Win();

        KillStreakManager.Get().RegisterScrollCollected();
        OnStatsChanged?.Invoke();
    }
}
