# Changelog — The Last Warden

빌드 버전별 변경 기록. 날짜는 한국 시간 기준 빌드 시각. 플레이어용 요약은 빌드 폴더의 `패치노트.txt` 참고.

## [0.1.7] — 2026-10-29

### 추가
- **상인 NPC** — 시작 지점 뒤(Medieval Civilian 3, Mixamo 리그). 2.5m 트리거 안에서 `[F] 대화` → 강화 창, 말하는 동안 Talk 애니메이션 + 플레이어 쪽으로 돌아봄(unscaled), 닫으면 Idle `0cc0452` `5f80eeb`
  - 입력 `Interact`(F) 추가, 창이 열린 동안 Gameplay 입력 차단(`PlayerInputReader.SetGameplayEnabled`)
  - 에디터 툴 `Tools/Models/Setup Merchant`(0.01배 크기 보정, Humanoid, URP Lit Specular 머티리얼), `Tools/UI/Add Interact Prompt`, `Tools/UI/Build Upgrade Menu`
- **강화 시스템** `Progression`(static, PlayerPrefs) `390447c`
  - 재화 = 보스에게 실제로 깎은 HP, 패배해도 유지(승리·패배 시 저장)
  - 강화 4종: 공격력 +1 / 체력 +10 / 최대 스태미나 +10 / 스태미나 회복 +3.5 (초당), 비용 100 + 레벨×50, 최대 10레벨
  - 상인 창 **초기화(환불)** — 쓴 재화 전액 반환 / 타이틀 **기록 초기화** — 레벨·재화 0, 확인 창 거침(감도·볼륨은 유지) `c0464b0`
  - 강화 즉시 반영: `PlayerCombatTarget.ApplyProgression`(최대 체력·스태미나), 공격력은 `Progression.Atk`를 바로 읽음
- **최대치에 따라 길어지는 HP·스태미나 바** — `IGauge.Max`, `GaugeBarUI._growWithMax`(플레이어 바만) `5f80eeb`
- **패링 슬로모션** — 0.12초 정지 → 0.15초 0.1배속, 셰이크 1.2 → 0.8 (`HitStop.Play(duration, scale)`) `ce39027`

### 변경
- 치명타: 고정 250 → **강공 대미지 × 5** (기본 150 = 보스 HP 30%, 공격력 강화에 비례) `390447c`
- 강화 창은 Esc를 `LateUpdate`에서 처리 — 같은 프레임에 일시정지 메뉴가 열리던 충돌 방지

### 제거
- 합성 타격음 `SFX_Impact_01~05`, `ControlsScreen`(IntroSequence로 통합) `1e60dad`

## [0.1.6] — 2026-10-28

### 변경
- **패링 사운드 교체** — Clash "팅"(피치 0.92) + 갑옷 타격 + 저음 붐 3겹 레이어, 일반 적중보다 +3dB(-18 LUFS). `SFX/Parry/SFX_Parry_01~03`, 가드는 기존 Clash 유지
- 음량 조정 (플레이 피드백: 너무 큼)
  - 승리·패배 사운드 -19 → -26 LUFS
  - 보스 등장 스팅어(braam) -18 → -29 LUFS
  - 타이틀·보스전 BGM -24 → -27 LUFS

### 수정
- 오디오 교체가 일부 반영되지 않았던 문제 — 같은 경로로 덮어쓴 파일 중 이전 버전이 남아 있던 것(승리·패배, braam, 타이틀 BGM)을 다시 반영하고 프로젝트 내 파일을 직접 측정해 확인

## [0.1.5] — 2026-10-28 19:39

### 추가
- **프롤로그 연출** `IntroSequence` — 검은 화면 문장 4줄(페이드, Space로 건너뛰기) → 보스 공개(IntroCam, 손떨림 노이즈) → 눈빛 + 스팅어(braam) → 이름 카드 `THE LAST WARDEN / 성문의 서약기사 알데리크` → 조작법 → 전투
  - 연출 중 `Time.timeScale = 0`, `CinemachineBrain.IgnoreTimeScale = true`, 대기·페이드는 `WaitForSecondsRealtime` / `unscaledDeltaTime`
  - R 재시작 시 `static _hasPlayed`로 생략
- 에디터 툴 `Tools/UI/Add Prologue` (`PrologueBuilder`) — PrologueGroup·NameCardGroup·IntroCam 생성, IntroSequence 필드 자동 연결
- `CameraBlends.asset` — IntroCam ↔ 다른 카메라 전환은 Cut

### 변경
- BGM은 Play On Awake 대신 전투 시작(`StartBattle`)에서 재생
- 조작법 화면 표시를 `ControlsScreen` → `IntroSequence`로 이관

### 수정
- 컷씬 종료 후 카메라가 보스 앞에서 플레이어 뒤로 날아오던 문제 — FreeLook의 Blend Hint `InheritPosition`을 전환하는 한 프레임만 끔 (락온 해제용으로 평소엔 유지)

## [0.1.4] — 2026-10-28 17:41

### 추가
- **볼륨 설정** — AudioMixer(BGM/SFX 그룹), 일시정지 메뉴 슬라이더, `VolumeSettings`(PlayerPrefs 저장), 타이틀에서도 저장값 적용 `7ffcae9`
- **BGM** — 타이틀 / 보스전 루프, `Tools/Audio/Setup BGM`
- **효과음** — `SfxPlayer`(2D 풀 8개), `HitboxSfx`(히트박스 이벤트 구독: 휘두름 `OnActivated`, 결과 `OnHitResult`)
  - 휘두름(가벼운/묵직한), 적중(플레이어→보스 갑옷음, 보스→플레이어 베임), 패링·가드 금속음
  - 히트박스마다 3D AudioSource(칼 위치에서 소리), 아레나 리버브 존(Forest), `Tools/Audio/Setup SFX`
- **승리·패배 사운드** — 교회 종 3번 / 시네마틱 붐, 결과 화면에서 BGM 페이드아웃 (`GameFlowController`)
- `HitboxComponent.OnActivated` 이벤트

### 변경
- 모든 오디오 라우드니스 정규화 (SFX 약 -21 LUFS, BGM -24 LUFS, 클리핑 제거)

## [0.1.3] — 2026-10-27 12:11

### 추가
- 타격감: 히트스톱(적중 0.06 / 강공 0.1 / 패링 0.12초) + 카메라 셰이크(Cinemachine Impulse) `7e4873b` `98c11ed`
- 보스 칼 궤적(면 트레일) + 그로기 시 칼을 땅에 짚는 손 IK `ece99fc`
- 치명타 후 보스 후딜(바로 반격하지 않음) `720322c`
- README `8ee4fff`

### 변경
- 마우스 감도 0.1~10.0 단위(기본 5.0 = 기존 속도) `8c1f177`
- 디버깅용 에셋·코드 정리 `720322c`, Unity 클라우드 연결 해제 `e88ae72`

## [0.1.2] — 2026-10-26 23:45

### 추가
- 첫 시작 시 조작법 화면(Space로 전투 시작) + 일시정지 메뉴(Esc: 계속하기 / 타이틀로 / 게임 종료, 감도 X·Y) `810ebc6`

### 수정
- 타이틀 `게임 시작` 버튼이 Arena로 넘어가지 않던 문제(TitleMenu 참조 미저장) `c95b82f`

## [0.1.1] — 2026-10-26 22:14 — 첫 체험판

- 전투: 이동, 락온, 약공 3콤보 / 강공(홀드), 구르기, 패링·가드, HP·스태미나
- 보스: 패턴 4종(ScriptableObject), 눈빛 = 패링 신호, 콤보 후 간 보기(`BossRecoverState`)
- 체간: 세키로식 바, 붕괴 시 그로기(무릎 꿇기), 그로기 중 강공 치명타 250
- 맵: 성문·성벽·숲·황혼 조명
- UI: 타이틀 화면, 보스 이름판, 결과 화면(WARDEN SLAIN / FALLEN, R 재시작)
- 알려진 문제: 타이틀 시작 버튼 미동작 → 0.1.2에서 수정
