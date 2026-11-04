using UnityEngine;
using Unity.Cinemachine; // Cinemachine 3.x (project của bạn đang dùng 3.1.5)

/// <summary>
/// Gắn vào object CinemachineCamera trong mỗi map.
/// Nhớ: trên CinemachineCamera cũng phải Add Extension > Cinemachine Impulse Listener.
/// </summary>
[RequireComponent(typeof(CinemachineImpulseSource))]
public class CameraShake : MonoBehaviour
{
    private static CameraShake instance;
    private CinemachineImpulseSource source;

    private void Awake()
    {
        instance = this;
        source = GetComponent<CinemachineImpulseSource>();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public static void Shake(float force = 0.3f)
    {
        if (instance != null && instance.source != null)
            instance.source.GenerateImpulseWithForce(force);
    }
}
