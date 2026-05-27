# DOTS / ECS Rules

## Base Rule

DOTS는 반복 처리량이 많거나 데이터 중심 처리가 명확한 영역에 사용한다.

3Match에서 DOTS 우선 적용 대상:

1. Board Cell 데이터
2. Tile 상태 데이터
3. Match Detection
4. Tile Falling
5. Board Refill
6. Combo Chain Resolution

DOTS 비우선 대상:

1. UI
2. 버튼 입력
3. 단순 사운드 재생
4. 카메라 연출
5. 일회성 애니메이션 트리거

## Component Rule

ECS Component는 데이터만 가진다.

```csharp
public struct Tile : IComponentData
{
    public int TypeId;
}

public struct BoardPosition : IComponentData
{
    public int X;
    public int Y;
}
```

금지:

```csharp
public struct Tile : IComponentData
{
    public void Clear()
    {
    }
}
```

## System Rule

- System은 상태 변경의 단위를 명확히 가진다.
- System 이름은 처리 목적을 기준으로 작성한다.

예:

```csharp
public partial struct MatchDetectionSystem : ISystem
public partial struct TileFallSystem : ISystem
public partial struct BoardRefillSystem : ISystem
```

## Authoring Rule

GameObject에서 ECS Entity로 변환해야 하는 데이터는 Baker를 사용한다.

```csharp
public sealed class BoardAuthoring : MonoBehaviour
{
    public int Width;
    public int Height;
}

public sealed class BoardBaker : Baker<BoardAuthoring>
{
    public override void Bake(BoardAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.None);

        AddComponent(entity, new BoardSize
        {
            Width = authoring.Width,
            Height = authoring.Height
        });
    }
}
```

## Data Flow

기본 흐름은 다음 순서를 따른다.

```txt
Input
→ Command Request
→ ECS Simulation
→ Result Event
→ Presentation Sync
→ UI / Effect / Sound
```

## Rule

- ECS 내부에서는 GameObject 직접 참조를 피한다.
- Entity에는 필요한 최소 데이터만 저장한다.
- Presentation은 ECS 결과를 읽고 표시만 한다.
- 매치 판정 로직은 순수 데이터 기반으로 작성한다.
- 랜덤 생성은 시드 기반으로 관리한다.

## Performance Rule

- 매 프레임 Entity 생성/삭제를 남발하지 않는다.
- 반복적으로 쓰는 임시 데이터는 NativeCollection 사용을 검토한다.
- Burst 적용 가능 영역은 구조체 기반으로 유지한다.
- 관리형 참조, string, class 참조를 ECS 핵심 루프에 넣지 않는다.
