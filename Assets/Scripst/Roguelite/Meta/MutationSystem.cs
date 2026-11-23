using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum MutationType
{
    FrostDash,       // Dash xuyên qua quái đóng băng (Frost) ngay phía sau lưng
    ChainLightning,  // Đòn Crit phóng tia sét lan sang 2 quái gần nhất
    VampiricCrit,    // Hạ gục quái bằng đòn Crit hồi +1 HP thật
    ComboFrenzy,     // Mỗi đòn đánh trúng tích tầng tăng tốc đánh (+8%) & sát thương (+10%), tối đa 5 tầng
    Berserker        // Khi máu <= 45%: +45% sát thương & mọi đòn đánh đều thành CRIT!
}

/// <summary>
/// Hệ thống Bùa Đột Biến (Mutations / Relics) kiểu Dead Cells (tối đa 3 ô hoạt động trong 1 Run)
/// với giao diện thẻ 3 dải gọn gàng (có RectMask2D chống tràn chữ) và không bao giờ gây kẹt slow-mo!
/// </summary>
public class MutationSystem : MonoBehaviour
{
    private static MutationSystem instance;

    public const int MaxSlots = 3;

    public static readonly List<MutationType> ActiveMutations = new List<MutationType>();
    public static bool HasElementalCoating { get; private set; }

    public static int FrenzyStacks { get; private set; }
    private static float frenzyExpireTime;
    private const float FrenzyDuration = 3.5f;
    private const int MaxFrenzyStacks = 5;

    public static event Action OnMutationsChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitStaticOnPlay()
    {
        instance = null;
        ActiveMutations.Clear();
        HasElementalCoating = false;
        FrenzyStacks = 0;
        frenzyExpireTime = 0f;
        OnMutationsChanged = null;
    }

    public static bool Has(MutationType type) => ActiveMutations.Contains(type);
    public static bool IsPickerOpen => instance != null && instance.pickerRoot != null && instance.pickerRoot.activeSelf;

    public static float GetDamageMultiplier(Player p)
    {
        float mult = 1f;
        if (Has(MutationType.ComboFrenzy) && Time.time <= frenzyExpireTime)
        {
            mult += FrenzyStacks * 0.10f;
        }
        if (Has(MutationType.Berserker) && p != null && p.MaxHealth > 0f && (p.CurrentHp / p.MaxHealth) <= 0.45f)
        {
            mult += 0.45f;
        }
        return mult;
    }

    public static float GetAttackCooldownMultiplier()
    {
        if (Has(MutationType.ComboFrenzy) && Time.time <= frenzyExpireTime)
        {
            return Mathf.Max(0.55f, 1f - FrenzyStacks * 0.08f);
        }
        return 1f;
    }

    public static bool IsBerserkerCritActive(Player p)
    {
        return Has(MutationType.Berserker) && p != null && p.MaxHealth > 0f && (p.CurrentHp / p.MaxHealth) <= 0.45f;
    }

    public static void ResetRun()
    {
        ActiveMutations.Clear();
        HasElementalCoating = false;
        FrenzyStacks = 0;
        frenzyExpireTime = 0f;
        OnMutationsChanged?.Invoke();
    }

    public static void EnableElementalCoating()
    {
        HasElementalCoating = true;
        OnMutationsChanged?.Invoke();
    }

    public static void AddMutation(MutationType type)
    {
        if (ActiveMutations.Contains(type)) return;
        if (ActiveMutations.Count >= MaxSlots) ActiveMutations.RemoveAt(0);
        ActiveMutations.Add(type);
        OnMutationsChanged?.Invoke();
    }

    public static void OnPlayerHitTarget(Player player, GameObject target, float damageDealt, bool isCrit)
    {
        if (player == null || target == null) return;

        if (Has(MutationType.ComboFrenzy))
        {
            if (Time.time > frenzyExpireTime) FrenzyStacks = 0;
            FrenzyStacks = Mathf.Min(MaxFrenzyStacks, FrenzyStacks + 1);
            frenzyExpireTime = Time.time + FrenzyDuration;
            OnMutationsChanged?.Invoke();
        }

        if (HasElementalCoating && UnityEngine.Random.value < 0.45f)
        {
            var st = StatusEffectController.GetOrAdd(target);
            if (st != null)
            {
                bool burn = UnityEngine.Random.value < 0.5f;
                st.Apply(burn ? StatusType.Burn : StatusType.Poison, 3.5f, 0.7f);
            }
        }

        if (isCrit && Has(MutationType.ChainLightning))
        {
            TriggerChainLightning(player, target, damageDealt * 0.6f);
        }
    }

    public static void OnDashThroughEnemy(GameObject enemyRoot)
    {
        if (enemyRoot == null || !Has(MutationType.FrostDash)) return;
        var st = StatusEffectController.GetOrAdd(enemyRoot);
        if (st != null && !st.IsFrosted)
        {
            st.Apply(StatusType.Frost, 3.2f);
            CombatVFX.SpawnShockwave(enemyRoot.transform.position, new Color(0.45f, 0.92f, 1f, 0.9f), 0.25f, 1.8f, 0.24f);
        }
    }

    public static void OnEnemyKilled(bool wasCritOrAfflicted, Vector3 enemyPos)
    {
        if (!Has(MutationType.VampiricCrit) || !wasCritOrAfflicted) return;
        var player = FindAnyObjectByType<Player>();
        if (player != null && player.CurrentHp > 0f && player.CurrentHp < player.MaxHealth)
        {
            player.Heal(1f);
            DamagePopup.SpawnText(player.transform.position + Vector3.up * 0.9f, "VAMPIRIC +1 HP", new Color(0.95f, 0.2f, 0.45f), 5.8f);
            CombatVFX.SpawnBurst(enemyPos, new Color(0.95f, 0.15f, 0.35f, 0.95f), 8, 2f, 5f, 0.18f, 0.35f, 2f);
        }
    }

    private static void TriggerChainLightning(Player player, GameObject originEnemy, float zapDamage)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(originEnemy.transform.position, 6.5f);
        var done = new HashSet<GameObject> { player.gameObject, originEnemy };
        int jumped = 0;

        foreach (var h in hits)
        {
            if (jumped >= 2) break;
            if (h == null) continue;
            GameObject root = h.attachedRigidbody != null ? h.attachedRigidbody.gameObject : h.gameObject;
            if (!done.Add(root)) continue;

            bool isEnemy = false;
            var e1 = root.GetComponent<PatrolEnemy1>();
            if (e1 != null && !e1.IsDead)
            {
                isEnemy = true;
                e1.takeDamageE(zapDamage, false, originEnemy.transform.position, bypassShield: true);
            }
            else
            {
                var boss = root.GetComponent<PatrolBoss>();
                if (boss != null && boss.IsAlive)
                {
                    isEnemy = true;
                    boss.takeDamageE(zapDamage, false);
                }
                else
                {
                    var d = root.GetComponent<IDamageable>();
                    if (d != null && root.GetComponent<Player>() == null)
                    {
                        isEnemy = true;
                        d.TakeDamage(zapDamage, root.transform.position);
                        DamagePopup.Spawn(root.transform.position, zapDamage, new Color(0.55f, 0.92f, 1f), false);
                    }
                }
            }

            if (isEnemy)
            {
                jumped++;
                CombatVFX.SpawnLightningBolt(originEnemy.transform.position + Vector3.up * 0.3f, root.transform.position + Vector3.up * 0.3f, new Color(0.45f, 0.92f, 1f, 1f), 0.24f);
                CombatVFX.SpawnBurst(root.transform.position, new Color(0.55f, 0.95f, 1f, 1f), 6, 2f, 5f, 0.15f, 0.25f, 3f);
                player.NotifyDamageDealt(zapDamage);
            }
        }
    }

    public static string GetShortName(MutationType t) => t switch
    {
        MutationType.FrostDash => "FROST DASH",
        MutationType.ChainLightning => "LIGHTNING",
        MutationType.VampiricCrit => "VAMPIRIC",
        MutationType.ComboFrenzy => "FRENZY",
        MutationType.Berserker => "BERSERKER",
        _ => t.ToString()
    };

    public static string GetStatLine(MutationType t) => t switch
    {
        MutationType.FrostDash => "Đóng băng khi Lướt\nFrost 3.2s + Làm chậm 45%",
        MutationType.ChainLightning => "Sét lan khi Chí mạng\n60% Sát thương lên 2 mục tiêu",
        MutationType.VampiricCrit => "Hút máu kết liễu\n+1 HP mỗi mạng Crit / Trạng thái",
        MutationType.ComboFrenzy => "Tích tầng Combo (Max 5)\n+50% Sát thương & +40% Tốc đánh",
        MutationType.Berserker => "Cuồng nộ máu thấp (<=45%)\n+45% Sát thương & 100% CRIT",
        _ => ""
    };

    public static string GetDescription(MutationType t) => t switch
    {
        MutationType.FrostDash => "• Dash xuyên qua quái sẽ đóng băng chúng ngay phía sau lưng.\n• Kết hợp cực mạnh với Backstab Crit & Synergy (+40%).",
        MutationType.ChainLightning => "• Mỗi đòn Chí mạng phóng tia sét nảy sang 2 kẻ địch gần nhất.\n• Giúp dọn đám đông cực nhanh khi đánh Combo 3 đòn.",
        MutationType.VampiricCrit => "• Hạ gục kẻ địch bằng đòn Crit hoặc khi địch đang dính Burn/Bleed/Frost sẽ hồi ngay +1 HP thật.",
        MutationType.ComboFrenzy => "• Mỗi đòn đánh trúng tích 1 tầng Frenzy trong 3.5 giây.\n• Giữ nhịp tấn công liên tục để chém siêu nhanh.",
        MutationType.Berserker => "• Khi máu xuống dưới 45%, toàn bộ đòn đánh đều trở thành Chí mạng kèm +45% sát thương.",
        _ => ""
    };

    public static Color GetColor(MutationType t) => t switch
    {
        MutationType.FrostDash => new Color(0.35f, 0.88f, 1f),
        MutationType.ChainLightning => new Color(1f, 0.88f, 0.25f),
        MutationType.VampiricCrit => new Color(0.95f, 0.22f, 0.42f),
        MutationType.ComboFrenzy => new Color(1f, 0.52f, 0.16f),
        MutationType.Berserker => new Color(0.92f, 0.14f, 0.2f),
        _ => Color.white
    };

    // ==================== MUTATION PICKER UI ====================

    private GameObject pickerRoot;
    private TextMeshProUGUI pickerTitleTxt;
    private readonly List<MutationType> currentChoices = new List<MutationType>();
    private readonly List<GameObject> cardObjects = new List<GameObject>();

    public static MutationSystem Get()
    {
        if (instance == null)
        {
            var go = new GameObject("MutationSystem");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<MutationSystem>();
            instance.BuildPickerUI();
        }
        return instance;
    }

    public static void ShowMutationPicker(string title = "MUTATION RELIC UNLOCKED!")
    {
        Get().OpenPicker(title);
    }

    private void BuildPickerUI()
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

        pickerRoot = new GameObject("MutationPickerRoot");
        pickerRoot.transform.SetParent(transform, false);
        var bg = pickerRoot.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.03f, 0.09f, 0.88f);
        StretchFill(bg.rectTransform);

        pickerTitleTxt = CreateFittedText(pickerRoot.transform, "MUTATION RELIC UNLOCKED!", 22, 34, new Color(0.84f, 0.54f, 1f), TextAlignmentOptions.Center);
        SetCenterBox(pickerTitleTxt.rectTransform, new Vector2(0, 210), new Vector2(920, 48));

        var sub = CreateFittedText(pickerRoot.transform, "Chọn 1 Bùa Đột Biến  —  Click chuột hoặc phím [1] [2] [3]  ([ESC] bỏ qua)", 13, 17, new Color(0.8f, 0.84f, 0.94f), TextAlignmentOptions.Center);
        SetCenterBox(sub.rectTransform, new Vector2(0, 170), new Vector2(920, 28));

        pickerRoot.SetActive(false);
    }

    private void OpenPicker(string title)
    {
        if (pickerRoot.activeSelf) return;

        var pool = new List<MutationType>();
        foreach (MutationType m in Enum.GetValues(typeof(MutationType)))
        {
            if (!ActiveMutations.Contains(m)) pool.Add(m);
        }
        if (pool.Count == 0)
        {
            ScrollUpgradeUI.ShowScrollChoice("ALL MUTATIONS OWNED — BONUS SCROLL!");
            return;
        }

        for (int i = 0; i < pool.Count; i++)
        {
            int j = UnityEngine.Random.Range(i, pool.Count);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        currentChoices.Clear();
        int count = Mathf.Min(3, pool.Count);
        for (int i = 0; i < count; i++) currentChoices.Add(pool[i]);

        foreach (var c in cardObjects) if (c != null) Destroy(c);
        cardObjects.Clear();

        float spacing = 315f;
        float startX = -(count - 1) * spacing * 0.5f;
        for (int i = 0; i < count; i++)
        {
            int idx = i;
            var mut = currentChoices[i];
            var card = CreateMutationCard(pickerRoot.transform, new Vector2(startX + i * spacing, -18), idx + 1, mut, () => SelectChoice(idx));
            cardObjects.Add(card);
        }

        HitStop.Cancel();
        Time.timeScale = 0f;
        if (pickerTitleTxt != null) pickerTitleTxt.text = title;
        pickerRoot.SetActive(true);
    }

    private GameObject CreateMutationCard(Transform parent, Vector2 pos, int keyNum, MutationType mut, Action onClick)
    {
        Color accent = GetColor(mut);
        const float cardW = 292f;
        const float cardH = 290f;

        var card = new GameObject("MutCard_" + mut);
        card.transform.SetParent(parent, false);
        var cardImg = card.AddComponent<Image>();
        cardImg.color = new Color(0.05f, 0.07f, 0.13f, 0.97f);
        SetCenterBox(cardImg.rectTransform, pos, new Vector2(cardW, cardH));

        var outline = card.AddComponent<Outline>();
        outline.effectColor = accent;
        outline.effectDistance = new Vector2(2f, -2f);
        card.AddComponent<RectMask2D>();

        var btn = card.AddComponent<Button>();
        btn.onClick.AddListener(() => onClick());

        // Dải 1: Header
        var header = CreateBand(card.transform, "Header", 0f, 54f, new Color(0.08f, 0.1f, 0.18f, 1f));
        var badge = new GameObject("Badge");
        badge.transform.SetParent(header.transform, false);
        var bImg = badge.AddComponent<Image>();
        bImg.color = new Color(accent.r * 0.35f, accent.g * 0.35f, accent.b * 0.35f, 1f);
        var br = bImg.rectTransform;
        br.anchorMin = new Vector2(0, 0.5f);
        br.anchorMax = new Vector2(0, 0.5f);
        br.pivot = new Vector2(0, 0.5f);
        br.anchoredPosition = new Vector2(10, 0);
        br.sizeDelta = new Vector2(36, 36);
        var bOut = badge.AddComponent<Outline>();
        bOut.effectColor = accent;
        bOut.effectDistance = new Vector2(1.5f, -1.5f);

        var kTxt = CreateFittedText(badge.transform, keyNum.ToString(), 16, 22, Color.white, TextAlignmentOptions.Center);
        StretchFill(kTxt.rectTransform, 2, 2, 2, 2);

        var tTxt = CreateFittedText(header.transform, GetShortName(mut), 16, 21, Color.Lerp(accent, Color.white, 0.35f), TextAlignmentOptions.Left);
        StretchFill(tTxt.rectTransform, 58, 12, 6, 6);

        // Dải 2: Stat Strip
        var statBand = CreateBand(card.transform, "StatBand", 54f, 58f, new Color(accent.r * 0.32f, accent.g * 0.28f, accent.b * 0.38f, 0.95f));
        var sTxt = CreateFittedText(statBand.transform, GetStatLine(mut), 12, 15.5f, Color.white, TextAlignmentOptions.Left);
        StretchFill(sTxt.rectTransform, 14, 14, 6, 6);

        // Dải 3: Mô tả
        var body = CreateBand(card.transform, "Body", 112f, 134f, new Color(0.04f, 0.06f, 0.11f, 1f));
        var dTxt = CreateFittedText(body.transform, GetDescription(mut), 11, 14.5f, new Color(0.86f, 0.9f, 0.96f), TextAlignmentOptions.TopLeft);
        StretchFill(dTxt.rectTransform, 14, 14, 10, 8);

        CreateBand(card.transform, "Divider", 246f, 2f, new Color(accent.r, accent.g, accent.b, 0.55f));

        // Dải 4: Footer
        var footer = CreateBand(card.transform, "Footer", 248f, 42f, new Color(accent.r * 0.22f, accent.g * 0.22f, accent.b * 0.28f, 1f));
        string slotNote = ActiveMutations.Count >= MaxSlots ? "(Thay ô cũ)" : $"Ô {ActiveMutations.Count + 1}/{MaxSlots}";
        var fTxt = CreateFittedText(footer.transform, $"TRANG BỊ [{keyNum}]  •  {slotNote}", 11, 13.5f, Color.Lerp(accent, Color.white, 0.45f), TextAlignmentOptions.Center);
        StretchFill(fTxt.rectTransform, 10, 10, 4, 4);

        return card;
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

    private void Update()
    {
        if (FrenzyStacks > 0 && Time.time > frenzyExpireTime)
        {
            FrenzyStacks = 0;
            OnMutationsChanged?.Invoke();
        }

        if (pickerRoot == null || !pickerRoot.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.Alpha1) && currentChoices.Count >= 1) SelectChoice(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2) && currentChoices.Count >= 2) SelectChoice(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3) && currentChoices.Count >= 3) SelectChoice(2);
        else if (Input.GetKeyDown(KeyCode.Escape)) ClosePicker();
    }

    private void SelectChoice(int idx)
    {
        if (idx < 0 || idx >= currentChoices.Count) return;
        var chosen = currentChoices[idx];
        AddMutation(chosen);
        ClosePicker();

        var p = FindAnyObjectByType<Player>();
        if (p != null)
        {
            Color col = GetColor(chosen);
            DamagePopup.SpawnText(p.transform.position + Vector3.up * 1.2f, $"MUTATION: {GetShortName(chosen)}!", col, 7.2f);
            CombatVFX.SpawnShockwave(p.transform.position, col, 0.35f, 3.0f, 0.38f);
        }
        if (AudioManager.Instance != null) AudioManager.Instance.SFX_Win();
    }

    private void ClosePicker()
    {
        pickerRoot.SetActive(false);
        HitStop.Cancel();
        Time.timeScale = 1f;
    }
}
