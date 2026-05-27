# 3MatchPuzzle

Unity 기반 3Match 퍼즐 게임 프로토타입입니다.

초기 목표는 보드 생성, 블록 선택, 인접 블록 교환, 매치 판정, 낙하, 리필로 이어지는 3Match 핵심 플레이 루프를 검증하는 것입니다.

## Development With AI

이 프로젝트는 AI를 개발 보조 도구로 적극 활용합니다. 개발자는 구현 방향과 제약 조건을 AI에 전달하고, AI가 제안한 설계와 코드를 다시 검토하는 방식으로 작업합니다.

- 초기 기획 문서 정리
- Unity 프로젝트 구조 설계
- 보드, 블록, 입력 처리 코드 작성
- Addressables, Object Pool, Stage Data 구조 설계
- 코드 리뷰 및 리팩터링 방향 검토

기본 워크플로우는 다음과 같습니다.

```txt
아이디어 정리
-> AI에 구현 방향과 제한 사항 전달
-> 설계 초안 검토
-> 코드 작성 또는 수정
-> 개발자 리뷰
-> 다음 작업 범위 결정
```

AI는 반복적인 구현과 구조 검토를 빠르게 진행하는 데 사용하며, 최종 설계와 게임 방향은 개발자가 검토하고 결정합니다.

## Agent Guideline Workflow

AI 작업 지침은 루트의 `AGENTS.md`와 `docs/agents/rules/` 하위 세부 지침으로 관리합니다.

```txt
AGENTS.md
-> docs/AGENTS.md
-> docs/agents/manifest.json
-> docs/agents/rules/*.md
```

작업 흐름은 다음을 기준으로 합니다.

1. 개발자가 구현 목표, 금지 사항, 우선순위를 AI에 전달합니다.
2. AI는 `AGENTS.md`를 시작점으로 프로젝트 지침을 확인합니다.
3. 필요한 세부 지침을 읽고 현재 요청에 맞는 작업 범위를 정합니다.
4. 구현 전 구조나 리스크를 먼저 설명합니다.
5. 코드 작성 후 빌드, 테스트, 변경 파일을 확인합니다.
6. 개발자가 결과를 리뷰하고 다음 작업 지침을 보강합니다.

세부 지침은 역할별로 분리합니다.

- `project_overview`: 프로젝트 목표와 개발 방향
- `csharp_style`: C# 작성 규칙
- `unity_architecture`: Unity 구조와 책임 분리
- `dots_ecs_rules`: DOTS / ECS 적용 기준
- `gameplay_3match`: 3Match 게임플레이 규칙
- `asset_scene_rules`: 에셋, 씬, 프리팹 관리 기준
- `git_workflow`: Git 작업 규칙

지침을 수정할 때는 세부 규칙 파일을 먼저 수정하고, 필요하면 manifest와 `docs/AGENTS.md`의 인덱스를 함께 갱신합니다.

## Current Scope

- Unity 3D / C#
- 2D 기반 3Match 보드
- 색상별 일반 블록 프리팹
- 셀 선택 입력 구조
- 스테이지 단위 에셋 로딩 및 풀링 구조 설계 중

## Planned Direction

- Excel 기반 스테이지 데이터 관리
- Addressables 기반 스테이지별 에셋 로딩
- 스테이지 전용 Object Pool
- 비정형 맵 레이아웃 지원
- 매치 판정, 낙하, 리필, 목표 달성 체크 구현

## Project Status

현재는 핵심 구조를 잡는 초기 개발 단계입니다.
