# Girls Evolution 2D

Unity 기반 2D 방치형/진화형 합성 게임 프로젝트의 기술 문서입니다.

## 목차
- 게임 개요
- 시스템 아키텍처
- 핵심 시스템 상세
- 데이터 파이프라인
- 저장/클라우드
- 성능 최적화
- 설계 패턴
- 프로젝트 구조
- 빌드 및 실행
- 관련 문서

---

## 게임 개요
### 핵심 게임플레이
- **합성 시스템**: 같은 레벨 캐릭터 2개를 합성하면 다음 레벨 캐릭터 생성
- **25단계 진화**: 레벨 1~25까지 단계적 진화
- **4층 구조**: 0층(1~8), 1층(9~16), 2층(17~24), 3층(25)
- **방치형 수익**: 캐릭터가 자동으로 골드 생성
- **환생 시스템**: 레벨 25 달성 후 환생하여 영구 보너스 획득

### 주요 시스템 요약
1. **골드 시스템**: 레벨 기반 지수 성장 수익
2. **자동화**: 자동 소환/합성 업그레이드
3. **업그레이드**: 수동 소환, 필드 확장, 클릭 보너스 등
4. **환생 상점**: 환생 포인트로 영구 강화 구매
5. **프리미엄 통화**: 보석 기반 강화 루트

---

## 시스템 아키텍처
### 초기화 플로우
```
SaveManager (ExecutionOrder: -200)
  └─> GameSystem (ExecutionOrder: -100)
      ├─> GirlDataManager.LoadAsync()        # CSV/TSV 데이터 로드
      ├─> GirlSpriteAddressableLoader        # SD/LD 스프라이트 비동기 로드
      └─> AssetsReadyEvent 브로드캐스트
```

### 게임 루프 구조
```
GirlFieldManager.Update()
  ├─> ComputeIdleGoldPerSec()                # 1초마다 골드 지급
  ├─> TryAutoSpawn()                         # 자동 소환 타이머
  ├─> TryAutoMerge()                         # 자동 합성 타이머
  └─> UpdateSpawnCharge()                    # 수동 소환 차지
```

---

## 핵심 시스템 상세
### 1) 수익 계산 흐름
```
ComputeIdleGoldPerSec()
  └─> EconomyManager.GetLevelIncomePerSec(level) 합산
  └─> 25단계 스택(level25UpgradeLevel)은 최종 단계 수익에 스택 곱
  └─> 계승 등급 배수 × 환생 상점 배수 적용
  └─> 1초마다 EconomyManager.AddGold() 호출
```

### 2) 클릭 보너스
- (기본 10% + 레벨당 1%) × 환생 클릭 배수
- 클릭 수익은 `Math.Ceiling()`으로 올림 처리

### 3) 합성 흐름
```
TryMerge()
  ├─> 레벨 검증
  ├─> +2단 도약 확률(계승 + 환생 상점, cap 20%)
  ├─> 합성 애니메이션(DOTween)
  └─> 새 캐릭터 생성
```

### 4) 환생 포인트 규칙(현재 기준)
- 25단계 스택: 1레벨 2500, 이후 레벨마다 +1000
- 하위 단계: 25단계 기준에서 단계 내려갈 때마다 1/2
- 강화 레벨 보정: 각 강화 레벨당 100 포인트
- 최종 획득량은 환생 상점 배율 적용

### 5) 환생 상점(12종)
- 수익 배수
- +2단 도약 확률
- 시작 자금 배수
- 환생 포인트 획득량
- 수동 소환 최대치/쿨타임 보정
- 자동 소환/합성 간격 보정
- 필드 최대 칸수 보정
- 클릭 보너스 배수
- 오프라인 보상 배수/시간 보정

---

## 데이터 파이프라인
### 캐릭터 데이터 로딩
- Addressables TextAsset 비동기 로드
- CSV/TSV 자동 판별(탭/쉼표)
- 컬럼명 자동 매칭

### 스프라이트 로딩
- SD 스프라이트 우선 표시
- LD 스프라이트 순차 로드(발견 연출)
- Addressables 라벨 그룹 로딩

---

## 저장/클라우드
- 로컬 JSON 저장 + PlayerPrefs 호환 저장
- 저장 백업(`save_backup.json`) 생성
- 오프라인 보상은 저장 시간 기준 계산
- Google Play Games 클라우드 저장 연동
- 로그인/광고 로드 재시도 로직 포함

---

## 성능 최적화
- 객체 풀링으로 UI 인스턴스 최소화
- 거리 계산 시 sqrMagnitude 사용
- 코루틴 대기 객체 재사용으로 GC 최소화
- 자동 합성 O(N²) 스파이크 방지(조기 종료)

---

## 설계 패턴
- 싱글톤: `GameSystem`, `SaveManager`, `PremiumCurrencyManager` 등
- 인터페이스 기반 저장: `ISaveable`
- 이벤트 기반 UI 갱신(값 변경 시만 갱신)
- 리플렉션 안전 호출로 선택적 연동 지원

---

## 프로젝트 구조
```
Assets/Scripts/
  Mainsystem/        핵심 시스템
  Girl/              캐릭터/필드
  Shop/              경제/상점
  Save_Datas/        저장/클라우드
  UI/                UI 패널
  makesomemoney/     광고/재화
```

---

## 빌드 및 실행
- 실행 씬: `Assets/Scenes/Ingame.unity`
- 에디터 저장: `EditorSaves/save.json`
- 빌드 저장: `Application.persistentDataPath/save.json`

---

## 관련 문서
- `old_md/PROJECT_SUMMARY.md` (기획/뉘앙스 기록용)

