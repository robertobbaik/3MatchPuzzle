# Project Overview

## Project

3Match는 Unity 3D로 개발하는 3D 매치 퍼즐 게임이다.

## Core Direction

- 기본 장르는 3Match Puzzle이다.
- 게임 로직은 데이터 중심으로 설계한다.
- 성능이 중요한 반복 처리 영역에는 Unity DOTS를 도입한다.
- 화면 표현, 입력, UI, 이펙트, 사운드는 필요에 따라 기존 GameObject 기반 Unity 구조를 함께 사용한다.

## Main Goals

1. 유지보수 가능한 3Match 보드 시스템 구현
2. DOTS 기반의 빠른 매치 판정, 낙하, 리필 처리
3. 명확한 데이터 구조와 책임 분리
4. Agent가 코드를 생성하더라도 프로젝트 스타일이 흔들리지 않도록 지침 고정

## Non-Goals

- 초기 단계에서 과도한 최적화 금지
- 모든 시스템을 무조건 DOTS로 작성하지 않음
- 단순 UI, 연출, 에디터 도구까지 ECS로 강제하지 않음

## Development Principle

먼저 명확하게 만들고, 이후 병목이 확인된 부분을 DOTS/Burst/Job으로 최적화한다.
