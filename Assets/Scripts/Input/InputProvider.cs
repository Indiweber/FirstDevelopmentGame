using UnityEngine;

public interface InputProvider
{
    Vector2 MovementInput { get; }
    bool HasInput { get; }
} 