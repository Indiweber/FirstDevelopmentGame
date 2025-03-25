using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("추적 설정")]
    [Tooltip("카메라가 추적할 대상 (플레이어)")]
    [SerializeField] private Transform target;

    [Tooltip("카메라 이동 속도 (값이 클수록 더 부드럽게 따라감)")]
    [Range(0.1f, 20f)]
    [SerializeField] private float smoothSpeed = 5f;

    private Vector3 initialCameraPosition;
    private Vector3 initialPlayerPosition;
    private Vector3 velocity = Vector3.zero;
    private bool initialized = false;

    private void Start()
    {
        // 초기 카메라 위치 저장
        initialCameraPosition = transform.position;

        if (target == null)
        {
            // 플레이어 자동 찾기
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                Debug.Log("카메라가 플레이어를 자동으로 찾았습니다.");
            }
            else
            {
                Debug.LogWarning("플레이어를 찾을 수 없습니다. 카메라 타겟을 수동으로 설정해주세요.");
                return;
            }
        }

        // 초기 플레이어 위치 저장
        initialPlayerPosition = target.position;
        initialized = true;
    }

    private void LateUpdate()
    {
        if (target == null || !initialized) return;

        // 플레이어의 이동 변화량 계산
        Vector3 playerDelta = target.position - initialPlayerPosition;
        
        // 카메라 위치 계산 (초기 카메라 위치 + 플레이어 이동 변화량)
        Vector3 desiredPosition = initialCameraPosition + new Vector3(playerDelta.x, 0, playerDelta.z);
        
        // 부드러운 이동
        Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, 1f/smoothSpeed);
        transform.position = smoothedPosition;
    }

    // 타겟 설정 메서드
    public void SetTarget(Transform newTarget)
    {
        if (newTarget != null)
        {
            // 새 타겟으로 설정 시 초기화
            target = newTarget;
            initialPlayerPosition = target.position;
            initialCameraPosition = transform.position;
            initialized = true;
        }
    }

    // 에디터에서 기즈모로 카메라 위치 시각화
    private void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, target.position);
        }
    }
} 