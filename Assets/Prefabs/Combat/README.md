# 전투 시스템 설정 방법

## 공격 범위 설정하기

1. 플레이어 캐릭터의 자식으로 빈 게임 오브젝트 생성 (이름: Hitbox)
2. 해당 오브젝트에 Box Collider 컴포넌트 추가
3. Box Collider의 Is Trigger 속성을 체크
4. 크기와 위치를 적절히 조정 (캐릭터 앞쪽에 위치하도록)
5. 필요한 경우 Edit Collider 버튼을 눌러 세부 조정

## 컴포넌트 설정하기

1. 플레이어 캐릭터에 HitboxController 컴포넌트 추가
2. 플레이어 캐릭터에 CombatController 컴포넌트 추가
3. Hitbox 오브젝트를 HitboxController의 hitboxObject 필드에 할당

## 애니메이션 이벤트 설정하기

1. 공격 애니메이션 클립을 열고 Animation 창에서 이벤트 추가
2. 공격 동작이 시작되는 프레임에 ActivateHitbox 이벤트 추가 (함수: ActivateHitbox)
3. 공격 동작이 끝나는 프레임에 DeactivateHitbox 이벤트 추가 (함수: DeactivateHitbox)
4. 애니메이터 컨트롤러에 Attack 트리거 파라미터 추가
5. Idle -> Attack, Attack -> Idle 트랜지션 설정

## 공격 버튼 설정하기

1. UI에 공격 버튼 추가
2. 버튼의 OnClick 이벤트에 InputManager 오브젝트와 OnAttackButtonPressed 함수 할당

## 트리거 설정 주의사항

- 적 오브젝트의 Tag를 "Enemy"로 설정해야 합니다.
- 적 오브젝트에 Collider가 있어야 감지됩니다.
- 히트박스의 크기를 너무 크게 설정하면 멀리서도 공격하게 되므로 적절히 조절하세요.

## 디버깅 팁

- HitboxController와 CombatController의 debugEnabled 옵션을 활성화하면 콘솔에 디버그 정보가 표시됩니다.
- 씬 뷰에서 Gizmos를 활성화하면 히트박스 콜라이더를 시각적으로 확인할 수 있습니다. 