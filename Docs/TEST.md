# Beat Defender — QA · MVP 완료 기준

> **연관:** [DEV.md](./DEV.md) · [BALANCE.md](./BALANCE.md)  
> **문서 동기화:** [PLAN.md §0](./PLAN.md#0-문서-체계--동기화-규칙)

---

## MVP Definition of Done

- [ ] StartScene · GameScene · **PracticeScene**
- [ ] **2분** 타이머 · **120s 승리**
- [ ] 타워 **3종** · 판매 50% · 교체 자동판매
- [ ] BeatTower 1.2s DPS / Fallback
- [ ] OverloadStrike **8 dmg dedupe** · BPMBoost **4 dmg dedupe**
- [ ] Pause: gameplay stop · **BeatClock+메트로놈 유지 40%**
- [ ] 적 **박자 바운스**
- [ ] RunStats · 결과 점수 ([BALANCE.md §10](./BALANCE.md))
- [ ] Windows 빌드

---

## 리듬

| # | Pass |
|---|------|
| R1 | GoldPulse +10G |
| R2 | RhythmShot → **평타 타워만** |
| R3 | Strike ≥1 + OverloadStrike → **8×1/적** |
| R4 | Boost ≥1 + BPMBoost → **4×1/적** + 150BPM |
| R5 | COOLDOWN → MISS |
| R6 | BeatTower: 1.2s 입력 2 dmg / 무입력 0.6 |

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
