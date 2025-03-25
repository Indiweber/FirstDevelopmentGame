using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterMovement))]
[RequireComponent(typeof(EnemyDetector))]
public class AutoCombat : MonoBehaviour
{
    [Header("자동 전투 설정")]
    [SerializeField] private bool autoModeEnabled = false;
    [Tooltip("타겟 업데이트 간격 (초 단위)")]
    [SerializeField, Range(0.1f, 3f)] private float targetUpdateInterval = 0.8f;
    [SerializeField, Range(0.1f, 10f)] private float minDistanceToEnemy = 1.5f;
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
    [SerializeField] private bool drawDebugInfo = false;  // 디버그 정보 비활성화
    
    private EnemyDetector enemyDetector;
    private CharacterMovement characterMovement;
    private InputManager inputManager;
    private Animator animator;
    
    private Transform currentTarget;
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

    private void Awake()
    {
        characterMovement = GetComponent<CharacterMovement>();
        
        if (characterMovement == null)
        {
            Debug.LogError("CharacterMovement를 찾을 수 없습니다!");
            enabled = false;
            return;
        }
        
        // 이동 속도를 캐릭터 설정에서 가져옴
        if (characterMovement.Settings != null)
        {
            targetUpdateInterval = characterMovement.Settings.moveSpeed;
        }
        
        inputManager = GetComponent<InputManager>();
        if (inputManager == null)
        {
            Debug.LogError("InputManager를 찾을 수 없습니다!");
        }
        
        enemyDetector = GetComponent<EnemyDetector>();
        if (enemyDetector == null)
        {
            Debug.LogError("EnemyDetector를 찾을 수 없습니다!");
            enabled = false;
        }
    }

    private void Start()
    {
        // 필요한 컴포넌트 찾기
        animator = GetComponent<Animator>();
        
        if (inputManager == null)
        {
            Debug.LogError("InputManager를 찾을 수 없습니다! 자동 전투 기능이 작동하지 않을 수 있습니다.");
        }
        
        // 조이스틱 오브젝트가 지정되지 않았다면 찾기
        if (joystickObject == null)
        {
            joystickObject = GameObject.FindGameObjectWithTag("Joystick");
            if (joystickObject == null)
            {
                Debug.LogWarning("조이스틱 오브젝트를 찾을 수 없습니다. 자동 모드에서 조이스틱 비활성화 기능이 동작하지 않을 수 있습니다.");
            }
        }
        
        // 마지막 위치 초기화
        lastPosition = transform.position;
        inactivityTimer = 0f;
        
        // 시작 시 자동 모드 비활성화
        SetAutoMode(false);
        
        // 타겟 체크 타이머 초기화
        targetCheckTimer = targetUpdateInterval;
        
        if (drawDebugInfo)
        {
            Debug.Log("AutoCombat 초기화 완료");
        }
    }
    
    private void Update()
    {
        if (!autoModeEnabled) return;
        
        // 타이머 업데이트
        targetCheckTimer += Time.deltaTime;
        
        // 정기적으로 적 탐색 및 타겟 업데이트
        if (targetCheckTimer >= targetUpdateInterval)
        {
            targetCheckTimer = 0f;
            
            // 타겟 업데이트 로직 실행
            UpdateTarget();
            
            // 로그 메시지 추가
            if (drawDebugInfo)
            {
                Debug.Log($"자동 전투 타겟 체크: 현재 타겟={currentTarget?.name ?? "없음"}");
            }
        }
        
        // 타겟이 있다면 이동 및 공격 처리
        if (currentTarget != null)
        {
            ProcessMovementTowardsTarget();
        }
        else
        {
            // 타겟이 없을 때는 이동 입력 부드럽게 줄임
            targetDirection = Vector2.zero;
        }
        
        // 부드러운 방향 전환 적용
        smoothedDirection = Vector2.SmoothDamp(
            smoothedDirection, 
            targetDirection, 
            ref directionSmoothVelocity, 
            directionSmoothTime
        );
        
        // 부드러운 입력을 InputManager에 전달
        if (inputManager != null)
        {
            inputManager.SetVirtualInput(smoothedDirection);
            
            if (drawDebugInfo && smoothedDirection.magnitude > 0.1f)
            {
                Debug.Log($"가상 입력 전달: {smoothedDirection}, 크기: {smoothedDirection.magnitude:F2}");
            }
        }
    }
    
    private void ProcessMovementTowardsTarget()
    {
        if (currentTarget == null) return;
        
        // 타겟과의 거리 계산
        Vector3 targetPosition = currentTarget.position;
        Vector3 directionToTarget = targetPosition - transform.position;
        float distanceToTarget = directionToTarget.magnitude;
        
        // 공격 범위 내에 있는지 확인
        bool inAttackRange = distanceToTarget <= minDistanceToEnemy;
        
        if (inAttackRange)
        {
            // 공격 범위 내에 있으면 이동 중지
            targetDirection = Vector2.zero;
            
            if (drawDebugInfo)
            {
                Debug.Log($"자동 전투: 공격 범위 내 ({distanceToTarget:F2}m), 이동 중지");
            }
            
            // 여기에 공격 로직 추가 (나중에 구현)
        }
        else
        {
            // 이동 방향 계산 (x,z 평면에서)
            directionToTarget.y = 0;
            directionToTarget.Normalize();
            
            // 방향을 조이스틱 입력으로 변환 (x: 좌우, y: 상하)
            float inputX = directionToTarget.x;
            float inputY = directionToTarget.z;  // 반전 제거
            
            // 디버그 로그 추가
            if (drawDebugInfo)
            {
                Debug.Log($"자동 전투 이동 계산: directionToTarget={directionToTarget}, inputX={inputX}, inputY={inputY}");
            }
            
            // 새로운 목표 방향 설정
            targetDirection = new Vector2(inputX, inputY).normalized;
            
            // 이동 상태 확인 (비활성화)
            Vector3 movementDelta = characterMovement.GetPositionDelta();
            float moveDistance = movementDelta.magnitude;
            
            // 이동 중이면 타이머 초기화 (항상 타이머 초기화하여 타겟 리셋 방지)
            inactivityTimer = 0f;
            
            if (drawDebugInfo && targetCheckTimer == 0f)
            {
                Debug.Log($"자동 전투: 타겟으로 이동 중 ({distanceToTarget:F2}m), 입력: ({inputX:F2}, {inputY:F2})");
            }
        }
    }
    
    private void CheckForNearbyEnemiesDuringGlobalSearch()
    {
        // 전역 탐색 중에는 탐지하지 않도록 변경
        if (!isGlobalSearching || currentTarget == null) return;
        
        // 현재 타겟으로 이동 중인지 확인
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        
        // 이미 타겟에 충분히 가까워졌으면 탐지 중단
        if (distanceToTarget < maxSearchDistance * 0.5f) return;
        
        // 일정 시간마다 주변에 적이 있는지 다시 확인
        enemyDetector.DetectEnemies();
        Transform nearbyEnemy = enemyDetector.NearestEnemy;
        
        if (nearbyEnemy != null)
        {
            int nearbyEnemyID = nearbyEnemy.GetInstanceID();
            // 현재 타겟과 동일한 적이면 무시
            if (nearbyEnemyID == currentTargetID) return;
            
            // 새로 감지된 적이 현재 타겟보다 가까우면 타겟 변경
            float distanceToNearbyEnemy = Vector3.Distance(transform.position, nearbyEnemy.position);
            if (distanceToNearbyEnemy < distanceToTarget * 0.7f)
            {
                isGlobalSearching = false;
                SetNewTarget(nearbyEnemy);
                
                if (drawDebugInfo)
                {
                    Debug.Log($"전역 탐색 중 더 가까운 적 발견: {currentTarget.name}, 거리: {distanceToNearbyEnemy:F2}");
                }
            }
        }
    }
    
    private void UpdateTargetPosition()
    {
        if (currentTarget != null)
        {
            targetPosition = currentTarget.position;
        }
    }
    
    private void SetNewTarget(Transform target)
    {
        if (target == null) return;
        
        // 동일한 타겟인지 확인
        int targetID = target.GetInstanceID();
        if (targetID == currentTargetID) return;
        
        currentTarget = target;
        currentTargetID = targetID;
        isTargetTracking = true;
        
        // 타겟 위치 초기화
        targetPosition = currentTarget.position;
        targetPositionUpdateTimer = 0f;
        
        // 적을 찾았으므로 계속해서 탐지할 필요 없음
        if (enemyDetector != null)
        {
            enemyDetector.SetActiveSearch(false);
        }
        
        if (drawDebugInfo)
        {
            Debug.Log($"새로운 타겟 설정: {currentTarget.name}");
        }
        
        // 비활성 타이머 리셋
        inactivityTimer = 0f;
    }
    
    // 타겟 및 상태 리셋
    private void ResetTarget()
    {
        currentTarget = null;
        currentTargetID = -1;
        isTargetTracking = false;
        isGlobalSearching = false;
        targetCheckTimer = 0f; // 즉시 새 타겟 검색
        globalSearchTimer = 0f;
        StopMovement();
        
        // 타겟 리셋 시 적 탐지 활성화
        if (enemyDetector != null)
        {
            enemyDetector.SetActiveSearch(true);
        }
        
        if (drawDebugInfo)
        {
            Debug.Log("타겟 리셋");
        }
    }
    
    private void UpdateTarget()
    {
        // 이미 타겟 추적 중이면 새 타겟 검색 안함
        if (isTargetTracking && currentTarget != null) return;
        
        // 일반 감지 범위 내에서 적 찾기
        if (enemyDetector != null)
        {
            // 적 탐지 활성화
            enemyDetector.SetActiveSearch(true);
            // 적 탐지 강제 실행
            enemyDetector.DetectEnemies();
            Transform nearestEnemy = enemyDetector.NearestEnemy;
            
            if (nearestEnemy != null)
            {
                // 감지 범위 내에서 적을 찾음
                isGlobalSearching = false;
                SetNewTarget(nearestEnemy);
            }
            else if (useGlobalSearch && !isGlobalSearching)
            {
                // 감지 범위 내에 적이 없고 전역 탐색이 활성화된 경우
                FindEnemyGlobally();
            }
            else if (!useGlobalSearch)
            {
                // 전역 탐색을 사용하지 않는 경우 타겟 해제
                if (currentTarget != null && drawDebugInfo)
                {
                    Debug.Log("타겟을 놓쳤습니다. 새 타겟을 찾는 중...");
                }
                
                ResetTarget();
            }
        }
    }
    
    // 전역 탐색으로 가장 가까운 적을 찾는 메서드
    private void FindEnemyGlobally()
    {
        Transform globalEnemy = enemyDetector.FindNearestEnemyInWorld();
        
        if (globalEnemy != null)
        {
            SetNewTarget(globalEnemy);
            isGlobalSearching = true;
            globalSearchTimer = 0f;
            
            // 탐지 중지 (전역 탐색 중에는 필요 없음)
            enemyDetector.SetActiveSearch(false);
            
            if (drawDebugInfo)
            {
                Debug.Log($"전역 탐색으로 새 타겟 발견: {currentTarget.name}");
            }
        }
        else
        {
            // 전체 월드에서도 적을 찾지 못함
            if (drawDebugInfo)
            {
                Debug.Log("월드에 적이 존재하지 않습니다.");
            }
            
            ResetTarget();
        }
    }
    
    // 이동 중지 메서드
    private void StopMovement()
    {
        movementInput = Vector2.zero;
        
        // 입력 매니저에 가상 입력 중지 전달
        if (inputManager != null)
        {
            inputManager.SetVirtualInput(movementInput);
        }
        
        if (drawDebugInfo)
        {
            Debug.Log("이동 중지");
        }
    }
    
    // 자동 모드 설정
    public void SetAutoMode(bool enabled)
    {
        if (autoModeEnabled == enabled) return; // 이미 같은 상태면 아무것도 하지 않음
        
        autoModeEnabled = enabled;
        
        // 입력 매니저 확인 및 설정
        if (inputManager == null)
        {
            Debug.LogError("InputManager가 없어 자동 모드를 설정할 수 없습니다!");
            inputManager = GetComponent<InputManager>();
            if (inputManager == null)
            {
                return;
            }
        }
        
        // 자동 모드일 때는 가상 입력 우선
        inputManager.SetPriorityInputType(enabled ? InputType.Virtual : InputType.Joystick);
        
        // 활성화 시 가상 입력 초기화 (임의의 작은 값으로 시작)
        if (enabled)
        {
            inputManager.SetVirtualInput(new Vector2(0.01f, 0.01f));
        }
        
        // 디버그 로그
        Debug.Log($"자동 모드 {(enabled ? "활성화" : "비활성화")}, 입력 우선순위: {inputManager.GetPriorityInput()}");
        
        // 조이스틱 UI 토글
        ToggleJoystickVisibility(!enabled);
        
        if (enabled)
        {
            // 자동 모드 활성화 시
            targetCheckTimer = 0f; // 즉시 타겟 검색 시작
            lastPosition = transform.position;
            inactivityTimer = 0f;
            
            // 적 탐지 활성화
            if (enemyDetector != null)
            {
                enemyDetector.SetActiveSearch(true);
                // 즉시 적 탐지 수행
                enemyDetector.DetectEnemies();
            }
            
            // 즉시 타겟 업데이트 시도
            UpdateTarget();
        }
        else
        {
            // 자동 모드 비활성화 시
            ResetTarget();
        }
    }
    
    // 조이스틱 가시성 토글
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
    
    // 전역 탐색 설정
    public void SetGlobalSearchEnabled(bool enabled)
    {
        if (useGlobalSearch == enabled) return; // 이미 같은 상태면 아무것도 하지 않음
        
        useGlobalSearch = enabled;
        
        // 설정이 변경되었고 현재 타겟이 없는 경우 즉시 타겟 검색
        if (autoModeEnabled && currentTarget == null)
        {
            targetCheckTimer = targetUpdateInterval; // 다음 프레임에 타겟 검색
        }
        
        if (drawDebugInfo)
        {
            Debug.Log($"전역 탐색 {(enabled ? "활성화" : "비활성화")}");
        }
    }
    
    // 외부에서 타겟 직접 설정
    public void SetTarget(Transform target)
    {
        if (target != null)
        {
            SetNewTarget(target);
            isGlobalSearching = false;
        }
    }
    
    // 자동 모드 상태 확인
    public bool IsAutoModeEnabled()
    {
        return autoModeEnabled;
    }
    
    // 디버그용 기즈모
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        // 타겟이 있으면 연결선 표시
        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.position);
            
            // 타겟 위치에 구체 표시
            Gizmos.DrawWireSphere(currentTarget.position, 0.3f);
        }
        
        // 이동 방향 표시
        if (movementInput.magnitude > 0.1f)
        {
            Gizmos.color = Color.blue;
            Vector3 moveDirection = new Vector3(movementInput.x, 0, -movementInput.y).normalized;
            Gizmos.DrawRay(transform.position, moveDirection * 2f);
        }
    }
} 