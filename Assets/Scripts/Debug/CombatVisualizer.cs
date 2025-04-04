// #define DEBUG_COMPONENT_NOT_FOUND
// #define DEBUG_VISUALIZATION

/* 디버그 정의
 * DEBUG_COMPONENT_NOT_FOUND: 필수 컴포넌트를 찾지 못했을 때 에러를 출력
 * DEBUG_VISUALIZATION: 시각화 관련 디버그 정보를 출력
 */

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
        if (GetComponent<AutoCombat>() == null)
        {
            #if DEBUG_COMPONENT_NOT_FOUND
            Debug.LogError("AutoCombat 컴포넌트가 필요합니다!");
            #endif
            enabled = false;
        }

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
        if (!Application.isPlaying) return;
        
        var autoCombat = GetComponent<AutoCombat>();
        if (autoCombat == null) return;
        
        #if DEBUG_VISUALIZATION
        Debug.Log("전투 시각화 업데이트");
        #endif

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