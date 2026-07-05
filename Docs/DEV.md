# Beat Defender — 개발 진행 문서

> **용도:** AI·개발자 진행·TODO·구현 상태 갱신  
> **기획:** [PLAN.md](./PLAN.md) · **밸런스:** [BALANCE.md](./BALANCE.md) · **흐름:** [FLOW.md](./FLOW.md) · **맵:** [MAP.md](./MAP.md) · **리소스:** [RESOURCES.md](./RESOURCES.md) · **QA:** [TEST.md](./TEST.md)  
> **구현 프롬프트:** [PROMPT_GUIDE.md](./PROMPT_GUIDE.md)  
> **문서 동기화:** [PLAN.md §0](./PLAN.md#0-문서-체계--동기화-규칙)

---

## 0. AI 개발 시 필독 규칙

### 0.1 문서 동기화 (PLAN · DEV = 주 축)

**[PLAN.md](./PLAN.md) 또는 본 문서(DEV)를 수정하면**, 변경과 연관된 **Docs/ 아래 모든 파일**을 같은 작업에서 검토·갱신한다.  
상세 매트릭스: [PLAN.md §0](./PLAN.md#0-문서-체계--동기화-규칙)

| 갱신 트리거 | 연관 문서 (해당 섹션 있을 때) |
|-------------|------------------------------|
| **PLAN.md** | DEV §1·§8, **BALANCE**, **MAP**, **FLOW**, **CONTROLS**, **RESOURCES**, **TEST**, **SUBMISSION** |
| **DEV.md** | **TEST**, **RESOURCES**, **BALANCE**(수치), **FLOW**(우선순위), PLAN(기획 역반영 시) |

연관 없으면 생략. **빠진 파일 없이** 위 목록 전체를 매번 **열람 후** 필요 시 수정.

### 0.2 구현 규칙

1. 매 작업 전후 **§2** · **§8 TODO** 확인
2. GameBootstrap 금지 · 웨이브 금지
3. **리듬 패턴 4종** (2/3/5/1 tap · 2/4 마디) 전부 구현
4. **DimensionRift·적 워프 구현 금지** — 패턴 D는 **BPMBoost** 버프만
5. **Salvo 이름 금지** → `GoldPulse` / `RhythmShot`
6. **「레이저」 UI·코멘트 금지** → `OverloadStrike` (강공격, 범위 dmg)
7. **Fallback:** **BeatTower only** · **1.2s** Space window · 가속 중 **150BPM**
8. **타워 3종** · Strike/Boost는 **커맨드 시에만** · 판매/교체 **50%**
9. 작업 후 §8·§9·§10 갱신 · PLAN/DEV 변경 시 §0.1 연관 Docs 동기화

---

## 1. 잠금 결정 (Locked Decisions)

| Key | Value |
|-----|-------|
| **Win** | **120s survive**, Core HP > 0 |
| **Lose** | Core HP = 0 |
| Base BPM | **120** (= 1초 마디 / 2박) · `measureDurationSeconds` 조절 · GameScene 테스트 **1s** |
| Boost | `EffectiveMeasureDuration` **×0.8** (6s) |
| Boost affects | Player rhythm, **BeatTower** beat-sync/fallback, metronome, **RhythmTimeline** |
| **Measure** | **2/4** · Reference **1초** · **채점=마디 종료(OnMeasureEnd)** |
| Boost does NOT affect | Enemy move, spawn interval, Strike/Boost towers auto-fire |
| **BeatTower DPS** | Last **1.2s** Space → **2 dmg**/beat; else **0.6** fallback — **BeatTower only** |
| RhythmShot | **BeatTowers only** FireOnce(2) |
| **OverloadStrike** | 5 tap, 10s CD → enemies in **StrikeTower** circles: **8 dmg once each** (deduped) |
| **BPMBoost** | **6 tap**, 18s CD → BoostTower circles: **4 dmg once each** (deduped) + measure×0.8 for 6s |
| COOLDOWN attempt | **MISS** |
| Judgment | 탭 수 + **타이밍** 모두 일치해야 성립 · GOOD **±0.22s** · PERFECT **±0.11s** (× PatternTimeScale) |
| Core HP | **3** |
| **Tower types** | Beat 20G · Strike 30G · Boost 25G · max 8 |
| **Sell / Replace** | **50%** refund · replace = auto-sell |
| **Pause** | timeScale=0 · BeatClock unscaled · rhythm **40%** |
| Scenes | Start + Game + **Practice** |
| Input | **Rhythm keys** (all unassigned keyboard keys) + mouse simultaneous |
| **Pause menu** | ESC — continue/restart/practice/settings/title |
| Score | [BALANCE.md §10](./BALANCE.md) · `RunStats` |
| Map | [MAP.md](./MAP.md) |

---

## 2. 현재 코드베이스 상태 (2026-07-04)

> Core Push 레거시 제거 중. **Phase C 디펜스 루프 구현 완료.**

| 영역 | 상태 |
|------|------|
| Core/BeatClock, ResourceManager, RunStats | **완료** |
| Rhythm/* (4패턴·쿨·효과·이월·입력보정) | **완료** |
| Tower/ Beat·Strike·Boost + Fallback | **완료** |
| UI/RhythmDebugUI, **RhythmTimelineUI**, JudgmentFlashUI | **완료** (Debug 기본 숨김) |
| UI/**GameHudUI**, **BeatPulseRailUI**, **RhythmScrollUI**, **TowerTypeSelectUI** | **Phase B 완료** |
| UI/TowerSellUI | **완료** |
| UI/PauseMenuUI, ResultScreenUI, StartMenuUI | **Phase D 완료** |
| PauseController, PracticeSceneLoader, ScoreCalculator | **Phase D 완료** |
| **EditMode 자동 테스트** | `Assets/Tests/EditMode/` — 패턴·키필터·입력보정 |
| ContinuousSpawner | **완료** — 120s 스케줄 · cap 22 |
| GameManager · BaseHealth | **완료** — 120s 승리 · Core HP 25 |
| MAP · PlacementGrid · TowerPlacer | **Phase C 완료** |
| StartScene / PracticeScene flow | **Phase D 대기** |

---

## 3. 목표 아키텍처

```
Assets/Scripts/
  Core/
    BeatClock.cs
    PauseController.cs
    GameManager.cs
    RunStats.cs, ScoreCalculator.cs, ResourceManager.cs
  Rhythm/
    CommandType.cs
    RhythmInputRecorder.cs
    RhythmInputSettings.cs
    RhythmKeyFilter.cs
    RhythmCommandDetector.cs
    CommandEffectController.cs
    SkillCooldownController.cs
  Map/
    MapLayout.cs
  Tower/
    TowerType.cs, Tower.cs, TowerPlacer.cs, TowerPlacementCell.cs
    PlacementGrid.cs, TowerSelection.cs, TowerRegistry.cs, BeatTower.cs
  Enemy/
    EnemyKind.cs, EnemyHealth.cs, EnemyPathProgress.cs, EnemyMovement.cs
    EnemyBeatBounce.cs, ContinuousSpawner.cs
  Base/ BaseHealth.cs
  UI/
    RhythmDebugUI.cs, RhythmTimelineUI.cs, JudgmentFlashUI.cs
    GameHudUI.cs, BeatPulseRailUI.cs, RhythmScrollUI.cs, TowerTypeSelectUI.cs
    TowerSellUI.cs, PauseMenuUI.cs, PracticeSceneLoader.cs
    SettingsPanelUI.cs, ResultScreenUI.cs
  Tutorial/
  Util/ GreyboxSprites.cs, SimpleAudio.cs, ...
```

**Scenes:** `StartScene.unity`, `GameScene.unity`, `PracticeScene.unity`

---

## 4. 패턴 · 효과

| CommandType | Taps | Hit times (Ref 1s) | Effect |
|-------------|------|-------------------|--------|
| GoldPulse | 2 | 0, 0.5 | AddGold(10) |
| RhythmShot | 3 | 0, 0.5, 0.75 | **BeatTowers** FireOnce (2 dmg) |
| **OverloadStrike** | 5 | 0, 0.25, 0.5, 0.75, 0.875 | Deduped **8 dmg**/enemy in Strike circles |
| BPMBoost | **6** | 0, 0.125, 0.25, 0.5, 0.625, 0.75 | Deduped **4 dmg** + SetBoost(6f) · measure **×0.8** |

### BeatTower beat-sync

- `OnBeat`: if `Time.time - lastSpaceTime <= 1.2f` → **2 dmg** to path leader; else **0.6** fallback
- **Strike/Boost towers**: no OnBeat fire

### OverloadStrike / BPMBoost dedupe

```csharp
// Per activation: HashSet<Enemy> hit; each enemy at most once per skill event
```

### SkillCooldownController

- COOLDOWN 중 OverloadStrike/BPMBoost attempt → **MISS** popup
- GoldPulse, RhythmShot: CD 0
- **판정 = 탭 수 + 타이밍:** 기대 시각 ±GOOD 이내. **패턴 완성·연장 불가 시 즉시** 실행(0.22s 지연 제거).
- **Gold vs RhythmShot:** 2타 후 3타(0.75) 창이 닫히면 GoldPulse 확정 · 3타 연속이면 RhythmShot 즉시.
- **탭 손실 방지:** `MinTapGapReference` **0.02s** · 한 프레임 다중 리듬 키 수집.
- **Early downbeat (이월):** |tap − cycleEnd| ≤ PERFECT · **현재 사이클 0 tap** → 다음 사이클 첫 박.
- **Late backfill:** **이미 완성된 패턴·중간 슬롯(0.75)에는 백필 안 함.** Overload 4타 + 경계 5타만.
- **Input offset:** Baseline **0.24s** + 플레이어 **감도 조정**(0 = baseline). PlayerPrefs `BeatDefender.InputOffsetAdjustment`. 판정·타임라인 felt 축.

---

## 5. Beat Pulse Rail · Rhythm Scroll

- 4 cards — PLAN §6.3
- Boost: rail 0.4s, orange border overlay; end = **remove overlay**
- Flash: gold / white / **red (strike)** / orange

---

## 6. 맵 ([MAP.md](./MAP.md))

- Core `(0,-3.5)`, S1/S2, choke, ~14 slots, max 8 towers

---

## 7. 밸런스

> **단일 소스:** [BALANCE.md](./BALANCE.md)

---

## 8. 구현 TODO

### Phase A — 리듬

- [x] BeatClock (120 + 150 via ×0.8) · Reference **1s** · GameScene/Builder **1s**
- [x] CommandType incl. **OverloadStrike**
- [x] Detector 2/3/5/1 · **마디 종료 일괄 채점** · COOLDOWN → MISS
- [x] **이월(early downbeat)** · 사이클 0 tap일 때만 · ±PERFECT
- [x] **RhythmKeyFilter** · ESC/Tab/修飾키/마우스 제외 · 다키 입력
- [x] **RhythmInputSettings** · Baseline 0.24s + 감도 조정(0=기본)
- [x] GoldPulse +10, RhythmShot (**Beat only**), deduped Strike/Boost
- [x] Fallback — **BeatTower**, 1.2s window
- [x] **RhythmTimelineUI** — playhead 좌→우 · 입력 mark · `OnTapVisualized`
- [x] **EditMode 테스트** — `RhythmPatternLibrary`, `RhythmKeyFilter`, `RhythmInputSettings`

### Phase B — UI

- [x] **GameHudUI** — HP(placeholder) · Gold · BPM · Strike/Boost CD · Judge stats
- [x] **BeatPulseRailUI** — OnBeat 펄스 · 커맨드 색 플래시 · BPMBoost 주황 테두리
- [x] **RhythmScrollUI** — 4패턴 카드 · CD · Tab 확대/축소
- [x] **TowerTypeSelectUI** + `TowerSelection` — Beat/Strike/Boost 선택 (배치 Phase C)
- [x] **JudgmentFlashUI** (Phase A) — 판정 팝업
- [x] SceneBuilder **Build Phase B Scene** · Debug 패널 기본 숨김
- [x] **TowerSellUI** — 타워 클릭 · 50% 환급

### Phase C — 맵 · 타워 · 스폰

- [x] **MAP greybox** — Core `(0,-3.5)` · S1/S2 · 합류 초크 · 14 슬롯
- [x] **PlacementGrid** · **TowerPlacer** — 설치/교체/판매 50% · max 8
- [x] **Beat/Strike/Boost** — BeatTower DPS/Fallback · Strike/Boost 커맨드만
- [x] **ContinuousSpawner** — BALANCE §7.1 · cap 22 · 3s delay
- [x] **GameManager** 120s · **BaseHealth** 25 · 승리 우선
- [x] **EnemyBeatBounce** · 처치 골드 · RunStats kill
- [x] SceneBuilder **Build Phase C Scene**

### Phase D — Flow

- [x] **StartScene**, **GameScene**, **PracticeScene** (SceneBuilder)
- [x] **PauseController** + PauseMenuUI · BeatClock unscaled · rhythm vol **40%**
- [x] Practice **Additive** from Pause · Start 단독 진입
- [x] **ResultScreenUI** + **ScoreCalculator** (BALANCE §10)
- [ ] SettingsPanelUI (후순위)

### Phase E — Juice · 제출

- [ ] **BGM + SFX polish** — 120BPM BGM 1곡 · 타격/스킬/마일스톤 SFX (후순위, 체감 필수)
- [ ] RESOURCES §M items
- [ ] Windows build · [SUBMISSION.md](./SUBMISSION.md)

### Phase F — 후순위

- [ ] **몹 아트** — 8분음표/강박 + 바운스 polish
- [ ] **첫 실행 튜토리얼** — PLAN §7

---

## 9. 당장 다음에 할 일

1. **Phase E** — RESOURCES §M · Juice · Windows 빌드
2. Unity **Build Settings**에 Start/Game/Practice 등록 후 빌드
3. **Beat Defender → Build Start/Practice Scene** + **Build Phase C Scene** 실행

---

## 10. 진행 로그

| 날짜 | 작업 |
|------|------|
| 2026-07-03 | PLAN/DEV 초안 |
| 2026-07-03 | 4패턴, MAP, BPMBoost |
| 2026-07-03 | RESOURCES, sync rule |
| 2026-07-03 | **3분 승리**, OverloadStrike, BALANCE/FLOW/CONTROLS/TEST/SUBMISSION, 2씬, Pause |
| 2026-07-03 | **2분**, 타워 3종, dedupe, PracticeScene, Pause BeatClock, sell/replace |
| 2026-07-03 | [PROMPT_GUIDE.md](./PROMPT_GUIDE.md) AI 구현 프롬프트 가이드 |
| 2026-07-03 | **Phase A** — BeatClock, 4패턴 Detector, GoldPulse, Strike/Boost dedupe, BeatTower Fallback, GameScene 테스트 필드 |
| 2026-07-03 | **2/4 마디** · Reference 1초 · measureDuration 스케일 · 패턴 2/3/5/1타 재정의 |
| 2026-07-04 | GameScene **1s** 마디 · 이월·입력보정(0.14s)·RhythmKeyFilter · RhythmTimelineUI 하이어라키 |
| 2026-07-04 | **Phase D** — StartScene, Pause, Practice Additive, ResultScreen, ScoreCalculator |
| 2026-07-04 | **Phase C** — MAP, PlacementGrid, TowerPlacer, ContinuousSpawner, GameManager |
| 2026-07-04 | **Phase B** — GameHudUI, BeatPulseRailUI, RhythmScrollUI, TowerTypeSelectUI, SceneBuilder Phase B |
| 2026-07-05 | **피버 8연속** · MatchMilestone(30/57/87/100/110) · Core 위기 연출 · BGM TODO |

---

## 11. 알려진 이슈 · REMIND TODO

| # | 항목 |
|---|------|
| 1 | 3tap vs 5tap 혼동 — tap count filter (**Phase A 해결**) |
| 2 | Boost interval scale — BeatClock single source (**Phase A 해결**) |
| 3 | **몹 이름·비주얼** — Scout/Brute → 8분음표/강박 리디자인 |
| 4 | **제출 보고서** — SUBMISSION.md 4항 나중 작성 |

---

## 12. AI 갱신 체크리스트

- [ ] §2 · §8 · §9 · §10
- [ ] PLAN 또는 DEV 변경 → §0.1 연관 Docs 전부 검토 (BALANCE, MAP, FLOW, CONTROLS, RESOURCES, TEST, SUBMISSION)

---

## 13. 자동 테스트 (EditMode)

> Unity Test Framework · `Window > General > Test Runner > EditMode`

| 파일 | 검증 |
|------|------|
| `RhythmPatternLibraryTests` | 기대 시각 · PERFECT/GOOD/MISS · ByTapCount |
| `RhythmKeyFilterTests` | ESC/Space/修飾키/마우스 · RegisterReservedKey |
| `RhythmInputSettingsTests` | baseline 0.24s · 감도 조정 · AdjustTapTime |

**실행:** Test Runner에서 `BeatDefender.Tests` 전체 Run. CI 연동은 Phase E.

**추가 예정:** PlayMode — Detector 이월·Gold 2타 통합 (Phase B 이후).
