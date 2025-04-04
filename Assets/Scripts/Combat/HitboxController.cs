// #define DEBUG_COMPONENT_NOT_FOUND
// #define DEBUG_HITBOX_STATE
// #define DEBUG_COLLISION

/* 디버그 정의
 * DEBUG_COMPONENT_NOT_FOUND: 필수 컴포넌트를 찾지 못했을 때 에러를 출력
 * DEBUG_HITBOX_STATE: 히트박스 상태 변경 관련 디버그 정보를 출력
 * DEBUG_COLLISION: 충돌 처리 관련 디버그 정보를 출력
 */

using UnityEngine;
using UnityEngine.Events;

public class HitboxController : MonoBehaviour
{
    [Header("히트박스 설정")]
    [SerializeField] private GameObject hitboxPrefab;
    [SerializeField] private Transform hitboxParent;
    [SerializeField] private string targetTag = "Enemy";
    
    [Header("디버그")]
    [SerializeField] private bool debugEnabled = false;
    
    public UnityEvent<GameObject> OnHitboxCollision = new UnityEvent<GameObject>();
    
    private HitboxTrigger _hitboxTrigger;
    private bool isHitboxActive = false;
    
    public HitboxTrigger HitboxTrigger => _hitboxTrigger;
    
    private void Awake()
    {
        // 히트박스 부모가 지정되지 않았다면 현재 오브젝트를 사용
        if (hitboxParent == null)
        {
            hitboxParent = transform;
        }
        
        // 히트박스 트리거 생성
        CreateHitboxTrigger();
    }
    
    private void CreateHitboxTrigger()
    {
        if (hitboxPrefab != null)
        {
            GameObject hitboxObj = Instantiate(hitboxPrefab, hitboxParent);
            _hitboxTrigger = hitboxObj.GetComponent<HitboxTrigger>();
            
            if (_hitboxTrigger == null)
            {
                _hitboxTrigger = hitboxObj.AddComponent<HitboxTrigger>();
            }
            
            _hitboxTrigger.Initialize(this, targetTag, debugEnabled);
            
            // 초기에는 비활성화
            hitboxObj.SetActive(false);
        }
        else
        {
            Debug.LogError("히트박스 프리팹이 설정되지 않았습니다!");
        }
    }
    
    public void ActivateHitbox()
    {
        if (_hitboxTrigger != null && !isHitboxActive)
        {
            _hitboxTrigger.gameObject.SetActive(true);
            isHitboxActive = true;
            #if DEBUG_HITBOX_STATE
            Debug.Log($"히트박스 활성화: {gameObject.name}");
            #endif
        }
    }
    
    public void DeactivateHitbox()
    {
        if (_hitboxTrigger != null && isHitboxActive)
        {
            _hitboxTrigger.gameObject.SetActive(false);
            isHitboxActive = false;
            #if DEBUG_HITBOX_STATE
            Debug.Log($"히트박스 비활성화: {gameObject.name}");
            #endif
        }
    }
    
    private void OnDestroy()
    {
        if (_hitboxTrigger != null)
        {
            Destroy(_hitboxTrigger.gameObject);
        }
    }
    
    // 충돌 발생 시 호출될 메서드
    public void OnHitboxHit(GameObject hitObject)
    {
        if (debugEnabled)
        {
            Debug.Log($"히트박스 충돌: {hitObject.name}");
        }
        
        OnHitboxCollision.Invoke(hitObject);
    }
    
    // 히트박스의 위치 및 크기 설정
    public void SetHitboxProperties(Vector3 localPosition, Vector3 size)
    {
        if (_hitboxTrigger != null)
        {
            _hitboxTrigger.transform.localPosition = localPosition;
            
            BoxCollider boxCollider = _hitboxTrigger.GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                boxCollider.size = size;
            }
        }
    }
} 