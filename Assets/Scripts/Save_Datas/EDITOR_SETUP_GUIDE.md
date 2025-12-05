# Unity 에디터 설정 가이드

## 필수 설정 항목

### 1. Google Play Games 설정

#### 1.1 CloudSaveManager GameObject 설정
1. **씬에 GameObject 생성**
   - Hierarchy에서 우클릭 → `Create Empty`
   - 이름: `CloudSaveManager`
   - `CloudSaveManager.cs` 스크립트 추가

2. **DontDestroyOnLoad 확인**
   - 스크립트가 자동으로 설정함

#### 1.2 AchievementManager GameObject 설정
1. **씬에 GameObject 생성**
   - Hierarchy에서 우클릭 → `Create Empty`
   - 이름: `AchievementManager`
   - `AchievementManager.cs` 스크립트 추가

2. **업적 ID 설정** (Inspector)
   - `Achievement Level 25`: 레벨 25 달성 업적 ID 입력
   - `Achievement Clicks 1000`: 클릭 1,000회 업적 ID 입력
   - `Achievement Play Time 1 Hour`: 1시간 플레이 업적 ID 입력
   - `Achievement Upgrade 10`: 강화 10레벨 업적 ID 입력
   - `Achievement Tier 1`: 1계층 발견 업적 ID 입력
   - **참고**: Google Play Console에서 업적 생성 후 ID 입력 필요

3. **DontDestroyOnLoad 확인**
   - 스크립트가 자동으로 설정함

#### 1.3 Google Play Games SDK 설정
1. **Window → Google Play Games → Setup**
2. **Android Resources XML 붙여넣기**
   - Google Play Console에서 복사한 XML 붙여넣기
   - "Web App Client ID (Optional)" 필드는 비워둠

3. **설정 확인**
   - `Assets/Plugins/Android/GooglePlayGamesManifest.androidlib/AndroidManifest.xml` 확인

### 2. PlayStatsTracker GameObject 설정

1. **씬에 GameObject 생성**
   - Hierarchy에서 우클릭 → `Create Empty`
   - 이름: `PlayStatsTracker`
   - `PlayStatsTracker.cs` 스크립트 추가

2. **DontDestroyOnLoad 확인**
   - 스크립트가 자동으로 설정함

### 3. SaveManager GameObject 설정

1. **씬에 GameObject 생성**
   - Hierarchy에서 우클릭 → `Create Empty`
   - 이름: `SaveManager`
   - `SaveManager.cs` 스크립트 추가

2. **Inspector 설정**
   - `Auto Save Interval`: 60 (초)
   - `Enable Cloud Save`: ✅ 체크
   - `Auto Cloud Save`: ✅ 체크
   - `Sync On Start`: ✅ 체크

3. **DontDestroyOnLoad 확인**
   - 스크립트가 자동으로 설정함


## 씬 설정 체크리스트

### 메인 게임 씬 (Ingame.unity)
- [ ] `CloudSaveManager` GameObject 존재
- [ ] `AchievementManager` GameObject 존재
- [ ] `PlayStatsTracker` GameObject 존재
- [ ] `SaveManager` GameObject 존재

### 각 매니저 참조 확인
- [ ] `GirlFieldManager`가 `TierManager` 참조 설정
- [ ] `GirlFieldManager`가 `EconomyManager` 참조 설정
- [ ] `GirlFieldManager`가 `GirlMergeManager` 참조 설정
- [ ] 기타 매니저 간 참조 확인

## Google Play Console 설정

### 업적 생성
자세한 내용은 `ACHIEVEMENT_SETUP.md` 파일을 참고하세요.

## 빌드 설정

### Android 빌드 설정
1. **File → Build Settings → Android**
2. **Player Settings 확인:**
   - Package Name 설정
   - Minimum API Level: 21 이상
   - Target API Level: 최신 권장
   - Internet Access: Required

### Keystore 설정
1. **Player Settings → Publishing Settings**
2. **Keystore Manager**에서 키스토어 생성 또는 기존 키스토어 사용
3. **SHA-1 인증서 지문 확인** (Google Play Console OAuth 클라이언트 ID 설정에 필요)

## 테스트 설정

### 테스트 계정 추가
1. **Google Play Console → 게임 서비스 → 설정 및 관리 → 테스트**
2. **테스트 계정 추가** (Gmail 주소)
3. **테스트 기기에서 해당 계정으로 로그인**

### 로그 확인
- Unity Console에서 다음 로그 확인:
  - `[CloudSaveManager]` 로그인 성공/실패
  - `[AchievementManager]` 업적 달성 로그

## 문제 해결

### 로그인이 안 되는 경우
1. SHA-1 인증서 지문이 Google Play Console에 등록되었는지 확인
2. OAuth 클라이언트 ID가 올바르게 설정되었는지 확인
3. Android Resources XML이 올바르게 붙여넣어졌는지 확인
4. 테스트 계정이 추가되었는지 확인

### 업적이 달성되지 않는 경우
1. 성취도 ID가 올바른지 확인
2. 성취도가 게시되었는지 확인
3. 로그인 상태 확인
4. 조건이 올바르게 달성되었는지 확인

## 참고 문서

- `GOOGLE_PLAY_GAMES_SETUP.md`: Google Play Games SDK 설정 상세 가이드
- `ACHIEVEMENT_SETUP.md`: 업적 설정 가이드
- `OFFLINE_ENVIRONMENT_CHECK.md`: 오프라인 환경 검수 보고서
