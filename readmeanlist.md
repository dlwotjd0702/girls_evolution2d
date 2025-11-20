# Girls Evolution 2D – Technical Highlights

게임 업계 포트폴리오 제출용 기술 문서 요약입니다. 프로젝트의 핵심 기능과 적용한 주요 기법을 모듈 단위로 정리했습니다.

## 1. Core Game Loop
- **GirlFieldManager**: 캐릭터 스폰, 합성, 자동 수익 계산 등을 관리하는 핵심 시스템.
  - Idle 골드 계산 시 계승/환생 배수를 동적으로 적용.
  - `JumpBounceLoop`로 자동 점프·바운스를 수행하며, 클릭/합성 시마다 즉시 수익 반영.
- **GirlMergeManager**: 드래그/자동 합성 로직과 +2단 도약 확률 적용.
- **PrestigeManager**: 환생/환생 포인트 및 환생 상점(12종 영구 업그레이드) 관리.

## 2. Data & Asset Pipeline
- **CSV/TSV 자동 로딩**: `GirlDataManager.LoadAsync()`가 Addressables TextAsset을 비동기로 읽고, 컬럼명 자동 매칭으로 TSV/CSV를 모두 지원.
- **Addressables 기반 스프라이트 로딩**: `GirlSpriteAddressableLoader`가 SD/LD 스프라이트 라벨을 비동기로 프리로드하여 메모리 효율 확보.
- **StreamingAssets/girls.tsv**: 25레벨 캐릭터 메타 데이터를 외부 파일로 분리해 손쉽게 밸런스 조정 가능.

## 3. Save System & Persistence
- **SaveManager**:
  - `Application.persistentDataPath`에 JSON 기반 로컬 파일 저장.
  - PlayerPrefs 백업 및 자동 로드, 테스트용 리셋/삭제/백업 복구 루틴 내장.
  - ISaveable 인터페이스(`ApplyLoadedData/CollectSaveData`)로 모듈별 상태를 독립적으로 직렬화.

## 4. UI & UX Systems
- **도감(Encyclopedia)**:
  - `EncyclopediaPanelController` + `EncyclopediaSlot` + `EncyclopediaDetailPanel`.
  - 발견 여부(discoveredLevels) 기반 UI 락 처리, LD 일러스트 팝업, 이름/레벨/수익 표시.
- **LD Discovery 연출**:
  - `PlayDiscoveryOnce`에서 LD 일러스트를 중앙 프리젠테이션 Root로 이동.
  - `discoverySpotlightPanel`의 Image 알파를 DOTween으로 제어하여 Spotlight 인/아웃.
  - SD 교체 시점에 빠른 페이드아웃(`FadeOutSpotlight`)으로 연출 마무리.

## 5. Animation & Feel
- **GirlCharacter 개선**:
  - 이동 방향 전환에 따라 스프라이트 Flip과 기준 스케일 재적용.
  - Idle Groove 애니메이션(미세 스케일 변화)과 자연스러운 점프/바운스 이징 적용.
  - Pulse/Bounce 시 드리프트 방지, Final 모드 시 화면 중앙 고정 연출.
- **DOTween 전면 활용**:
  - Jump, Bounce, Spotlight 페이드, 발견 팝업 등 모든 애니메이션을 DOTween으로 구성.
  - Sequence 기반의 OutBack, OutCubic, InOutSine 등 다양한 Ease 조합으로 생동감 강화.

## 6. Modular Architecture
- **GameSystem**: 모든 매니저 간 의존성 주입, Addressables 로딩 완료 시점 브로드캐스트.
- **EconomyManager**: 골드/업그레이드/자동화 로직을 중앙 집중화하고 이벤트로 UI 연동.
- **TierManager**: 4층 기반 배경 전환, 층별 Unlock 규칙, Ascend UI 제어.
- **ISaveable 기반 확장성**: 신규 시스템을 저장/로드에 쉽게 편입 가능.

## 7. Tools & Workflow Notes
- **Addressables**: SD/LD 라벨 구성 및 Key 규칙(숫자 기반) 문서화.
- **DOTween**: 프로젝트 시작 시 `DOTween Utility Panel`에서 Setup 필수.
- **에디터 체크리스트**: README 하단에 반드시 연결해야 할 필드 목록(Spotlight 패널, 도감 프리팹, 세이브 매니저 등) 정리.

---
이 문서(readmeanlist.md)는 포트폴리오 제출 시 프로젝트의 기술적 강점을 빠르게 파악할 수 있도록 만든 요약본입니다. 보다 상세한 사용법 및 실행 방법은 README.md의 본문을 참고하세요.

