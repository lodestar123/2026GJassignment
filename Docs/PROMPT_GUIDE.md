# Beat Defender — AI 구현 프롬프트 가이드

> **용도:** Cursor 등 AI에게 **어떻게 지시하면** `Docs/` 기획대로 Beat Defender를 잘 구현하는지 정리  
> **대상:** 구현을 시작하는 팀원 · AI 세션을 여는 사람  
> **선행:** [PLAN.md](./PLAN.md) · [DEV.md](./DEV.md) · [BALANCE.md](./BALANCE.md)  
> **문서 동기화:** [PLAN.md §0](./PLAN.md#0-문서-체계--동기화-규칙)

---

## 1. 이 문서를 쓰는 방법

1. **한 번에 전부 구현하지 말 것** — [DEV.md §8 Phase](./DEV.md) 단위로 나눠 요청
2. 매 프롬프트 **맨 위에** 「아래 Docs 따르기」+ **Phase 번호** + **완료 기준**을 적기
3. AI가 코드를 짠 뒤 **DEV.md §2·§8·§10** 갱신을 **같은 프롬프트에** 요청
4. 플레이 확인 후 [TEST.md](./TEST.md) 항목을 하나씩 체크

---

## 2. AI가 먼저 읽어야 할 문서 (우선순위)

| 순위 | 문서 | 언제 |
|------|------|------|
| 1 | [DEV.md §0·§1·§8](./DEV.md) | **매 작업** — 금지·잠금·TODO |
| 2 | [BALANCE.md](./BALANCE.md) | 수치·타워·스폰·점수 |
| 3 | [PLAN.md](./PLAN.md) | UX·패턴·흐름 확인 |
| 4 | [MAP.md](./MAP.md) | 씬·좌표·레이아웃 |
| 5 | [FLOW.md](./FLOW.md) | 씬 전환·Pause·Practice |
| 6 | [CONTROLS.md](./CONTROLS.md) | 입력 |
| 7 | [TEST.md](./TEST.md) | 완료 검증 |

> 프롬프트에 `@Docs/DEV.md @Docs/BALANCE.md` 처럼 **파일을 @ 멘션**하면 컨텍스트가 안정적이다.

---

## 3. 절대 어기면 안 되는 규칙 (프롬프트에 반복해도 좋음)

```
- GameBootstrap 런타임 스폰 금지
- WaveManager / 웨이브 금지 → ContinuousSpawner
- DimensionRift / 적 워프 금지
- 「레이저」 명칭 금지 → OverloadStrike (원형 범위, 적당 8 dmg 1회)
- Salvo 금지 → GoldPulse / RhythmShot
- 승리: 2분(120s) 생존 · Core Push PlayerController 폐기
- 타워 3종: Beat(박자 DPS) / Strike(Overload만) / Boost(BPMBoost만)
- Strike·Boost dmg: HashSet dedupe — 적당 1회만
- BeatTower: 1.2s Space → 2 dmg/beat, 무입력 → 0.6 fallback
- Pause: timeScale=0, BeatClock은 unscaled로 계속, 메트로놈 40%
- Space + 마우스 클릭 동시 가능
```

---

## 4. 권장 구현 순서 (Phase)

| Phase | 범위 | 플레이 가능 목표 |
|-------|------|------------------|
| **A** | BeatClock, 리듬 4패턴, Gold+10 | Space 치면 골드·판정 로그 |
| **B** | Rail, Scroll, Judgment UI | 리듬 UI 보임 |
| **C** | 맵 greybox, 타워 3종, 스폰, 120s | **1판 돌려볼 수 있음** ← MVP 핵심 |
| **D** | 3씬, Pause, Practice, 결과·점수 | 제출 수준 흐름 |
| **E** | Juice, SFX, Windows 빌드 | 제출물 |
| **F** | 몹 아트, 튜토리얼 | 후순위 |

**C까지가 “게임이다”** — A·B를 너무 길게 잡지 말 것.

---

## 5. 좋은 프롬프트 vs 나쁜 프롬프트

### 나쁜 예

> Beat Defender 만들어줘

→ 범위·Phase·Docs·레거시 처리 불명. Core Push와 섞이거나 웨이브가 들어갈 수 있음.

### 좋은 예 (템플릿)

```
Beat Defender Phase A 구현해줘.

참고: @Docs/DEV.md @Docs/BALANCE.md @Docs/PLAN.md §4
규칙: DEV §0.2 전부 준수. Core Push 코드 건드리지 말거나 명시적으로 제거.

이번 범위:
- BeatClock (120 BPM, boost 150, unscaledDeltaTime)
- CommandType, RhythmInputRecorder, RhythmCommandDetector (tap 3/4/5/6)
- GoldPulse +10, COOLDOWN 시 MISS

범위 밖: 타워, 스폰, UI 그래픽

완료 후:
- DEV.md §2·§8 Phase A 체크·§10 로그 갱신
- Unity에서 확인 방법 3줄로 알려줘
```

### 좋은 프롬프트 4요소

| # | 내용 |
|---|------|
| 1 | **Phase / 파일 @멘션** |
| 2 | **이번에 할 것 + 하지 말 것** |
| 3 | **DEV §0.2 규칙** (복붙) |
| 4 | **완료 후 DEV·TEST 갱신** 요청 |

---

## 6. Phase별 복붙용 프롬프트

### Phase A — 리듬 코어

```
Phase A: 리듬 코어 구현.

@Docs/DEV.md @Docs/BALANCE.md @Docs/PLAN.md §4
DEV §0.2 준수. Assets/Scripts/Rhythm/, Core/BeatClock.cs 생성.

구현:
- BeatClock: 120/150 BPM, OnBeat 이벤트, Pause 시 unscaled 진행
- RhythmCommandDetector: tap 3/4/5/6, ±0.12s, 1.2s idle reset
- GoldPulse +10G, SkillCooldown Overload/Boost, CD 중 시도 → MISS
- CommandEffectController 스텁 (타워 연동은 Phase C)

하지 말 것: WaveManager, PlayerController, 레이저 빔

완료: DEV §8 A 체크, §10 로그, Console 테스트 방법
```

### Phase B — 리듬 UI

```
Phase B: Beat Pulse Rail + Rhythm Scroll 4장 + JudgmentPopup.

@Docs/PLAN.md §6 @Docs/DEV.md @Docs/RESOURCES.md §1.3
- Scroll 4패턴 카드, Tab 확대
- 150 BPM 시 간격 압축, boost 주황 테두리
- PERFECT/GOOD/MISS / COOLDOWN 표시

Phase A 코드와 연결. 아트 없으면 TMP+색상 placeholder.

완료: DEV §8 B 체크
```

### Phase C — 맵 · 타워 · 스폰 (가장 중요)

```
Phase C: greybox 디펜스 루프. @Docs/MAP.md @Docs/BALANCE.md @Docs/DEV.md §3·§4

GameScene (SampleScene 대체 또는 신규):
- Core (0,-3.5), S1/S2, waypoint 경로, PlacementGrid ~14칸
- TowerType Beat/Strike/Boost — BALANCE §3 가격·사거리 2.5
- BeatTower: OnBeat 1.2s DPS / fallback, RhythmShot → Beat만
- Strike: OverloadStrike 시 dedupe 8 dmg
- Boost: BPMBoost 시 dedupe 4 dmg + SetBoost
- 판매 50%, 교체=자동판매+배치, TowerTypeSelect HUD
- ContinuousSpawner 120s 스케줄, cap 22
- GameManager 120s win, enemy OnBeat scale bounce
- Core Push WaveManager/Player 제거

Space+클릭 동시. greybox SpriteFactory OK.

완료: DEV §8 C, TEST.md MVP 핵심 항목 가능 여부 보고
```

### Phase D — Flow

```
Phase D: 씬·Pause·Practice·결과.

@Docs/FLOW.md @Docs/BALANCE.md §8·§10 @Docs/CONTROLS.md

- StartScene, GameScene, PracticeScene
- ESC Pause: timeScale=0, BeatClock 유지, rhythm vol 0.4
- Practice: Start 진입 + Pause Additive (불가 시 fallback 문서대로)
- ResultScreen + RunStats + ScoreCalculator (BALANCE §10)
- 120s 승리 우선 over 동시 피격

완료: DEV §8 D, FLOW 우선순위 반영
```

### Phase E — Polish · 빌드

```
Phase E: Juice + Windows 빌드.

@Docs/RESOURCES.md §4 M 우선 @Docs/SUBMISSION.md
- 메트로놈, 패턴 SFX, 타워 발사 placeholder
- Build Settings: Start+Game+Practice
- build_windows.bat 또는 Unity batchmode 빌드

완료: SUBMISSION §1 체크리스트, DEV §8 E
```

---

## 7. 세션 중 자주 쓰는 추가 프롬프트

| 상황 | 프롬프트 |
|------|----------|
| **버그** | 「@Docs/BALANCE.md 기준으로 [현상] 수정. DEV §11 이슈 참고. 범위 최소 diff」 |
| **밸런스** | 「BALANCE §3 수치만 조정하고 PLAN·DEV·TEST 동기화」 |
| **레거시 정리** | 「Core Push 잔재 grep 후 Beat Defender에 불필요한 것만 제거, DEV §2 갱신」 |
| **씬 세팅** | 「Editor 메뉴 SceneBuilder로 MAP.md 계층 생성 (GameBootstrap 금지)」 |
| **한 Phase 검증** | 「TEST.md Phase C 항목 기준으로 누락 목록만 알려줘」 |
| **다음 할 일** | 「DEV §9 기준 다음 Phase만 구현해줘」 |

---

## 8. 구현 시 Unity 주의사항 (프롬프트에 넣을 내용)

```
- Unity 6, URP 2D
- 씬: Assets/Scenes/StartScene, GameScene, PracticeScene
- TextMeshPro 사용
- BeatClock은 Pause에서 Time.unscaledDeltaTime
- AudioMixer: Master / SFX / Rhythm — Pause 시 Rhythm 40%
- 에디터가 프로젝트 열려 있으면 batchmode 빌드 실패할 수 있음 → 에디터 종료 후 빌드
```

---

## 9. AI에게 맡기면 안 되는 것

| 항목 | 이유 |
|------|------|
| **git commit / push** | 명시 요청 전까지 하지 않음 (user rule) |
| **기획 대폭 변경** | PLAN 먼저 합의 |
| **과제 보고서 본문** | SUBMISSION placeholder — 사람이 작성 |
| **한 프롬프트에 A~E 전부** | 품질·디버그 불가 |

---

## 10. Phase 완료 체크 (사람이 Play 후)

- [ ] [TEST.md](./TEST.md) 해당 Phase 항목
- [ ] DEV §8 해당 Phase `[x]`
- [ ] DEV §2 코드베이스 표 갱신
- [ ] 이상 동작 → DEV §11에 한 줄 추가

---

## 11. 첫 구현 세션 추천 (Day 1)

**프롬프트 1개로 시작:**

```
Beat Defender 구현 시작. @Docs/PROMPT_GUIDE.md @Docs/DEV.md @Docs/BALANCE.md @Docs/MAP.md

1) Phase A 전부 구현
2) 이어서 Phase C의 GameScene greybox + BeatTower + 스폰 + 120s 타이머만 (UI는 최소)
3) DEV §2·§8·§10 갱신

목표: Play 눌러 2분 버티는 greybox 1판.
Core Push Player/WaveManager는 제거 또는 비활성.
```

UI·Flow는 **2일차 Phase B+D**로 나누는 것을 권장.

---

## 12. 문서 ↔ 구현 매핑 (빠른 참조)

| 구현 대상 | 단일 소스 |
|-----------|-----------|
| 패턴 간격·효과 | PLAN §4 · DEV §4 |
| 타워·스폰·점수 수치 | BALANCE §3·§7·§10 |
| 좌표·레이아웃 | MAP.md |
| Pause·Practice | FLOW.md · BALANCE §8 |
| 조작 | CONTROLS.md |
| 클래스 구조 | DEV §3 |
| MVP 완료 | TEST.md |

---

## 13. 수정 이력

| 날짜 | 변경 |
|------|------|
| 2026-07-03 | 초안 — Phase 프롬프트·규칙·Day1 추천 |
