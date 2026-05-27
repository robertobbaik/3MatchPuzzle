# Git Workflow Rules

## Branch

```txt
main
develop
feature/*
fix/*
refactor/*
test/*
```

## Commit Message

형식:

```txt
type: summary
```

예:

```txt
feat: add board initialization system
fix: prevent invalid tile swap
refactor: split match detection logic
test: add board match detection tests
docs: update dots rule guide
```

## Commit Type

| Type | Meaning |
|---|---|
| `feat` | 기능 추가 |
| `fix` | 버그 수정 |
| `refactor` | 구조 개선 |
| `test` | 테스트 추가/수정 |
| `docs` | 문서 수정 |
| `chore` | 기타 작업 |

## Rule

- 한 커밋에는 하나의 목적만 담는다.
- 자동 생성 파일과 수동 작성 파일을 섞어 커밋하지 않는다.
- Unity 메타 파일 `.meta`는 반드시 함께 커밋한다.
- 큰 리팩토링 전에는 먼저 테스트 또는 검증 씬을 만든다.

## Code Review Rule

- Agent가 코드를 작성하거나 수정한 뒤에는 사용자에게 코드 리뷰를 요청한다.
- 코드 리뷰를 받기 전에는 후속 기능 구현, 대규모 리팩토링, 커밋을 진행하지 않는다.
- 리뷰 요청 시 변경 파일, 핵심 의도, 검증 결과를 함께 요약한다.
