using TMPro;
using UnityEngine;

/// <summary>
/// Vật phẩm Cuộn Sức Mạnh (Power Scroll) đặt trong màn chơi: đứng gần và nhấn [F] để mở bảng chọn 1 trong 3 nhánh.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PowerScrollPickup : MonoBehaviour
{
    private bool playerNear;
    private GameObject promptObj;
    private Vector3 basePos;
    private Transform playerTransform;

    private void Start()
    {
        basePos = transform.position;
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        var sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            var res = Resources.Load<Sprite>("Roguelite/power_scroll");
            if (res != null)
            {
                sr = gameObject.AddComponent<SpriteRenderer>();
                sr.sprite = res;
                CombatVFX.ApplyVfxLayer(sr, 25, useWorldLayer: true);
            }
            else
            {
                BuildScrollSprite();
            }
        }

        var p = FindAnyObjectByType<Player>();
        if (p != null) playerTransform = p.transform;

        promptObj = new GameObject("ScrollPrompt");
        promptObj.transform.SetParent(transform);
        promptObj.transform.localPosition = new Vector3(0f, 2.45f, 0f);

        var bgObj = new GameObject("PromptBg");
        bgObj.transform.SetParent(promptObj.transform);
        bgObj.transform.localPosition = Vector3.zero;
        bgObj.transform.localScale = new Vector3(1.22f, 0.90f, 1f);
        var bgSr = bgObj.AddComponent<SpriteRenderer>();
        bgSr.sprite = GetPromptBadge();
        CombatVFX.ApplyVfxLayer(bgSr, 111);

        var tmp = promptObj.AddComponent<TextMeshPro>();
        tmp.text = "<color=#FBBF24><b>[F]</b></color> <color=#FFFFFF><b>POWER SCROLL</b></color>\n<size=78%><color=#FEF08A><b>(Chọn 1 nhánh sức mạnh)</b></color></size>";
        tmp.fontSize = 3.8f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        CombatVFX.ApplyVfxLayer(tmp.renderer, 112);
        promptObj.SetActive(false);
    }

    private static Sprite cachedPromptBadge;

    private static Sprite GetPromptBadge()
    {
        if (cachedPromptBadge != null) return cachedPromptBadge;
        const int bw = 140, bh = 32;
        var tex = new Texture2D(bw, bh, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        Color border = new Color(0.98f, 0.75f, 0.14f, 0.95f);
        Color inner = new Color(0.04f, 0.06f, 0.12f, 0.92f);
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

    private void BuildScrollSprite()
    {
        const int w = 18, h = 20;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        Color clear = new Color(0, 0, 0, 0);
        Color dark = new Color(0.14f, 0.09f, 0.06f, 1f);
        Color paper = new Color(0.96f, 0.89f, 0.68f, 1f);
        Color paperShade = new Color(0.82f, 0.72f, 0.48f, 1f);
        Color rod = new Color(0.85f, 0.62f, 0.16f, 1f);
        Color ribbon = new Color(0.92f, 0.2f, 0.3f, 1f);

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, clear);

        for (int y = 3; y <= 16; y++)
        {
            for (int x = 4; x <= 13; x++)
            {
                bool edge = x == 4 || x == 13;
                bool tie = (y == 9 || y == 10);
                if (edge) tex.SetPixel(x, y, dark);
                else if (tie) tex.SetPixel(x, y, ribbon);
                else tex.SetPixel(x, y, x > 10 ? paperShade : paper);
            }
        }
        for (int x = 2; x <= 15; x++)
        {
            tex.SetPixel(x, 17, dark); tex.SetPixel(x, 16, rod); tex.SetPixel(x, 15, dark);
            tex.SetPixel(x, 4, dark);  tex.SetPixel(x, 3, rod);  tex.SetPixel(x, 2, dark);
        }
        tex.Apply();

        var sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        CombatVFX.ApplyVfxLayer(sr, 25, useWorldLayer: true);
    }

    private void Update()
    {
        transform.position = basePos + Vector3.up * (0.65f + Mathf.Sin(Time.time * 3f) * 0.14f);

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

        if (UnityEngine.Random.value < 0.06f)
        {
            CombatVFX.SpawnBurst(transform.position + Vector3.up * 0.2f, new Color(1f, 0.85f, 0.35f, 0.8f), 1, 0.3f, 1.2f, 0.12f, 0.35f, 0.5f);
        }

        if (playerNear && Input.GetKeyDown(KeyCode.F) && !ScrollUpgradeUI.IsOpen && !MutationSystem.IsPickerOpen)
        {
            ScrollUpgradeUI.ShowScrollChoice("POWER SCROLL ACQUIRED!");
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

    private void OnTriggerStay2D(Collider2D other)
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
