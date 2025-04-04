// #define DEBUG_COMPONENT_NOT_FOUND
// #define DEBUG_MOVEMENT_STATE
// #define DEBUG_SETTINGS

/* 디버그 정의
 * DEBUG_COMPONENT_NOT_FOUND: 필수 컴포넌트를 찾지 못했을 때 에러를 출력
 * DEBUG_MOVEMENT_STATE: 이동 상태 변경 관련 디버그 정보를 출력
 * DEBUG_SETTINGS: 설정 관련 디버그 정보를 출력
 */

using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
public class CharacterMovement : MonoBehaviour
{
    [SerializeField] private MovementSettings settings;
    [SerializeField] private InputManager inputManager;
    [SerializeField] private Animator animator;
    
    [Range(0f, 20f)]
    [SerializeField] private float fallbackMoveSpeed = 5f;
    
    private Vector3 lastPosition;
    private bool isFacingRight = true;
    private Rigidbody rb;
    private Vector3 currentMoveDirection;
    private Vector3 targetVelocity;
    private Vector2 lastInput = Vector2.zero;
    private Vector3 smoothedVelocity;
    
    // 애니메이션 상태 변경 임계값
    private const float MOVEMENT_THRESHOLD = 0.1f;
    private const float INPUT_CHANGE_THRESHOLD = 0.05f;
    
    // 이동 안정화 파라미터
    private const float VELOCITY_SMOOTHING_TIME = 0.1f;
    private const float MAX_VELOCITY_CHANGE = 1.0f;
    
    // 이벤트
    public UnityEvent<bool> OnDirectionChanged;
    [SerializeField]
    private UnityEvent<bool> onMovementStateChanged = new UnityEvent<bool>();
    public UnityEvent<bool> OnMovementStateChanged => onMovementStateChanged;
    
    private bool isMoving = false;
    
    public Vector3 MovementDelta { get { return transform.position - lastPosition; } }
    
    // MovementSettings 접근자 추가
    public MovementSettings Settings => settings;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            #if DEBUG_COMPONENT_NOT_FOUND
            Debug.LogError("Rigidbody 컴포넌트를 찾을 수 없습니다!");
            #endif
            return;
        }
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;  // Y축 위치와 회전 제한
        rb.interpolation = RigidbodyInterpolation.Interpolate;  // 부드러운 이동을 위한 보간 설정
        
        if (settings == null)
        {
            #if DEBUG_SETTINGS
            Debug.LogWarning("MovementSettings가 할당되지 않았습니다. 기본값을 사용합니다.");
            #endif
        }
        
        if (inputManager == null)
        {
            inputManager = GetComponent<InputManager>();
            if (inputManager == null)
            {
                Debug.LogError("InputManager를 찾을 수 없습니다!");
            }
        }
        
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError("Animator를 찾을 수 없습니다!");
            }
        }
    }
    
    private void Start()
    {
        lastPosition = transform.position;
        currentMoveDirection = Vector3.forward;
        smoothedVelocity = Vector3.zero;
    }
    
    private void FixedUpdate()
    {
        MoveCharacter();
    }
    
    private void Update()
    {
        // 이전 위치와 현재 위치의 차이로 이동 계산
        Vector3 currentDelta = transform.position - lastPosition;
        
        // 애니메이션 상태 업데이트 - 실제 위치 변화 기반
        UpdateAnimationState(currentDelta.magnitude);
        
        // 현재 위치 저장
        lastPosition = transform.position;
    }
    
    private void MoveCharacter()
    {
        if (inputManager == null) return;
        
        Vector2 input = inputManager.GetMovementInput();
        
        // 디버그 로그 추가
        if (input.magnitude > MOVEMENT_THRESHOLD)
        {
            Debug.Log($"이동 입력 감지: input={input}, magnitude={input.magnitude:F2}");
        }
        
        // 입력 변화가 임계값보다 작으면 이전 입력 사용 (작은 떨림 방지)
        if (Vector2.Distance(input, lastInput) < INPUT_CHANGE_THRESHOLD && input.magnitude > 0)
        {
            input = lastInput;
        }
        else
        {
            lastInput = input;
        }
        
        if (input.magnitude > MOVEMENT_THRESHOLD)
        {
            // 이동 방향 계산 (z축이 앞뒤 방향)
            Vector3 moveDirection = new Vector3(input.x, 0, input.y).normalized;
            currentMoveDirection = moveDirection;
            
            // 목표 속도 설정
            float currentSpeed = settings != null ? settings.moveSpeed : fallbackMoveSpeed;
            targetVelocity = moveDirection * currentSpeed;
            
            // X 입력값에 따라 회전
            UpdateRotation(input.x);
            
            // 디버그 로그
            //Debug.Log($"캐릭터 이동: direction={moveDirection}, speed={currentSpeed}, velocity={targetVelocity}");
        }
        else
        {
            targetVelocity = Vector3.zero;
        }
        
        // 속도 직접 적용 (y축 속도는 유지)
        rb.velocity = new Vector3(targetVelocity.x, rb.velocity.y, targetVelocity.z);
    }
    
    private void UpdateAnimationState(float deltaMovement)
    {
        // 실제 위치 변화량이나 입력 상태를 기준으로 이동 여부 판단
        bool isCurrentlyMoving = deltaMovement > 0.001f || 
                               (inputManager != null && inputManager.GetMovementInput().magnitude > MOVEMENT_THRESHOLD);
        
        if (isCurrentlyMoving != isMoving)
        {
            isMoving = isCurrentlyMoving;
            onMovementStateChanged.Invoke(isMoving);
            
            if (animator != null)
            {
                animator.SetBool("Walk", isMoving);
                Debug.Log($"이동 상태 변경: isMoving={isMoving}, deltaMovement={deltaMovement}");
            }
        }
    }
    
    private void UpdateRotation(float xInput)
    {
        if (Mathf.Abs(xInput) > MOVEMENT_THRESHOLD)
        {
            bool shouldFaceRight = xInput > 0;  // xInput이 양수일 때 오른쪽 방향으로 회전
            if (shouldFaceRight != isFacingRight)
            {
                isFacingRight = shouldFaceRight;
                float rotationY = isFacingRight ? 0f : -180f;  // 오른쪽: 0도, 왼쪽: -180도
                transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
                OnDirectionChanged?.Invoke(isFacingRight);
                
                // 디버그 로그 추가
                Debug.Log($"캐릭터 회전: {(isFacingRight ? "오른쪽" : "왼쪽")}, Y값: {rotationY}");
            }
        }
    }
    
    public Vector3 GetPositionDelta()
    {
        return transform.position - lastPosition;
    }

    public void SetMovementState(bool isMoving)
    {
        if (this.isMoving != isMoving)
        {
            this.isMoving = isMoving;
            OnMovementStateChanged?.Invoke(isMoving);
            
            #if DEBUG_MOVEMENT_STATE
            Debug.Log($"이동 상태 변경: {isMoving}");
            #endif
        }
    }
} 