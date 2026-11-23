using TMPro;
using UnityEngine;

/// <summary>
/// Rương Kho Báu Vàng (Treasure Chest) rải ngẫu nhiên trên các màn (map 1, map 2, map 3):
/// Đứng gần nhấn [F] để mở — rơi ra Vàng (Gold), Cells và phần thưởng ngẫu nhiên (Hồi máu / Nạp Bình Máu / Cơ hội nhận Bùa Đột Biến!).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class TreasureChest : MonoBehaviour
{
    [SerializeField] private int minGold = 15;
    [SerializeField] private int maxGold = 28;
    [SerializeField] private int bonusCells = 5;
    [Range(0f, 1f)] [SerializeField] private float bonusMutationChance = 0.25f;

    private bool opened;
    private bool playerNear;
    private GameObject promptObj;
    private SpriteRenderer chestSr;
    private Transform playerTransform;

    public void ConfigureForMapTier(int mapTier)
    {
        int tier = Mathf.Max(1, mapTier);
        minGold = 12 + tier * 5;
        maxGold = 22 + tier * 8;
        bonusCells = 4 + tier * 3;
        bonusMutationChance = Mathf.Clamp(0.2f + (tier - 1) * 0.1f, 0.2f, 0.45f);
    }

    private void Start()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        chestSr = GetComponent<SpriteRenderer>();
        if (chestSr == null)
        {
            var res = Resources.Load<Sprite>("Roguelite/treasure_chest");
            if (res != null)
            {
                chestSr = gameObject.AddComponent<SpriteRenderer>();
                chestSr.sprite = res;
                CombatVFX.ApplyVfxLayer(chestSr, 22, useWorldLayer: true);
            }
            else
            {
                BuildProceduralGoldChestVisual();
            }
        }

        promptObj = new GameObject("TreasureChestPrompt");
        promptObj.transform.SetParent(transform);
        promptObj.transform.localPosition = new Vector3(0f, 2.45f, 0f);

        // Khung nền đen than viền vàng rộng rãi bao trọn toàn bộ chữ bên trong theo yêu cầu
        var bgObj = new GameObject("PromptBg");
        bgObj.transform.SetParent(promptObj.transform);
        bgObj.transform.localPosition = Vector3.zero;
        bgObj.transform.localScale = new Vector3(1.22f, 0.92f, 1f);
        var bgSr = bgObj.AddComponent<SpriteRenderer>();
        bgSr.sprite = GetPromptBadge();
        CombatVFX.ApplyVfxLayer(bgSr, 114);

        var tmp = promptObj.AddComponent<TextMeshPro>();
        tmp.text = "<color=#FFD700><b>[F]</b></color> <color=#FFFFFF><b>TREASURE CHEST</b></color>\n<size=78%><color=#FFE57F><b>(Vàng + Vật phẩm ngẫu nhiên)</b></color></size>";
        tmp.fontSize = 3.7f;
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
        const int bw = 148, bh = 34;
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

    private void BuildProceduralGoldChestVisual()
    {
        // Rương gỗ cổ điển nạm vàng rực rỡ kiểu Dead Cells (24x18 px sắc nét)
        const int w = 24, h = 18;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        Color clear = new Color(0, 0, 0, 0);
        Color outline = new Color(0.12f, 0.07f, 0.04f, 1f);
        Color woodDark = new Color(0.38f, 0.2f, 0.1f, 1f);
        Color woodHi = new Color(0.56f, 0.32f, 0.16f, 1f);
        Color gold = new Color(0.95f, 0.74f, 0.18f, 1f);
        Color goldHi = new Color(1f, 0.94f, 0.48f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool cornerCut = (y == h - 1) && (x < 3 || x > w - 4);
                bool border = x <= 1 || x >= w - 2 || y <= 1 || y >= h - 2;
                bool bandVert = (x == 5 || x == 6 || x == w - 6 || x == w - 7);
                bool lidLine = (y == 10 || y == 11);
                bool lockBox = (y >= 7 && y <= 12) && (x >= 10 && x <= 13);

                if (cornerCut) tex.SetPixel(x, y, clear);
                else if (lockBox) tex.SetPixel(x, y, (y >= 10 ? goldHi : gold));
                else if (border) tex.SetPixel(x, y, outline);
                else if (lidLine || bandVert) tex.SetPixel(x, y, gold);
                else tex.SetPixel(x, y, y > 11 ? woodHi : woodDark);
            }
        }
        tex.Apply();

        chestSr = gameObject.AddComponent<SpriteRenderer>();
        chestSr.sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 16f);
        CombatVFX.ApplyVfxLayer(chestSr, 22, useWorldLayer: true);
    }

    private void Update()
    {
        if (opened) return;

        if (playerTransform == null)
        {
            var p = FindAnyObjectByType<Player>();
            if (p != null) playerTransform = p.transform;
        }

        // Tự động kiểm tra khoảng cách người chơi (2.2m) để 100% bấm F được
        if (playerTransform != null)
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);
            bool isNear = dist <= 2.2f;
            if (isNear != playerNear)
            {
                playerNear = isNear;
                if (promptObj != null) promptObj.SetActive(isNear);
            }
        }

        // Lấp lánh ánh vàng nhẹ mời gọi người chơi khám phá
        if (UnityEngine.Random.value < 0.06f)
        {
            CombatVFX.SpawnBurst(transform.position + new Vector3(UnityEngine.Random.Range(-0.45f, 0.45f), 0.6f, 0f),
                new Color(1f, 0.88f, 0.3f, 0.85f), 1, 0.4f, 1.4f, 0.12f, 0.35f, -1.2f);
        }

        if (playerNear && Input.GetKeyDown(KeyCode.F) && !ScrollUpgradeUI.IsOpen && !MutationSystem.IsPickerOpen)
        {
            OpenChest();
        }
    }

    private void OpenChest()
    {
        opened = true;
        playerNear = false;
        if (promptObj != null) promptObj.SetActive(false);
        if (chestSr != null) chestSr.color = new Color(0.5f, 0.46f, 0.4f, 0.72f);

        int gold = UnityEngine.Random.Range(minGold, maxGold + 1);
        RunManager.Get().AddGold(gold);
        RunManager.Get().AddCells(bonusCells);

        CombatVFX.SpawnShockwave(transform.position + Vector3.up * 0.4f, new Color(1f, 0.86f, 0.25f, 0.95f), 0.3f, 2.5f, 0.32f);
        CombatVFX.SpawnBurst(transform.position + Vector3.up * 0.5f, new Color(1f, 0.88f, 0.28f, 1f), 16, 2.5f, 7f, 0.2f, 0.45f, 5f);
        if (AudioManager.Instance != null) AudioManager.Instance.SFX_TakeCoin();

        // Phần thưởng phụ ngẫu nhiên: Nạp Flask / Hồi máu / hoặc Bùa Đột Biến!
        var player = FindAnyObjectByType<Player>();
        string extraLabel = "";

        float roll = UnityEngine.Random.value;
        if (roll < bonusMutationChance)
        {
            extraLabel = " + MUTATION RELIC!";
            DamagePopup.SpawnText(transform.position + Vector3.up * 1.15f, $"+{gold}G  +{bonusCells}C{extraLabel}", new Color(1f, 0.9f, 0.3f), 6.2f);
            MutationSystem.ShowMutationPicker("TREASURE RELIC FOUND!");
            return;
        }
        else if (roll < 0.62f && player != null)
        {
            var flask = player.GetComponent<RallyAndFlask>();
            if (flask != null && flask.FlaskCharges < flask.MaxFlaskCharges)
            {
                flask.AddFlaskCharge(1, false);
                extraLabel = " +1 FLASK!";
            }
            else
            {
                float heal = Mathf.Max(2f, player.MaxHealth * 0.35f);
                player.Heal(heal);
                extraLabel = $" +{heal:0.#} HP!";
            }
        }
        else if (player != null)
        {
            float heal = Mathf.Max(2f, player.MaxHealth * 0.35f);
            player.Heal(heal);
            extraLabel = $" +{heal:0.#} HP!";
        }

        DamagePopup.SpawnText(transform.position + Vector3.up * 1.15f, $"+{gold}G  +{bonusCells}C{extraLabel}", new Color(1f, 0.9f, 0.3f), 6.0f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (opened) return;
        if (other.GetComponentInParent<Player>() != null)
        {
            playerNear = true;
            if (promptObj != null) promptObj.SetActive(true);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (opened) return;
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
