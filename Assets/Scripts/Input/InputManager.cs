using UnityEngine;
using System.Collections.Generic;

public enum InputType
{
    Joystick,
    Virtual
}

public class InputManager : MonoBehaviour, IVirtualInputReceiver, IJoystickInput
{
    [Header("디버그")]
    [SerializeField] private bool enableDebugLogs = false;
    
    [Header("입력 우선순위")]
    [SerializeField] private InputType priorityInput = InputType.Joystick;
    [SerializeField] private bool useVirtualInput = true;
    [SerializeField] private bool useJoystick = true;
    
    [Header("조이스틱 참조")]
    [SerializeField] private JoystickController joystickController;
    
    [Header("추가 참조")]
    [SerializeField] private GameObject joystickObject;
    [SerializeField] private GameObject attackButtonObject;
    
    private Vector2 virtualInput = Vector2.zero;
    private bool _virtualInputEnabled = false;
    private bool joystickEnabled = true;
    private CombatController combatController;
    
    private void Awake()
    {
        // 조이스틱 컨트롤러가 없으면 찾기
        if (joystickController == null)
        {
            joystickController = FindObjectOfType<JoystickController>();
            if (joystickController == null && enableDebugLogs)
            {
                Debug.LogError("조이스틱 컨트롤러를 찾을 수 없습니다!");
            }
        }
        
        combatController = GetComponent<CombatController>();
        if (combatController == null && transform.parent != null)
        {
            combatController = transform.parent.GetComponent<CombatController>();
        }
        
        // 가상 입력 초기화
        virtualInput = Vector2.zero;
        _virtualInputEnabled = false;
        
        // 조이스틱 사용 설정
        useJoystick = true;
        priorityInput = InputType.Joystick;
        
        if (enableDebugLogs)
        {
            Debug.Log($"InputManager 초기화 - 조이스틱: {joystickController != null}, useJoystick: {useJoystick}");
        }
    }

    private void Start()
    {
        if (enableDebugLogs)
        {
            Debug.Log($"InputManager 시작 - 가상 입력: {useVirtualInput}, 조이스틱: {useJoystick}, 우선순위: {priorityInput}");
        }
    }

    private void Update()
    {
        // 가상 입력이 활성화된 경우 가상 입력 사용
        if (_virtualInputEnabled && priorityInput == InputType.Virtual)
        {
            // 입력이 있을 때만 로그 출력
            if (virtualInput.magnitude > 0.1f)
            {
                Debug.Log($"[가상 입력 사용 중] 값: {virtualInput}, 크기: {virtualInput.magnitude:F3}");
            }
        }
        else
        {
            // 가상 입력 비활성화 시 virtualInput 초기화
            if (virtualInput != Vector2.zero)
            {
                virtualInput = Vector2.zero;
            }
        }
    }

    public Vector2 GetMovementInput()
    {
        // 조이스틱 입력 처리 (우선순위가 Virtual이 아닐 때)
        if (useJoystick && joystickController != null && priorityInput != InputType.Virtual)
        {
            Vector2 joystickInput = joystickController.GetJoystickValue();
            if (enableDebugLogs && joystickInput.magnitude > 0.1f)
            {
                Debug.Log($"조이스틱 입력 감지: {joystickInput}, 크기: {joystickInput.magnitude}");
            }
            return joystickInput;
        }
        
        // 가상 입력 처리
        if (_virtualInputEnabled && priorityInput == InputType.Virtual)
        {
            if (enableDebugLogs && virtualInput.magnitude > 0.1f)
            {
                Debug.Log($"가상 입력 사용: {virtualInput}, 크기: {virtualInput.magnitude}");
            }
            return virtualInput;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"유효한 입력 없음 - 조이스틱: {useJoystick}, 컨트롤러: {joystickController != null}, 우선순위: {priorityInput}");
        }
        return Vector2.zero;
    }

    public Vector2 GetJoystickValue()
    {
        if (joystickController != null)
        {
            return joystickController.GetJoystickValue();
        }
        return Vector2.zero;
    }

    public void SetPriorityInputType(InputType inputType)
    {
        priorityInput = inputType;
        _virtualInputEnabled = (inputType == InputType.Virtual);
        
        if (enableDebugLogs)
        {
            Debug.Log($"입력 우선순위 변경: {inputType}, 가상 입력 활성화: {_virtualInputEnabled}");
        }
    }

    public InputType GetPriorityInput()
    {
        return priorityInput;
    }

    public void OnAttackButtonPressed()
    {
        if (combatController != null)
        {
            combatController.TryAttack();
            
            if (enableDebugLogs)
            {
                Debug.Log("공격 버튼 누름");
            }
        }
        else
        {
            Debug.LogWarning("CombatController를 찾을 수 없습니다.");
        }
    }

    public void SetVirtualInput(Vector2 input)
    {
        virtualInput = input;
        _virtualInputEnabled = true;
        
        if (enableDebugLogs && input.magnitude > 0.1f)
        {
            Debug.Log($"가상 입력 설정: {input}, 크기: {input.magnitude}");
        }
    }
} 