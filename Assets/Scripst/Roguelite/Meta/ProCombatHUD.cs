using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Giao diện HUD Chiến Đấu Chuyên Nghiệp kiểu Dead Cells (tự khởi tạo runtime, không làm hỏng UI cũ):
/// - Thanh Máu + Thanh Máu Cam (Rally) + Bình Máu [H]
/// - 4 Ô Kỹ Năng (Dash, Parry, Fire Grenade [Q], Frost Wave [E]) có hiển thị hồi chiêu thời gian thực
/// - Chỉ số Brutality / Tactics / Survival + Huy hiệu Kill Streak
/// - Minimap Radar góc trên bên phải (Player, Quái, Boss, Vật phẩm/Chìa khóa)
/// - Thanh máu Boss khổng lồ ở đáy màn hình khi giao chiến
/// - Bảng Tổng Kết Run (Run Summary) khi kết thúc màn chơi
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

    // Top-left Stats & Streak
    private TextMeshProUGUI scrollStatsTxt;
    private TextMeshProUGUI streakTxt;
    private GameObject streakBadge;

    // Bottom-center Boss Bar
    private GameObject bossBarRoot;
    private TextMeshProUGUI bossNameTxt;
    private Image bossHpFill;
    private Image bossTrailFill;
    private float bossTrailAmount = 1f;

    // Top-right Minimap
    private RectTransform minimapContainer;
    private readonly List<Image> minimapDots = new List<Image>();
    private float minimapTimer;
    private const float MinimapRangeX = 28f;
    private const float MinimapRangeY = 16f;

    // Run Summary Banner (on GameOver / GameWin)
    private GameObject summaryBanner;
    private TextMeshProUGUI summaryTxt;

    public static void EnsureExists(Player targetPlayer)
    {
        if (instance == null)
        {
            var go = new GameObject("ProCombatHUD");
            instance = go.AddComponent<ProCombatHUD>();
            instance.BuildHUD();
        }
        instance.BindPlayer(targetPlayer);
        instance.HideRunSummary();
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

        BuildBottomLeftCombatBar();
        BuildTopLeftStats();
        BuildTopRightMinimap();
        BuildBossBar();
        BuildRunSummaryBanner();
    }

    private void BuildBottomLeftCombatBar()
    {
        var root = CreatePanel(transform, "BottomLeftHUD", new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0),
            new Vector2(20, 18), new Vector2(430, 112), new Color(0.06f, 0.05f, 0.12f, 0.78f));
        AddOutline(root, new Color(0.35f, 0.28f, 0.58f, 0.9f));

        // 1. Thanh HP + Rally Cam ở phía trên khung
        var barBg = CreatePanel(root.transform, "HpBarBg", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(12, -12), new Vector2(290, 24), new Color(0.12f, 0.08f, 0.14f, 0.95f));
        AddOutline(barBg, new Color(0.02f, 0.02f, 0.04f, 1f));

        // Thanh Máu Cam (nằm sau thanh máu thật)
        rallyFill = CreateBarImage(barBg.transform, "RallyFill", new Color(1f, 0.58f, 0.1f, 0.95f));
        // Thanh Máu Thật (đỏ thẫm sang tươi)
        hpFill = CreateBarImage(barBg.transform, "HpFill", new Color(0.88f, 0.18f, 0.24f, 1f));

        hpLabel = CreateText(barBg.transform, "HP 3 / 3", 14, Color.white, Vector2.zero, new Vector2(280, 22), TextAlignmentOptions.Center);

        // 2. Nút Bình Máu [H] bên cạnh thanh HP
        flaskBg = CreatePanel(root.transform, "FlaskBox", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(312, -10), new Vector2(106, 28), new Color(0.08f, 0.34f, 0.2f, 0.92f)).GetComponent<Image>();
        AddOutline(flaskBg.gameObject, new Color(0.25f, 0.92f, 0.48f, 0.9f));
        flaskLabel = CreateText(flaskBg.transform, "[H] FLASK 2/2", 13, new Color(0.75f, 1f, 0.82f), Vector2.zero, new Vector2(102, 24), TextAlignmentOptions.Center);

        // 3. 4 Ô Kỹ năng (Dash, Parry, Q Bomb, E Wave)
        CreateSkillSlot(root.transform, new Vector2(12, 12), "SHIFT/K", "DASH", new Color(0.16f, 0.48f, 0.62f), out dashCdMask, out dashCdTxt);
        CreateSkillSlot(root.transform, new Vector2(114, 12), "L / RMB", "PARRY", new Color(0.24f, 0.42f, 0.78f), out parryCdMask, out parryCdTxt);
        CreateSkillSlot(root.transform, new Vector2(216, 12), "[Q]", "BOMB", new Color(0.72f, 0.3f, 0.12f), out qCdMask, out qCdTxt);
        CreateSkillSlot(root.transform, new Vector2(318, 12), "[E]", "WAVE", new Color(0.18f, 0.56f, 0.72f), out eCdMask, out eCdTxt);
    }

    private void CreateSkillSlot(Transform parent, Vector2 pos, string keyHint, string skillName, Color accent, out Image cdMask, out TextMeshProUGUI cdTxt)
    {
        var slot = CreatePanel(parent, "Slot_" + skillName, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0),
            pos, new Vector2(94, 54), new Color(accent.r * 0.42f, accent.g * 0.42f, accent.b * 0.42f, 0.95f));
        AddOutline(slot, accent);

        CreateText(slot.transform, skillName, 14, Color.white, new Vector2(0, 9), new Vector2(88, 22), TextAlignmentOptions.Center);
        CreateText(slot.transform, keyHint, 11, new Color(0.85f, 0.9f, 1f, 0.85f), new Vector2(0, -13), new Vector2(88, 18), TextAlignmentOptions.Center);

        // Lớp phủ tối thể hiện hồi chiêu
        var maskGo = new GameObject("CdMask");
        maskGo.transform.SetParent(slot.transform, false);
        cdMask = maskGo.AddComponent<Image>();
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

        cdTxt = CreateText(slot.transform, "", 16, new Color(1f, 0.92f, 0.4f), Vector2.zero, new Vector2(88, 30), TextAlignmentOptions.Center);
    }

    private void BuildTopLeftStats()
    {
        var box = CreatePanel(transform, "TopLeftStats", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(20, -68), new Vector2(250, 34), new Color(0.06f, 0.05f, 0.12f, 0.72f));
        AddOutline(box, new Color(0.35f, 0.28f, 0.58f, 0.75f));

        scrollStatsTxt = CreateText(box.transform, "<color=#FF4C55>B:0</color>   <color=#C378FF>T:0</color>   <color=#4CE880>S:0</color>",
            15, Color.white, Vector2.zero, new Vector2(236, 28), TextAlignmentOptions.Center);

        streakBadge = CreatePanel(transform, "KillStreakBadge", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(20, -108), new Vector2(250, 30), new Color(0.24f, 0.15f, 0.04f, 0.85f));
        AddOutline(streakBadge, new Color(1f, 0.78f, 0.2f, 0.9f));
        streakTxt = CreateText(streakBadge.transform, "STREAK: 0 (0/5 SCROLL)", 13, new Color(1f, 0.9f, 0.45f), Vector2.zero, new Vector2(240, 26), TextAlignmentOptions.Center);
    }

    private void BuildTopRightMinimap()
    {
        var frame = CreatePanel(transform, "MinimapFrame", new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-20, -20), new Vector2(184, 108), new Color(0.05f, 0.04f, 0.11f, 0.82f));
        AddOutline(frame, new Color(0.45f, 0.35f, 0.72f, 0.95f));

        CreateText(frame.transform, "RADAR MAP", 10, new Color(0.72f, 0.68f, 0.92f, 0.85f), new Vector2(0, 44), new Vector2(170, 16), TextAlignmentOptions.Center);

        var inner = new GameObject("DotsArea");
        inner.transform.SetParent(frame.transform, false);
        minimapContainer = inner.AddComponent<RectTransform>();
        minimapContainer.anchorMin = new Vector2(0.5f, 0.5f);
        minimapContainer.anchorMax = new Vector2(0.5f, 0.5f);
        minimapContainer.sizeDelta = new Vector2(168, 82);
        minimapContainer.anchoredPosition = new Vector2(0, -6);

        // Tạo sẵn 32 chấm tái sử dụng (object pool) để không gây rác bộ nhớ GC
        for (int i = 0; i < 32; i++)
        {
            var dotGo = new GameObject("Dot_" + i);
            dotGo.transform.SetParent(minimapContainer, false);
            var img = dotGo.AddComponent<Image>();
            img.rectTransform.sizeDelta = new Vector2(6, 6);
            dotGo.SetActive(false);
            minimapDots.Add(img);
        }
    }

    private void BuildBossBar()
    {
        bossBarRoot = CreatePanel(transform, "BossHealthBar", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(200, 145), new Vector2(540, 56), new Color(0.08f, 0.03f, 0.08f, 0.88f));
        AddOutline(bossBarRoot, new Color(0.85f, 0.18f, 0.32f, 0.95f));

        bossNameTxt = CreateText(bossBarRoot.transform, "THE CONCIERGE WARDEN", 16, new Color(1f, 0.82f, 0.85f), new Vector2(0, 15), new Vector2(520, 22), TextAlignmentOptions.Center);

        var barBg = CreatePanel(bossBarRoot.transform, "BossHpBg", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 10), new Vector2(510, 18), new Color(0.14f, 0.05f, 0.08f, 1f));

        bossTrailFill = CreateBarImage(barBg.transform, "BossTrail", new Color(1f, 0.92f, 0.85f, 0.85f));
        bossHpFill = CreateBarImage(barBg.transform, "BossHp", new Color(0.88f, 0.12f, 0.28f, 1f));

        bossBarRoot.SetActive(false);
    }

    private void BuildRunSummaryBanner()
    {
        summaryBanner = CreatePanel(transform, "RunSummaryBanner", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -18), new Vector2(680, 96), new Color(0.06f, 0.04f, 0.12f, 0.92f));
        AddOutline(summaryBanner, new Color(1f, 0.82f, 0.25f, 0.95f));

        summaryTxt = CreateText(summaryBanner.transform, "", 16, Color.white, Vector2.zero, new Vector2(650, 84), TextAlignmentOptions.Center);
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

        minimapTimer -= Time.unscaledDeltaTime;
        if (minimapTimer <= 0f)
        {
            minimapTimer = 0.1f;
            UpdateMinimap();
        }
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
            scrollStatsTxt.text = $"<color=#FF4C55>BRUT:{ScrollUpgradeUI.BrutalityLevel}</color>  " +
                                  $"<color=#C378FF>TACT:{ScrollUpgradeUI.TacticsLevel}</color>  " +
                                  $"<color=#4CE880>SURV:{ScrollUpgradeUI.SurvivalLevel}</color>";
        }

        if (streakTxt != null)
        {
            var ks = KillStreakManager.Get();
            int mod = ks.CurrentStreak % KillStreakManager.ScrollMilestoneInterval;
            streakTxt.text = $"STREAK: {ks.CurrentStreak}x  ({mod}/{KillStreakManager.ScrollMilestoneInterval} SCROLL)";
            if (streakBadge != null)
            {
                streakBadge.SetActive(true);
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

    private void UpdateMinimap()
    {
        if (minimapContainer == null || player == null) return;

        int idx = 0;
        Vector3 pPos = player.transform.position;

        // 1. Chấm Player ở chính giữa
        PlaceDot(ref idx, Vector2.zero, new Color(0.3f, 0.95f, 1f, 1f), 8f);

        // 2. Quái cận chiến
        var enemies = FindObjectsByType<PatrolEnemy1>(FindObjectsSortMode.None);
        foreach (var e in enemies)
        {
            if (e == null || e.IsDead) continue;
            PlaceWorldDot(ref idx, e.transform.position - pPos, new Color(1f, 0.28f, 0.28f, 0.95f), 6f);
        }

        // 3. Quái bắn xa
        var ranged = FindObjectsByType<RangedEnemy>(FindObjectsSortMode.None);
        foreach (var r in ranged)
        {
            if (r == null) continue;
            PlaceWorldDot(ref idx, r.transform.position - pPos, new Color(1f, 0.55f, 0.2f, 0.95f), 6f);
        }

        // 4. Boss
        var bosses = FindObjectsByType<PatrolBoss>(FindObjectsSortMode.None);
        foreach (var b in bosses)
        {
            if (b == null || !b.IsAlive) continue;
            PlaceWorldDot(ref idx, b.transform.position - pPos, new Color(0.95f, 0.25f, 0.95f, 1f), 9f);
        }

        // 5. Power Scrolls
        var scrolls = FindObjectsByType<PowerScrollPickup>(FindObjectsSortMode.None);
        foreach (var s in scrolls)
        {
            if (s == null) continue;
            PlaceWorldDot(ref idx, s.transform.position - pPos, new Color(1f, 0.9f, 0.2f, 1f), 7f);
        }

        // 6. Chìa khóa qua màn (Tag Key)
        var keys = GameObject.FindGameObjectsWithTag("Key");
        foreach (var k in keys)
        {
            if (k == null) continue;
            PlaceWorldDot(ref idx, k.transform.position - pPos, new Color(1f, 0.85f, 0.15f, 1f), 8f);
        }

        // Ẩn các chấm dư
        for (int i = idx; i < minimapDots.Count; i++)
        {
            minimapDots[i].gameObject.SetActive(false);
        }
    }

    private void PlaceWorldDot(ref int idx, Vector3 delta, Color col, float size)
    {
        float nx = Mathf.Clamp(delta.x / MinimapRangeX, -1f, 1f) * (minimapContainer.sizeDelta.x * 0.46f);
        float ny = Mathf.Clamp(delta.y / MinimapRangeY, -1f, 1f) * (minimapContainer.sizeDelta.y * 0.44f);
        PlaceDot(ref idx, new Vector2(nx, ny), col, size);
    }

    private void PlaceDot(ref int idx, Vector2 pos, Color col, float size)
    {
        if (idx >= minimapDots.Count) return;
        var dot = minimapDots[idx++];
        dot.gameObject.SetActive(true);
        dot.color = col;
        dot.rectTransform.anchoredPosition = pos;
        dot.rectTransform.sizeDelta = new Vector2(size, size);
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
