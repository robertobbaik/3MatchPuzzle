# Unity Architecture Rules

## Folder Structure

```txt
Assets/
├─ 01.Scenes/
├─ 02.Scripts/
│  ├─ Runtime/
│  │  ├─ Core/
│  │  ├─ Board/
│  │  ├─ Tiles/
│  │  ├─ Match/
│  │  ├─ Input/
│  │  ├─ Presentation/
│  │  └─ UI/
│  ├─ Authoring/
│  └─ Editor/
├─ 03.Prefabs/
├─ 04.Textures/
├─ ScriptableObjects/
├─ Materials/
└─ Settings/
```

## Asset File Placement

`Assets/` 하위에 새 파일을 만들 때는 먼저 기존 폴더 구조와 파일 역할을 확인한 뒤 가장 가까운 책임의 폴더를 선택한다.

- Runtime C# 코드는 `Assets/02.Scripts/Runtime/` 아래 기능별 폴더에 둔다.
- Baker, Authoring 등 변환 연결 코드는 `Assets/02.Scripts/Authoring/` 아래에 둔다.
- 에디터 전용 코드는 `Assets/02.Scripts/Editor/` 아래에 둔다.
- Scene 파일은 `Assets/01.Scenes/` 아래에 둔다.
- Prefab 파일은 `Assets/03.Prefabs/` 아래에 둔다.
- Texture 파일은 `Assets/04.Textures/` 아래에 둔다.
- ScriptableObject 에셋은 `Assets/ScriptableObjects/` 아래 목적별 하위 폴더에 둔다.
- Material 에셋은 `Assets/Materials/` 아래 목적별 하위 폴더에 둔다.
- 적절한 폴더가 없을 경우 임의 위치에 만들지 말고, 기존 구조와 같은 규칙으로 최소 범위의 하위 폴더를 만든다.

## Assembly Definition

기능 단위로 asmdef를 분리한다.

```txt
Project.Core
Project.Board
Project.Match
Project.DOTS
Project.Presentation
Project.UI
Project.Editor
```

## MonoBehaviour Usage

MonoBehaviour는 다음 목적으로만 사용한다.

1. Unity 입력 연결
2. 씬 오브젝트 연결
3. 프리팹 참조
4. UI 표시
5. DOTS World와 기존 GameObject 영역 연결

게임의 핵심 규칙은 MonoBehaviour에 직접 작성하지 않는다.

## Lifecycle Rule

- `Awake`, `Start`, `Update`, `OnEnable`, `OnDisable`, `OnDestroy`, `OnValidate` 같은 Unity LifeCycle 함수는 가급적 사용하지 않는다.
- LifeCycle 함수는 씬 또는 기능 단위의 가장 상위 Manager / Bootstrapper 클래스에서만 사용한다.
- 하위 컴포넌트는 `Initialize`, `EnableInput`, `DisableInput`, `Dispose`처럼 명시적인 public 메서드로 초기화와 정리를 제공한다.
- Manager / Bootstrapper는 직렬화된 참조를 통해 하위 컴포넌트를 연결하고, 정해진 순서로 초기화한다.
- 반복 처리가 필요할 때도 개별 컴포넌트의 `Update`를 늘리지 말고, 상위 실행 흐름이나 이벤트 기반 구조를 먼저 검토한다.

## ScriptableObject Usage

ScriptableObject는 설정 데이터에 사용한다.

예:

- BoardConfig
- TileTypeConfig
- LevelConfig
- ScoreConfig
- VisualThemeConfig

## Scene Rule

- 테스트 씬과 실제 게임 씬을 분리한다.
- 씬에 직접 게임 로직을 의존시키지 않는다.
- 씬 오브젝트는 Bootstrapper를 통해 초기화한다.
