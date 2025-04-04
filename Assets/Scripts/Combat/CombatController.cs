// #define DEBUG_COMPONENT_NOT_FOUND
// #define DEBUG_COMBAT_STATE
// #define DEBUG_ATTACK_PROCESS

/* 디버그 정의
 * DEBUG_COMPONENT_NOT_FOUND: 필수 컴포넌트를 찾지 못했을 때 에러를 출력
 * DEBUG_COMBAT_STATE: 전투 상태 변경 관련 디버그 정보를 출력
 * DEBUG_ATTACK_PROCESS: 공격 처리 관련 디버그 정보를 출력
 */

using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
public class CombatController : MonoBehaviour
{
    [Header("공격 설정")]
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private string attackAnimTrigger = "Attack";
    
    [Header("히트박스 설정")]
    [SerializeField] private HitboxController hitboxController;
    
    [Header("디버그")]
    [SerializeField] private bool debugEnabled = false;
    
    [Header("전투 설정")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private LayerMask enemyLayer;
    
    private Animator animator;
    private bool canAttack = true;
    private int attackAnimHash;
    private GameObject currentTarget;
    private bool isInCombat;
    
    private void Awake()
    {
        animator = GetComponent<Animator>();
        attackAnimHash = Animator.StringToHash(attackAnimTrigger);
        
        // 히트박스 컨트롤러 설정
        SetupHitboxController();
    }
    
    private void SetupHitboxController()
    {
        if (hitboxController == null)
        {
            hitboxController = GetComponent<HitboxController>();
            if (hitboxController == null)
            {
                hitboxController = gameObject.AddComponent<HitboxController>();
            }
        }
        
        // 이전 이벤트 리스너 제거
        RemoveEventListeners();
        
        // 새로운 이벤트 리스너 등록
        if (hitboxController != null)
        {
            hitboxController.OnHitboxCollision.AddListener(OnEnemyHit);
            StartCoroutine(SetupHitboxTriggerEvents());
        }
    }
    
    private void RemoveEventListeners()
    {
        if (hitboxController != null)
        {
            hitboxController.OnHitboxCollision.RemoveListener(OnEnemyHit);
            
            if (hitboxController.HitboxTrigger != null)
            {
                var trigger = hitboxController.HitboxTrigger;
                trigger.OnEnemyEntered.RemoveListener(HandleEnemyEntered);
                trigger.OnEnemyExited.RemoveListener(HandleEnemyExited);
            }
        }
    }
    
    private IEnumerator SetupHitboxTriggerEvents()
    {
        yield return new WaitForSeconds(0.1f); // 약간의 지연을 주어 초기화 완료 대기
        
        if (hitboxController != null && hitboxController.HitboxTrigger != null)
        {
            var trigger = hitboxController.HitboxTrigger;
            trigger.OnEnemyEntered.AddListener(HandleEnemyEntered);
            trigger.OnEnemyExited.AddListener(HandleEnemyExited);
            
            if (debugEnabled)
            {
                Debug.Log("히트박스 트리거 이벤트 연결 완료");
            }
        }
    }

    private void OnDestroy()
    {
        RemoveEventListeners();
    }
    
    private void HandleEnemyEntered(GameObject enemy)
    {
        if (enemy != null && enemy.CompareTag("Enemy"))
        {
            if (animator != null)
            {
                animator.SetBool("Attack", true);
                animator.SetBool("Walk", false);
                animator.SetBool("Idle", false);
            }
        }
    }

    private void HandleEnemyExited(GameObject enemy)
    {
        if (enemy != null && enemy.CompareTag("Enemy"))
        {
            if (animator != null)
            {
                animator.SetBool("Attack", false);
                animator.SetBool("Walk", false);
                animator.SetBool("Idle", true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 추가적인 안전장치: 트리거에서 나가는 경우에도 상태 확인
        if (other.CompareTag("Enemy"))
        {
            HandleEnemyExited(other.gameObject);
        }
    }

    private void Update()
    {
        if (animator != null && animator.GetBool("Attack"))
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, 2f);
            bool enemyFound = false;
            
            foreach (Collider col in colliders)
            {
                if (col.CompareTag("Enemy"))
                {
                    enemyFound = true;
                    break;
                }
            }
            
            if (!enemyFound)
            {
                animator.SetBool("Attack", false);
                animator.SetBool("Idle", true);
            }
        }
    }

    public void TryAttack()
    {
        if (animator != null)
        {
            // 공격 애니메이션 재생
            animator.SetBool("Attack", true);
            animator.SetBool("Walk", false);
            animator.SetBool("Idle", false);
            
            if (debugEnabled)
            {
                Debug.Log("공격 시도");
            }
        }
    }
    
    private IEnumerator PerformAttack()
    {
        canAttack = false;
        
        // 공격 애니메이션 시작
        animator.SetTrigger(attackAnimHash);
        
        if (debugEnabled)
        {
            Debug.Log("공격 시작");
        }
        
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }
    
    // 애니메이션 이벤트에서 호출될 메서드
    public void ActivateHitbox()
    {
        hitboxController.ActivateHitbox();
    }
    
    // 애니메이션 이벤트에서 호출될 메서드
    public void DeactivateHitbox()
    {
        hitboxController.DeactivateHitbox();
    }
    
    private void OnEnemyHit(GameObject enemy)
    {
        if (debugEnabled)
        {
            Debug.Log($"적 타격 성공: {enemy.name}");
        }
        
        // 여기에 대미지 처리 로직 추가
        // 예: enemy.GetComponent<EnemyHealth>().TakeDamage(10);
    }
    
    // 자동 공격을 위한 메서드 (AutoCombat 등에서 사용)
    public void AutoAttack(GameObject target)
    {
        if (target != null && canAttack)
        {
            // 타겟 방향으로 캐릭터 회전
            Vector3 direction = target.transform.position - transform.position;
            direction.y = 0f;
            
            if (direction != Vector3.zero)
            {
                transform.forward = direction.normalized;
            }
            
            // 공격 실행
            TryAttack();
        }
    }

    private void Start()
    {
        if (animator == null)
        {
            #if DEBUG_COMPONENT_NOT_FOUND
            Debug.LogError("Animator 컴포넌트가 필요합니다!");
            #endif
            enabled = false;
            return;
        }
    }

    public void StartCombat(GameObject target)
    {
        if (target == null) return;

        currentTarget = target;
        isInCombat = true;
        #if DEBUG_COMBAT_STATE
        Debug.Log($"전투 시작: 대상 - {target.name}");
        #endif
    }

    public void EndCombat()
    {
        currentTarget = null;
        isInCombat = false;
        #if DEBUG_COMBAT_STATE
        Debug.Log("전투 종료");
        #endif
    }

    private void ProcessAttack()
    {
        if (!isInCombat || currentTarget == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
        if (distanceToTarget <= attackRange)
        {
            #if DEBUG_ATTACK_PROCESS
            Debug.Log($"공격 처리 중: 대상 - {currentTarget.name}, 거리 - {distanceToTarget}");
            #endif
            animator.SetTrigger("Attack");
        }
    }

    public bool IsInCombat => isInCombat;
    public GameObject CurrentTarget => currentTarget;
} 