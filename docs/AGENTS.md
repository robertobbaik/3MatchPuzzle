# AGENTS.md

이 파일은 3Match Unity 3D 프로젝트의 Agent 지침 진입점이다.

## 역할

이 파일에는 세부 개발 규칙을 직접 작성하지 않는다.

AGENTS.md는 다음 역할만 가진다.

1. 프로젝트 전체 지침 파일의 위치 안내
2. 하위 지침 파일의 해시 인덱스 제공
3. Agent가 어떤 순서로 지침을 읽어야 하는지 정의
4. 하위 지침 파일의 무결성 확인 기준 제공

세부 규칙은 `agents/rules/` 아래의 해싱된 Markdown 파일에 작성한다.

## 프로젝트 기본 정보

- Project Name: 3Match
- Engine: Unity 3D
- Language: C#
- Code Style: C# Standard / Unity C# Style Guide 기반
- Architecture: Unity DOTS + Hybrid Unity Architecture
- Main Gameplay: 3Match Puzzle Game
- Primary Goal: 유지보수 가능한 DOTS 기반 3D 매치 퍼즐 게임 구현

## Rule Manifest

| Order | Rule File | Hash | Purpose |
|---:|---|---|---|
| 01 | `agents/rules/01_project_overview.7d0192.md` | `7d0192` | 프로젝트 목표와 전체 방향 |
| 02 | `agents/rules/02_csharp_style.3c9d23.md` | `3c9d23` | C# 코드 스타일 |
| 03 | `agents/rules/03_unity_architecture.59acc1.md` | `59acc1` | Unity 프로젝트 구조 |
| 04 | `agents/rules/04_dots_ecs_rules.f89d1d.md` | `f89d1d` | DOTS / ECS 작성 규칙 |
| 05 | `agents/rules/05_gameplay_3match.626554.md` | `626554` | 3Match 게임플레이 규칙 |
| 06 | `agents/rules/06_asset_scene_rules.ffd8d6.md` | `ffd8d6` | 에셋, 씬, 프리팹 관리 |
| 07 | `agents/rules/07_git_workflow.734d13.md` | `734d13` | Git 작업 규칙 |

## Agent Read Order

Agent는 작업을 시작하기 전에 아래 순서대로 지침을 읽는다.

1. `project_overview`
2. `csharp_style`
3. `unity_architecture`
4. `dots_ecs_rules`
5. `gameplay_3match`
6. `asset_scene_rules`
7. `git_workflow`

## Modification Rule

- `AGENTS.md`에는 세부 규칙을 추가하지 않는다.
- 규칙 변경은 반드시 `agents/rules/` 하위 파일에서 수행한다.
- 하위 파일 내용이 변경되면 파일명과 manifest의 hash 값을 갱신한다.
- Agent는 AGENTS.md의 해시와 실제 파일 해시가 다를 경우 사용자에게 확인을 요청한다.

## Priority

지침 우선순위는 다음과 같다.

1. 사용자의 현재 요청
2. `agents/rules/` 하위 지침 파일
3. `AGENTS.md`
4. 일반적인 Unity / C# 관례

단, 현재 요청이 기존 프로젝트 구조를 파괴하거나 DOTS 설계를 위반할 경우, Agent는 먼저 위험성을 설명하고 대안을 제시한다.
