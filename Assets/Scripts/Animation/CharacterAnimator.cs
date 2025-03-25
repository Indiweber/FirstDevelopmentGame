using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Animator))]
public class CharacterAnimator : MonoBehaviour
{
    [Tooltip("애니메이션 파라미터 중 걷기 상태를 나타내는 파라미터 이름")]
    [SerializeField] private string walkParameterName = "Walk";
    
    [SerializeField] private CharacterMovement characterMovement;
    
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
        
        foreach (var param in parameters)
        {
            if (param.name == walkParameterName && param.type == AnimatorControllerParameterType.Bool)
            {
                hasWalkParameter = true;
                break;
            }
        }
        
        if (!hasWalkParameter)
        {
            Debug.LogError($"Animator에 '{walkParameterName}' Bool 파라미터가 없습니다!");
        }
    }
    
    private void Update()
    {
        // 이동 상태 실시간 체크
        if (characterMovement != null)
        {
            // 이동 상태 확인
            Vector3 movement = characterMovement.MovementDelta;
            bool isMoving = movement.magnitude > 0.01f;
            
            // 애니메이션 상태 갱신
            if (animator != null && animator.gameObject.activeInHierarchy)
            {
                animator.SetBool(walkParameterName, isMoving);
                
                // 현재 애니메이션 상태 로깅
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                Debug.Log($"캐릭터 이동: {isMoving}, 애니메이션 상태: {stateInfo.normalizedTime}, 속도: {movement.magnitude}");
            }
        }
    }
    
    public void UpdateMovementAnimation(bool isMoving)
    {
        if (animator != null && animator.gameObject.activeInHierarchy)
        {
            animator.SetBool(walkParameterName, isMoving);
            Debug.Log($"애니메이션 상태 변경: {isMoving}");
        }
    }
} 