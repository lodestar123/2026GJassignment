# Beat Defender — 탑뷰 리듬 디펜스

Unity 6 + URP 2D

## 소개

**120BPM** 리듬 4패턴 + **타워 3종** 배치. **2분 생존** 클리어.

| 패턴 | 효과 |
|------|------|
| 골드 | +10G |
| 평타 | **평타 타워** 즉시 1발 |
| 강공격 | Strike 범위 **8 dmg/적 1회** |
| 가속 | Boost 범위 **4 dmg/적 1회** + 150BPM 6s |

## 문서

| 파일 | 용도 |
|------|------|
| [PLAN.md](Docs/PLAN.md) | 기획 |
| [BALANCE.md](Docs/BALANCE.md) | 수치·타워·스폰 |
| [DEV.md](Docs/DEV.md) | 구현·TODO |
| [FLOW.md](Docs/FLOW.md) | 씬·Pause·Practice |
| [MAP.md](Docs/MAP.md) | 맵 |
| [CONTROLS.md](Docs/CONTROLS.md) | 조작 |
| [TEST.md](Docs/TEST.md) | QA |
| [PROMPT_GUIDE.md](Docs/PROMPT_GUIDE.md) | **AI 구현 프롬프트 가이드** |
| [SUBMISSION.md](Docs/SUBMISSION.md) | 제출 |

## 실행 (레거시)

`SampleScene` → Core Push hierarchy

## 빌드

StartScene + GameScene + PracticeScene 등록 후 `build_windows.bat`
