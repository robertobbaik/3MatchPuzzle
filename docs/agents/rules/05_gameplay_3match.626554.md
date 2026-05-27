# 3Match Gameplay Rules

## Board

보드는 2D Grid 개념을 가진 3D 표현물이다.

기본 좌표계:

```txt
X: Column
Y: Row
Z: Visual Depth or Height
```

게임 로직에서는 `X, Y`를 기준으로 처리한다.

## Tile

Tile은 최소한 다음 상태를 가진다.

- TypeId
- BoardPosition
- State
- IsMatched
- IsFalling
- IsLocked

## Tile State

```csharp
public enum TileState
{
    Idle,
    Selected,
    Swapping,
    Matched,
    Falling,
    Spawning,
    Locked
}
```

## Match Rule

기본 매치 조건:

- 같은 TypeId의 타일이 가로 또는 세로로 3개 이상 연속될 경우 Match
- 대각선 매치는 기본적으로 허용하지 않음
- 교차 매치는 하나의 결과 그룹으로 병합 가능

## Swap Rule

- 인접한 두 타일만 교환 가능
- 교환 후 Match가 발생하지 않으면 원위치
- 교환 중에는 추가 입력을 막는다

## Resolve Order

```txt
1. Player Input
2. Swap Request
3. Swap Validation
4. Match Detection
5. Matched Tile Clear
6. Falling
7. Refill
8. Chain Match Detection
9. Score Calculation
10. Return To Idle
```

## Combo Rule

- 연쇄 매치가 발생하면 Combo Count 증가
- 첫 매치는 Combo 1로 취급
- 낙하/리필 이후 자동 매치는 Chain으로 처리

## Random Rule

- 타일 생성은 시드 기반 Random 사용
- 초기 보드 생성 시 즉시 매치가 발생하지 않도록 한다
- 이동 가능한 수가 없는 보드는 Shuffle 또는 Regenerate 처리한다
