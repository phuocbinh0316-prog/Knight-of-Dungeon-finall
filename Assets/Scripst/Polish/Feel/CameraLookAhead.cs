using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Camera Look-ahead kiểu Dead Cells:
/// Tạo 1 điểm neo mượt dịch nhẹ về phía trước hướng nhân vật đang nhìn để người chơi quan sát sớm quái và bẫy.
/// Tự động nối vào CinemachineCamera nếu đang theo dõi Player.
/// </summary>
public class CameraLookAhead : MonoBehaviour
{
    [SerializeField] private float lookAheadDistance = 2.2f;
    [SerializeField] private float smoothSpeed = 4.5f;
    [SerializeField] private float verticalOffset = 0.6f;

    private Player player;
    private Transform proxyTarget;
    private float currentOffsetX;

    private void Start()
    {
        player = GetComponent<Player>();
        if (player == null) return;

        var go = new GameObject("CameraLookAheadTarget");
        proxyTarget = go.transform;
        proxyTarget.position = transform.position + new Vector3(0f, verticalOffset, 0f);

        // Tự động gắn vào CinemachineCamera trong scene nếu nó đang follow Player
        var vcam = FindAnyObjectByType<CinemachineCamera>();
        if (vcam != null)
        {
            var target = vcam.Target;
            if (target.TrackingTarget == null || target.TrackingTarget == transform)
            {
                target.TrackingTarget = proxyTarget;
                vcam.Target = target;
            }
        }
    }

    private void LateUpdate()
    {
        if (proxyTarget == null || player == null) return;

        float desiredX = (player.IsFacingRight ? 1f : -1f) * lookAheadDistance;
        currentOffsetX = Mathf.Lerp(currentOffsetX, desiredX, Time.deltaTime * smoothSpeed);

        Vector3 targetPos = transform.position + new Vector3(currentOffsetX, verticalOffset, 0f);
        proxyTarget.position = Vector3.Lerp(proxyTarget.position, targetPos, Time.deltaTime * 12f);
    }

    private void OnDestroy()
    {
        if (proxyTarget != null) Destroy(proxyTarget.gameObject);
    }
}
