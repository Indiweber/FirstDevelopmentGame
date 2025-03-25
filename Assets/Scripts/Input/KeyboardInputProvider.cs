using UnityEngine;

public class KeyboardInputProvider : MonoBehaviour, InputProvider
{
    private Vector2 movementInput;
    
    public Vector2 MovementInput { get { return movementInput; } }
    public bool HasInput { get { return movementInput.magnitude > 0.1f; } }
    
    private void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        movementInput = new Vector2(horizontal, vertical);
        
        // 입력이 1을 초과하지 않도록 정규화
        if (movementInput.magnitude > 1f)
        {
            movementInput.Normalize();
        }
    }
} 