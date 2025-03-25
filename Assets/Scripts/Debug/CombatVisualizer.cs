using UnityEngine;

public class CombatVisualizer : MonoBehaviour
{
    [Tooltip("전투 관련 설정이 포함된 설정 파일")]
    [SerializeField] private MovementSettings settings;
    
    [Tooltip("적 감지 범위를 시각적으로 표시할지 여부")]
    [SerializeField] private bool showSearchRadius = true;
    
    [Tooltip("현재 타겟까지의 거리를 선으로 표시할지 여부")]
    [SerializeField] private bool showTargetLine = true;
    
    private Transform currentTarget;

    private void Start()
    {
        // MovementSettings 자동 찾기
        if (settings == null)
        {
            var playerMovement = GetComponent<CharacterMovement>();
            if (playerMovement != null)
            {
                settings = playerMovement.Settings;
            }
        }
    }

    public void SetCurrentTarget(Transform target)
    {
        currentTarget = target;
    }

    private void OnDrawGizmos()
    {
        if (!settings) return;

        // 탐지 범위 시각화
        if (showSearchRadius)
        {
            Gizmos.color = settings.searchRadiusColor;
            Gizmos.DrawWireSphere(transform.position, settings.searchRadius);
        }

        // 현재 타겟까지의 선 그리기
        if (showTargetLine && currentTarget)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.position);
            
            // 최소 거리 표시
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, settings.minEnemyDistance);
        }
    }
} 