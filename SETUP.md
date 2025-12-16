# Girls Evolution 2D - 프로젝트 설정 가이드

이 문서는 프로젝트를 처음 설정하거나 새로운 개발 환경에서 프로젝트를 열 때 필요한 모든 설정 단계를 안내합니다.

## 📋 목차

- [필수 요구사항](#-필수-요구사항)
- [Unity 프로젝트 열기](#-unity-프로젝트-열기)
- [패키지 설치 확인](#-패키지-설치-확인)
- [Addressables 설정](#-addressables-설정)
- [Google Play Games 설정](#-google-play-games-설정)
- [씬 설정](#-씬-설정)
- [튜토리얼 시스템 설정](#-튜토리얼-시스템-설정)
- [빌드 설정](#-빌드-설정)
- [테스트](#-테스트)

---

## 🔧 필수 요구사항

### Unity 버전
- **Unity 2022.3.62f3** (LTS)
- 다른 버전 사용 시 호환성 문제가 발생할 수 있습니다.

### 필수 도구
- Unity Hub
- Android SDK (Android 빌드 시)
- JDK (Java Development Kit)
- Git (버전 관리)

### 플랫폼
- **주 타겟**: Android (API Level 24+)
- **준비 중**: iOS

---

## 🚀 Unity 프로젝트 열기

1. **Unity Hub 실행**
2. **프로젝트 추가**
   - `Add` 버튼 클릭
   - 프로젝트 폴더 선택 (`girls_evolution2d`)
3. **Unity 버전 확인**
   - Unity Hub에서 프로젝트의 Unity 버전이 **2022.3.62f3**인지 확인
   - 버전이 다르면 올바른 버전으로 변경

---

## 📦 패키지 설치 확인

프로젝트를 열면 Unity가 자동으로 패키지를 설치합니다. 다음 패키지들이 설치되어 있는지 확인하세요:

### 필수 패키지
- **Addressables**: 1.22.3
- **TextMeshPro**: 3.0.7
- **Unity Purchasing**: 5.0.2
- **Unity Services Core**: 1.15.2

### 확인 방법
1. `Window` > `Package Manager` 열기
2. `In Project` 탭에서 패키지 목록 확인
3. 누락된 패키지가 있으면 `Packages/manifest.json` 확인

### 외부 패키지 (수동 설치 필요)
- **Google Play Games Plugin for Unity**
  - `Window` > `Package Manager` > `+` > `Add package from git URL...`
  - URL: `https://github.com/playgameservices/play-games-plugin-for-unity.git`
- **Google Mobile Ads SDK**
  - 이미 프로젝트에 포함되어 있음 (`Assets/GoogleMobileAds`)
- **DOTween**
  - Asset Store에서 구매 및 임포트 필요
  - 또는 프로젝트에 이미 포함되어 있을 수 있음

---

## 🎯 Addressables 설정

### 1. Addressables 그룹 확인

1. `Window` > `Asset Management` > `Addressables` > `Groups` 열기
2. 다음 그룹이 있는지 확인:
   - **Data**: CSV/TSV 데이터 파일
   - **SD**: 저해상도 스프라이트 (SD Label)
   - **LD**: 고해상도 스프라이트 (LD Label)

### 2. Addressables 빌드

**에디터에서 테스트할 때:**
- Fast Mode 사용 (빌드 불필요)

**실제 빌드 전:**
1. `Window` > `Asset Management` > `Addressables` > `Build` > `New Build` > `Default Build Script` 선택
2. 빌드 완료 대기
3. 빌드된 파일은 `ServerData/[BuildTarget]` 폴더에 생성됨

### 3. 스프라이트 라벨 설정

각 스프라이트에 다음 라벨 중 하나를 설정:
- `SD`: 저해상도 스프라이트
- `LD`: 고해상도 스프라이트
- `GirlSprites`: 모든 캐릭터 스프라이트

---

## 🎮 Google Play Games 설정

### 1. Google Play Console 설정

자세한 내용은 `old_md/GOOGLE_PLAY_GAMES_SETUP.md` 참고

**요약:**
1. Google Play Console에서 게임 서비스 생성
2. OAuth 2.0 클라이언트 ID 설정
3. SHA-1 인증서 지문 등록
4. Android Resources XML 다운로드
5. 업적 생성 (5개)

### 2. Unity 프로젝트 설정

1. `Window` > `Google Play Games` > `Setup` 열기
2. **Android Resources XML 붙여넣기**
   - Google Play Console에서 다운로드한 XML 내용 복사
   - "Resources Definition" 필드에 붙여넣기
3. **"Web App Client ID (Optional)" 필드는 비워두기**
4. Android 패키지 이름 확인 (`Project Settings` > `Player` > `Android`)
   - 현재: `com.MaybrSoft.girls_evolution`
5. "Setup" 버튼 클릭

### 3. SHA-1 인증서 지문 확인

**프로덕션 키스토어:**
```bash
keytool -list -v -keystore Assets/keystore.keystore -alias evolution
```

**디버그 키스토어:**
```bash
keytool -list -v -keystore "%USERPROFILE%\.android\debug.keystore" -alias androiddebugkey -storepass android -keypass android
```

---

## 🎬 씬 설정

### 메인 게임 씬 (Ingame.unity)

#### 필수 GameObject 설정

1. **GameSystem**
   - `GameSystem.cs` 스크립트 추가
   - Inspector에서 다음 참조 설정:
     - `spriteLoader`: `GirlSpriteAddressableLoader` 컴포넌트
     - `girlDataManager`: 자동 생성됨
     - `fieldManager`: `GirlFieldManager` 컴포넌트
     - `mergeManager`: `GirlMergeManager` 컴포넌트
     - `prestigeManager`: `PrestigeManager` 컴포넌트
     - `economy`: `EconomyManager` 컴포넌트

2. **SaveManager**
   - 빈 GameObject 생성 → 이름: `SaveManager`
   - `SaveManager.cs` 스크립트 추가
   - Inspector 설정:
     - `Auto Save Interval`: 60 (초)
     - `Enable Cloud Save`: ✅ 체크
     - `Auto Cloud Save`: ✅ 체크
     - `Sync On Start`: ✅ 체크 (선택사항)

3. **CloudSaveManager**
   - 빈 GameObject 생성 → 이름: `CloudSaveManager`
   - `CloudSaveManager.cs` 스크립트 추가
   - `DontDestroyOnLoad` 자동 적용됨

4. **AchievementManager**
   - 빈 GameObject 생성 → 이름: `AchievementManager`
   - `AchievementManager.cs` 스크립트 추가
   - Inspector에서 업적 ID 입력:
     - `Achievement Level 25`
     - `Achievement Clicks 1000`
     - `Achievement Play Time 1 Hour`
     - `Achievement Upgrade 10`
     - `Achievement Tier 1`

5. **PlayStatsTracker**
   - 빈 GameObject 생성 → 이름: `PlayStatsTracker`
   - `PlayStatsTracker.cs` 스크립트 추가
   - `DontDestroyOnLoad` 자동 적용됨

6. **TutorialManager**
   - 빈 GameObject 생성 → 이름: `TutorialManager`
   - `TutorialManager.cs` 스크립트 추가
   - 튜토리얼 UI 패널 설정 (아래 참고)

### 매니저 간 참조 확인

- `GirlFieldManager` → `TierManager` 참조 설정
- `GirlFieldManager` → `EconomyManager` 참조 설정
- `GirlFieldManager` → `GirlMergeManager` 참조 설정
- `PrestigeManager` → `TierManager` 참조 설정

---

## 📚 튜토리얼 시스템 설정

### 1. 튜토리얼 UI 패널 생성

Unity 에디터에서 다음 UI 요소들을 생성:

1. **튜토리얼 패널 루트** (`tutorialPanelRoot`)
   - Canvas 하위에 Panel 생성
   - 초기 상태: 비활성화

2. **튜토리얼 패널 구성 요소**
   - `titleText`: TextMeshProUGUI (제목)
   - `messageText`: TextMeshProUGUI (메시지)
   - `nextButton`: Button (다음 버튼)
   - `skipButton`: Button (스킵 버튼)
   - `closeButton`: Button (닫기 버튼)

3. **오버레이 패널** (`overlayPanel`)
   - 불투명한 Panel (알파 0.8)
   - 튜토리얼 중 다른 UI 상호작용 방지용

4. **하이라이트 오버레이** (`highlightOverlay`)
   - 선택사항
   - 특정 UI 요소 강조 표시용

### 2. TutorialManager Inspector 설정

1. `TutorialManager` GameObject 선택
2. Inspector에서 다음 설정:
   - `Tutorial Panel` 섹션:
     - `Tutorial Panel Root`: 튜토리얼 패널 루트 GameObject 드래그
     - `Title Text`: 제목 TextMeshProUGUI 드래그
     - `Message Text`: 메시지 TextMeshProUGUI 드래그
     - `Next Button`: 다음 버튼 드래그
     - `Skip Button`: 스킵 버튼 드래그
     - `Close Button`: 닫기 버튼 드래그
   - `Tutorial Steps` 섹션:
     - 배열 크기 설정 (튜토리얼 단계 수)
     - 각 단계별 설정:
       - `Step Name`: 단계 이름
       - `Title`: 제목 텍스트
       - `Message`: 메시지 텍스트
       - `Target Object Name`: 하이라이트할 GameObject 이름 (선택사항)
       - `Wait For Action`: 사용자 액션 대기 여부
       - `Action To Wait`: 대기할 액션 이름
         - `Summon`: 소환 버튼 클릭
         - `SummonTwice`: 소환 버튼 2번 클릭
         - `Merge`: 합성 완료
         - `ShopOpen`: 상점 열기
         - `TierSwitch`: 티어 이동
         - `AutoSpawn`: 자동소환 활성화
         - `AutoMerge`: 자동합성 활성화

### 3. 튜토리얼 단계 예시

```csharp
// 예시: 첫 번째 단계 (게임 소개)
Step Name: "Welcome"
Title: "게임에 오신 것을 환영합니다!"
Message: "이 게임은 캐릭터를 합성하여 레벨을 올리는 방치형 게임입니다."
Wait For Action: false

// 예시: 소환 안내
Step Name: "Summon"
Title: "캐릭터 소환하기"
Message: "소환 버튼을 눌러 캐릭터를 소환하세요."
Target Object Name: "SummonButton"  // 실제 버튼 이름
Wait For Action: true
Action To Wait: "SummonTwice"
```

---

## 🏗️ 빌드 설정

### Android 빌드 설정

1. **File** > **Build Settings** > **Android** 선택
2. **Player Settings** 확인:
   - **Package Name**: `com.MaybrSoft.girls_evolution`
   - **Minimum API Level**: 24 (Android 7.0)
   - **Target API Level**: 최신 권장
   - **Internet Access**: Required
   - **Scripting Backend**: IL2CPP (권장)
   - **Target Architectures**: ARM64, ARMv7

### Keystore 설정

1. **Player Settings** > **Publishing Settings**
2. **Keystore Manager**에서 키스토어 생성 또는 기존 키스토어 사용
3. **프로젝트 키스토어 사용**:
   - Keystore: `Assets/keystore.keystore`
   - Alias: `evolution`
   - 비밀번호는 프로젝트 설정에서 확인

### Addressables 빌드 (빌드 전 필수)

1. `Window` > `Asset Management` > `Addressables` > `Build` > `New Build` > `Default Build Script`
2. 빌드 완료 대기
3. 빌드된 파일이 `ServerData/[BuildTarget]` 폴더에 생성됨

### 빌드 실행

1. **File** > **Build Settings**
2. **Build** 또는 **Build And Run** 클릭
3. APK/AAB 파일 생성 완료

---

## 🧪 테스트

### 에디터에서 테스트

1. **Play 버튼** 클릭
2. 다음 사항 확인:
   - 게임 시작 시 데이터 로드
   - 튜토리얼 표시 (첫 실행 시)
   - 캐릭터 소환/합성 동작
   - 골드 수익 계산
   - 저장 시스템 동작

### 실제 기기에서 테스트

1. **개발 빌드 생성**
2. **테스트 기기에 설치**
3. **Google Play Games 로그인 확인**
   - 테스트 계정으로 로그인
   - 클라우드 저장 동작 확인
   - 업적 달성 확인

### 로그 확인

Unity Console에서 다음 로그 확인:
- `[GameSystem]`: 시스템 초기화
- `[SaveManager]`: 저장/로드
- `[CloudSaveManager]`: 클라우드 저장
- `[AchievementManager]`: 업적 달성
- `[TutorialManager]`: 튜토리얼 진행

---

## 📝 체크리스트

### 초기 설정 완료 체크리스트

- [ ] Unity 2022.3.62f3 설치 및 프로젝트 열기
- [ ] 필수 패키지 설치 확인
- [ ] Addressables 그룹 설정 확인
- [ ] Google Play Games SDK 설정
- [ ] SHA-1 인증서 지문 등록
- [ ] 씬에 필수 GameObject 생성 및 설정
- [ ] TutorialManager UI 패널 생성 및 설정
- [ ] 매니저 간 참조 설정 확인
- [ ] Android 빌드 설정 확인
- [ ] Keystore 설정 확인
- [ ] 에디터에서 테스트
- [ ] 실제 기기에서 테스트

---

## 🔍 문제 해결

### Addressables 빌드 오류
- Addressables 그룹이 올바르게 설정되었는지 확인
- 스프라이트 라벨이 올바르게 설정되었는지 확인

### Google Play Games 로그인 실패
- SHA-1 인증서 지문이 Google Play Console에 등록되었는지 확인
- Android Resources XML이 올바르게 붙여넣어졌는지 확인
- 테스트 계정이 추가되었는지 확인

### 튜토리얼이 표시되지 않음
- `TutorialManager` GameObject가 씬에 있는지 확인
- `tutorialPanelRoot`가 Inspector에 설정되었는지 확인
- `tutorialSteps` 배열이 올바르게 설정되었는지 확인

### 저장 시스템 오류
- `SaveManager` GameObject가 씬에 있는지 확인
- 저장 경로 권한 확인 (Android)

---

## 📚 추가 문서

- **README.md**: 프로젝트 전체 개요 및 아키텍처
- **old_md/GOOGLE_PLAY_GAMES_SETUP.md**: Google Play Games 상세 설정 가이드
- **old_md/EDITOR_SETUP_GUIDE.md**: Unity 에디터 설정 체크리스트
- **old_md/ACHIEVEMENT_SETUP.md**: 업적 설정 가이드

---

**마지막 업데이트**: 2024년

