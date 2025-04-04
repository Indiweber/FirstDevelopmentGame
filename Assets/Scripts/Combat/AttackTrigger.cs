// #define DEBUG_COMPONENT_NOT_FOUND
// #define DEBUG_ATTACK_TRIGGER
// #define DEBUG_DAMAGE

/* 디버그 정의
 * DEBUG_COMPONENT_NOT_FOUND: 필수 컴포넌트를 찾지 못했을 때 에러를 출력
 * DEBUG_ATTACK_TRIGGER: 공격 트리거 관련 디버그 정보를 출력
 * DEBUG_DAMAGE: 데미지 처리 관련 디버그 정보를 출력
 */

using UnityEngine;
using UnityEngine.Events;

public class AttackTrigger : MonoBehaviour
{
    [Tooltip("공격 감지 범위 트리거")]
    [SerializeField] private string targetTag = "Enemy";
    
    // 공격 범위 이벤트
    public UnityEvent<GameObject> OnEnemyEnter = new UnityEvent<GameObject>();
    public UnityEvent<GameObject> OnEnemyExit = new UnityEvent<GameObject>();
    
    [SerializeField] private bool debugEnabled = false;
    
    private void Start()
    {
        if (GetComponent<Collider>() == null)
        {
            #if DEBUG_COMPONENT_NOT_FOUND
            Debug.LogError("Collider 컴포넌트가 필요합니다!");
            #endif
            enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            if (debugEnabled)
            {
                Debug.Log($"[AttackTrigger] 적 감지: {other.gameObject.name}");
            }
            
            // 적이 공격 범위에 들어왔을 때 이벤트 발생
            OnEnemyEnter.Invoke(other.gameObject);
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            if (debugEnabled)
            {
                Debug.Log($"[AttackTrigger] 적 이탈: {other.gameObject.name}");
            }
            
            // 적이 공격 범위에서 나갔을 때 이벤트 발생
            OnEnemyExit.Invoke(other.gameObject);
        }
    }
} 