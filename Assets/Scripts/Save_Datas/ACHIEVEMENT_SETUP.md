# 업적 설정 가이드

## 1. Google Play Console에서 업적 생성

### 1.1 업적 생성
1. [Google Play Console](https://play.google.com/console) 접속
2. 게임 선택
3. `게임 서비스` > `성취도` 이동
4. `성취도 만들기` 클릭

### 1.2 업적 설정 (5개 생성 필요)

#### 업적 1: 레벨 25 달성
- **이름**: "레벨 25 달성" 또는 "Level 25 Reached"
- **설명**: "최고 레벨 25에 도달하세요"
- **업적 ID 복사**: 예) `CgkI-XXXXXXXXXXXXXX`

#### 업적 2: 클릭 1,000회
- **이름**: "클릭 1,000회" 또는 "1,000 Clicks"
- **설명**: "총 1,000회 클릭하세요"
- **업적 ID 복사**: 예) `CgkI-YYYYYYYYYYYYYY`

#### 업적 3: 1시간 플레이
- **이름**: "1시간 플레이" 또는 "1 Hour Played"
- **설명**: "총 1시간 플레이하세요"
- **업적 ID 복사**: 예) `CgkI-ZZZZZZZZZZZZZ`

#### 업적 4: 강화 10레벨
- **이름**: "강화 10레벨" 또는 "Upgrade Level 10"
- **설명**: "어떤 강화라도 10레벨에 도달하세요"
- **업적 ID 복사**: 예) `CgkI-WWWWWWWWWWWWW`

#### 업적 5: 1계층 발견
- **이름**: "1계층 발견" 또는 "Tier 1 Discovered"
- **설명**: "1계층을 발견하세요"
- **업적 ID 복사**: 예) `CgkI-VVVVVVVVVVVVV`

### 1.3 업적 게시
- 각 업적을 생성한 후 **"게시"** 버튼 클릭
- 게시되지 않은 업적은 사용할 수 없습니다

## 2. Unity 프로젝트에 업적 ID 설정

### 방법 1: Inspector에서 설정 (권장)

1. Unity 에디터에서 `AchievementManager` GameObject 선택
2. Inspector 창에서 다음 필드에 업적 ID 입력:
   - **Achievement Level 25**: 레벨 25 달성 업적 ID
   - **Achievement Clicks 1000**: 클릭 1,000회 업적 ID
   - **Achievement Play Time 1 Hour**: 1시간 플레이 업적 ID
   - **Achievement Upgrade 10**: 강화 10레벨 업적 ID
   - **Achievement Tier 1**: 1계층 발견 업적 ID

### 방법 2: 코드에서 직접 수정

`Assets/Scripts/AchievementManager.cs` 파일을 열고 다음 부분 수정:

```csharp
[Header("Achievement IDs")]
[Tooltip("Google Play Console에서 설정한 업적 ID를 입력하세요")]
[SerializeField] private string achievementLevel25 = "CgkI-XXXXXXXXXXXXXX"; // 여기에 실제 ID 입력
[SerializeField] private string achievementClicks1000 = "CgkI-YYYYYYYYYYYYYY"; // 여기에 실제 ID 입력
[SerializeField] private string achievementPlayTime1Hour = "CgkI-ZZZZZZZZZZZZZ"; // 여기에 실제 ID 입력
[SerializeField] private string achievementUpgrade10 = "CgkI-WWWWWWWWWWWWW"; // 여기에 실제 ID 입력
[SerializeField] private string achievementTier1 = "CgkI-VVVVVVVVVVVVV"; // 여기에 실제 ID 입력
```

**예시:**
```csharp
[SerializeField] private string achievementLevel25 = "CgkI-1234567890123456";
[SerializeField] private string achievementClicks1000 = "CgkI-2345678901234567";
[SerializeField] private string achievementPlayTime1Hour = "CgkI-3456789012345678";
[SerializeField] private string achievementUpgrade10 = "CgkI-4567890123456789";
[SerializeField] private string achievementTier1 = "CgkI-5678901234567890";
```

## 3. 업적 ID 확인 방법

### Google Play Console에서 확인
1. `게임 서비스` > `성취도` 이동
2. 생성한 업적 클릭
3. 업적 상세 페이지에서 **"업적 ID"** 확인
4. 형식: `CgkI-`로 시작하는 긴 문자열

### 업적 ID 형식
- 항상 `CgkI-`로 시작
- 그 뒤에 숫자와 문자가 조합된 긴 문자열
- 예: `CgkI-12345678901234567890123456789012`

## 4. 자동 업적 체크 확인

업적 ID를 설정하면 다음 상황에서 자동으로 업적이 체크됩니다:

### 자동 체크 시점
1. **5초마다**: 모든 업적 조건 자동 체크
2. **레벨 25 달성 시**: 자동으로 업적 달성
3. **클릭 1,000회 달성 시**: 자동으로 업적 달성
4. **1시간 플레이 달성 시**: 자동으로 업적 달성
5. **강화 10레벨 달성 시**: 자동으로 업적 달성
6. **1계층 발견 시**: 자동으로 업적 달성

### 업적 체크 조건

- **레벨 25 달성**: `GirlFieldManager.CurrentMaxLevel >= 25`
- **클릭 1,000회**: `PlayStatsTracker.GetTotalClicks() >= 1000`
- **1시간 플레이**: `PlayStatsTracker.GetTotalPlayTimeSeconds() >= 3600`
- **강화 10레벨**: 모든 강화 중 최대값 >= 10
- **1계층 발견**: `unlockedTierMask`에 1계층 비트가 설정됨

## 5. 업적 UI 표시

### 업적 UI 열기
```csharp
// 업적 UI 표시
AchievementManager.Instance.ShowAchievements();
```

### UI 버튼 연결 예시
```csharp
// 버튼 OnClick 이벤트에 연결
public void OnClickAchievementButton()
{
    AchievementManager.Instance.ShowAchievements();
}
```

## 6. 테스트

### 테스트 계정 설정
1. Google Play Console > `게임 서비스` > `설정 및 관리` > `테스트` 이동
2. 테스트 계정 추가 (Gmail 주소)
3. 테스트 계정으로 로그인하여 업적 확인

### 테스트 체크리스트
- [ ] 업적 ID가 올바르게 설정되었는지 확인
- [ ] 업적이 게시되었는지 확인
- [ ] 테스트 계정이 추가되었는지 확인
- [ ] 실제 기기에서 로그인 후 업적 달성 테스트
- [ ] 업적 UI가 정상적으로 표시되는지 확인

## 7. 문제 해결

### 업적이 달성되지 않는 경우
- 업적 ID가 올바른지 확인
- 업적이 게시되었는지 확인
- 로그인 상태 확인 (`CloudSaveManager.Instance.IsAuthenticated`)
- 인터넷 연결 확인
- 조건이 올바르게 달성되었는지 확인 (Unity Console 로그 확인)

### 업적 UI가 표시되지 않는 경우
- 업적 ID가 올바른지 확인
- 로그인 상태 확인 (로그인되지 않으면 자동으로 로그인 시도)
- Google Play Games 앱이 설치되어 있는지 확인

### 업적이 중복으로 달성되는 경우
- `AchievementManager`가 중복 생성되지 않았는지 확인
- `DontDestroyOnLoad`가 올바르게 설정되었는지 확인

## 8. 참고사항

- 업적 ID는 게임이 출시된 후에도 변경할 수 없으므로 신중하게 설정하세요
- 업적은 게시된 후 약간의 시간이 지나야 표시될 수 있습니다
- 테스트 계정으로 달성한 업적은 프로덕션 업적에 표시되지 않습니다
- 오프라인에서 달성한 업적은 온라인 복구 시 자동으로 동기화됩니다
