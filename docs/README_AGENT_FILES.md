# 3Match Agent Guideline Files

이 압축 파일은 Unity 3D 기반 3Match 프로젝트에 바로 넣을 수 있는 Agent 지침 파일 묶음이다.

## 포함 파일

```txt
AGENTS.md
agents/
├─ manifest.json
└─ rules/
   ├─ 01_project_overview.<hash>.md
   ├─ 02_csharp_style.<hash>.md
   ├─ 03_unity_architecture.<hash>.md
   ├─ 04_dots_ecs_rules.<hash>.md
   ├─ 05_gameplay_3match.<hash>.md
   ├─ 06_asset_scene_rules.<hash>.md
   └─ 07_git_workflow.<hash>.md
```

## 사용법

압축을 Unity 프로젝트 루트에 풀면 된다.

`AGENTS.md`는 인덱스 역할만 하고, 실제 세부 룰은 `agents/rules/` 하위 파일에 분리되어 있다.
