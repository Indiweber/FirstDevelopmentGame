// #define DEBUG_COMPONENT_NOT_FOUND
// #define DEBUG_INITIALIZATION
// #define DEBUG_AUTO_MODE
// #define DEBUG_ENEMY_DETECTION
// #define DEBUG_COMBAT_STATE
// #define DEBUG_ENEMY_MANAGEMENT
// #define DEBUG_MOVEMENT

/* 디버그 정의
 * DEBUG_COMPONENT_NOT_FOUND: 필수 컴포넌트를 찾지 못했을 때 에러를 출력
 * DEBUG_INITIALIZATION: 초기화 관련 디버그 정보를 출력
 * DEBUG_AUTO_MODE: 자동 모드 전환 관련 디버그 정보를 출력
 * DEBUG_ENEMY_DETECTION: 적 감지 관련 디버그 정보를 출력
 * DEBUG_COMBAT_STATE: 전투 상태 변경 관련 디버그 정보를 출력
 * DEBUG_ENEMY_MANAGEMENT: 적 등록/해제 관련 디버그 정보를 출력
 * DEBUG_MOVEMENT: 이동 관련 디버그 정보를 출력
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Enemy;

[RequireComponent(typeof(CharacterMovement))]
public class AutoCombat : MonoBehaviour
{
    [Header("자동 전투 설정")]
    [SerializeField] private bool autoModeEnabled = false;
    [Tooltip("타겟 업데이트 간격 (초 단위)")]
    [SerializeField, Range(0.1f, 3f)] private float targetUpdateInterval = 0.8f;
    [SerializeField, Range(0.1f, 10f)] private float minDistanceToEnemy = 2f;
    [SerializeField, Range(0.1f, 10f)] private float maxSearchDistance = 10f;
    [SerializeField] private LayerMask enemyLayer;
    
    [Header("전역 탐색 설정")]
    [Tooltip("감지 범위 내에 적이 없을 때 전역 탐색 사용")]
    [SerializeField] private bool useGlobalSearch = true;
    [Tooltip("전역 탐색 시 이동 중 주변 감지 시도 간격")]
    [SerializeField, Range(1f, 10f)] private float globalSearchInterval = 2f;

    [Header("UI 설정")]
    [Tooltip("자동 모드일 때 비활성화할 조이스틱")]
    [SerializeField] private GameObject joystickObject;

    [Header("상태 표시")]
    [SerializeField] private bool drawDebugInfo = false;  // Inspector에서 필요할 때만 활성화하도록 설정
    
    [Header("컴포넌트 참조")]
    [SerializeField] private DetectionRange detectionRange;
    [SerializeField] private AttackRange attackRange;
    [SerializeField] private InputManager inputManager;
    [SerializeField] private Animator animator;
    
    private CharacterMovement characterMovement;
    private GameObject currentTarget;
    private bool isTargetTracking = false;
    private Vector2 movementInput = Vector2.zero;
    private bool isGlobalSearching = false;
    private float globalSearchTimer = 0f;
    private float targetCheckTimer = 0f;
    private Vector3 lastPosition;
    private float inactivityTimer = 0f;
    private const float MAX_INACTIVITY_TIME = 10.0f; // 비활성 시간 크게 증가 (2에서 10으로)
    private const float MOVEMENT_THRESHOLD = 0.1f; // 임계값 증가
    
    // 실시간 위치 업데이트 캐싱 변수
    private Vector3 targetPosition;
    private float targetPositionUpdateTimer = 0f;
    private const float TARGET_POSITION_UPDATE_INTERVAL = 0.2f; // 타겟 위치 업데이트 간격
    
    // 타겟 ID 캐싱
    private int currentTargetID = -1;

    // 이동 안정화 변수
    private Vector2 targetDirection = Vector2.zero;
    private Vector2 smoothedDirection = Vector2.zero;
    private float directionSmoothTime = 0.3f;
    private Vector2 directionSmoothVelocity;
    
    private const float ACTIVITY_THRESHOLD = 0.1f;

    private float validationTimer;
    private float lastAttackTime;
    private bool isAttacking;
    private Coroutine attackDelayCoroutine;

    [Header("자동 전투 설정")]
    [SerializeField] private float validationInterval = 1f;
    [SerializeField] private float attackAnimationDelay = 0.1f;

    private Vector3 lastDebuggedPosition;
    private Vector3 lastDebuggedTargetPosition;
    private bool hasLoggedInitialSearch = false;

    private static HashSet<GameObject> allEnemiesInWorld = new HashSet<GameObject>();
    private static bool staticDebugEnabled = false;  // static 디버그 플래그 추가

    private void Awake()
    {
        characterMovement = GetComponent<CharacterMovement>();
        
        if (detectionRange == null)
        {
            detectionRange = GetComponentInChildren<DetectionRange>();
            if (detectionRange == null)
            {
                #if DEBUG_COMPONENT_NOT_FOUND
                Debug.LogError("DetectionRange가 없습니다!");
                #endif
            }
        }
        
        if (attackRange == null)
        {
            attackRange = GetComponentInChildren<AttackRange>();
            if (attackRange == null)
            {
                #if DEBUG_COMPONENT_NOT_FOUND
                Debug.LogError("AttackRange가 없습니다!");
                #endif
            }
        }
        
        if (inputManager == null)
        {
            #if DEBUG_COMPONENT_NOT_FOUND
            Debug.LogError("InputManager를 찾을 수 없습니다!");
            #endif
        }
        
        if (characterMovement == null)
        {
            #if DEBUG_COMPONENT_NOT_FOUND
            Debug.LogError("CharacterMovement 컴포넌트를 찾을 수 없습니다!");
            #endif
        }
        
        lastPosition = transform.position;
        staticDebugEnabled = drawDebugInfo;  // 인스턴스의 디버그 설정을 static 플래그에 반영
    }

    private void Start()
    {
        if (inputManager == null)
        {
            #if DEBUG_COMPONENT_NOT_FOUND
            Debug.LogError("InputManager를 찾을 수 없습니다! 자동 전투 기능이 작동하지 않을 수 있습니다.");
            #endif
        }
        
        if (joystickObject == null)
        {
            joystickObject = GameObject.FindGameObjectWithTag("Joystick");
            if (joystickObject == null)
            {
                #if DEBUG_COMPONENT_NOT_FOUND
                Debug.LogWarning("조이스틱 오브젝트를 찾을 수 없습니다. 자동 모드에서 조이스틱 비활성화 기능이 동작하지 않을 수 있습니다.");
                #endif
            }
        }
        
        lastPosition = transform.position;
        inactivityTimer = 0f;
        
        targetCheckTimer = targetUpdateInterval;
        
        // 시작할 때 현재 씬의 모든 적을 등록
        GameObject[] existingEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in existingEnemies)
        {
            RegisterEnemy(enemy);
        }
        
        #if DEBUG_INITIALIZATION
        Debug.Log("AutoCombat 초기화 완료");
        Debug.Log($"[시작] 등록된 적 수: {allEnemiesInWorld.Count}");
        #endif
    }
    
    private void Update()
    {
        if (!autoModeEnabled) return;
        
        targetCheckTimer += Time.deltaTime;
        if (targetCheckTimer >= targetUpdateInterval)
        {
            targetCheckTimer = 0f;
            UpdateTarget();
        }
        
        if (isGlobalSearching)
        {
            globalSearchTimer += Time.deltaTime;
            if (globalSearchTimer >= globalSearchInterval)
            {
                globalSearchTimer = 0f;
                CheckForNearbyEnemiesDuringGlobalSearch();
            }
        }
        
        UpdateTargetPosition();
        UpdateMovement();
    }
    
    private void ProcessMovementTowardsTarget()
    {
        if (currentTarget == null) return;
        
        Vector3 targetPosition = currentTarget.transform.position;
        Vector3 directionToTarget = targetPosition - transform.position;
        float distanceToTarget = directionToTarget.magnitude;
        
        if (distanceToTarget <= minDistanceToEnemy)
        {
            targetDirection = Vector2.zero;
            
            if (animator != null)
            {
                animator.SetBool("Attack", true);
                animator.SetBool("Walk", false);
            }
            
            #if DEBUG_MOVEMENT
            Debug.Log($"공격 범위 내 도달: 거리={distanceToTarget:F2}");
            #endif
        }
        else
        {
            if (animator != null)
            {
                animator.SetBool("Attack", false);
            }
            
            Vector3 normalizedDirection = directionToTarget.normalized;
            targetDirection = new Vector2(normalizedDirection.x, normalizedDirection.z);
            
            #if DEBUG_MOVEMENT
            Debug.Log($"타겟으로 이동 중: 거리={distanceToTarget:F2}, 방향={targetDirection}");
            #endif
        }
    }
    
    private void CheckForNearbyEnemiesDuringGlobalSearch()
    {
        if (allEnemiesInWorld.Count == 0)
        {
            if (drawDebugInfo)
            {
                Debug.Log("[전역 탐색] 월드에 적이 없습니다.");
            }
            return;
        }

        GameObject nearestEnemy = null;
        float nearestDistance = float.MaxValue;
        Vector3 currentPos = transform.position;

        foreach (GameObject enemy in allEnemiesInWorld)
        {
            if (enemy == null) continue;
            
            float distance = Vector3.Distance(currentPos, enemy.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestEnemy = enemy;
            }
        }

        if (nearestEnemy != null)
        {
            currentTarget = nearestEnemy;
            Vector3 direction = (nearestEnemy.transform.position - currentPos);
            direction.y = 0;
            Vector2 input = new Vector2(direction.x, direction.z).normalized;
            
            if (drawDebugInfo)
            {
                Debug.Log($"[전역 탐색] 가장 가까운 적 발견: {nearestEnemy.name}, 거리: {nearestDistance:F2}, 입력: {input}");
            }
            
            inputManager?.SetVirtualInput(input);
        }
    }
    
    private void UpdateTargetPosition()
    {
        if (currentTarget != null)
        {
            targetPosition = currentTarget.transform.position;
        }
    }
    
    private void SetNewTarget(GameObject target)
    {
        if (target == null) return;
        
        currentTarget = target;
        isTargetTracking = true;
        
        targetPosition = target.transform.position;
        targetPositionUpdateTimer = 0f;
        
        inactivityTimer = 0f;
    }
    
    private void ResetTarget()
    {
        currentTarget = null;
        isTargetTracking = false;
        targetCheckTimer = 0f;
        globalSearchTimer = 0f;
        StopMovement();
        
        if (drawDebugInfo)
        {
            Debug.Log("타겟 리셋");
        }
    }
    
    private void UpdateTarget()
    {
        if (isTargetTracking && currentTarget != null && currentTarget.activeInHierarchy) return;
        
        var detectedEnemiesCopy = new HashSet<GameObject>(detectionRange.GetDetectedEnemies());
        if (detectedEnemiesCopy.Count > 0)
        {
            isGlobalSearching = false;
            GameObject firstEnemy = detectedEnemiesCopy.First();
            SetNewTarget(firstEnemy);
            
            if (drawDebugInfo)
            {
                Debug.Log($"새로운 타겟 설정: {firstEnemy.name}");
            }
        }
        else if (useGlobalSearch)
        {
            FindEnemyGlobally();
        }
        else
        {
            if (drawDebugInfo)
            {
                Debug.Log("전역 탐색 비활성화: 새 타겟을 찾지 않습니다.");
            }
            ResetTarget();
        }
    }
    
    private void FindEnemyGlobally()
    {
        if (!isGlobalSearching)
        {
            isGlobalSearching = true;
            globalSearchTimer = 0f;
            
            if (drawDebugInfo)
            {
                Debug.Log($"전역 탐색 시작: 현재 월드 내 총 적 수: {allEnemiesInWorld.Count}");
            }
        }
        
        if (allEnemiesInWorld.Count > 0)
        {
            // 가장 가까운 적 찾기
            GameObject nearestEnemy = null;
            float nearestDistance = float.MaxValue;
            Vector3 currentPos = transform.position;
            
            foreach (GameObject enemy in allEnemiesInWorld)
            {
                if (!enemy.activeInHierarchy) continue;
                
                float distance = Vector3.Distance(currentPos, enemy.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestEnemy = enemy;
                }
            }
            
            if (nearestEnemy != null)
            {
                isGlobalSearching = false;
                SetNewTarget(nearestEnemy);
                
                if (drawDebugInfo)
                {
                    Debug.Log($"[전역 탐색] 가장 가까운 적 발견: {nearestEnemy.name}, 거리: {nearestDistance}");
                }
            }
        }
    }
    
    private void StopMovement()
    {
        movementInput = Vector2.zero;
        
        if (inputManager != null)
        {
            inputManager.SetVirtualInput(movementInput);
        }
        
        if (drawDebugInfo)
        {
            Debug.Log("이동 중지");
        }
    }
    
    public void SetAutoMode(bool enabled)
    {
        autoModeEnabled = enabled;
        hasLoggedInitialSearch = false;
        lastDebuggedPosition = transform.position;
        lastDebuggedTargetPosition = Vector3.zero;
        
        if (enabled)
        {
            validationTimer = validationInterval;
            joystickObject?.SetActive(false);
            
            detectionRange?.ValidateDetectedEnemies();
            attackRange?.ValidateEnemiesInRange();
            
            var detectedEnemies = detectionRange.GetDetectedEnemies();
            if (detectedEnemies.Count == 0 && useGlobalSearch)
            {
                isGlobalSearching = true;
                globalSearchTimer = 0f;
                #if DEBUG_AUTO_MODE
                Debug.Log("자동 모드 활성화: 적이 없어 월드 탐색을 시작합니다.");
                #endif
            }
            else if (detectedEnemies.Count > 0)
            {
                SetNewTarget(detectedEnemies.First());
            }
            
            ValidateCurrentState();
            #if DEBUG_AUTO_MODE
            Debug.Log("[AutoCombat] 자동 모드 활성화 - 현재 상태 검증");
            #endif
        }
        else
        {
            ResetTarget();
            joystickObject?.SetActive(true);
            isAttacking = false;
            
            if (attackDelayCoroutine != null)
            {
                StopCoroutine(attackDelayCoroutine);
            }
            
            #if DEBUG_AUTO_MODE
            Debug.Log("[AutoCombat] 자동 모드 비활성화");
            #endif
        }
        
        #if DEBUG_AUTO_MODE
        Debug.Log($"자동 모드 {(enabled ? "활성화" : "비활성화")}");
        #endif
    }
    
    private void ToggleJoystickVisibility(bool show)
    {
        if (joystickObject != null && joystickObject.activeSelf != show)
        {
            joystickObject.SetActive(show);
            
            if (drawDebugInfo)
            {
                Debug.Log($"조이스틱 UI {(show ? "표시" : "숨김")}");
            }
        }
    }
    
    public void SetGlobalSearchEnabled(bool enabled)
    {
        if (useGlobalSearch == enabled) return;
        
        useGlobalSearch = enabled;
        
        if (autoModeEnabled && currentTarget == null)
        {
            targetCheckTimer = targetUpdateInterval;
        }
        
        if (drawDebugInfo)
        {
            Debug.Log($"전역 탐색 {(enabled ? "활성화" : "비활성화")}");
        }
    }
    
    public void SetTarget(GameObject target)
    {
        if (target != null)
        {
            SetNewTarget(target);
            isGlobalSearching = false;
        }
    }
    
    public bool IsAutoModeEnabled()
    {
        return autoModeEnabled;
    }
    
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.transform.position);
            
            Gizmos.DrawWireSphere(currentTarget.transform.position, 0.3f);
        }
        
        if (movementInput.magnitude > 0.1f)
        {
            Gizmos.color = Color.blue;
            Vector3 moveDirection = new Vector3(movementInput.x, 0, -movementInput.y).normalized;
            Gizmos.DrawRay(transform.position, moveDirection * 2f);
        }
    }
    
    private void UpdateMovement()
    {
        if (currentTarget != null)
        {
            Vector3 currentPos = transform.position;
            Vector3 targetPos = currentTarget.transform.position;
            float distanceToTarget = Vector3.Distance(currentPos, targetPos);
            
            if (distanceToTarget > minDistanceToEnemy)
            {
                // XZ 평면에서의 방향을 입력값으로 변환
                Vector3 direction = targetPos - currentPos;
                Vector2 input = new Vector2(direction.x, direction.z).normalized;
                
                if (drawDebugInfo && Vector3.Distance(currentPos, lastDebuggedPosition) > 1f)
                {
                    Debug.Log($"[타겟 추적] 현재: {currentPos}, 목표: {targetPos}");
                    Debug.Log($"[입력값] 방향: {direction}, 정규화된 입력: {input}");
                    lastDebuggedPosition = currentPos;
                }
                
                // InputManager에 입력값 전달
                inputManager?.SetVirtualInput(input);
            }
            else
            {
                inputManager?.SetVirtualInput(Vector2.zero);
            }
        }
    }
    
    public void OnEnemyDetected(GameObject enemy)
    {
        if (!autoModeEnabled) return;
        
        if (currentTarget == null)
        {
            currentTarget = enemy;
        }
    }
    
    public void OnEnemyLost(GameObject enemy)
    {
        if (enemy == currentTarget)
        {
            // DetectionRange.GetNearestEnemy 대신 allEnemiesInWorld에서 찾기
            GameObject newTarget = null;
            float nearestDistance = float.MaxValue;
            Vector3 currentPos = transform.position;

            foreach (GameObject potentialTarget in allEnemiesInWorld)
            {
                if (potentialTarget == null) continue;
                
                float distance = Vector3.Distance(currentPos, potentialTarget.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    newTarget = potentialTarget;
                }
            }

            currentTarget = newTarget;
            
            if (currentTarget == null && drawDebugInfo)
            {
                Debug.Log("감지된 적이 없습니다.");
            }
        }
    }
    
    public void OnEnemyEnteredAttackRange(GameObject enemy)
    {
        if (!autoModeEnabled) return;
        
        if (enemy == currentTarget)
        {
            StartCoroutine(StartAttackWithDelay());
            if (drawDebugInfo)
            {
                Debug.Log($"[AutoCombat] 적 공격 범위 진입: {enemy.name}");
            }
        }
    }
    
    public void OnEnemyExitedAttackRange(GameObject enemy)
    {
        if (!autoModeEnabled) return;
        
        if (enemy == currentTarget)
        {
            // 공격 중지
            if (animator != null)
            {
                animator.SetBool("Attack", false);
                
                // 이동 중이면 Walk, 아니면 Idle
                if (characterMovement != null && characterMovement.MovementDelta.magnitude > 0.1f)
                {
                    animator.SetBool("Walk", true);
                    animator.SetBool("Idle", false);
                }
                else
                {
                    animator.SetBool("Walk", false);
                    animator.SetBool("Idle", true);
                }
            }
            
            if (drawDebugInfo)
            {
                Debug.Log($"[AutoCombat] 타겟이 공격 범위를 벗어남: {enemy.name}");
            }
        }
    }
    
    private IEnumerator DelayedAttackStop()
    {
        yield return new WaitForSeconds(attackAnimationDelay);
        
        if (!attackRange.HasEnemiesInRange())
        {
            animator?.SetBool("Attack", false);
            isAttacking = false;
        }
    }

    private void ValidateCurrentState()
    {
        // 현재 AttackRange 내에 있는 적 체크
        if (attackRange != null && attackRange.HasEnemiesInRange())
        {
            // 공격 범위 내 적이 있으면 즉시 공격 상태로
            StartCoroutine(StartAttackWithDelay());
            if (drawDebugInfo)
            {
                Debug.Log("[AutoCombat] 공격 범위 내 적 발견 - 공격 시작");
            }
        }
        // DetectionRange 내 적 체크
        else if (detectionRange != null && detectionRange.GetDetectedEnemies().Count > 0)
        {
            // 감지 범위 내 적이 있으면 이동 상태로
            animator?.SetBool("Walk", true);
            animator?.SetBool("Attack", false);
            if (drawDebugInfo)
            {
                Debug.Log("[AutoCombat] 감지 범위 내 적 발견 - 이동 시작");
            }
        }
    }

    private IEnumerator StartAttackWithDelay()
    {
        if (drawDebugInfo)
        {
            Debug.Log($"[AutoCombat] 공격 시작 대기 중... (딜레이: {attackAnimationDelay}초)");
        }
        
        yield return new WaitForSeconds(attackAnimationDelay);
        
        if (animator != null && currentTarget != null)
        {
            animator.SetBool("Attack", true);
            animator.SetBool("Walk", false);
            
            if (drawDebugInfo)
            {
                Debug.Log("[AutoCombat] 공격 애니메이션 시작");
            }
        }
    }

    // Enemy 프리팹에서 호출할 정적 메서드들
    public static void RegisterEnemy(GameObject enemy)
    {
        if (enemy != null && enemy.CompareTag("Enemy"))
        {
            allEnemiesInWorld.Add(enemy);
            #if DEBUG_ENEMY_MANAGEMENT
            Debug.Log($"[전역 적 관리] 적 등록: {enemy.name}, 총 적 수: {allEnemiesInWorld.Count}");
            #endif
        }
    }
    
    public static void UnregisterEnemy(GameObject enemy)
    {
        if (allEnemiesInWorld.Remove(enemy))
        {
            #if DEBUG_ENEMY_MANAGEMENT
            Debug.Log($"[전역 적 관리] 적 제거: {enemy.name}, 남은 적 수: {allEnemiesInWorld.Count}");
            #endif
        }
    }

    private void OnValidate()
    {
        staticDebugEnabled = drawDebugInfo;  // Inspector에서 값이 변경될 때도 반영
    }

    public Animator Animator => animator; // 추가: AttackRange에서 접근하기 위한 프로퍼티

    public bool DrawDebugInfo => drawDebugInfo;
} 