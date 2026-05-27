# AGENTS.md

이 파일은 AI Agent가 프로젝트 지침을 찾기 위한 루트 진입점입니다.

세부 지침은 [docs/AGENTS.md](docs/AGENTS.md)에 정리되어 있습니다. 작업을 시작하는 Agent는 먼저 해당 파일을 읽고, 그 안에 정의된 순서대로 `docs/agents/rules/` 하위 규칙을 확인해야 합니다.

프로젝트 작업 시 우선순위는 다음과 같습니다.

1. 사용자의 현재 요청
2. `docs/agents/rules/` 하위 세부 지침
3. `docs/AGENTS.md`
4. 일반적인 Unity / C# 관례

세부 규칙을 변경할 때는 `docs/AGENTS.md`의 manifest와 해시 관리 규칙을 함께 확인합니다.
