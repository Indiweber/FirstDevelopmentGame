using UnityEngine;

public interface IInputProvider
{
    Vector2 GetMovementInput();
    bool IsAttackButtonPressed();
} 