using UnityEngine;

/// <summary>Mọi thứ có thể nhận sát thương (Player, Enemy, Boss, vật thể phá được...).</summary>
public interface IDamageable
{
    bool IsDead { get; }
    void TakeDamage(float amount, Vector2 hitPoint);
}
