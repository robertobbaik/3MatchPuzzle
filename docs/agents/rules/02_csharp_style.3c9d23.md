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
