// #define DEBUG_COMPONENT_NOT_FOUND
// #define DEBUG_INPUT_RECEIVED

/* 디버그 정의
 * DEBUG_COMPONENT_NOT_FOUND: 필수 컴포넌트를 찾지 못했을 때 에러를 출력
 * DEBUG_INPUT_RECEIVED: 가상 입력 수신 관련 디버그 정보를 출력
 */

using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class Vector2Event : UnityEvent<Vector2> { }

public class VirtualInputReceiver : MonoBehaviour
{
    [SerializeField] private IInputProvider inputProvider;
    
    [Header("입력 이벤트")]
    public Vector2Event OnMovementInput = new Vector2Event();
    public UnityEvent OnAttackInput = new UnityEvent();

    private void Start()
    {
        if (inputProvider == null)
        {
            #if DEBUG_COMPONENT_NOT_FOUND
            Debug.LogError("InputProvider가 설정되지 않았습니다!");
            #endif
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        Vector2 movement = inputProvider.GetMovementInput();
        if (movement != Vector2.zero)
        {
            #if DEBUG_INPUT_RECEIVED
            Debug.Log($"이동 입력 수신: {movement}");
            #endif
            OnMovementInput.Invoke(movement);
        }

        if (inputProvider.IsAttackButtonPressed())
        {
            #if DEBUG_INPUT_RECEIVED
            Debug.Log("공격 입력 수신");
            #endif
            OnAttackInput.Invoke();
        }
    }
} 