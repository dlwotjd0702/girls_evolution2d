# Girls Evolution 2D - TODO 리스트

> **마지막 업데이트**: 플레이 디버그 정보 시스템 구현 완료

---

## 📊 프로젝트 개요

**Unity 2022.3.62f3** 기반 2D 방치형/진화형 합성 게임
- **핵심 시스템**: 25단계 캐릭터 진화, 합성 시스템, 방치형 수익, 환생 시스템
- **기술 스택**: Addressables, DOTween, TextMeshPro, Google Mobile Ads, Unity Purchasing
- **플랫폼**: Android (iOS 준비 중)

---

## 🔥 우선순위 작업 (즉시 진행)

### 1. 튜토리얼 시스템
- **설명**: 첫 실행 시 게임 조작법 안내
- **기능 요구사항**:
  - [ ] `TutorialManager.cs` 클래스 생성
  - [ ] 단계별 가이드 UI 패널
  - [ ] 소환 방법 안내
  - [ ] 합성 방법 안내 (드래그 앤 드롭)
  - [ ] 상점 사용법 안내
  - [ ] 티어 이동 방법 안내
  - [ ] 자동소환/자동합성 활성화 방법 안내
  - [ ] 튜토리얼 진행 상태 저장 (`SaveData`에 `tutorialCompleted` 필드 추가)
  - [ ] 스킵 기능
- **관련 파일**: 
  - `Assets/Scripts/UI/TutorialManager.cs` (신규 생성)
  - `Assets/Scripts/Save_Datas/SaveData.cs` (필드 추가)

### 2. 온라인 저장 (Google Play Games)
- **설명**: Google Play Games 로그인 연동 및 클라우드 저장
- **기능 요구사항**:
  - [ ] Google Play Games SDK 연동
  - [ ] 로그인/로그아웃 기능
  - [ ] 클라우드 저장/로드 기능
  - [ ] 기기 간 동기화
  - [ ] 로컬 저장과 클라우드 저장 병행
  - [ ] 충돌 해결 로직 (최신 세이브 우선 또는 사용자 선택)
- **관련 파일**: 
  - `Assets/Scripts/Save_Datas/CloudSaveManager.cs` (신규 생성)
  - `Assets/Scripts/Save_Datas/SaveManager.cs` (클라우드 저장 연동)

### 3. 플레이 디버그 정보 (SaveData 추가) ✅ 완료
- **설명**: 밸런스 패치용 플레이 통계 수집
- **추가할 필드** (`SaveData.cs`):
  - [x] `totalPlayTimeSeconds` (double) - 총 플레이타임 (초)
  - [x] `firstPlayTime` (string) - 최초 플레이 시간
  - [x] `lastPlayTime` (string) - 마지막 플레이 시간
  - [x] `level25ReachedCount` (int) - 레벨 25 달성 횟수
  - [x] `level25ReachedTimes` (List<string>) - 레벨 25 달성 시간 목록
  - [x] `totalClicks` (long) - 총 클릭 횟수
  - [x] `totalMerges` (long) - 총 합성 횟수
  - [x] `totalSpawns` (long) - 총 소환 횟수
  - [x] `totalGoldEarned` (double) - 총 획득 골드
  - [x] `totalGoldSpent` (double) - 총 소비 골드
- **구현 작업**:
  - [x] `PlayStatsTracker.cs` 클래스 생성 (통계 수집)
  - [x] 각 매니저에서 통계 업데이트 호출
  - [x] 세이브/로드 시 통계 저장/복원 (ISaveable 인터페이스로 자동 연동)
- **관련 파일**: 
  - `Assets/Scripts/Save_Datas/SaveData.cs` (필드 추가 완료)
  - `Assets/Scripts/PlayStatsTracker.cs` (생성 완료)
  - `Assets/Scripts/Girl/GirlFieldManager.cs` (통계 업데이트 완료)
  - `Assets/Scripts/Mainsystem/GirlMergeManager.cs` (통계 업데이트 완료)
  - `Assets/Scripts/Shop/EconomyManager.cs` (통계 업데이트 완료)
  - `Assets/Scripts/Girl/GirlCharacter.cs` (클릭 통계 업데이트 완료)
- **참고**: PlayStatsTracker는 씬에 GameObject로 추가되어야 합니다 (DontDestroyOnLoad 적용)

---

## ⚖️ 중간 우선순위 (간단한 최적화)

### 4. 간단한 최적화 작업

#### 4-1. 디버그 로그 레벨링 (간단)
- **작업 내용**:
  - [ ] `Log.cs` 래퍼 클래스 생성
  - [ ] `#if UNITY_EDITOR` 또는 빌드 플래그로 디버그 로그 제어
  - [ ] 주요 `Debug.Log` 호출을 `Log.Info`로 변경
- **예상 작업량**: 낮음
- **관련 파일**: 전체 프로젝트

#### 4-2. 자동합성 티어 필터링 (간단)
- **작업 내용**:
  - [ ] `TryAutoMerge()`에서 현재 티어의 캐릭터만 필터링
  - [ ] `TierManager.CurrentTierIndex`와 `TierRules.TierIndexFromLevel()` 사용
- **예상 작업량**: 낮음
- **관련 파일**: 
  - `Assets/Scripts/Mainsystem/GirlMergeManager.cs` (line 207-271)

#### 4-3. 합성 후 발견 연출 개선 (간단)
- **작업 내용**:
  - [ ] `GirlCharacter.cs`에 `PlayMergeBounce()` 메서드 추가
  - [ ] `MergeRoutine()`에서 발견 여부 확인 후 바운스 연출
- **예상 작업량**: 낮음
- **관련 파일**: 
  - `Assets/Scripts/Girl/GirlCharacter.cs`
  - `Assets/Scripts/Mainsystem/GirlMergeManager.cs`

---

## 📦 후순위 작업 (복잡한 최적화)

### 5. 복잡한 최적화 (성능 지장 없으면 나중에)

#### 5-1. SaveManager 최적화 (복잡)
- **설명**: `FindObjectsOfType` 제거, 등록 시스템으로 변경
- **상태**: 현재 성능 지장 없음 → 후순위
- **관련 파일**: `Assets/Scripts/Save_Datas/SaveManager.cs`

#### 5-2. 코루틴 최적화 (복잡)
- **설명**: 글로벌 타이머 방식으로 전환
- **상태**: 현재 성능 지장 없음 → 후순위
- **관련 파일**: `Assets/Scripts/Girl/GirlCharacter.cs`

---

## 📋 UI 작업 (Unity 에디터)

### 6. UI 프리팹 제작
- **설명**: 코드는 완료, Unity 에디터에서 UI만 제작 필요

#### 6-1. 튜토리얼 UI
- [ ] 튜토리얼 가이드 패널
- [ ] 단계별 안내 UI
- [ ] 스킵 버튼

#### 6-2. 쿠폰 입력 UI
- [ ] 쿠폰 패널 루트 GameObject
- [ ] 타이틀 텍스트 (TextMeshProUGUI)
- [ ] 쿠폰 코드 입력 필드 (TMP_InputField)
- [ ] 확인 버튼
- [ ] 닫기 버튼
- [ ] Reason Label (TextMeshProUGUI)

#### 6-3. 도감 패널 UI
- [ ] 슬롯 프리팹 제작
- [ ] 그리드 레이아웃 구성
- [ ] 상세 팝업 패널 구성
- **관련 파일**: `Assets/Scripts/UI/EncyclopediaPanelController.cs`

#### 6-4. 세이브 파일 관리 UI
- [ ] 리셋/삭제 버튼 UI
- [ ] 세이브 파일 경로 표시
- **관련 파일**: `Assets/Scripts/Save_Datas/SaveManager.cs`

#### 6-5. 종료 확인 패널 UI
- [ ] 종료 확인 패널 UI 제작
- **관련 파일**: `Assets/Scripts/ExitPanelToggler.cs`

#### 6-6. 골드 부족 패널 UI
- [ ] 패널 루트 GameObject
- [ ] 메시지 텍스트, 광고 보상 텍스트
- [ ] 광고 시청 버튼, 아이콘
- **관련 파일**: `Assets/Scripts/makesomemoney/InsufficientFundsPanel.cs`

---

## 🎯 작업 우선순위 순서

### 1단계: 핵심 기능 구현 (즉시)
1. ✅ **플레이 디버그 정보 추가** - SaveData에 필드 추가 및 통계 수집 시스템 (완료)
2. **튜토리얼 시스템** - 첫 실행 가이드
3. **온라인 저장** - Google Play Games 연동

### 2단계: 간단한 최적화 (1-2주)
4. 디버그 로그 레벨링
5. 자동합성 티어 필터링
6. 합성 후 발견 연출 개선

### 3단계: UI 작업 (Unity 에디터)
7. 튜토리얼 UI
8. 쿠폰 입력 UI
9. 도감 패널 UI
10. 기타 UI 프리팹

### 4단계: 복잡한 최적화 (나중에)
11. SaveManager 최적화
12. 코루틴 최적화

---

## 📚 참고 문서

- [프로젝트 README](README.md) - 전체 개요 및 아키텍처
- [패치 노트](PATCH_NOTES.md) - 완료된 작업 목록
- [기술 분석 보고서](Assets/Scripts/read/analysis_report.md) - 시스템 분석
- [에디터 설정 체크리스트](Assets/Scripts/old_md/EDITOR_SETUP_CHECKLIST.md) - Unity 에디터 설정

---

## 📌 참고사항

- 각 작업은 독립적으로 진행 가능
- 대규모 기능들은 세부 설계가 필요할 수 있음
- 밸런스 조정은 플레이 테스트가 필수
- 성능 최적화는 프로파일링 후 진행 권장
- UI 작업은 Unity 에디터에서 직접 작업 필요

---

**마지막 업데이트**: 플레이 디버그 정보 시스템 구현 완료 (2024년)
