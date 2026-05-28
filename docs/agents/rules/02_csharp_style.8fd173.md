# C# Style Guide

## Base Rule

이 프로젝트의 C# 코드는 표준 C# 스타일과 Unity C# 스타일 가이드를 따른다.

## Naming

### Type

```csharp
public sealed class BoardPresenter
public struct TilePosition
public enum TileType
```

- Class, Struct, Enum, Interface: PascalCase
- Interface: `I` prefix 사용
- Method: PascalCase
- Property: PascalCase
- Public Field: PascalCase 지양, 가능하면 Property 사용
- Private Field: `_camelCase`
- Local Variable: camelCase
- Constant: PascalCase

```csharp
private int _moveCount;
private readonly BoardConfig _config;

public int Width { get; private set; }

private const int MaxMatchCount = 5;
```

## Formatting

- 들여쓰기: 4 spaces
- 한 줄에는 하나의 의미만 작성
- 중괄호는 C# 표준 방식 사용

```csharp
if (isMatched)
{
    ClearTile(tile);
}
```

## Access Modifier

명시적으로 작성한다.

```csharp
private void ResolveMatches()
{
}
```

## Enum Rule

- 프로젝트에서 사용하는 enum은 `Assets/02.Scripts/Runtime/Core/GlobalEnum.cs`에서 관리한다.
- 새 enum을 만들 때는 별도 enum 파일을 만들지 않는다.
- enum은 PascalCase 이름을 사용하고, 값 이름도 PascalCase로 작성한다.
- enum은 색상, 기능, 상태처럼 코드 전반에서 공유되는 고정 ID와 상태값에 사용한다.

## Null Rule

- 가능하면 null을 상태값으로 사용하지 않는다.
- Unity Object 참조는 사용 전 검증한다.
- 일반 C# 객체는 생성자 주입 또는 명확한 초기화 메서드를 사용한다.

## Method Rule

- 메서드는 하나의 책임만 가진다.
- 50줄을 넘기면 분리 검토.
- Boolean 인자가 2개 이상이면 별도 옵션 타입을 고려한다.

## Scope Rule

- 현재 요구사항을 해결하는 데 필요한 코드만 작성한다.
- 미래 확장 가능성만을 이유로 옵션, 분기, 추상화, 범용 처리를 추가하지 않는다.
- 프로젝트에서 당장 사용하지 않는 플랫폼, 입력 방식, 렌더링 방식, 예외 케이스는 구현하지 않는다.
- 같은 기능을 더 짧고 명확한 코드로 표현할 수 있으면 짧은 쪽을 선택한다.
- 기능 추가 후 코드가 요구사항보다 넓어졌다면 즉시 줄인다.

## API Usage Rule

- `out` 파라미터 문법은 가능하면 사용하지 않는다.
- 여러 값을 반환해야 할 때는 작은 결과 타입, struct, class, tuple보다 명확한 전용 타입을 우선 검토한다.
- 성능상 필요한 `Try...` 패턴이나 .NET/Unity 표준 API 호출처럼 불가피한 경우에만 `out`을 제한적으로 사용한다.
- `GetComponent`, `GetComponentInChildren`, `FindObjectOfType`, `FindFirstObjectByType` 계열 호출은 가능하면 사용하지 않는다.
- Unity 컴포넌트 참조는 `[SerializeField]`로 명시적으로 연결하거나 초기화 단계에서 주입한다.
- 런타임 반복 경로, `Update`, 매치 검사, 입력 처리 중에는 `GetComponent` 계열 호출을 피한다.
- `GetComponent` 계열 호출이 필요한 경우 `Awake`, `OnValidate`, 초기화 메서드에서 한 번만 실행하고 결과를 캐싱한다.

## Comment Rule

- 코드가 무엇을 하는지 설명하지 않는다.
- 왜 그렇게 했는지 설명한다.

좋은 예:

```csharp
// Match resolution is delayed until all swap animations finish.
```

나쁜 예:

```csharp
// tile을 제거한다.
```
