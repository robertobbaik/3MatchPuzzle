# Bomb Block Combination Rules

## Purpose

이 문서는 폭탄 블록끼리 조합되었을 때의 효과를 구현 전에 먼저 고정하기 위한 설계 문서다.

## Bomb Types

| Type | Current Meaning |
|---|---|
| `Bomb` | 2x2 매치로 생성되는 단일 타격 폭탄 |
| `HorizontalRocket` | 가로 4개 매치로 생성되는 행 제거 폭탄 |
| `VerticalRocket` | 세로 4개 매치로 생성되는 열 제거 폭탄 |
| `ColorBomb` | 5개 이상 라인 매치로 생성되는 색상 폭탄 |

## Activation Rule

- 폭탄 조합은 폭탄 블록 2개를 서로 스왑했을 때 발동한다.
- 조합 발동 시 두 폭탄 블록은 모두 소비된다.
- 조합 발동은 일반 매치 성공 여부와 무관하게 유효한 이동으로 취급한다.
- 조합 효과로 다른 폭탄이 제거 범위에 포함되면 해당 폭탄도 연쇄 발동한다.
- 같은 조합은 순서에 무관하다. 예: `Bomb + HorizontalRocket`과 `HorizontalRocket + Bomb`은 같은 효과다.

## Single Bomb Baseline

조합 효과는 단일 폭탄 효과보다 강해야 한다.

| Type | Single Effect |
|---|---|
| `Bomb` | 자기 자신과 가장 유효한 일반 블록 1개를 제거 |
| `HorizontalRocket` | 자신의 행 전체 제거 |
| `VerticalRocket` | 자신의 열 전체 제거 |
| `ColorBomb` | 클릭 시 랜덤 색상 1종 전체 제거, 드래그 시 대상 색상 전체 제거 |

## Combination Effects

| Combination | Effect |
|---|---|
| `Bomb + Bomb` | 두 폭탄 위치를 중심으로 각각 가장 가까운 유효 블록 1개씩 제거하고, 추가로 두 폭탄 사이의 직선 경로에 있는 블록을 제거한다. |
| `Bomb + HorizontalRocket` | 폭탄 위치의 행 전체를 제거하고, 그 행에서 가장 유효한 블록 1개를 추가 타격한다. |
| `Bomb + VerticalRocket` | 폭탄 위치의 열 전체를 제거하고, 그 열에서 가장 유효한 블록 1개를 추가 타격한다. |
| `HorizontalRocket + VerticalRocket` | 교차 지점 기준으로 해당 행과 열을 모두 제거한다. |
| `HorizontalRocket + HorizontalRocket` | 두 로켓 위치의 행을 모두 제거한다. 같은 행이면 인접한 위/아래 행 중 유효한 행 1개를 추가 제거한다. |
| `VerticalRocket + VerticalRocket` | 두 로켓 위치의 열을 모두 제거한다. 같은 열이면 인접한 좌/우 열 중 유효한 열 1개를 추가 제거한다. |
| `Bomb + ColorBomb` | 보드에 있는 폭탄의 주변 우선순위 색상 1종을 선택하고, 해당 색상 블록을 모두 `Bomb` 효과로 변환해 순차 발동한다. |
| `HorizontalRocket + ColorBomb` | 대상 색상 블록을 모두 `HorizontalRocket` 효과로 변환해 순차 발동한다. 대상 색상은 스왑 상대가 일반 색이면 그 색상, 아니면 랜덤 색상이다. |
| `VerticalRocket + ColorBomb` | 대상 색상 블록을 모두 `VerticalRocket` 효과로 변환해 순차 발동한다. 대상 색상은 스왑 상대가 일반 색이면 그 색상, 아니면 랜덤 색상이다. |
| `ColorBomb + ColorBomb` | 보드의 모든 일반 색 블록과 모든 폭탄 블록을 제거한다. 장애물은 별도 피해 규칙이 정의되기 전까지 제외한다. |

## Target Selection

`가장 유효한 블록`은 아래 순서로 선택한다.

1. 조합에 참여한 폭탄과 가장 가까운 일반 색 블록
2. 거리가 같으면 낮은 `Y`, 낮은 `X` 순서
3. 일반 색 블록이 없으면 대상 없음

`랜덤 색상`은 현재 보드에 존재하는 `CanMatch == true` 블록 타입 중 하나를 선택한다.

## Chain Rule

- 조합 효과 범위에 다른 폭탄이 포함되면 해당 폭탄은 제거만 되지 않고 자신의 효과를 발동한다.
- 한 번 발동한 폭탄은 같은 resolve 단계에서 다시 발동하지 않는다.
- 연쇄 중 새로 생성된 폭탄은 현재 resolve 단계에서 자동 발동하지 않는다.

## Pending Decisions

- 장애물(`Box`)에 대한 폭탄 피해량은 아직 정의하지 않는다.
- `Bomb + Bomb`의 직선 경로 제거가 대각선일 때 계단형 경로를 사용할지, 두 축 경로를 모두 사용할지는 구현 직전에 결정한다.
- `ColorBomb` 조합의 순차 발동 연출 시간은 별도 presentation 단계에서 결정한다.
