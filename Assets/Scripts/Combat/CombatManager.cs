using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
public class CombatManager : MonoBehaviour
{
    [Header("공격 설정")]
    [SerializeField] private AttackTrigger attackTrigger;
    [SerializeField] private string attackAnimationParameter = "Attack";
    [SerializeField] private float attackCooldown = 1.0f;

    [Header("디버그")]
    [SerializeField] private bool debugEnabled = false;
    
    private Animator animator;
    private CharacterAnimator characterAnimator;
    private bool canAttack = true;
    private GameObject currentTarget;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        characterAnimator = GetComponent<CharacterAnimator>();
        
        if (attackTrigger == null)
        {
            attackTrigger = GetComponentInChildren<AttackTrigger>();
            if (attackTrigger == null)
            {
                Debug.LogError("AttackTrigger를 찾을 수 없습니다. 자식 오브젝트에 추가해주세요.");
            }
        }
        
        if (characterAnimator == null)
        {
            characterAnimator = GetComponent<CharacterAnimator>();
            if (characterAnimator == null)
            {
                Debug.Log("CharacterAnimator를 찾을 수 없습니다. Animator를 직접 제어합니다.");
            }
        }
    }
    
    private void OnEnable()
    {
        if (attackTrigger != null)
        {
            attackTrigger.OnEnemyEnter.AddListener(HandleEnemyEnter);
            attackTrigger.OnEnemyExit.AddListener(HandleEnemyExit);
        }
    }
    
    private void OnDisable()
    {
        if (attackTrigger != null)
        {
            attackTrigger.OnEnemyEnter.RemoveListener(HandleEnemyEnter);
            attackTrigger.OnEnemyExit.RemoveListener(HandleEnemyExit);
        }
    }

    private void HandleEnemyEnter(GameObject enemy)
    {
        currentTarget = enemy;
        
        if (canAttack)
        {
            StartAttack();
        }
        
        if (debugEnabled)
        {
            Debug.Log($"[CombatManager] 적 감지, 공격 시작: {enemy.name}");
        }
    }
    
    private void HandleEnemyExit(GameObject enemy)
    {
        // 현재 타겟이 범위를 벗어난 경우에만 처리
        if (currentTarget == enemy)
        {
            StopAttack();
            currentTarget = null;
            
            if (debugEnabled)
            {
                Debug.Log("[CombatManager] 적이 범위를 벗어남, 공격 중지");
            }
        }
    }
    
    private void StartAttack()
    {
        if (canAttack)
        {
            // CharacterAnimator가 있으면 사용하고, 없으면 직접 Animator 사용
            if (characterAnimator != null)
            {
                characterAnimator.SetAttackAnimation(true);
            }
            else if (animator != null)
            {
                animator.SetBool(attackAnimationParameter, true);
            }
            
            StartCoroutine(AttackCooldown());
            
            if (debugEnabled)
            {
                Debug.Log("[CombatManager] 공격 애니메이션 재생");
            }
        }
    }
    
    private void StopAttack()
    {
        // CharacterAnimator가 있으면 사용하고, 없으면 직접 Animator 사용
        if (characterAnimator != null)
        {
            characterAnimator.SetAttackAnimation(false);
        }
        else if (animator != null)
        {
            animator.SetBool(attackAnimationParameter, false);
        }
        
        if (debugEnabled)
        {
            Debug.Log("[CombatManager] 공격 애니메이션 중지");
        }
    }
    
    private IEnumerator AttackCooldown()
    {
        canAttack = false;
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
        
        // 쿨다운 후 적이 여전히 범위 내에 있다면 다시 공격
        if (currentTarget != null)
        {
            StartAttack();
        }
    }
} 