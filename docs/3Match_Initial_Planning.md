# 3Match 초기 버전 기획 문서

## 1. 문서 목적

이 문서는 Unity 3D 기반 3Match 게임의 초기 버전 범위를 정의한다.

초기 버전은 Royal Match류의 매치 퍼즐 구조를 참고하되, 전체 기능을 그대로 구현하지 않고 핵심 플레이 루프를 먼저 검증하는 것을 목표로 한다.

초기 개발의 핵심은 다음과 같다.

```txt
보드 생성
→ 블록 선택
→ 인접 블록 교환
→ 매치 판정
→ 블록 제거
→ 아이템 생성
→ 낙하
→ 리필
→ 연쇄 매치
→ 목표 달성 체크
```

초기 버전에서는 메타 시스템, 꾸미기, 상점, 하트, 라이브 이벤트, 팀 시스템은 제외한다.

---

## 2. 초기 버전 개발 목표

### 목표

1. 3Match 보드의 핵심 루프 구현
2. DOTS 기반 보드 시뮬레이션 구조 검증
3. GameObject 기반 블록 표현과 ECS 데이터 연결 검증
4. UGUI 기반 HUD와 게임 상태 표시 구현
5. 추후 Royal Match식 아이템, 장애물, 레벨 확장을 위한 구조 확보

### 제외 범위

- 메타 진행 시스템
- 방 꾸미기/성 꾸미기 시스템
- 하트/라이프 시스템
- 상점/인앱 결제
- 이벤트/랭킹/팀 시스템
- 부스터 구매 및 소모 시스템
- 복잡한 아이템 조합 전체 구현

---

## 3. 전체 방향

초기 버전은 다음 구조를 따른다.

```txt
Engine: Unity 3D
Language: C#
Architecture: DOTS + GameObject Hybrid
Board Simulation: DOTS
Block View: 일반 GameObject
HUD / Popup: UGUI
Level Data: ScriptableObject
```

핵심 게임 규칙은 데이터 중심으로 작성하고, 화면 표현은 일반 GameObject와 UGUI를 사용한다.

---

## 4. 보드 기본 사양

### 초기 보드 크기

```txt
Width: 8
Height: 8
```

8x8 보드는 매치 퍼즐에서 가장 일반적이고, 테스트와 레벨 설계가 쉽다.

### 좌표계

```txt
X: Column
Y: Row
Z: Visual Depth or Height
```

게임 로직에서는 X, Y 좌표만 사용한다. Z는 3D 표현과 연출용으로만 사용한다.

### 보드 구조

보드는 Cell과 Block을 분리한다.

```txt
Cell
└─ Block
```

Cell은 위치와 칸의 속성을 가진다. Block은 실제 매치 대상 또는 아이템/장애물 상태를 가진다.

---

## 5. 블록 색 가짓수

### 초기 결정

```txt
초기 블록 색상 수: 5개
```

초기 색상은 다음과 같이 정의한다.

```txt
Red
Blue
Green
Yellow
Purple
```

### 이유

| 색상 수 | 특징 | 초기 적합도 |
|---:|---|---|
| 4색 | 매치가 너무 자주 발생함 | 낮음 |
| 5색 | 테스트와 재미 균형이 좋음 | 높음 |
| 6색 | 난이도 조절 폭이 넓음 | 중간 |
| 7색 이상 | 초반 플레이가 답답해질 수 있음 | 낮음 |

초기에는 DOTS 기반 보드 로직을 검증해야 하므로 5색으로 시작한다. 이후 레벨 난이도가 필요해지면 6색으로 확장한다.

---

## 6. 블록 종류

## 6.1 일반 컬러 블록

일반 컬러 블록은 기본 매치 대상이다.

```txt
ColorBlock
```

속성:

```txt
ColorType
BoardPosition
BlockState
```

일반 컬러 블록은 다음 규칙을 따른다.

- 매치 가능
- 낙하 가능
- 스폰 가능
- 교환 가능
- 아이템 효과로 제거 가능

---

## 6.2 장애물 블록

초기 장애물은 1차와 2차로 나눈다.

```txt
1차 프로토타입: Box
2차 프로토타입: Chain
```

### Box

Box는 가장 기본적인 장애물이다.

역할:

- 직접 매치 대상이 아님
- 주변 매치가 발생하면 내구도 감소
- 아이템 효과를 받으면 내구도 감소
- 내구도 0이 되면 제거

속성:

```txt
Hp: 1~2
CanFall: false
CanSwap: false
CanMatch: false
```

### Chain

Chain은 컬러 블록을 잠그는 장애물이다.

역할:

- 일반 컬러 블록 위에 덮이는 잠금 상태
- 잠긴 블록은 이동 불가
- 주변 매치 또는 아이템 효과로 Chain이 먼저 제거됨

속성:

```txt
Hp: 1
CanFall: false
CanSwap: false
CanMatch: underlying block 기준
```

Chain은 구현 복잡도가 Box보다 높으므로 초기 1차 버전에서는 제외하고, 2차 버전에서 추가한다.

---

## 7. 아이템 구조

초기 아이템은 3종으로 제한한다.

```txt
Rocket
Bomb
ColorBomb
```

Royal Match류의 구조를 참고하되, 초기 버전에서는 Propeller와 복잡한 아이템 조합은 제외한다.

| 아이템 | 역할 | 초기 포함 여부 |
|---|---|---|
| Rocket | 행 또는 열 제거 | 포함 |
| Bomb | 주변 범위 제거 | 포함 |
| ColorBomb | 특정 색 전체 제거 | 포함 |
| Propeller | 목표 타겟팅 후 제거 | 제외 |

Propeller는 목표 타겟팅 로직이 필요하기 때문에 초기 구현 범위에서는 제외한다.

---

## 8. 아이템 생성 규칙

### 3개 매치

```txt
같은 색 3개 연속
→ 일반 제거
```

### 4개 직선 매치

```txt
같은 색 4개 가로/세로
→ Rocket 생성
```

초기 방향 규칙:

```txt
가로 4매치 → HorizontalRocket
세로 4매치 → VerticalRocket
```

### 5개 L/T 매치

```txt
같은 색 5개 이상이 L 또는 T 형태
→ Bomb 생성
```

### 5개 직선 매치

```txt
같은 색 5개 직선
→ ColorBomb 생성
```

---

## 9. 아이템 효과

### Rocket

```txt
HorizontalRocket:
- 현재 위치의 Row 전체 제거

VerticalRocket:
- 현재 위치의 Column 전체 제거
```

장애물 처리:

```txt
NormalBlock: 제거
Box: Hp -1
Chain: Hp -1
```

---

### Bomb

초기 Bomb은 중심 기준 3x3 범위를 제거한다.

```txt
Bomb Range: 3x3
```

확장 버전에서는 5x5 또는 반경 2칸으로 늘릴 수 있다.

---

### ColorBomb

ColorBomb은 교환한 블록의 색상과 같은 모든 일반 블록을 제거한다.

예시:

```txt
ColorBomb + Red Block
→ 보드 위 모든 Red Block 제거
```

초기 버전에서는 ColorBomb 단독 탭 발동은 제외하고, 일반 블록과 스왑했을 때만 발동한다.

---

## 10. 아이템 조합

초기 버전에서는 아이템 조합을 구현하지 않는다.

제외 조합:

```txt
Rocket + Rocket
Rocket + Bomb
Bomb + Bomb
ColorBomb + Rocket
ColorBomb + Bomb
ColorBomb + ColorBomb
```

단, 코드 구조는 향후 확장이 가능하도록 열어둔다.

```txt
ItemEffectResolver
ItemCombinationResolver
```

초기 `ItemCombinationResolver`는 대부분 미지원 처리하거나 단일 아이템 발동으로 처리한다.

---

## 11. 맵 구조

### 맵 데이터 관리

맵과 레벨 데이터는 ScriptableObject로 관리한다.

```txt
LevelConfig
BoardConfig
GoalConfig
SpawnConfig
```

LevelConfig는 다음 정보를 가진다.

```txt
LevelId
Width
Height
MoveLimit
Goals
Cells
SpawnRule
```

---

## 12. 초기 레벨 구성

### Level 1 - 기본 매치 테스트

```txt
Size: 8x8
Colors: 5
Obstacle: 없음
Goal: 블록 20개 제거
MoveLimit: 20
```

목적:

- 보드 생성 확인
- 3매치 판정 확인
- 낙하/리필 확인
- 기본 HUD 확인

---

### Level 2 - Rocket 테스트

```txt
Size: 8x8
Colors: 5
Obstacle: 없음
Goal: Rocket 3회 사용
MoveLimit: 20
```

목적:

- 4매치 판정 확인
- Rocket 생성 확인
- Row/Column 제거 확인

---

### Level 3 - Box 장애물 테스트

```txt
Size: 8x8
Colors: 5
Obstacle: Box
Goal: Box 8개 제거
MoveLimit: 25
```

목적:

- Box 배치 확인
- 주변 매치로 Box 데미지 확인
- 아이템 효과로 Box 데미지 확인
- 장애물 제거 목표 확인

---

## 13. 셀 구조

초기 CellType은 3개만 사용한다.

```csharp
public enum CellType
{
    Normal,
    Empty,
    Blocked
}
```

| CellType | 의미 |
|---|---|
| Normal | 일반 블록이 들어갈 수 있는 칸 |
| Empty | 구멍, 블록이 존재하지 않는 칸 |
| Blocked | 완전히 막힌 칸 |

1차 프로토타입에서는 전부 Normal Cell로 시작한다. Empty와 Blocked는 낙하 로직이 안정화된 뒤 추가한다.

---

## 14. 목표 구조

초기 목표 타입은 다음과 같다.

```csharp
public enum GoalType
{
    ClearColorBlock,
    ClearObstacle,
    UseItem,
    Score
}
```

우선순위:

```txt
1. ClearColorBlock
2. ClearObstacle
3. UseItem
4. Score
```

초기에는 점수 목표보다 제거 목표를 우선한다. 제거 목표가 레벨 디자인과 테스트에 더 적합하다.

---

## 15. UGUI vs 일반 GameObject

### 최종 결정

```txt
게임 보드/블록: 일반 GameObject
HUD/버튼/목표/팝업: UGUI
```

---

## 15.1 블록은 일반 GameObject 사용

블록은 UGUI가 아니라 일반 GameObject로 만든다.

```txt
Block = GameObject
Renderer = MeshRenderer 또는 SpriteRenderer
Collider = 클릭/선택 처리용
Presentation = ECS 결과를 Transform에 반영
```

이유:

- 3D 연출에 적합
- 블록 낙하/스왑 애니메이션이 자연스러움
- 카메라와 조명 사용 가능
- DOTS 시뮬레이션 결과를 Transform으로 반영하기 쉬움
- 나중에 3D 모델 블록으로 확장 가능

---

## 15.2 UGUI 사용 영역

UGUI는 화면 UI에만 사용한다.

```txt
Move Count
Goal Panel
Score
Pause Button
Booster Button
Level Start Popup
Level Clear Popup
Level Fail Popup
```

---

## 15.3 일반 GameObject 사용 영역

```txt
Board
Cell
Block
Item
Obstacle
Tile Effect
Explosion Effect
Selection Outline
```

---

## 16. DOTS 적용 범위

초기 DOTS는 보드 데이터와 판정 로직에 집중한다.

### DOTS 담당

```txt
Board Data
Cell Data
Block Data
Match Detection
Fall Calculation
Refill Calculation
Goal Progress Calculation
```

### MonoBehaviour 담당

```txt
Input
Camera
UGUI
Animation Trigger
Sound Trigger
Particle Trigger
Presentation Sync
```

### 처리 흐름

```txt
Player Input
→ MonoBehaviour Input Handler
→ SwapRequest 생성
→ ECS Board Simulation
→ Match / Fall / Refill 처리
→ BoardEvent 생성
→ GameObject View 반영
→ UGUI 갱신
```

---

## 17. 버전별 구현 범위

### Version 0.1 - Board Prototype

```txt
- 8x8 보드 생성
- 5색 일반 블록 생성
- 인접 블록 선택/교환
- 3개 이상 매치 판정
- 매치 제거
- 낙하
- 리필
- 연쇄 처리
```

아이템은 제외한다.

---

### Version 0.2 - Item Prototype

```txt
- 4개 매치 Rocket 생성
- 5개 L/T 매치 Bomb 생성
- 5개 직선 매치 ColorBomb 생성
- Rocket 발동
- Bomb 발동
- ColorBomb 발동
```

아이템 조합은 제외한다.

---

### Version 0.3 - Goal Prototype

```txt
- MoveLimit
- ClearColorBlock 목표
- ClearObstacle 목표
- Level Clear 판정
- Level Fail 판정
- UGUI HUD 표시
```

---

### Version 0.4 - Obstacle Prototype

```txt
- Box 장애물
- Box HP
- 주변 매치로 Box 데미지
- 아이템 효과로 Box 데미지
```

---

## 18. 초기 버전 최종 결정안

```txt
Engine:
- Unity 3D

Language:
- C#

Architecture:
- DOTS + GameObject Hybrid

Board:
- 8x8

Block Colors:
- 5 colors
- Red, Blue, Green, Yellow, Purple

Normal Block:
- ColorBlock

Initial Obstacles:
- Box only
- Chain은 2차 추가

Initial Items:
- Rocket
- Bomb
- ColorBomb

Excluded Initially:
- Propeller
- Item Combination
- Meta Progression
- Shop
- Lives
- Event System
- Team System
- Decoration System

UI:
- UGUI 사용

Board/Block:
- 일반 GameObject 사용

Data:
- LevelConfig ScriptableObject
- BoardConfig ScriptableObject
- GoalConfig ScriptableObject

DOTS:
- Board Simulation
- Match Detection
- Fall
- Refill
- Goal Progress
```

---

## 19. 한 줄 요약

초기 3Match는 8x8 보드, 5색 블록, Rocket/Bomb/ColorBomb 3종 아이템, Box 장애물, UGUI HUD, GameObject 기반 블록 표현, DOTS 기반 보드 시뮬레이션으로 만든다.
