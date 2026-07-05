# Beat Defender — 사운드 · 아트 리소스 정리

> **용도:** 필요한 오디오·비주얼 에셋 목록, 우선순위, 폴더·스펙 가이드  
> **연관:** [PLAN.md](./PLAN.md) · [DEV.md](./DEV.md) · [MAP.md](./MAP.md)  
> **문서 동기화:** [PLAN.md §0](./PLAN.md#0-문서-체계--동기화-규칙)

---

## 0. 현재 상태 (플레이스홀더)

| 구분 | 현재 | 비고 |
|------|------|------|
| 스프라이트 | `Assets/Sprites/Circle.png`, `Square.png` | 프로시저럴 `SpriteFactory` 병행 |
| 오디오 | `SimpleAudio.cs` — 사인파 비프 | 실제 클립 없음 |
| 타일맵 | 미구현 | MAP.md 그리드 기준 greybox |
| UI 아트 | TMP + Phase B/C UI · greybox 맵/타워/적 | 그래픽 에셋 플레이스홀더 |

**MVP:** 위 플레이스홀더로 기능 검증 → **Phase E (Juice)** 에서 본 문서 **필수(M)** 항목 교체.

---

## 1. 아트 리소스

### 1.1 맵 · 타일 ([MAP.md](./MAP.md) §2)

| ID | 이름 | 설명 | 권장 스펙 | 우선순위 |
|----|------|------|-----------|----------|
| ART-T01 | **벽 타일** | 🟫 이동·배치 불가 | 16×16 px, PPU 16, 타일맵용 | M |
| ART-T02 | **경로 타일** | ⬜ 적 이동 구역 | 16×16, 반복 가능 패턴 | M |
| ART-T03 | **배치 슬롯 (기본)** | 🟩 타워 설치 칸 | 16×16, 반투명 녹색 | M |
| ART-T04 | **배치 슬롯 (호버)** | 마우스 올림 하이라이트 | ART-T03 변형 | M |
| ART-T05 | **배치 슬롯 (불가)** | 골드 부족·슬롯 가득 참 | 빨간 테두리 변형 | M |
| ART-T06 | **초크 마커** (선택) | 합류 지점 `(0,-1)` 강조 | 32×32 오버레이 | S |
| ART-T07 | **배경/바닥** | Playfield 바닥 | 14×10 유닛 커버, 단색 또는 그리드 | S |

### 1.2 게임 오브젝트

| ID | 이름 | 설명 | 권장 스펙 | 우선순위 |
|----|------|------|-----------|----------|
| ART-O01 | **본진 (Core)** | 하단 중앙 `(0,-3.5)` | 32×32~48×48, 중심 별·코어 형태 | M |
| ART-O02 | **평타 타워** | BeatTower 20G | 24×32 | M |
| ART-O02b | **강공격 타워** | StrikeTower 30G | 빨강 링 | M |
| ART-O02c | **버프 타워** | BoostTower 25G | 주황 링 | M |
| ART-O03 | **8분음표** | + **박자 bounce** | 16~24px | M |
| ART-O04 | **강박** (Downbeat) | 10HP / 1.6spd / 14G — 구 Brute | 24×32~32×32, 크고 느림 | M |
| ART-O05 | **투사체** | 타워 박자 사격 1발 | 8×8 bullet | M |
| ART-O06 | **강공격 링** | OverloadStrike 범위 플래시 | 원형 링 VFX, 빨강·플라즈마 | M |
| ART-O07 | **스폰 포인트** (선택) | S1/S2 표시 | 16×16 포털/게이트 | S |

### 1.3 UI · 리듬 연출 ([PLAN.md](./PLAN.md) §6)

| ID | 이름 | 설명 | 색·스타일 | 우선순위 |
|----|------|------|-----------|----------|
| ART-U01 | **HUD** | HP·Gold·**2:00**·타워 선택 | 상단 | S |
| ART-U02 | **아이콘 — HP** | Core 체력 | 하트/실드 | S |
| ART-U03 | **아이콘 — Gold** | 재화 | 코인/보석 | S |
| ART-U04 | **아이콘 — 강공격 CD** | 10s 쿨 | 🔴 계열 | S |
| ART-U05 | **아이콘 — 가속 CD** | 18s 쿨 | 🟣/주황 | S |
| ART-U06 | **Scroll 노트 — 골드** | 🟡 4연 정박 | 노랑 `■` | M |
| ART-U07 | **Scroll 노트 — 평타** | ⚪ 싱코 3타 | 흰 `■`/`▪` | M |
| ART-U08 | **Scroll 노트 — 강공격** | 🔴 5타 | 빨 `■`/`▪` | M |
| ART-U09 | **Scroll 노트 — 가속** | 🟣 6타 | 보라/주황 | M |
| ART-U10 | **Beat Pulse Rail** | 120/150 BPM 펄스 링 | 하단 12~15%, 원형 펄스 | M |
| ART-U11 | **판정 텍스트** | PERFECT / GOOD / MISS | TMP 또는 스프라이트 폰트 | M |
| ART-U12 | **COOLDOWN 표시** | 쿨 중 입력 거부 | 회색/빨강 텍스트 | M |
| ART-U13 | **스킬 체인** | `3/5` 등 진행 표시 | Rail 위 작은 인디케이터 | S |
| ART-U14 | **가속 버프 테두리** | 6초간 화면 주황 펄스 | Full-screen overlay, additive | M |
| ART-U15 | **튜토리얼 고스트** | 박자 연습 마커 | 반투명 Scroll 노트 | S |
| ART-U16 | **결과 화면** | 점수·등급·breakdown | CLEAR 15,025 (A) | S |

### 1.4 VFX · Juice ([PLAN.md](./PLAN.md) §6.4, DEV §8 Phase E)

| ID | 이름 | 트리거 | 설명 | 우선순위 |
|----|------|--------|------|----------|
| ART-V01 | **비트 바운스** | 정박·Rail 펄스 | 카메라/오브젝트 미세 스케일 | S |
| ART-V02 | **패턴 성공 플래시** | Gold/Shot/Strike/Boost | Rail 색: 노랑/흰/빨/주황 | M |
| ART-V03 | **피격 Red Flash** | Core·적 Hit | 스프라이트 순간 적색 | — |
| ART-V04 | **데미지 팝업** | 타워·강공격 히트 | 숫자 float-up | S |
| ART-V05 | **적 사망** | 8분음표/강박 처치 | 작은 파편 또는 fade | S |
| ART-V06 | **골드 획득** | GoldPulse +10 | `+10` 팝업, 노랑 | M |
| ART-V07 | **타워 설치** | 슬롯 클릭 성공 | 짧은 링/먼지 | S |
| ART-V09 | **적 박자 바운스** | OnBeat scale 1.0→1.12→1.0 | 120BPM | M |

### 1.5 색상 팔레트 (기획 고정)

| 용도 | 색 | HEX (가안) |
|------|-----|------------|
| 골드 패턴 | 🟡 | `#FFD54F` |
| 평타 패턴 | ⚪ | `#EEEEEE` |
| 강공격 패턴 | 🔴 | `#EF5350` |
| 가속 패턴 | 🟣/주황 | `#FF9800` |
| 배치 슬롯 | 🟩 | `#66BB6A` |
| 벽 | 🟫 | `#5D4037` |
| 경로 | ⬜ | `#BDBDBD` |
| 8분음표 (placeholder) | — | `#42A5F5` |
| 강박 (placeholder) | — | `#AB47BC` |
| Core | — | `#FFC107` |

---

## 2. 사운드 리소스

### 2.1 리듬 · BPM ([PLAN.md](./PLAN.md) §6.4)

| ID | 이름 | 설명 | 스펙 | 우선순위 |
|----|------|------|------|----------|
| SFX-R01 | **메트로놈 — 정박** | 120 BPM (0.5s) | 짧은 tick, 루프 가능 | M |
| SFX-R02 | **메트로놈 — 가속** | 150 BPM (0.4s) | R01 피치↑ 또는 별도 클립 | M |
| SFX-R03 | **Space 입력 tap** | Spacebar 누름 | 저음 click, ~0.05s | M |
| SFX-R04 | **PERFECT** | 패턴 성공 (정확) | 밝은 chime | M |
| SFX-R05 | **GOOD** | 패턴 성공 (관대) | PERFECT보다 약한 chime | M |
| SFX-R06 | **MISS** | 판정 실패 | dull thud / buzz | M |
| SFX-R07 | **COOLDOWN 거부** | 쿨 중 스킬 시도 | muted click | S |
| SFX-R08 | **시퀀스 리셋** | 1.2s 무입력 (선택) | 거의 무음 또는 soft whoosh | S |

### 2.2 패턴별 스킬 SFX

| ID | 이름 | 패턴 | 설명 | 우선순위 |
|----|------|------|------|----------|
| SFX-S01 | **GoldPulse** | 🟡 쿵×4 | 코인/캐시 register, +10 연출 | M |
| SFX-S02 | **RhythmShot** | ⚪ 딴 따단 | 일제 사격 트리거 — snare/staccato | M |
| SFX-S03 | **OverloadStrike** | 🔴 따다단딴딴 | 차지 + 원형 범위 zap | M |
| SFX-S04 | **BPMBoost 발동** | 🟣 다다다닥… | whoosh + tempo rise | M |
| SFX-S05 | **BPMBoost 종료** | 6s 후 | soft downshift | S |

### 2.3 전투 · 디펜스

| ID | 이름 | 트리거 | 설명 | 우선순위 |
|----|------|--------|------|----------|
| SFX-C01 | **타워 발사** | beat-sync / fallback | 짧은 pew, BPM 연동 | M |
| SFX-C02 | **강공격 범위** | OverloadStrike | burst zap | M |
| SFX-C03 | **적 피격** | 투사체 hit | soft impact | S |
| SFX-C04 | **8분음표 사망** | 4HP 소진 | 가벼운 pop | M |
| SFX-C05 | **강박 사망** | 10HP 소진 | 무거운 crunch | M |
| SFX-C06 | **본진 피격** | Core -HP | low thump (현재 110Hz beep) | M |
| SFX-C07 | **적 본진 도달** | Core attack | 경고음 + C06 | M |
| SFX-C08 | **골드 드롭** | 처치 보상 | 작은 coin (S01과 구분) | S |

### 2.4 UI · 시스템

| ID | 이름 | 트리거 | 설명 | 우선순위 |
|----|------|--------|------|----------|
| SFX-U01 | **타워 설치** | 슬롯 클릭 성공 | build/place | M |
| SFX-U02 | **설치 실패** | 골드 부족 | error beep | S |
| SFX-U03 | **슬롯 호버** | BuildSlot enter | subtle tick | S |
| SFX-U04 | **Scroll Tab** | 패널 확대/축소 | ui slide | S |
| SFX-U05 | **게임 시작** | 첫 스폰 전 | short sting | S |
| SFX-U06 | **승리** | 2분 생존 | fanfare (짧게) | S |
| SFX-U07 | **패배** | HP 0 | defeat sting | S |
| SFX-U08 | **일시정지** | Pause — gameplay mute, **metronome 40%** | rhythm duck | M |

### 2.5 튜토리얼 ([PLAN.md](./PLAN.md) §7)

| ID | 이름 | 설명 | 우선순위 |
|----|------|------|----------|
| SFX-T01 | **연습 모드 진입** | 설정 → 박자 연습 | S |
| SFX-T02 | **연습 PERFECT/GOOD** | R04/R05 재사용 가능 | — |
| SFX-T03 | **ms 오차 표시** | 시각만으로도 가능 | S |

### 2.6 오디오 스펙 · 믹싱

| 항목 | 가이드 |
|------|--------|
| 포맷 | **WAV** (소스) → Unity import; OGG Vorbis (빌드) |
| 샘플레이트 | 44.1 kHz |
| 채널 | SFX mono, BGM(있을 경우) stereo |
| 길이 | UI/타격 **≤0.3s**, 스킬 **≤2s**, loop는 seamless |
| 볼륨 그룹 | `Master` / `SFX` / `Rhythm` / `UI` — AudioMixer 권장 |
| 동시 재생 | 메트로놈 + 패턴 SFX + 타워 발사 동시 — peak clipping 주의 |
| BPM 연동 | `BeatClock` beatInterval에 R01/R02 스케줄 (코드) |

**BGM:** MVP **없음** (메트로놈이 리듬 앵커). 추후 Phase F에서 loop BGM 검토.

---

## 3. 폴더 구조 (권장)

```
Assets/
  Art/
    Tiles/          # ART-T01~07
    Units/          # ART-O01~07 (Core, Tower, EighthNote, Downbeat)
    VFX/            # ART-V*, projectile, strike ring
    UI/             # ART-U*, icons, scroll notes
  Audio/
    Rhythm/         # SFX-R*, SFX-S*
    Combat/         # SFX-C*
    UI/             # SFX-U*
    Tutorial/       # SFX-T*
  Sprites/          # (레거시) Circle/Square — 교체 후 deprecated
```

---

## 4. 우선순위 요약

### MVP 필수 (M) — Phase C~E 전에 확보 또는 플레이스홀더 유지

**아트:** ART-T01~05, ART-O01~06, ART-U06~12, ART-U14, ART-V02, ART-V06  
**사운드:** SFX-R01~06, SFX-S01~04, SFX-C01~02, SFX-C04~07, SFX-U01

### 폴리시 (S) — 기능 완료 후

나머지 S 항목 + ART-V01, V04~V08, BGM 검토

---

## 5. PLAN ↔ 리소스 매핑

| PLAN 섹션 | 관련 리소스 |
|-----------|-------------|
| §4 패턴 4종 | ART-U06~09, SFX-S01~04, ART-V02 |
| §5 디펜스 | ART-T*, ART-O*, SFX-C*, SFX-U01 |
| §6 UI/연출 | ART-U*, ART-V*, SFX-R* |
| §7 튜토리얼 | ART-U15, SFX-T* |
| MAP §2 맵 | ART-T*, ART-O01, ART-O07 |

---

## 6. 체크리스트 (에셋 도입 시)

- [ ] `Assets/Art`, `Assets/Audio` 폴더 생성 및 import 설정 (Sprite: Point filter)
- [ ] `SimpleAudio` → AudioClip 참조 방식으로 교체
- [ ] `SpriteFactory` greybox → 실 스프라이트 할당
- [ ] Prefab EighthNote/Downbeat/Tower/Core에 아트 반영
- [ ] **몹 리듬 테마 비주얼 디자인** (TODO — DEV §11 #4)
- [ ] Rhythm Scroll 4장에 ART-U06~09 적용
- [ ] BeatClock BPM에 SFX-R01/R02 동기
- [ ] AudioMixer 그룹 분리 및 볼륨 밸런스
- [ ] 본 문서 **§0 현재 상태** 갱신

---

## 7. 수정 이력

| 날짜 | 변경 |
|------|------|
| 2026-07-03 | PLAN/DEV/MAP 기준 초안 |
| 2026-07-03 | Scout/Brute→8분음표/강박, 레이저→강공격 링 VFX |
| 2026-07-03 | **2분**, 3타워, bounce, Pause metronome duck |
