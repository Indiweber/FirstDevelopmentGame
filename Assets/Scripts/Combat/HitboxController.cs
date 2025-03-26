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
        }
    }
    
    public void DeactivateHitbox()
    {
        if (_hitboxTrigger != null && isHitboxActive)
        {
            _hitboxTrigger.gameObject.SetActive(false);
            isHitboxActive = false;
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