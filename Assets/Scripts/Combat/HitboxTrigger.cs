// #define DEBUG_COMPONENT_NOT_FOUND
// #define DEBUG_TRIGGER_EVENT

/* 디버그 정의
 * DEBUG_COMPONENT_NOT_FOUND: 필수 컴포넌트를 찾지 못했을 때 에러를 출력
 * DEBUG_TRIGGER_EVENT: 트리거 이벤트 관련 디버그 정보를 출력
 */

using UnityEngine;
using UnityEngine.Events;

public class HitboxTrigger : MonoBehaviour
{
    // 이벤트 선언
    public UnityEvent<GameObject> OnEnemyEntered = new UnityEvent<GameObject>();
    public UnityEvent<GameObject> OnEnemyExited = new UnityEvent<GameObject>();
    
    private HitboxController controller;
    private string targetTag;
    private bool debugEnabled;
    
    public void Initialize(HitboxController controller, string targetTag, bool debugEnabled)
    {
        this.controller = controller;
        this.targetTag = targetTag;
        this.debugEnabled = debugEnabled;
    }
    
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
        if (other.CompareTag("Enemy"))
        {
            #if DEBUG_TRIGGER_EVENT
            Debug.Log($"적이 히트박스 트리거에 진입: {other.gameObject.name}");
            #endif
            OnEnemyEntered.Invoke(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            #if DEBUG_TRIGGER_EVENT
            Debug.Log($"적이 히트박스 트리거를 벗어남: {other.gameObject.name}");
            #endif
            OnEnemyExited.Invoke(other.gameObject);
        }
    }
} 