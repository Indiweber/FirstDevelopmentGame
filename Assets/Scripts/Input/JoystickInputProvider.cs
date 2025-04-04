// #define DEBUG_COMPONENT_NOT_FOUND
// #define DEBUG_INPUT_UPDATE

/* 디버그 정의
 * DEBUG_COMPONENT_NOT_FOUND: 필수 컴포넌트를 찾지 못했을 때 에러를 출력
 * DEBUG_INPUT_UPDATE: 입력값 업데이트 관련 디버그 정보를 출력
 */

using UnityEngine;

public class JoystickInputProvider : MonoBehaviour, InputProvider
{
    [SerializeField] private JoystickController joystickController;
    [SerializeField] private float deadZone = 0.1f;
    
    public Vector2 MovementInput 
    { 
        get { return joystickController != null ? joystickController.InputVector : Vector2.zero; } 
    }
    
    public bool HasInput 
    { 
        get { return MovementInput.magnitude > deadZone; } 
    }
    
    private void Start()
    {
        if (joystickController == null)
        {
            #if DEBUG_COMPONENT_NOT_FOUND
            Debug.LogError("JoystickController가 설정되지 않았습니다!");
            #endif
            enabled = false;
            return;
        }
    }

    public Vector2 GetMovementInput()
    {
        Vector2 input = joystickController.GetJoystickValue();
        #if DEBUG_INPUT_UPDATE
        if (input != Vector2.zero)
        {
            Debug.Log($"조이스틱 입력 감지: {input}");
        }
        #endif
        return input;
    }

    public bool IsAttackButtonPressed()
    {
        bool isPressed = MovementInput.magnitude > deadZone;
        #if DEBUG_INPUT_UPDATE
        if (isPressed)
        {
            Debug.Log("공격 버튼 눌림");
        }
        #endif
        return isPressed;
    }
} 