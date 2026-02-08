# OpenClaw Unity Plugin - 문서

이 폴더에는 OpenClaw Unity Plugin의 종합 문서가 포함되어 있습니다.

## 문서 목록

| 문서 | 설명 |
|------|------|
| [index.md](index.md) | **메인 문서** - 아키텍처 개요, 핵심 컴포넌트, 주요 패턴, 빠른 참조 |
| [index_KO.md](index_KO.md) | 메인 문서 (한국어) |
| [DEVELOPMENT.md](DEVELOPMENT.md) | **개발 가이드** - 도구 확장, 새 기능 추가, 기여 방법 |
| [DEVELOPMENT_KO.md](DEVELOPMENT_KO.md) | 개발 가이드 (한국어) |
| [TESTING.md](TESTING.md) | **테스트 가이드** - 50개 도구 수동 테스트 절차 |
| [TESTING_KO.md](TESTING_KO.md) | 테스트 가이드 (한국어) |

## 파일 설명

### index.md / index_KO.md
플러그인 문서의 메인 진입점입니다. 포함 내용:
- 플러그인 개요 및 주요 기능
- 아키텍처 다이어그램 (Unity Editor ↔ Gateway 통신)
- 핵심 컴포넌트 설명 (EditorBridge, ConnectionManager, Tools, Config, Logger)
- 주요 구현 패턴 (SafeDestroy, 도구 별칭, 리플렉션)
- 설치 요약
- 빠른 참조 테이블 (엔드포인트, 도구 수)

### DEVELOPMENT.md / DEVELOPMENT_KO.md
플러그인을 확장하거나 기여하려는 개발자를 위한 가이드:
- 프로젝트 구조 및 파일 구성
- OpenClawTools.cs에 새 도구 추가 방법
- Gateway 확장 개발
- 디버깅 팁 및 일반적인 문제
- 코드 스타일 가이드라인
- Pull Request 절차

### TESTING.md / TESTING_KO.md
종합 테스트 절차:
- 테스트 환경 설정
- 도구별 테스트 명령어
- 각 도구의 예상 결과
- Editor 모드 vs Play 모드 차이점
- 테스트 실패 문제 해결

## 관련 파일 (상위 디렉토리)

| 파일 | 설명 |
|------|------|
| [README.md](../README.md) | 메인 프로젝트 README - 기능, 설치, 사용 예시 |
| [CHANGELOG.md](../CHANGELOG.md) | Keep a Changelog 형식의 버전 히스토리 |
| [LICENSE](../LICENSE) | MIT 라이선스 |
| [package.json](../package.json) | Unity 패키지 매니페스트 |

## 빠른 링크

- **GitHub 저장소:** https://github.com/TomLeeLive/openclaw-unity-plugin
- **OpenClaw Gateway:** https://github.com/openclaw/openclaw
- **Companion Skill:** https://github.com/TomLeeLive/openclaw-unity-skill

## 버전

- **플러그인 버전:** 1.2.3
- **Unity 호환성:** 2021.3+
- **총 도구 수:** 50개

---

*마지막 업데이트: 2026-02-08*
