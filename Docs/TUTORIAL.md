# Beat Defender — 튜토리얼

> **연관:** [FLOW.md](./FLOW.md) · [CONTROLS.md](./CONTROLS.md) · [PLAN.md](./PLAN.md)

---

## 개요

**PracticeScene**을 **TutorialScene**으로 대체합니다. StartScene의 「튜토리얼」 버튼으로 단독 진입하며, Pause 메뉴의 박자 연습(Additive)은 제거됩니다.

---

## 튜토리얼 단계 (10단계)

| # | 제목 | 내용 | 진행 조건 |
|---|------|------|-----------|
| 1 | Beat Defender | 양손 조작·2분 생존 목표 | 「다음」 |
| 2 | 박자와 타임라인 | Space 탭, PERFECT/GOOD 마커 | Good 이상 1회 탭 |
| 3 | GoldPulse | 0·0.5s 2타, +10G | GoldPulse 성공 |
| 4 | Scroll | 휠로 패턴 변경, Tab 확대 | 패턴 1회 변경 |
| 5 | RhythmShot | 3타, 평타 사격 | RhythmShot 성공 |
| 6 | 타워 배치 | HUD 선택·슬롯 설치·판매·양손 | 「다음」 |
| 7 | OverloadStrike | 3타 광역, CD 10s (튜토리얼 CD off) | Overload 성공 |
| 8 | Chain · Tempo | ChainZap, TempoUp/Down | 「다음」 |
| 9 | Pause · 설정 | ESC, BeatClock 유지 | 「다음」 |
| 10 | 완료 | GameScene 안내 | 「다음」→ 게임 시작 |

---

## 씬 연결

```
StartScene ──「튜토리얼」──► TutorialScene ──완료/건너뛰기──► StartScene
                              └──마지막 「다음」──► GameScene
```

---

## 구현 파일

| 파일 | 역할 |
|------|------|
| `TutorialController.cs` | 단계 진행·이벤트 구독 |
| `TutorialUI.cs` | 안내 패널 |
| `TutorialSceneController.cs` | 씬 초기화 |
| `TutorialHudUI.cs` | BPM·CD 표시 |
| `TutorialProgress.cs` | 완료 PlayerPrefs |
| `TutorialSceneEditor.cs` | Practice 템플릿 → Tutorial bake |

---

## Unity 설정

1. **Beat Defender → Build Tutorial Scene** (최초 1회)
2. **Beat Defender → Sync Tutorial UI From Game** (리듬 UI 동기화)

Build Settings: StartScene · GameScene · **TutorialScene**
