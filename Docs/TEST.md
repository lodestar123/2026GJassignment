# Beat Defender — QA · MVP 완료 기준

> **연관:** [DEV.md](./DEV.md) · [BALANCE.md](./BALANCE.md)  
> **문서 동기화:** [PLAN.md §0](./PLAN.md#0-문서-체계--동기화-규칙)

---

## MVP Definition of Done

- [x] StartScene + GameScene + **PracticeScene**
- [x] **2분** 타이머 · **120s 승리** · 결과 화면
- [x] Pause: gameplay stop · **BeatClock+메트로놈 유지 40%**
- [x] 타워 **3종** · 판매 50% · 교체 자동판매
- [x] RunStats · 결과 점수 ([BALANCE.md §10](./BALANCE.md))
- [ ] BeatTower 1.2s DPS / Fallback (수동 QA)
- [ ] OverloadStrike **8 dmg dedupe** · BPMBoost **4 dmg dedupe**
- [ ] 적 **박자 바운스**
- [ ] Windows 빌드 (Phase E)

---

## 리듬

| # | Pass |
|---|------|
| R1 | GoldPulse +10G |
| R2 | RhythmShot → **평타 타워만** |
| R3 | Strike ≥1 + OverloadStrike → **8×1/적** |
| R4 | Boost ≥1 + BPMBoost(1 tap @ 0s) → **4×1/적** + EffectiveMeasure×0.8 |
| R5 | COOLDOWN → MISS |
| R6 | BeatTower: 1.2s 입력 2 dmg / 무입력 0.6 |
| R7 | **RhythmTimelineUI:** playhead 좌→우 · Space mark · 마디 시작 시 clear |
| R8 | 채점 **마디 종료** only · 2/2박 중간 채점 없음 |
| R9 | 사이클 끝 **±PERFECT** 조기 입력 → 다음 1박 **판정** 이월 · mark는 `--` 구간에 1회만 |

### EditMode 자동 테스트

| # | Pass |
|---|------|
| T1 | `RhythmPatternLibraryTests` — 기대 시각 · PERFECT/GOOD/MISS |
| T2 | `RhythmKeyFilterTests` — ESC/Space/修飾키/마우스 |
| T4 | `ScoreCalculatorTests` — 승리/패배 점수·등급 |

실행: Unity **Test Runner > EditMode > BeatDefender.Tests**

### Phase B UI (수동)

| # | Pass |
|---|------|
| B1 | **GameHudUI** — Gold · BPM · Strike/Boost CD 갱신 |
| B2 | **BeatPulseRailUI** — 박마다 펄스 · Gold/Shot/Strike/Boost 성공 시 색 플래시 |
| B3 | **BPMBoost** 중 화면 **주황 테두리** |
| B4 | **RhythmScrollUI** — 4카드 · Strike/Boost CD · **Tab** 축소/확대 |
| B5 | **TowerTypeSelectUI** — Beat/Strike/Boost 선택 · 골드 부족 시 비활성 |
| B6 | SceneBuilder **Beat Defender → Build Phase B Scene** 후 UI 배치 |

### Phase C 디펜스 (수동)

| # | Pass |
|---|------|
| C1 | **Build Phase C Scene** — Core · 14 슬롯 · 경로 마커 |
| C2 | 슬롯 클릭 → 타워 설치 (Beat 20 / Strike 30 / Boost 25G) |
| C3 | 점유 슬롯 클릭 → **교체** (기존 50% 환급) · max **8기** |
| C4 | 타워 클릭 → **Sell 50%** 패널 |
| C5 | **3s** 후 스폰 · 8분음표/강박 이동 · **박자 바운스** |
| C6 | BeatTower 박자 DPS / Fallback · RhythmShot → Beat만 |
| C7 | OverloadStrike → Strike 범위 **8×1** · BPMBoost → Boost **4×1** + 150BPM |
| C8 | 적 Core 도달 → HP 감소 · **2:00** 생존 승리 · HP0 패배 |

### Phase D Flow (수동)

| # | Pass |
|---|------|
| D1 | **Build Start/Practice/Phase C Scene** 메뉴 실행 |
| D2 | StartScene → 게임 시작 → GameScene |
| D3 | StartScene → 박자 연습 → PracticeScene (CD 없음) |
| D4 | GameScene **ESC** → Pause · 타이머/스폰 정지 · Rail/메트로놈 유지 |
| D5 | Pause → **박자 연습** → Practice Additive → **닫기** → Pause 복귀 |
| D6 | **120s 승리** / **HP0 패배** → ResultScreen + 점수·등급 |
| D7 | Result/Pause → **시작 화면** / **재시작** |

---

## Flow

| # | Pass |
|---|------|
| F1 | Pause BeatClock unscaled |
| F2 | Practice from Start + Pause Additive |
| F3 | 120s win priority over simultaneous hit |

---

## 수정 이력

| 날짜 | 변경 |
|------|------|
| 2026-07-03 | 초안 |
| 2026-07-03 | **2분**, 3타워, dedupe, Pause BeatClock, PracticeScene |
| 2026-07-04 | EditMode 자동 테스트 (T1–T3) · 1s 마디 · 이월·입력보정 |
