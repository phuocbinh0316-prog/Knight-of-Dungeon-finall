using UnityEngine;

/// <summary>
/// Điểm vào duy nhất cho mọi hiệu ứng "đánh trúng".
/// Gọi GameFeel.OnHit(...) là có đủ: số sát thương, nháy màu, hit stop, rung màn hình.
/// </summary>
public static class GameFeel
{
    public static void OnHit(Vector2 position, float damage, GameObject target, bool isCritical = false)
    {
        bool isPlayer = target != null && target.CompareTag("Player");

        DamagePopup.Spawn(position, damage, isPlayer ? new Color(1f, 0.35f, 0.35f) : Color.white, isCritical);

        if (target != null)
        {
            var flash = target.GetComponent<HitFlash>();
            if (flash != null) flash.Flash();
        }

        float hitstopTime = isCritical ? 0.09f : (isPlayer ? 0.08f : 0.05f);
        float shakeStrength = isCritical ? 0.35f : (isPlayer ? 0.5f : 0.2f);

        HitStop.Do(hitstopTime);
        CameraShake.Shake(shakeStrength);
    }
}
