using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Giao diện chọn Kỹ Năng Bị Động Nguyền Rủa (Cursed Passive Skill Picker) khi mở Rương Nguyền Rủa:
/// - Hiển thị 3 thẻ ngẫu nhiên kèm icon pixel-art (agent-sprite-forge), cấp độ và mô tả rõ ràng.
/// - Chọn bằng Click chuột hoặc phím [1] [2] [3], tự khôi phục Time.timeScale = 1f an toàn.
/// - Hiển thị thêm dải icon kỹ năng đang sở hữu ở góc trên phải màn hình.
/// </summary>
public class CursedSkillUI : MonoBehaviour
{
    private static CursedSkillUI instance;

    public static bool IsOpen => instance != null && instance.panelRoot != null && instance.panelRoot.activeSelf;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    private GameObject panelRoot;
    private TextMeshProUGUI titleText;
    private readonly List<CursedSkillId> currentChoices = new List<CursedSkillId>(3);

    private class CardView
    {
        public GameObject Root;
        public Image BorderImg;
        public Image HeaderBg;
        public Image IconImg;
        public TextMeshProUGUI TitleTxt;
        public TextMeshProUGUI LevelTxt;
        public TextMeshProUGUI DescTxt;
        public Button Btn;
    }

    private readonly CardView[] cards = new CardView[3];
    private Transform hudStripRoot;

    public static CursedSkillUI Get()
    {
        if (instance == null)
        {
            var go = new GameObject("CursedSkillUI");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<CursedSkillUI>();
            instance.BuildUI();
            CursedPassiveSystem.OnSkillsChanged += instance.RefreshActiveHud;
        }
        return instance;
    }

    public static void ShowSkillChoice(string title = "CURSED CHEST — CHỌN KỸ NĂNG BỊ ĐỘNG!")
    {
        Get().Open(title);
    }

    private void OnDestroy()
    {
        CursedPassiveSystem.OnSkillsChanged -= RefreshActiveHud;
    }

    private void BuildUI()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 510;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        // Dải icon kỹ năng bị động đang kích hoạt (góc trên bên phải)
        var hudObj = new GameObject("ActiveCursedSkillsHUD");
        hudObj.transform.SetParent(transform, false);
        var hudRt = hudObj.AddComponent<RectTransform>();
        hudRt.anchorMin = new Vector2(1f, 1f);
        hudRt.anchorMax = new Vector2(1f, 1f);
        hudRt.pivot = new Vector2(1f, 1f);
        hudRt.anchoredPosition = new Vector2(-18f, -16f);
        hudRt.sizeDelta = new Vector2(340f, 64f);
        var hlg = hudObj.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleRight;
        hlg.spacing = 10f;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hudStripRoot = hudObj.transform;

        // Màn hình chọn 3 thẻ kỹ năng
        panelRoot = new GameObject("PanelRoot");
        panelRoot.transform.SetParent(transform, false);
        var bg = panelRoot.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.02f, 0.09f, 0.90f);
        StretchFill(bg.rectTransform);

        titleText = CreateFittedText(panelRoot.transform, "CURSED CHEST — CHỌN KỸ NĂNG BỊ ĐỘNG!", 22, 32, new Color(1f, 0.34f, 0.62f), TextAlignmentOptions.Center);
        SetCenterBox(titleText.rectTransform, new Vector2(0, 222), new Vector2(980, 46));

        var sub = CreateFittedText(panelRoot.transform, "Nhận sức mạnh bị động vĩnh viễn trong lượt chơi  —  Click chuột hoặc nhấn phím [1]  [2]  [3]", 13, 17, new Color(0.86f, 0.78f, 0.96f), TextAlignmentOptions.Center);
        SetCenterBox(sub.rectTransform, new Vector2(0, 182), new Vector2(980, 28));

        float[] xs = { -320f, 0f, 320f };
        for (int i = 0; i < 3; i++)
        {
            int idx = i;
            cards[i] = CreateSkillCard(panelRoot.transform, new Vector2(xs[i], -22f), (i + 1).ToString(), () => PickIndex(idx));
        }

        panelRoot.SetActive(false);
        RefreshActiveHud();
    }

    private CardView CreateSkillCard(Transform parent, Vector2 pos, string keyLabel, UnityEngine.Events.UnityAction onClick)
    {
        var view = new CardView();

        var root = new GameObject("SkillCard_" + keyLabel);
        root.transform.SetParent(parent, false);
        var rt = root.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(292, 344);
        rt.anchoredPosition = pos;
        view.Root = root;

        view.BorderImg = root.AddComponent<Image>();
        view.BorderImg.color = new Color(0.85f, 0.25f, 0.55f);

        view.Btn = root.AddComponent<Button>();
        view.Btn.targetGraphic = view.BorderImg;
        view.Btn.onClick.AddListener(onClick);

        var inner = new GameObject("Inner");
        inner.transform.SetParent(root.transform, false);
        var innerImg = inner.AddComponent<Image>();
        innerImg.color = new Color(0.08f, 0.06f, 0.13f, 0.98f);
        innerImg.raycastTarget = false;
        var irt = innerImg.rectTransform;
        irt.anchorMin = Vector2.zero;
        irt.anchorMax = Vector2.one;
        irt.offsetMin = new Vector2(4, 4);
        irt.offsetMax = new Vector2(-4, -4);
        inner.AddComponent<RectMask2D>();

        // 1. Header bar
        var header = new GameObject("Header");
        header.transform.SetParent(inner.transform, false);
        view.HeaderBg = header.AddComponent<Image>();
        view.HeaderBg.color = new Color(0.2f, 0.1f, 0.28f, 0.95f);
        view.HeaderBg.raycastTarget = false;
        var hrt = view.HeaderBg.rectTransform;
        hrt.anchorMin = new Vector2(0, 1);
        hrt.anchorMax = new Vector2(1, 1);
        hrt.pivot = new Vector2(0.5f, 1);
        hrt.anchoredPosition = Vector2.zero;
        hrt.sizeDelta = new Vector2(0, 50);

        var badge = new GameObject("KeyBadge");
        badge.transform.SetParent(header.transform, false);
        var badgeImg = badge.AddComponent<Image>();
        badgeImg.color = new Color(0.05f, 0.04f, 0.09f, 0.92f);
        badgeImg.raycastTarget = false;
        var brt = badgeImg.rectTransform;
        brt.anchorMin = new Vector2(0, 0.5f);
        brt.anchorMax = new Vector2(0, 0.5f);
        brt.pivot = new Vector2(0, 0.5f);
        brt.anchoredPosition = new Vector2(10, 0);
        brt.sizeDelta = new Vector2(30, 30);

        var keyTxt = CreateFittedText(badge.transform, keyLabel, 14, 18, Color.white, TextAlignmentOptions.Center);
        StretchFill(keyTxt.rectTransform);

        view.TitleTxt = CreateFittedText(header.transform, "SKILL NAME", 13, 19, Color.white, TextAlignmentOptions.MidlineLeft);
        var trt = view.TitleTxt.rectTransform;
        trt.anchorMin = new Vector2(0, 0);
        trt.anchorMax = new Vector2(1, 1);
        trt.offsetMin = new Vector2(48, 4);
        trt.offsetMax = new Vector2(-10, -4);

        // 2. Pixel-art Icon showcase frame
        var iconFrame = new GameObject("IconFrame");
        iconFrame.transform.SetParent(inner.transform, false);
        var iconFrameImg = iconFrame.AddComponent<Image>();
        iconFrameImg.color = new Color(0.04f, 0.03f, 0.08f, 0.95f);
        iconFrameImg.raycastTarget = false;
        var ifrt = iconFrameImg.rectTransform;
        ifrt.anchorMin = new Vector2(0.5f, 1f);
        ifrt.anchorMax = new Vector2(0.5f, 1f);
        ifrt.pivot = new Vector2(0.5f, 1f);
        ifrt.anchoredPosition = new Vector2(0, -62);
        ifrt.sizeDelta = new Vector2(96, 96);

        var iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(iconFrame.transform, false);
        view.IconImg = iconObj.AddComponent<Image>();
        view.IconImg.preserveAspect = true;
        view.IconImg.raycastTarget = false;
        var icrt = view.IconImg.rectTransform;
        icrt.anchorMin = Vector2.zero;
        icrt.anchorMax = Vector2.one;
        icrt.offsetMin = new Vector2(8, 8);
        icrt.offsetMax = new Vector2(-8, -8);

        // 3. Level strip
        view.LevelTxt = CreateFittedText(inner.transform, "MỚI — CẤP 1", 12, 16, new Color(1f, 0.86f, 0.35f), TextAlignmentOptions.Center);
        var lrt = view.LevelTxt.rectTransform;
        lrt.anchorMin = new Vector2(0, 1);
        lrt.anchorMax = new Vector2(1, 1);
        lrt.pivot = new Vector2(0.5f, 1);
        lrt.anchoredPosition = new Vector2(0, -166);
        lrt.sizeDelta = new Vector2(-20, 26);

        // 4. Description box
        view.DescTxt = CreateFittedText(inner.transform, "", 11, 14, new Color(0.9f, 0.92f, 0.98f), TextAlignmentOptions.TopLeft);
        var drt = view.DescTxt.rectTransform;
        drt.anchorMin = new Vector2(0, 0);
        drt.anchorMax = new Vector2(1, 1);
        drt.offsetMin = new Vector2(14, 14);
        drt.offsetMax = new Vector2(-14, -198);

        return view;
    }

    private void Open(string title)
    {
        if (titleText != null) titleText.text = title;

        // Chọn ngẫu nhiên 3 kỹ năng từ danh mục (ưu tiên các kỹ năng chưa đạt Max Level)
        var pool = new List<CursedSkillId>();
        foreach (var item in CursedPassiveSystem.Catalog)
        {
            if (CursedPassiveSystem.GetLevel(item.Id) < CursedPassiveSystem.MaxLevel)
                pool.Add(item.Id);
        }
        if (pool.Count < 3)
        {
            foreach (var item in CursedPassiveSystem.Catalog)
                if (!pool.Contains(item.Id)) pool.Add(item.Id);
        }

        // Fisher-Yates shuffle
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        currentChoices.Clear();
        for (int i = 0; i < 3 && i < pool.Count; i++)
        {
            var id = pool[i];
            currentChoices.Add(id);
            PopulateCard(cards[i], CursedPassiveSystem.GetInfo(id));
        }

        HitStop.Cancel();
        panelRoot.SetActive(true);
        Time.timeScale = 0f;
    }

    private void PopulateCard(CardView card, CursedSkillInfo info)
    {
        int curLv = CursedPassiveSystem.GetLevel(info.Id);
        int nextLv = Mathf.Min(curLv + 1, CursedPassiveSystem.MaxLevel);

        card.BorderImg.color = info.AccentColor;
        card.HeaderBg.color = new Color(info.AccentColor.r * 0.32f, info.AccentColor.g * 0.32f, info.AccentColor.b * 0.32f, 0.96f);
        card.TitleTxt.text = info.Name;
        card.IconImg.sprite = Resources.Load<Sprite>(info.IconPath);

        if (curLv == 0)
            card.LevelTxt.text = $"<color=#4ADE80><b>★ MỚI — CẤP 1</b></color>  <color=#CBD5E1>({info.Tag})</color>";
        else if (curLv < CursedPassiveSystem.MaxLevel)
            card.LevelTxt.text = $"<color=#FACC15><b>▲ NÂNG CẤP LV.{curLv} → LV.{nextLv}</b></color>";
        else
            card.LevelTxt.text = $"<color=#F472B6><b>★ ĐÃ TỐI ĐA (LV.{curLv})</b></color>";

        card.DescTxt.text = $"{info.Description}\n\n<color=#FDE68A><b>Nâng cấp:</b> {info.UpgradeHint}</color>";
    }

    private void Update()
    {
        if (!IsOpen) return;
        if (Input.GetKeyDown(KeyCode.Alpha1)) PickIndex(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) PickIndex(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) PickIndex(2);
    }

    private void PickIndex(int idx)
    {
        if (!IsOpen || idx < 0 || idx >= currentChoices.Count) return;
        var chosen = currentChoices[idx];

        panelRoot.SetActive(false);
        HitStop.Cancel();
        Time.timeScale = 1f;

        CursedPassiveSystem.GrantOrUpgrade(chosen);
    }

    private void RefreshActiveHud()
    {
        if (hudStripRoot == null) return;
        for (int i = hudStripRoot.childCount - 1; i >= 0; i--)
            Destroy(hudStripRoot.GetChild(i).gameObject);

        foreach (var kv in CursedPassiveSystem.ActiveSkills)
        {
            if (kv.Value <= 0) continue;
            var info = CursedPassiveSystem.GetInfo(kv.Key);

            var slot = new GameObject("ActiveSkill_" + kv.Key);
            slot.transform.SetParent(hudStripRoot, false);
            var srt = slot.AddComponent<RectTransform>();
            srt.sizeDelta = new Vector2(54f, 54f);
            var border = slot.AddComponent<Image>();
            border.color = info.AccentColor;

            var inner = new GameObject("Inner");
            inner.transform.SetParent(slot.transform, false);
            var innerImg = inner.AddComponent<Image>();
            innerImg.color = new Color(0.06f, 0.05f, 0.11f, 0.92f);
            var irt = innerImg.rectTransform;
            irt.anchorMin = Vector2.zero;
            irt.anchorMax = Vector2.one;
            irt.offsetMin = new Vector2(2, 2);
            irt.offsetMax = new Vector2(-2, -2);

            var ico = new GameObject("Icon");
            ico.transform.SetParent(inner.transform, false);
            var icoImg = ico.AddComponent<Image>();
            icoImg.sprite = Resources.Load<Sprite>(info.IconPath);
            icoImg.preserveAspect = true;
            var icrt = icoImg.rectTransform;
            icrt.anchorMin = Vector2.zero;
            icrt.anchorMax = Vector2.one;
            icrt.offsetMin = new Vector2(4, 4);
            icrt.offsetMax = new Vector2(-4, -4);

            var lvTxt = CreateFittedText(slot.transform, $"Lv{kv.Value}", 10, 12, Color.white, TextAlignmentOptions.BottomRight);
            var lrt = lvTxt.rectTransform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(2, 2);
            lrt.offsetMax = new Vector2(-3, -2);
        }
    }

    private static TextMeshProUGUI CreateFittedText(Transform parent, string content, float minSize, float maxSize, Color color, TextAlignmentOptions align)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.color = color;
        tmp.alignment = align;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = minSize;
        tmp.fontSizeMax = maxSize;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void StretchFill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void SetCenterBox(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }
}
