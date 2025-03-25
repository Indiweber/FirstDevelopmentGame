using UnityEngine;

/// <summary>
/// 가상 입력을 받을 수 있는 컴포넌트를 위한 인터페이스
/// 주로 AI나 자동화된 컨트롤러에서 InputManager에 입력값을 전달할 때 사용
/// </summary>
public interface IVirtualInputReceiver
{
    /// <summary>
    /// 가상 입력 벡터 설정
    /// </summary>
    /// <param name="input">이동 방향을 나타내는 2D 벡터 (x, y)</param>
    void SetVirtualInput(Vector2 input);
} 