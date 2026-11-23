using TMPro;
using UnityEngine;

/// <summary>
/// Rương Nguyền Rủa (Cursed Chest) đặt trong màn chơi:
/// Đứng gần nhấn [F] -> nhận ngay 1 Power Scroll + 25 Vàng + 10 Cells, nhưng dính Lời Nguyền 5 mạng (1 đòn chết ngay)!
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CursedChest : MonoBehaviour
{
    [SerializeField] private int curseKills = 5;
    [SerializeField] private int bonusGold = 25;
    [SerializeField] private int bonusCells = 10;

    private bool opened;
    private bool playerNear;
    private GameObject promptObj;
    private SpriteRenderer chestSr;
    private Transform playerTransform;

    private void Start()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        chestSr = GetComponent<SpriteRenderer>();
        if (chestSr == null)
        {
            var res = Resources.Load<Sprite>("Roguelite/cursed_chest");
            if (res != null)
            {
                chestSr = gameObject.AddComponent<SpriteRenderer>();
                chestSr.sprite = res;
                CombatVFX.ApplyVfxLayer(chestSr, 22, useWorldLayer: true);
            }
            else
            {
                BuildProceduralChestVisual();
            }
        }

        var p = FindAnyObjectByType<Player>();
        if (p != null) playerTransform = p.transform;

        promptObj = new GameObject("CursedChestPrompt");
        promptObj.transform.SetParent(transform);
        promptObj.transform.localPosition = new Vector3(0f, 2.45f, 0f);

        var bgObj = new GameObject("PromptBg");
        bgObj.transform.SetParent(promptObj.transform);
        bgObj.transform.localPosition = Vector3.zero;
        bgObj.transform.localScale = new Vector3(1.25f, 0.92f, 1f);
        var bgSr = bgObj.AddComponent<SpriteRenderer>();
        bgSr.sprite = GetPromptBadge();
        CombatVFX.ApplyVfxLayer(bgSr, 114);

        var tmp = promptObj.AddComponent<TextMeshPro>();
        tmp.text = $"<color=#FF2A6D><b>[F]</b></color> <color=#FFFFFF><b>CURSED CHEST</b></color>\n<size=76%><color=#FFB2D9><b>(Scroll + {bonusGold}G | Nguyền rủa x2.5 Sát thương)</b></color></size>";
        tmp.fontSize = 3.6f;
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
        const int bw = 152, bh = 34;
        var tex = new Texture2D(bw, bh, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        Color border = new Color(0.95f, 0.2f, 0.45f, 0.95f);
        Color inner = new Color(0.08f, 0.04f, 0.12f, 0.92f);
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

    private void BuildProceduralChestVisual()
    {
        const int w = 24, h = 18;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        Color clear = new Color(0, 0, 0, 0);
        Color dark = new Color(0.12f, 0.05f, 0.18f, 1f);
        Color body = new Color(0.28f, 0.1f, 0.38f, 1f);
        Color trim = new Color(0.88f, 0.2f, 0.42f, 1f);
        Color glow = new Color(1f, 0.35f, 0.95f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool border = x <= 1 || x >= w - 2 || y <= 1 || y >= h - 2;
                bool lidLine = y == 10 || y == 11;
                bool eye = (y >= 12 && y <= 14) && ((x >= 6 && x <= 8) || (x >= 15 && x <= 17));
                bool lockCenter = (y >= 7 && y <= 11) && (x >= 10 && x <= 13);

                if ((y == h - 1) && (x < 3 || x > w - 4)) tex.SetPixel(x, y, clear);
                else if (eye) tex.SetPixel(x, y, glow);
                else if (lockCenter) tex.SetPixel(x, y, trim);
                else if (border || lidLine) tex.SetPixel(x, y, dark);
                else tex.SetPixel(x, y, body);
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

        // Tự động kiểm tra khoảng cách người chơi (2.2m)
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

        if (UnityEngine.Random.value < 0.08f)
        {
            CombatVFX.SpawnBurst(transform.position + Vector3.up * 0.55f, new Color(0.85f, 0.2f, 1f, 0.75f), 1, 0.5f, 1.5f, 0.14f, 0.4f, -1.5f);
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

        if (chestSr != null) chestSr.color = new Color(0.4f, 0.35f, 0.45f, 0.75f);

        RunManager.Get().AddGold(bonusGold);
        RunManager.Get().AddCells(bonusCells);
        CurseSystem.ApplyCurse(curseKills);

        DamagePopup.SpawnText(transform.position + Vector3.up * 1.1f, $"+{bonusGold}G  +{bonusCells} CELLS!", new Color(1f, 0.85f, 0.2f), 6.2f);
        CombatVFX.SpawnBurst(transform.position + Vector3.up * 0.5f, new Color(0.95f, 0.25f, 1f, 1f), 16, 2.5f, 7f, 0.2f, 0.45f, 4f);

        ScrollUpgradeUI.ShowScrollChoice("CURSED CHEST REWARD!");
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
