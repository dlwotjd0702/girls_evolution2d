# Girls Evolution 2D – 스크립트 구조 & 읽기 가이드

이 폴더는 실제 개발/유지보수 시 참고하기 좋은 **정리본 문서들만** 모아두는 영역입니다.  
이전 작업 메모, TODO, 상세 체크리스트 등은 `Assets/Scripts/old_md` 폴더에 보관합니다.

---

## 1. 전체 개요

- **장르**: 2D 방치형/진화형 합성 게임
- **핵심 루프**
  - 같은 레벨 소녀 2명을 합성 → 상위 레벨 생성
  - 자동/수동 골드 수익 → 업그레이드/자동화 구매
  - 레벨 25 달성 후 **환생(Prestige)** → 영구 보너스 획득

게임 전체 개요와 데이터 구조, 상세 TODO는 기존 문서들을 참고하세요:

- 프로젝트 하이레벨 설명: `Assets/Scripts/old_md/README.md`
- 포트폴리오용 기술 요약: `Assets/Scripts/old_md/readmeanalist.md`
- 패널/TODO 상세: `Assets/Scripts/old_md/PANEL_TODO.md`
- 에디터 필드 체크리스트: `Assets/Scripts/old_md/EDITOR_SETUP_CHECKLIST.md`

---

## 2. 스크립트 폴더 구조 (요약)

```text
Assets/Scripts/
├── Mainsystem/          # 핵심 게임 시스템 (GameSystem, PrestigeManager 등)
├── Girl/                # 캐릭터 로직 & 데이터 (GirlFieldManager, GirlDataManager 등)
├── Shop/                # Shop, Economy, PrestigeShop 관련
├── Save_Datas/          # SaveData, SaveManager
├── Tiers/               # 층(Tier) 시스템
├── UI/                  # 공통 UI 관리
├── Interface/           # ISaveable 등 인터페이스 모음
├── read/                # ✅ 정리된 읽기용 문서 (현재 파일 위치)
└── old_md/              # 📦 예전/작업용 문서 아카이브
```

각 서브 시스템에 대한 더 깊은 설명이 필요하면 `old_md/README.md`와 `old_md/readmeanalist.md`를 함께 보면 흐름을 빠르게 이해할 수 있습니다.

---

## 3. 빠르게 보면 좋은 흐름들

- **게임 시작**
  - `GameSystem.Awake()` → 매니저 싱글톤/의존성 주입
  - `SaveManager.Start()` → 저장 데이터 로드
  - `GameSystem.Start()` → CSV/TSV + 스프라이트 로드 → `AssetsReady` 브로드캐스트

- **골드 수익**
  - `GirlFieldManager.Update()` → `ComputeIdleGoldPerSec()` → `EconomyManager.AddGold()`

- **합성**
  - 드래그/자동 합성 → `GirlMergeManager.TryMerge()` → 합성 애니메이션 + 보너스 골드

- **환생**
  - `PrestigeManager.OnClickPrestigeButton()` → 확인 패널 → `DoPrestige()`

상세 시그니처/필드는 실제 C# 코드 쪽에서, 연결/세팅 관련 내용은 `old_md` 내 체크리스트 문서들을 참고해 주세요.

---

## 4. 문서 사용 규칙 제안

- **새로 문서 정리할 때**
  - 최종본/정리본은 이 `read` 폴더에 추가
  - 초안, 회의 메모, 작업용 TODO는 `old_md`에 저장

- **기존 md를 손댈 때**
  - “지우기 애매한” 내용은 삭제 대신 `old_md`로 이동
  - 새 구조/요약만 이 폴더에 간단하게 남기기

이 규칙만 지키면, 나중에 프로젝트를 다시 열었을 때도 **`read` 폴더만 보면 현재 상태를 바로 파악**할 수 있습니다.  
더 세분화된 문서(예: `UI_Guide.md`, `SaveSystem_Design.md`)가 필요하면 이 폴더 안에 추가로 만들어도 좋습니다.


