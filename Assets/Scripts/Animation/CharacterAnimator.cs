using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Animator))]
public class CharacterAnimator : MonoBehaviour
{
    [Tooltip("애니메이션 파라미터 중 걷기 상태를 나타내는 파라미터 이름")]
    [SerializeField] private string walkParameterName = "Walk";
    [Tooltip("애니메이션 파라미터 중 공격 상태를 나타내는 파라미터 이름")]
    [SerializeField] private string attackParameterName = "Attack";
    
    [SerializeField] private CharacterMovement characterMovement;
    
    [SerializeField] private bool enableDebugLogs = false;  // 디버그 로그 기본값 false로 설정
    
    private Animator animator;
    
    private void Awake()
    {
        animator = GetComponent<Animator>();
        
        if (characterMovement == null)
        {
            characterMovement = GetComponent<CharacterMovement>();
            if (characterMovement == null)
            {
                characterMovement = GetComponentInParent<CharacterMovement>();
                if (characterMovement == null)
                {
                    Debug.LogError("CharacterMovement를 찾을 수 없습니다!");
                }
            }
        }
    }
    
    private void OnEnable()
    {
        if (characterMovement != null)
        {
            characterMovement.OnMovementStateChanged.AddListener(UpdateMovementAnimation);
        }
    }
    
    private void OnDisable()
    {
        if (characterMovement != null)
        {
            characterMovement.OnMovementStateChanged.RemoveListener(UpdateMovementAnimation);
        }
    }
    
    private void Start()
    {
        // 애니메이터 확인
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError("Animator 컴포넌트를 찾을 수 없습니다!");
                return;
            }
        }
        
        // 애니메이션 파라미터 확인
        AnimatorControllerParameter[] parameters = animator.parameters;
        bool hasWalkParameter = false;
        bool hasAttackParameter = false;
        
        foreach (var param in parameters)
        {
            if (param.name == walkParameterName && param.type == AnimatorControllerParameterType.Bool)
            {
                hasWalkParameter = true;
            }
            if (param.name == attackParameterName && param.type == AnimatorControllerParameterType.Bool)
            {
                hasAttackParameter = true;
            }
        }
        
        if (!hasWalkParameter)
        {
            Debug.LogError($"Animator에 '{walkParameterName}' Bool 파라미터가 없습니다!");
        }
        
        if (!hasAttackParameter)
        {
            Debug.LogError($"Animator에 '{attackParameterName}' Bool 파라미터가 없습니다!");
        }
    }
    
    private void Update()
    {
        // 이동 상태 실시간 체크 (디버그 로그는 조건부로만 출력)
        if (characterMovement != null && enableDebugLogs)
        {
            Vector3 movement = characterMovement.MovementDelta;
            if (movement.magnitude > 0.01f)  // 움직임이 있을 때만 로그 출력
            {
                if (animator != null && animator.gameObject.activeInHierarchy)
                {
                    AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                    Debug.Log($"캐릭터 이동 상태: 속도={movement.magnitude:F2}, 애니메이션={stateInfo.normalizedTime:F2}");
                }
            }
        }
    }
    
    public void UpdateMovementAnimation(bool isMoving)
    {
        if (animator != null && animator.gameObject.activeInHierarchy)
        {
            bool currentState = animator.GetBool(walkParameterName);
            if (currentState != isMoving)
            {
                animator.SetBool(walkParameterName, isMoving);
                if (enableDebugLogs)
                {
                    Debug.Log($"애니메이션 상태 변경: {isMoving}");
                }
            }
        }
    }
    
    // 공격 애니메이션을 재생하는 메서드
    public void SetAttackAnimation(bool isAttacking)
    {
        if (animator != null && animator.gameObject.activeInHierarchy)
        {
            animator.SetBool(attackParameterName, isAttacking);
            Debug.Log($"공격 애니메이션 상태 변경: {isAttacking}");
        }
    }
} 