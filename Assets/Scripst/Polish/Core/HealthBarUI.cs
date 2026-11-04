using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Thanh máu mượt: thanh chính tụt ngay, thanh "trễ" (màu vàng/trắng) tụt dần phía sau.
/// Chỉ cập nhật khi máu đổi (event), không chạy mỗi frame cho việc tính toán máu.
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private Image fill;          // Image type = Filled
    [SerializeField] private Image delayedFill;   // (tuỳ chọn) Image Filled nằm dưới fill
    [SerializeField] private float catchUpSpeed = 0.6f;

    private float target = 1f;

    private void OnEnable()
    {
        if (health != null) health.onHealthChanged.AddListener(OnChanged);
    }

    private void OnDisable()
    {
        if (health != null) health.onHealthChanged.RemoveListener(OnChanged);
    }

    private void OnChanged(float current, float max)
    {
        target = max <= 0f ? 0f : current / max;
        if (fill != null) fill.fillAmount = target;
    }

    private void Update()
    {
        if (delayedFill == null) return;
        delayedFill.fillAmount = Mathf.MoveTowards(
            delayedFill.fillAmount, target, catchUpSpeed * Time.unscaledDeltaTime);
    }
}
