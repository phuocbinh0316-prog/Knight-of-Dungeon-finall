using System;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Quản lý dấu chấm than (!) cảnh báo đòn đánh kiểu Dead Cells và trạng thái Stun/Interrupt của quái vật.
/// </summary>
public class EnemyTelegraph : MonoBehaviour
{
    [Header("Telegraph Settings")]
    [SerializeField] private float telegraphOffset = 1.3f;
    [SerializeField] private Color warningColor = new Color(1f, 0.85f, 0.1f); // Vàng rực neon
    [SerializeField] private Color stunColor = new Color(1f, 0.15f, 0.15f);   // ĐỎ RỰC

    private GameObject indicatorObj;
    private TextMeshPro tmp;
    private Coroutine warningCoroutine;
    private float stunTimer = 0f;

    public bool IsPreparingAttack { get; private set; }
    public bool IsStunned => stunTimer > 0f;

    private void Awake()
    {
        CreateIndicator();
    }

    private void CreateIndicator()
    {
        if (indicatorObj != null) return;

        indicatorObj = new GameObject("Telegraph_Indicator");
        indicatorObj.transform.SetParent(transform);
        indicatorObj.transform.localPosition = new Vector3(0f, telegraphOffset, 0f);

        tmp = indicatorObj.AddComponent<TextMeshPro>();
        tmp.text = "!";
        tmp.fontSize = 8.5f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = warningColor;
        CombatVFX.ApplyVfxLayer(tmp.renderer, 120);

        indicatorObj.SetActive(false);
    }

    private EliteEnemy cachedElite;
    private UnityEngine.UI.Image cachedHpImage;

    private void UpdateIndicatorPosition()
    {
        if (indicatorObj == null) return;
        indicatorObj.transform.rotation = Quaternion.identity;

        if (cachedElite == null) cachedElite = GetComponent<EliteEnemy>();
        if (cachedElite != null)
        {
            if (cachedHpImage == null) cachedHpImage = GetComponentInChildren<UnityEngine.UI.Image>();
            float yPos = cachedHpImage != null ? cachedHpImage.transform.position.y : (transform.position.y + 2.45f);
            // Hiện dấu chấm than bên trái thanh máu của quái tinh anh theo đúng yêu cầu
            indicatorObj.transform.position = new Vector3(transform.position.x - 1.15f, yPos, transform.position.z);
        }
        else
        {
            indicatorObj.transform.localPosition = new Vector3(0f, telegraphOffset, 0f);
        }
    }

    private void Update()
    {
        if (stunTimer > 0f)
        {
            stunTimer -= Time.deltaTime;

            if (indicatorObj != null)
            {
                indicatorObj.SetActive(true);
                tmp.text = "STUN";
                tmp.fontSize = 6.0f;
                tmp.color = stunColor;
                UpdateIndicatorPosition();
                // Hiệu ứng rung/nhấp nháy nhẹ của chữ STUN màu đỏ
                float pulse = 1f + 0.15f * Mathf.Sin(Time.time * 20f);
                indicatorObj.transform.localScale = Vector3.one * pulse;
            }

            if (stunTimer <= 0f)
            {
                // Hết stun
                if (indicatorObj != null && !IsPreparingAttack)
                    indicatorObj.SetActive(false);
            }
        }

        // Đảm bảo dấu ! hoặc STUN luôn cập nhật vị trí chuẩn xác
        if (indicatorObj != null && indicatorObj.activeSelf)
        {
            UpdateIndicatorPosition();
        }
    }

    /// <summary>
    /// Hiển thị dấu ! cảnh báo trước khi ra đòn. Khi hết thời gian duration sẽ gọi onReady.
    /// </summary>
    public void ShowWarning(float duration, Action onReady)
    {
        if (IsStunned) return;

        if (warningCoroutine != null)
            StopCoroutine(warningCoroutine);

        warningCoroutine = StartCoroutine(WarningRoutine(duration, onReady));
    }

    private IEnumerator WarningRoutine(float duration, Action onReady)
    {
        IsPreparingAttack = true;

        if (indicatorObj == null) CreateIndicator();
        tmp.text = "!";
        tmp.color = warningColor;
        if (cachedElite == null) cachedElite = GetComponent<EliteEnemy>();
        tmp.fontSize = cachedElite != null ? 7.0f : 8.5f;
        UpdateIndicatorPosition();
        indicatorObj.SetActive(true);

        float elapsed = 0f;
        Vector3 baseScale = Vector3.one;

        while (elapsed < duration)
        {
            if (IsStunned)
            {
                CancelWarning();
                yield break;
            }

            elapsed += Time.deltaTime;
            UpdateIndicatorPosition();
            // Hiệu ứng giật nảy / nhấp nháy báo động
            float pulse = 1f + 0.35f * Mathf.Sin(elapsed * 25f);
            indicatorObj.transform.localScale = baseScale * pulse;
            yield return null;
        }

        indicatorObj.SetActive(false);
        IsPreparingAttack = false;
        warningCoroutine = null;

        onReady?.Invoke();
    }

    /// <summary>
    /// Huỷ cảnh báo (khi quái bị đánh ngắt chiêu hoặc bị stun).
    /// </summary>
    public void CancelWarning()
    {
        if (warningCoroutine != null)
        {
            StopCoroutine(warningCoroutine);
            warningCoroutine = null;
        }

        IsPreparingAttack = false;
        if (indicatorObj != null && stunTimer <= 0f)
        {
            indicatorObj.SetActive(false);
        }
    }

    /// <summary>
    /// Làm choáng quái vật trong khoảng thời gian duration (mặc định 0.75s, chữ STUN màu đỏ).
    /// </summary>
    public void Stun(float duration = 0.75f)
    {
        CancelWarning();
        stunTimer = duration;

        if (indicatorObj == null) CreateIndicator();
        tmp.text = "STUN";
        tmp.fontSize = 6.0f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = stunColor;
        UpdateIndicatorPosition();
        indicatorObj.SetActive(true);
    }

    /// <summary>
    /// Ngắt chiêu khi bị đánh trúng lúc đang chuẩn bị tấn công. Giới hạn tỉ lệ stun ở mức 10%.
    /// </summary>
    public bool TryInterrupt(float stunChance = 0.10f)
    {
        if (IsPreparingAttack)
        {
            if (UnityEngine.Random.value < stunChance)
            {
                CancelWarning();
                Stun(0.75f); // Làm choáng khi tỉ lệ 10% kích hoạt
                return true;
            }
        }
        return false;
    }
}
