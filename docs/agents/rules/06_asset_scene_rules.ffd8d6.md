# Asset / Scene Rules

## Asset Naming

에셋 이름은 역할이 먼저 오고 구체 이름이 뒤에 온다.

```txt
MAT_Tile_Red
PF_Tile_Normal
SO_BoardConfig_Default
SCN_Gameplay
VFX_Tile_Clear
SFX_Tile_Swap
```

## Prefix

| Prefix | Meaning |
|---|---|
| `SCN_` | Scene |
| `PF_` | Prefab |
| `MAT_` | Material |
| `TEX_` | Texture |
| `SO_` | ScriptableObject |
| `VFX_` | Visual Effect |
| `SFX_` | Sound Effect |
| `ANIM_` | Animation |
| `CTRL_` | Animator Controller |

## Scene

```txt
Assets/01.Scenes/
├─ SCN_Bootstrap.unity
├─ SCN_Gameplay.unity
├─ SCN_Test_Board.unity
└─ SCN_Test_DOTS.unity
```

## Prefab Rule

- 프리팹에는 로직을 많이 넣지 않는다.
- View 역할의 컴포넌트만 둔다.
- 데이터는 ScriptableObject 또는 ECS Component로 분리한다.

## Material Rule

- 타일 타입별 Material은 명확히 분리한다.
- 런타임에서 Material 인스턴스를 무분별하게 생성하지 않는다.
- 색상만 바꾸는 경우 MaterialPropertyBlock 사용을 검토한다.
