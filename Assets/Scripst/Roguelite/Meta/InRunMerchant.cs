using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Thương Nhân Trong Màn (In-Run Merchant NPC) kiểu Dead Cells:
/// Đứng gần nhấn [F] để dùng Vàng (Gold nhặt từ Coin / Quái / Rương) mua vật phẩm chiến đấu ngay giữa màn!
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class InRunMerchant : MonoBehaviour
{
    private bool playerNear;
    private GameObject promptObj;

    private static MerchantShopUI shopUI;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticShop()
    {
        shopUI = null;
    }

    private void Start()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        var sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            var res = Resources.Load<Sprite>("Roguelite/merchant");
            if (res != null)
            {
                sr = gameObject.AddComponent<SpriteRenderer>();
                sr.sprite = res;
                CombatVFX.ApplyVfxLayer(sr, 22, useWorldLayer: true);
            }
            else
            {
                BuildProceduralMerchantSprite();
            }
        }

        promptObj = new GameObject("MerchantPrompt");
        promptObj.transform.SetParent(transform);
        promptObj.transform.localPosition = new Vector3(0f, 2.75f, 0f);

        // Khung nền đen than viền vàng tương phản cao, bề ngang rộng rãi bao trọn toàn bộ chữ
        var bgObj = new GameObject("PromptBg");
        bgObj.transform.SetParent(promptObj.transform);
        bgObj.transform.localPosition = Vector3.zero;
        bgObj.transform.localScale = new Vector3(1.22f, 0.90f, 1f);
        var bgSr = bgObj.AddComponent<SpriteRenderer>();
        bgSr.sprite = GetPromptBadge();
        CombatVFX.ApplyVfxLayer(bgSr, 114);

        var tmp = promptObj.AddComponent<TextMeshPro>();
        tmp.text = "<color=#FFD700><b>[F]</b></color> <color=#FFFFFF><b>MERCHANT SHOP</b></color>\n<size=78%><color=#FFE57F><b>(Dùng Vàng mua nâng cấp)</b></color></size>";
        tmp.fontSize = 3.8f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        CombatVFX.ApplyVfxLayer(tmp.renderer, 115);
        promptObj.SetActive(false);
    }

    private static Sprite cachedPromptBadge;

    private static Sprite GetPromptBadge()
    {
        if (cachedPromptBadge != null) return cachedPromptBadge;
        const int bw = 136, bh = 32; // Mở rộng bề ngang dài ra để tất cả các chữ nằm trọn bên trong khung
        var tex = new Texture2D(bw, bh, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        Color border = new Color(1f, 0.84f, 0.28f, 0.95f);   // Viền vàng kim tinh tế
        Color inner = new Color(0.04f, 0.06f, 0.12f, 0.92f);  // Đen than navy đậm đà, tương phản cực rõ
        for (int y = 0; y < bh; y++)
        {
            for (int x = 0; x < bw; x++)
            {
                bool isBorder = x == 0 || x == bw - 1 || y == 0 || y == bh - 1;
                tex.SetPixel(x, y, isBorder ? border : inner);
            }
        }
        tex.Apply();
        cachedPromptBadge = Sprite.Create(tex, new Rect(0, 0, bw, bh), new Vector2(0.5f, 0.5f), 16f);
        return cachedPromptBadge;
    }

    private void BuildProceduralMerchantSprite()
    {
        // Tạo NPC Thương nhân áo choàng xanh ngọc + đèn lồng vàng pixel art sắc nét
        const int w = 22, h = 28;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        Color clear = new Color(0, 0, 0, 0);
        Color dark = new Color(0.08f, 0.08f, 0.14f, 1f);
        Color cloak = new Color(0.14f, 0.42f, 0.48f, 1f);
        Color cloakHi = new Color(0.24f, 0.65f, 0.72f, 1f);
        Color gold = new Color(1f, 0.84f, 0.22f, 1f);
        Color eye = new Color(1f, 0.92f, 0.45f, 1f);

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, clear);

        // Thân áo choàng
        for (int y = 1; y <= 17; y++)
        {
            int pad = y > 12 ? 4 : 3;
            for (int x = pad; x < w - pad; x++)
            {
                bool edge = x == pad || x == w - pad - 1 || y == 1;
                tex.SetPixel(x, y, edge ? dark : (x < w / 2 ? cloakHi : cloak));
            }
        }
        // Mũ trùm đầu
        for (int y = 17; y <= 25; y++)
        {
            int pad = y > 22 ? 6 : 5;
            for (int x = pad; x < w - pad; x++)
            {
                bool edge = x == pad || x == w - pad - 1 || y == 25;
                bool face = y >= 18 && y <= 22 && x >= 7 && x <= 14;
                if (edge) tex.SetPixel(x, y, dark);
                else if (face) tex.SetPixel(x, y, dark);
                else tex.SetPixel(x, y, cloakHi);
            }
        }
        // Đôi mắt phát sáng vàng dưới mũ trùm
        tex.SetPixel(9, 20, eye);
        tex.SetPixel(12, 20, eye);
        // Túi vàng / Đèn lồng bên hông
        for (int y = 7; y <= 12; y++)
            for (int x = 16; x <= 20; x++)
                tex.SetPixel(x, y, (x == 16 || x == 20 || y == 7 || y == 12) ? dark : gold);

        tex.Apply();

        var sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 16f);
        CombatVFX.ApplyVfxLayer(sr, 22, useWorldLayer: true);
    }

    private void Update()
    {
        var p = FindAnyObjectByType<Player>();
        if (p != null)
        {
            float dist = Vector2.Distance(transform.position, p.transform.position);
            bool isNear = dist <= 2.4f;
            if (isNear != playerNear)
            {
                playerNear = isNear;
                if (promptObj != null) promptObj.SetActive(isNear);
            }
        }

        if (UnityEngine.Random.value < 0.05f)
        {
            CombatVFX.SpawnBurst(transform.position + new Vector3(0.45f, 0.7f, 0f), new Color(1f, 0.85f, 0.25f, 0.7f), 1, 0.3f, 1.0f, 0.12f, 0.35f, -1.2f);
        }

        if (playerNear && Input.GetKeyDown(KeyCode.F) && !ScrollUpgradeUI.IsOpen && !MutationSystem.IsPickerOpen)
        {
            OpenShop();
        }
    }

    private void OpenShop()
    {
        if (shopUI == null)
        {
            var go = new GameObject("InRunMerchantShopUI");
            DontDestroyOnLoad(go);
            shopUI = go.AddComponent<MerchantShopUI>();
            shopUI.BuildUI();
        }
        shopUI.Open();
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

public class MerchantShopUI : MonoBehaviour
{
    private GameObject root;
    private TextMeshProUGUI titleHeaderTxt;
    private TextMeshProUGUI goldHeaderTxt;
    private TextMeshProUGUI feedbackTxt;
    private float savedTimeScale = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic() { }

    public void BuildUI()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 490;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        root = new GameObject("ShopPanel");
        root.transform.SetParent(transform, false);
        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.05f, 0.11f, 0.88f);
        var br = bg.rectTransform;
        br.anchorMin = Vector2.zero;
        br.anchorMax = Vector2.one;
        br.offsetMin = Vector2.zero;
        br.offsetMax = Vector2.zero;

        var box = new GameObject("Window");
        box.transform.SetParent(root.transform, false);
        var bImg = box.AddComponent<Image>();
        bImg.color = new Color(0.08f, 0.09f, 0.18f, 0.96f);
        bImg.rectTransform.sizeDelta = new Vector2(760, 480);
        var outl = box.AddComponent<Outline>();
        outl.effectColor = new Color(0.35f, 0.85f, 0.8f);
        outl.effectDistance = new Vector2(2.5f, -2.5f);

        titleHeaderTxt = MakeText(box.transform, "IN-RUN MERCHANT SHOP", 28, new Color(0.45f, 0.96f, 0.88f), new Vector2(0, 196), new Vector2(700, 44));
        goldHeaderTxt = MakeText(box.transform, "YOUR GOLD: 0 G", 20, new Color(1f, 0.86f, 0.25f), new Vector2(0, 155), new Vector2(700, 32));

        CreateShopRow(box.transform, new Vector2(0, 95), "[1] REFILL HEALTH FLASK", "Nạp thêm +1 lượt uống Bình Máu [H]", 20, BuyFlask);
        CreateShopRow(box.transform, new Vector2(0, 32), "[2] FULL HEAL FEAST", "Hồi 100% Máu ngay lập tức", 25, BuyFullHeal);
        CreateShopRow(box.transform, new Vector2(0, -31), "[3] ELEMENTAL BLADE COATING", "Tẩm Lửa/Độc lên kiếm (45% gây Burn/Poison mỗi nhát)", 30, BuyCoating);
        CreateShopRow(box.transform, new Vector2(0, -94), "[4] MUTATION RELIC", "Mở bảng chọn 1 trong 3 Bùa Đột Biến", 35, BuyMutation);
        CreateShopRow(box.transform, new Vector2(0, -157), "[5] POWER SCROLL", "Mở bảng chọn Brutality / Tactics / Survival", 40, BuyScroll);

        feedbackTxt = MakeText(box.transform, "Nhấn [1]–[5] hoặc Click để mua  |  [ESC] hoặc [F] để đóng", 15, new Color(0.8f, 0.84f, 0.94f), new Vector2(0, -210), new Vector2(700, 28));

        root.SetActive(false);
    }

    private void CreateShopRow(Transform parent, Vector2 pos, string title, string desc, int cost, Action onBuy)
    {
        var row = new GameObject("Row_" + title);
        row.transform.SetParent(parent, false);
        var img = row.AddComponent<Image>();
        img.color = new Color(0.14f, 0.15f, 0.26f, 0.95f);
        img.rectTransform.sizeDelta = new Vector2(690, 54);
        img.rectTransform.anchoredPosition = pos;

        var outl = row.AddComponent<Outline>();
        outl.effectColor = new Color(0.32f, 0.36f, 0.56f);
        outl.effectDistance = new Vector2(1.5f, -1.5f);

        var btn = row.AddComponent<Button>();
        btn.onClick.AddListener(() => onBuy());

        MakeText(row.transform, title, 16, Color.white, new Vector2(-135, 11), new Vector2(400, 24), TextAlignmentOptions.Left);
        MakeText(row.transform, desc, 13, new Color(0.76f, 0.82f, 0.92f), new Vector2(-80, -11), new Vector2(510, 22), TextAlignmentOptions.Left);
        MakeText(row.transform, $"{cost} G", 20, new Color(1f, 0.86f, 0.25f), new Vector2(275, 0), new Vector2(110, 36), TextAlignmentOptions.Right);
    }

    private TextMeshProUGUI MakeText(Transform parent, string txt, float size, Color color, Vector2 pos, Vector2 dims, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var go = new GameObject("Txt");
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = txt;
        tmp.fontSize = size;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = color;
        tmp.alignment = align;
        tmp.rectTransform.sizeDelta = dims;
        tmp.rectTransform.anchoredPosition = pos;
        return tmp;
    }

    public void Open()
    {
        if (root.activeSelf) return;
        HitStop.Cancel();
        savedTimeScale = 1f;
        Time.timeScale = 0f;
        if (titleHeaderTxt != null)
        {
            string mapName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToUpperInvariant();
            titleHeaderTxt.text = $"MERCHANT SHOP  —  {mapName}";
        }
        RefreshGold();
        if (feedbackTxt != null) feedbackTxt.text = "Nhấn [1]–[5] hoặc Click để mua  |  [ESC] để đóng";
        root.SetActive(true);
    }

    public void Close()
    {
        if (!root.activeSelf) return;
        root.SetActive(false);
        HitStop.Cancel();
        Time.timeScale = 1f;
    }

    private void RefreshGold()
    {
        if (goldHeaderTxt != null) goldHeaderTxt.text = $"YOUR GOLD: {RunManager.Get().Gold} G";
    }

    private void Update()
    {
        if (root == null || !root.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
        if (Input.GetKeyDown(KeyCode.Alpha1)) BuyFlask();
        else if (Input.GetKeyDown(KeyCode.Alpha2)) BuyFullHeal();
        else if (Input.GetKeyDown(KeyCode.Alpha3)) BuyCoating();
        else if (Input.GetKeyDown(KeyCode.Alpha4)) BuyMutation();
        else if (Input.GetKeyDown(KeyCode.Alpha5)) BuyScroll();
    }

    private bool TryPurchase(int cost, string successMsg)
    {
        if (!RunManager.Get().TrySpendGold(cost))
        {
            if (feedbackTxt != null) feedbackTxt.text = "<color=#FF5566>Không đủ Vàng! Hãy nhặt Coin hoặc hạ thêm quái.</color>";
            return false;
        }
        RefreshGold();
        if (feedbackTxt != null) feedbackTxt.text = $"<color=#4CE880>{successMsg}</color>";
        if (AudioManager.Instance != null) AudioManager.Instance.SFX_TakeCoin();
        return true;
    }

    private void BuyFlask()
    {
        var p = FindAnyObjectByType<Player>();
        var flask = p != null ? p.GetComponent<RallyAndFlask>() : null;
        if (flask == null) return;
        if (flask.FlaskCharges >= flask.MaxFlaskCharges)
        {
            if (feedbackTxt != null) feedbackTxt.text = "<color=#FFD940>Bình máu đã đầy rồi!</color>";
            return;
        }
        if (TryPurchase(20, "Đã nạp thêm +1 Bình Máu [H]!"))
        {
            flask.AddFlaskCharge(1, false);
        }
    }

    private void BuyFullHeal()
    {
        var p = FindAnyObjectByType<Player>();
        if (p == null) return;
        if (TryPurchase(25, "Đã hồi phục 100% HP!"))
        {
            p.Heal(p.MaxHealth);
        }
    }

    private void BuyCoating()
    {
        if (MutationSystem.HasElementalCoating)
        {
            if (feedbackTxt != null) feedbackTxt.text = "<color=#FFD940>Kiếm của bạn đã được Tẩm Lửa/Độc rồi!</color>";
            return;
        }
        if (TryPurchase(30, "Đã tẩm Lửa/Độc lên vũ khí!"))
        {
            MutationSystem.EnableElementalCoating();
        }
    }

    private void BuyMutation()
    {
        if (TryPurchase(35, "Đang mở Bùa Đột Biến..."))
        {
            Close();
            MutationSystem.ShowMutationPicker("MERCHANT MUTATION RELIC!");
        }
    }

    private void BuyScroll()
    {
        if (TryPurchase(40, "Đang mở Cuộn Sức Mạnh..."))
        {
            Close();
            ScrollUpgradeUI.ShowScrollChoice("MERCHANT POWER SCROLL!");
        }
    }
}
