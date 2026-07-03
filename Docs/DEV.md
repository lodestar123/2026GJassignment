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
3. **리듬 패턴 4종** (3/4/5/6 tap) 전부 구현
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
| Base BPM | 120 (0.5s) |
| Boost BPM | 150 (0.4s), 6s |
| Boost affects | Player rhythm, **BeatTower** beat-sync/fallback, Rail/Scroll/metronome |
| Boost does NOT affect | Enemy move, spawn interval, Strike/Boost towers auto-fire |
| **BeatTower DPS** | Last **1.2s** Space → **2 dmg**/beat; else **0.6** fallback — **BeatTower only** |
| RhythmShot | **BeatTowers only** FireOnce(2) |
| **OverloadStrike** | 5 tap, 10s CD → enemies in **StrikeTower** circles: **8 dmg once each** (deduped) |
| **BPMBoost** | 6 tap, 18s CD → BoostTower circles: **4 dmg once each** (deduped) + 150BPM 6s |
| COOLDOWN attempt | **MISS** |
| Judgment | ±0.12s |
| Core HP | **25** |
| **Tower types** | Beat 20G · Strike 30G · Boost 25G · max 8 |
| **Sell / Replace** | **50%** refund · replace = auto-sell |
| **Pause** | timeScale=0 · BeatClock unscaled · rhythm **40%** |
| Scenes | Start + Game + **Practice** |
| Input | Space + mouse simultaneous |
| **Pause menu** | ESC — continue/restart/practice/settings/title |
| Score | [BALANCE.md §10](./BALANCE.md) · `RunStats` |
| Map | [MAP.md](./MAP.md) |

---

## 2. 현재 코드베이스 상태 (2026-07-03)

> Core Push 레거시. Beat Defender **미구현**.

| 영역 | 상태 |
|------|------|
| Rhythm/* | **신규 전부** |
| Tower/, PlacementGrid | **신규** |
| ContinuousSpawner | WaveManager **대체** |
| Player/* | **폐기** |
| StartScene / GameScene / PracticeScene | **미생성** |
| MAP layout | **미구현** |

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
    RhythmCommandDetector.cs
    CommandEffectController.cs
    SkillCooldownController.cs
  Tower/
    TowerType.cs, Tower.cs, TowerPlacer.cs, TowerSellUI.cs, PlacementGrid.cs
  Enemy/
    EnemyAI.cs, EnemyHealth.cs, EnemyType.cs, ContinuousSpawner.cs
  Base/ BaseHealth.cs
  UI/
    BeatPulseRailUI.cs, RhythmScrollUI.cs
    TowerTypeSelectUI.cs, PauseMenuUI.cs, PracticeSceneLoader.cs
    SettingsPanelUI.cs, ResultScreenUI.cs
  Tutorial/
  Util/ ...
```

**Scenes:** `StartScene.unity`, `GameScene.unity`, `PracticeScene.unity`

---

## 4. 패턴 · 효과

| CommandType | Taps | Effect |
|-------------|------|--------|
| RhythmShot | 3 | **BeatTowers** FireOnce (2 dmg) |
| GoldPulse | 4 | AddGold(10) |
| **OverloadStrike** | 5 | Deduped **8 dmg**/enemy in Strike circles |
| BPMBoost | 6 | Deduped **4 dmg**/enemy in Boost circles + SetBoost(150,6f) |

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

- [ ] BeatClock (120 + 150)
- [ ] CommandType incl. **OverloadStrike**
- [ ] Detector 3/4/5/6 + **COOLDOWN → MISS**
- [ ] GoldPulse +10, RhythmShot (**Beat only**), deduped Strike/Boost
- [ ] Fallback — **BeatTower**, 1.2s window

### Phase B — UI

- [ ] BeatPulseRailUI, RhythmScrollUI (4)
- [ ] **TowerTypeSelectUI**, TowerSellUI
- [ ] JudgmentPopup

### Phase C — 맵 · 타워 · 스폰

- [ ] MAP, PlacementGrid, **Beat/Strike/Boost**, sell/replace
- [ ] ContinuousSpawner **120s**, cap 22
- [ ] GameManager **120s** · enemy beat bounce

### Phase D — Flow

- [ ] StartScene, GameScene, **PracticeScene**
- [ ] PauseMenu + BeatClock unscaled + vol duck
- [ ] Practice **Additive** from Pause
- [ ] ResultScreen + RunStats ([BALANCE.md §10](./BALANCE.md))
- [ ] Remove Core Push Player/WaveManager

### Phase E — Juice · 제출

- [ ] RESOURCES §M items
- [ ] Windows build · [SUBMISSION.md](./SUBMISSION.md)

### Phase F — 후순위

- [ ] **몹 아트** — 8분음표/강박 + 바운스 polish
- [ ] **첫 실행 튜토리얼** — PLAN §7

---

## 9. 당장 다음에 할 일

1. `BeatClock` + `CommandType.OverloadStrike`
2. Detector + Gold +10 + **MISS on CD attempt**
3. `Tower` circle range + path leader + beat-sync
4. GameScene greybox + **120s** timer
5. Pause + BeatClock unscaled

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

---

## 11. 알려진 이슈 · REMIND TODO

| # | 항목 |
|---|------|
| 1 | 3tap vs 5tap 혼동 — tap count filter |
| 2 | Boost interval scale — BeatClock single source |
| 3 | **몹 이름·비주얼** — Scout/Brute → 8분음표/강박 리디자인 |
| 4 | **제출 보고서** — SUBMISSION.md 4항 나중 작성 |

---

## 12. AI 갱신 체크리스트

- [ ] §2 · §8 · §9 · §10
- [ ] PLAN 또는 DEV 변경 → §0.1 연관 Docs 전부 검토 (BALANCE, MAP, FLOW, CONTROLS, RESOURCES, TEST, SUBMISSION)
