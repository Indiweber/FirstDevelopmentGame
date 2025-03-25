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
    
    private void Awake()
    {
        if (joystickController == null)
        {
            joystickController = FindObjectOfType<JoystickController>();
            if (joystickController == null)
            {
                Debug.LogError("JoystickController를 찾을 수 없습니다! JoystickController 컴포넌트를 TouchControl에 추가하세요.");
            }
        }
    }
} 