using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Giao diện HUD Chiến Đấu Chuyên Nghiệp kiểu Dead Cells (tự khởi tạo runtime, không làm hỏng UI cũ):
/// - Góc trên bên trái lùi phải gọn gàng để hiển thị rõ đủ thông tin Scroll, Gold, Kill Streak & Mutations.
/// - Góc dưới bên trái nâng cao lên trên để thấy rõ đủ thanh HP + Máu Cam Rally, Flask [H] và 4 ô kỹ năng (Click được hoặc bấm phím!).
/// - Minimap Radar góc trên phải, Thanh máu Boss đáy màn hình, Banner Lời Nguyền & Run Summary.
/// </summary>
public class ProCombatHUD : MonoBehaviour
{
    private static ProCombatHUD instance;
    private static PatrolBoss activeBoss;

    private Player player;
    private DashRoll dash;
    private ParryShield parry;
    private RallyAndFlask rallyFlask;
    private ActiveSkills skills;

    // HP & Rally
    private Image hpFill;
    private Image rallyFill;
    private TextMeshProUGUI hpLabel;
    private TextMeshProUGUI flaskLabel;
    private Image flaskBg;

    // 4 Ability Slots
    private Image dashCdMask, parryCdMask, qCdMask, eCdMask;
    private TextMeshProUGUI dashCdTxt, parryCdTxt, qCdTxt, eCdTxt;

    // Top-left Stats, Gold, Streak & Mutations
    private TextMeshProUGUI scrollStatsTxt;
    private TextMeshProUGUI goldTxt;
    private TextMeshProUGUI streakTxt;
    private GameObject streakBadge;
    private TextMeshProUGUI mutationsTxt;

    // Top-center Curse Warning Banner
    private GameObject curseBanner;
    private TextMeshProUGUI curseBannerTxt;

    // Bottom-center/right Boss Bar
    private GameObject bossBarRoot;
    private TextMeshProUGUI bossNameTxt;
    private Image bossHpFill;
    private Image bossTrailFill;
    private float bossTrailAmount = 1f;

    // Run Summary Banner
    private GameObject summaryBanner;
    private TextMeshProUGUI summaryTxt;

    public static void EnsureExists(Player targetPlayer)
    {
        activeBoss = null;
        if (instance == null)
        {
            var go = new GameObject("ProCombatHUD");
            instance = go.AddComponent<ProCombatHUD>();
            instance.BuildHUD();
        }
        instance.BindPlayer(targetPlayer);
        instance.HideRunSummary();
        if (instance.bossBarRoot != null) instance.bossBarRoot.SetActive(false);
    }

    public static void RegisterActiveBoss(PatrolBoss boss)
    {
        activeBoss = boss;
        if (instance != null && instance.bossBarRoot != null && boss != null)
        {
            instance.bossBarRoot.SetActive(true);
            instance.bossTrailAmount = boss.MaxHealth > 0f ? boss.CurrentHp / boss.MaxHealth : 1f;
        }
    }

    public static void UnregisterBoss(PatrolBoss boss)
    {
        if (activeBoss == boss) activeBoss = null;
        if (instance != null && instance.bossBarRoot != null)
        {
            instance.bossBarRoot.SetActive(false);
        }
    }

    public static void ShowRunSummary(bool isVictory)
    {
        if (instance == null) return;
        instance.DisplayRunSummary(isVictory);
    }

    private void BindPlayer(Player p)
    {
        player = p;
        if (p == null) return;
        dash = p.GetComponent<DashRoll>();
        parry = p.GetComponent<ParryShield>();
        rallyFlask = p.GetComponent<RallyAndFlask>();
        skills = p.GetComponent<ActiveSkills>();
    }

    private void BuildHUD()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 180;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        BuildBottomLeftCombatBar();
        BuildTopLeftStats();
        BuildBossBar();
        BuildRunSummaryBanner();
    }

    private void BuildBottomLeftCombatBar()
    {
        // Lùi phải nhẹ & đẩy LÊN TRÊN (y = 64) để hiển thị rõ 100% thanh máu + cả 4 ô kỹ năng & phím tắt
        var root = CreatePanel(transform, "BottomLeftHUD", new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0),
            new Vector2(128, 64), new Vector2(400, 104), new Color(0.06f, 0.05f, 0.12f, 0.84f));
        AddOutline(root, new Color(0.35f, 0.28f, 0.58f, 0.92f));

        // 1. Thanh HP + Rally Cam
        var barBg = CreatePanel(root.transform, "HpBarBg", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(10, -10), new Vector2(266, 24), new Color(0.12f, 0.08f, 0.14f, 0.96f));
        AddOutline(barBg, new Color(0.02f, 0.02f, 0.04f, 1f));

        rallyFill = CreateBarImage(barBg.transform, "RallyFill", new Color(1f, 0.58f, 0.1f, 0.95f));
        hpFill = CreateBarImage(barBg.transform, "HpFill", new Color(0.88f, 0.18f, 0.24f, 1f));
        hpLabel = CreateText(barBg.transform, "HP 3 / 3", 13, Color.white, Vector2.zero, new Vector2(256, 22), TextAlignmentOptions.Center);

        // 2. Nút Bình Máu [H] (Click được!)
        var flaskGo = CreatePanel(root.transform, "FlaskBox", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(284, -9), new Vector2(106, 26), new Color(0.08f, 0.34f, 0.2f, 0.94f));
        flaskBg = flaskGo.GetComponent<Image>();
        AddOutline(flaskGo, new Color(0.25f, 0.92f, 0.48f, 0.92f));
        var flaskBtn = flaskGo.AddComponent<Button>();
        flaskBtn.onClick.AddListener(() => { if (rallyFlask != null) rallyFlask.TryUseFlask(); });
        flaskLabel = CreateText(flaskGo.transform, "[H] FLASK 2/2", 12, new Color(0.75f, 1f, 0.82f), Vector2.zero, new Vector2(102, 22), TextAlignmentOptions.Center);

        // 3. 4 Ô Kỹ năng (Dash, Parry, Q Bomb, E Wave — hỗ trợ cả BẤM PHÍM lẫn CLICK CHUỘT!)
        CreateSkillSlot(root.transform, new Vector2(10, 10), "SHIFT/K", "DASH", new Color(0.16f, 0.48f, 0.62f),
            () => { if (dash != null) dash.TryTriggerDash(); }, out dashCdMask, out dashCdTxt);
        CreateSkillSlot(root.transform, new Vector2(107, 10), "L / RMB", "PARRY", new Color(0.24f, 0.42f, 0.78f),
            () => { if (parry != null) parry.TryTriggerParry(); }, out parryCdMask, out parryCdTxt);
        CreateSkillSlot(root.transform, new Vector2(204, 10), "[Q] / [U]", "BOMB", new Color(0.76f, 0.32f, 0.12f),
            () => { if (skills != null) skills.TryCastBomb(); }, out qCdMask, out qCdTxt);
        CreateSkillSlot(root.transform, new Vector2(301, 10), "[E]", "WAVE", new Color(0.18f, 0.56f, 0.72f),
            () => { if (skills != null) skills.TryCastWave(); }, out eCdMask, out eCdTxt);
    }

    private void CreateSkillSlot(Transform parent, Vector2 pos, string keyHint, string skillName, Color accent, Action onClick, out Image cdMask, out TextMeshProUGUI cdTxt)
    {
        var slot = CreatePanel(parent, "Slot_" + skillName, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0),
            pos, new Vector2(89, 52), new Color(accent.r * 0.44f, accent.g * 0.44f, accent.b * 0.44f, 0.96f));
        AddOutline(slot, accent);

        var btn = slot.AddComponent<Button>();
        btn.onClick.AddListener(() => onClick?.Invoke());

        CreateText(slot.transform, skillName, 13.5f, Color.white, new Vector2(0, 9), new Vector2(84, 20), TextAlignmentOptions.Center);
        CreateText(slot.transform, keyHint, 10.5f, new Color(0.88f, 0.93f, 1f, 0.92f), new Vector2(0, -12), new Vector2(84, 16), TextAlignmentOptions.Center);

        var maskGo = new GameObject("CdMask");
        maskGo.transform.SetParent(slot.transform, false);
        cdMask = maskGo.AddComponent<Image>();
        cdMask.raycastTarget = false;
        cdMask.color = new Color(0.02f, 0.02f, 0.05f, 0.75f);
        var mr = cdMask.rectTransform;
        mr.anchorMin = Vector2.zero;
        mr.anchorMax = Vector2.one;
        mr.offsetMin = Vector2.zero;
        mr.offsetMax = Vector2.zero;
        cdMask.type = Image.Type.Filled;
        cdMask.fillMethod = Image.FillMethod.Vertical;
        cdMask.fillOrigin = (int)Image.OriginVertical.Bottom;
        cdMask.fillAmount = 0f;

        cdTxt = CreateText(slot.transform, "", 15, new Color(1f, 0.92f, 0.4f), Vector2.zero, new Vector2(84, 28), TextAlignmentOptions.Center);
        cdTxt.raycastTarget = false;
    }

    private void BuildTopLeftStats()
    {
        // Lùi ra bên phải (x = 128) để hiển thị trọn vẹn B / T / S, Gold, Kill Streak và Mutations!
        var box = CreatePanel(transform, "TopLeftStats", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(128, -22), new Vector2(296, 32), new Color(0.06f, 0.05f, 0.12f, 0.84f));
        AddOutline(box, new Color(0.35f, 0.28f, 0.58f, 0.85f));

        scrollStatsTxt = CreateText(box.transform, "<color=#FF4C55>B:0</color>  <color=#C378FF>T:0</color>  <color=#4CE880>S:0</color>",
            13.5f, Color.white, new Vector2(-46, 0), new Vector2(184, 26), TextAlignmentOptions.Left);

        goldTxt = CreateText(box.transform, "0 G", 14.5f, new Color(1f, 0.86f, 0.22f), new Vector2(92, 0), new Vector2(92, 26), TextAlignmentOptions.Right);

        streakBadge = CreatePanel(transform, "KillStreakBadge", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(128, -60), new Vector2(296, 26), new Color(0.24f, 0.15f, 0.04f, 0.88f));
        AddOutline(streakBadge, new Color(1f, 0.78f, 0.2f, 0.92f));
        streakTxt = CreateText(streakBadge.transform, "STREAK: 0x  (0/5 SCROLL)", 12.5f, new Color(1f, 0.9f, 0.45f), Vector2.zero, new Vector2(282, 22), TextAlignmentOptions.Center);

        var mutBox = CreatePanel(transform, "MutationSlotsBar", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(128, -92), new Vector2(296, 28), new Color(0.11f, 0.05f, 0.18f, 0.86f));
        AddOutline(mutBox, new Color(0.7f, 0.35f, 0.95f, 0.88f));
        mutationsTxt = CreateText(mutBox.transform, "MUTATIONS: (Hạ Elite / Shop [F])", 11, new Color(0.86f, 0.8f, 0.97f), Vector2.zero, new Vector2(284, 22), TextAlignmentOptions.Center);
        mutationsTxt.enableAutoSizing = true;
        mutationsTxt.fontSizeMin = 9f;
        mutationsTxt.fontSizeMax = 12f;

        curseBanner = CreatePanel(transform, "CurseWarningBanner", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(128, -126), new Vector2(296, 28), new Color(0.28f, 0.03f, 0.28f, 0.94f));
        AddOutline(curseBanner, new Color(1f, 0.22f, 0.95f, 1f));
        curseBannerTxt = CreateText(curseBanner.transform, "CURSED: 5 KILLS (x2.5 DMG)", 12, new Color(1f, 0.55f, 1f), Vector2.zero, new Vector2(284, 24), TextAlignmentOptions.Center);
        curseBanner.SetActive(false);
    }

    private void BuildBossBar()
    {
        // Đặt thanh máu Boss ở GÓC DƯỚI BÊN PHẢI, ngang bằng độ cao (y = 64) với HUD thanh máu nhân vật để không cản tầm nhìn!
        bossBarRoot = CreatePanel(transform, "BossHealthBar", new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0),
            new Vector2(-128, 64), new Vector2(440, 56), new Color(0.08f, 0.03f, 0.08f, 0.9f));
        AddOutline(bossBarRoot, new Color(0.85f, 0.18f, 0.32f, 0.95f));

        bossNameTxt = CreateText(bossBarRoot.transform, "THE CONCIERGE WARDEN", 14.5f, new Color(1f, 0.82f, 0.85f), new Vector2(0, 14), new Vector2(420, 22), TextAlignmentOptions.Center);

        var barBg = CreatePanel(bossBarRoot.transform, "BossHpBg", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 10), new Vector2(414, 18), new Color(0.14f, 0.05f, 0.08f, 1f));

        bossTrailFill = CreateBarImage(barBg.transform, "BossTrail", new Color(1f, 0.92f, 0.85f, 0.85f));
        bossHpFill = CreateBarImage(barBg.transform, "BossHp", new Color(0.88f, 0.12f, 0.28f, 1f));

        bossBarRoot.SetActive(false);
    }

    private void BuildRunSummaryBanner()
    {
        summaryBanner = CreatePanel(transform, "RunSummaryBanner", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -18), new Vector2(660, 96), new Color(0.06f, 0.04f, 0.12f, 0.92f));
        AddOutline(summaryBanner, new Color(1f, 0.82f, 0.25f, 0.95f));

        summaryTxt = CreateText(summaryBanner.transform, "", 15.5f, Color.white, Vector2.zero, new Vector2(630, 84), TextAlignmentOptions.Center);
        summaryBanner.SetActive(false);
    }

    private void DisplayRunSummary(bool isVictory)
    {
        if (summaryBanner == null || summaryTxt == null) return;
        var ks = KillStreakManager.Get();
        float sec = ks.GetElapsedSeconds();
        int mins = Mathf.FloorToInt(sec / 60f);
        int remSec = Mathf.FloorToInt(sec % 60f);

        string header = isVictory ? "<color=#FFD940>VICTORY — RUN SUMMARY</color>" : "<color=#FF4C55>DEFEATED — RUN SUMMARY</color>";
        summaryTxt.text = $"{header}\n" +
                          $"Kills: <b>{ks.TotalKills}</b>   |   Best Streak: <b>{ks.BestStreak}x</b>   |   Parries: <b>{ks.TotalParries}</b>\n" +
                          $"Damage: <b>{Mathf.RoundToInt(ks.TotalDamageDealt)}</b>   |   Scrolls: <b>{ks.ScrollsCollected}</b>   |   Time: <b>{mins:00}:{remSec:00}</b>";
        summaryBanner.SetActive(true);
    }

    private void HideRunSummary()
    {
        if (summaryBanner != null) summaryBanner.SetActive(false);
    }

    private void Update()
    {
        if (player == null)
        {
            var found = FindAnyObjectByType<Player>();
            if (found != null) BindPlayer(found);
            else return;
        }

        UpdateHealthAndFlask();
        UpdateSkillSlots();
        UpdateTopLeftStats();
        UpdateBossBar();
    }

    private void UpdateHealthAndFlask()
    {
        float maxHp = Mathf.Max(0.01f, player.MaxHealth);
        float hpRatio = Mathf.Clamp01(player.CurrentHp / maxHp);
        float rallyRatio = rallyFlask != null ? Mathf.Clamp01((player.CurrentHp + rallyFlask.RallyPool) / maxHp) : hpRatio;

        if (hpFill != null) hpFill.fillAmount = hpRatio;
        if (rallyFill != null) rallyFill.fillAmount = rallyRatio;
        if (hpLabel != null) hpLabel.text = $"HP  {Mathf.CeilToInt(player.CurrentHp)} / {Mathf.CeilToInt(maxHp)}";

        if (rallyFlask != null && flaskLabel != null)
        {
            flaskLabel.text = $"[H] FLASK {rallyFlask.FlaskCharges}/{rallyFlask.MaxFlaskCharges}";
            if (flaskBg != null)
            {
                flaskBg.color = rallyFlask.FlaskCharges > 0
                    ? new Color(0.08f, 0.36f, 0.22f, 0.95f)
                    : new Color(0.18f, 0.16f, 0.2f, 0.75f);
            }
        }
    }

    private void UpdateSkillSlots()
    {
        UpdateSlot(dashCdMask, dashCdTxt, dash != null ? dash.CooldownNormalized : 1f, dash != null ? dash.CooldownRemaining : 0f);
        UpdateSlot(parryCdMask, parryCdTxt, parry != null ? parry.CooldownNormalized : 1f, parry != null ? parry.CooldownRemaining : 0f);
        UpdateSlot(qCdMask, qCdTxt, skills != null ? skills.GrenadeCooldownNormalized : 1f, skills != null ? skills.GrenadeCooldownRemaining : 0f);
        UpdateSlot(eCdMask, eCdTxt, skills != null ? skills.WaveCooldownNormalized : 1f, skills != null ? skills.WaveCooldownRemaining : 0f);
    }

    private void UpdateSlot(Image mask, TextMeshProUGUI txt, float readyNorm, float remSec)
    {
        if (mask != null) mask.fillAmount = 1f - Mathf.Clamp01(readyNorm);
        if (txt != null) txt.text = remSec > 0.05f ? $"{remSec:0.0}s" : "";
    }

    private void UpdateTopLeftStats()
    {
        if (scrollStatsTxt != null)
        {
            scrollStatsTxt.text = $"<color=#FF4C55>B:{ScrollUpgradeUI.BrutalityLevel}</color>  " +
                                  $"<color=#C378FF>T:{ScrollUpgradeUI.TacticsLevel}</color>  " +
                                  $"<color=#4CE880>S:{ScrollUpgradeUI.SurvivalLevel}</color>";
        }

        if (goldTxt != null)
        {
            goldTxt.text = $"{RunManager.Get().Gold} G";
        }

        if (streakTxt != null)
        {
            var ks = KillStreakManager.Get();
            int mod = ks.CurrentStreak % KillStreakManager.ScrollMilestoneInterval;
            streakTxt.text = $"STREAK: {ks.CurrentStreak}x  ({mod}/{KillStreakManager.ScrollMilestoneInterval} SCROLL)";
            if (streakBadge != null) streakBadge.SetActive(true);
        }

        if (mutationsTxt != null)
        {
            if (MutationSystem.ActiveMutations.Count == 0 && !MutationSystem.HasElementalCoating)
            {
                mutationsTxt.text = "MUTATIONS: (Hạ Elite / Shop [F])";
            }
            else
            {
                var parts = new List<string>();
                foreach (var m in MutationSystem.ActiveMutations)
                {
                    string hex = ColorUtility.ToHtmlStringRGB(MutationSystem.GetColor(m));
                    string extra = (m == MutationType.ComboFrenzy && MutationSystem.FrenzyStacks > 0) ? $" x{MutationSystem.FrenzyStacks}" : "";
                    parts.Add($"<color=#{hex}>[{MutationSystem.GetShortName(m)}{extra}]</color>");
                }
                if (MutationSystem.HasElementalCoating) parts.Add("<color=#FF8833>[BLADE+]</color>");
                mutationsTxt.text = string.Join(" ", parts);
            }
        }

        if (curseBanner != null)
        {
            bool cursed = CurseSystem.IsCursed;
            if (curseBanner.activeSelf != cursed) curseBanner.SetActive(cursed);
            if (cursed && curseBannerTxt != null)
            {
                float pulse = 0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * 8f);
                curseBannerTxt.text = $"CURSED: {CurseSystem.CurseKillsRemaining} KILLS  (x2.5 DMG TAKEN)";
                curseBannerTxt.color = new Color(1f, 0.38f * pulse, 1f, 1f);
            }
        }
    }

    private void UpdateBossBar()
    {
        if (bossBarRoot == null) return;
        if (activeBoss == null || !activeBoss.IsAlive)
        {
            if (bossBarRoot.activeSelf) bossBarRoot.SetActive(false);
            return;
        }

        if (!bossBarRoot.activeSelf) bossBarRoot.SetActive(true);

        float ratio = activeBoss.MaxHealth > 0f ? Mathf.Clamp01(activeBoss.CurrentHp / activeBoss.MaxHealth) : 0f;
        bossTrailAmount = Mathf.Lerp(bossTrailAmount, ratio, Time.deltaTime * 3.5f);

        if (bossHpFill != null)
        {
            bossHpFill.fillAmount = ratio;
            bossHpFill.color = activeBoss.IsPhase2 ? new Color(1f, 0.18f, 0.45f, 1f) : new Color(0.88f, 0.14f, 0.24f, 1f);
        }
        if (bossTrailFill != null) bossTrailFill.fillAmount = bossTrailAmount;
        if (bossNameTxt != null)
        {
            bossNameTxt.text = activeBoss.IsPhase2
                ? $"{activeBoss.BossTitle}  —  <color=#FF5577>PHASE II ENRAGED</color>"
                : activeBoss.BossTitle;
        }
    }

    private GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var rect = img.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        return go;
    }

    private Image CreateBarImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.raycastTarget = false;
        img.sprite = CombatVFX.GetPixelSprite();
        img.color = color;
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = (int)Image.OriginHorizontal.Left;
        img.fillAmount = 1f;
        var r = img.rectTransform;
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
        return img;
    }

    private TextMeshProUGUI CreateText(Transform parent, string text, float size, Color color, Vector2 pos, Vector2 dims, TextAlignmentOptions align)
    {
        var go = new GameObject("Txt");
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = color;
        tmp.alignment = align;
        var r = tmp.rectTransform;
        r.anchoredPosition = pos;
        r.sizeDelta = dims;
        return tmp;
    }

    private void AddOutline(GameObject go, Color color)
    {
        var o = go.AddComponent<Outline>();
        o.effectColor = color;
        o.effectDistance = new Vector2(1.5f, -1.5f);
    }
}
