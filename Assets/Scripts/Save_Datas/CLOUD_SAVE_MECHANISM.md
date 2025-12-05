# 클라우드 저장 메커니즘 상세 설명

## 클라우드 저장 과정

클라우드에 저장되는 전체 과정을 단계별로 설명합니다.

## 저장 플로우

### 1단계: SaveManager에서 저장 요청

```csharp
// SaveManager.cs - SaveGame() 메서드
SaveData data = new SaveData();
data.SetSaveTime();

// 모든 게임 데이터 수집
var saveables = FindObjectsOfType<MonoBehaviour>(true).OfType<ISaveable>().ToList();
foreach (var s in saveables)
{
    s.CollectSaveData(data);  // 각 매니저에서 데이터 수집
}

// 로컬 저장 (항상 먼저)
File.WriteAllText(SaveFilePath, JsonUtility.ToJson(data, true));

// 클라우드 저장 (조건부)
if (enableCloudSave && autoCloudSave && CloudSaveManager.Instance != null)
{
    CloudSaveManager.Instance.SaveToCloud(data);  // 클라우드 저장 요청
}
```

### 2단계: CloudSaveManager에서 데이터 변환

```csharp
// CloudSaveManager.cs - SaveToCloud() 메서드

// 1. SaveData 객체를 JSON 문자열로 변환
string saveJson = JsonUtility.ToJson(saveData, true);

// 2. JSON 문자열을 UTF-8 바이트 배열로 변환
byte[] saveBytes = Encoding.UTF8.GetBytes(saveJson);
```

**변환 예시:**
```json
{
    "totalPlayTimeSeconds": 3600.5,
    "totalGoldEarned": 1000000,
    "maxLevelReached": 25,
    "prestigeCount": 3,
    "unlockedTierMask": 7,
    "saveTime": "2024-01-15 14:30:00",
    // ... 기타 게임 데이터
}
```

### 3단계: 메타데이터 생성

```csharp
// 저장 시간과 플레이 시간을 메타데이터에 포함
SavedGameMetadataUpdate.Builder updateBuilder = new SavedGameMetadataUpdate.Builder()
    .WithUpdatedDescription($"Saved at {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
    .WithUpdatedPlayedTime(TimeSpan.FromSeconds(saveData.totalPlayTimeSeconds));

SavedGameMetadataUpdate metadataUpdate = updateBuilder.Build();
```

**메타데이터 정보:**
- **설명**: 저장 시간 (예: "Saved at 2024-01-15 14:30:00")
- **플레이 시간**: 총 플레이 시간 (초 단위)
- **파일명**: `"girls_evolution_save"` (고정)

### 4단계: Google Play Games 클라우드에 저장

```csharp
// Google Play Games SavedGame API 사용
savedGameClient.OpenWithAutomaticConflictResolution(
    CLOUD_SAVE_FILENAME,  // "girls_evolution_save"
    DataSource.ReadCacheOrNetwork,  // 오프라인에서도 캐시 사용
    ConflictResolutionStrategy.UseManual,  // 충돌 시 수동 해결
    (status, game) =>
    {
        if (status == SavedGameRequestStatus.Success)
        {
            // 파일 열기 성공 → 데이터 저장
            savedGameClient.CommitUpdate(
                game,              // 열린 게임 파일
                metadataUpdate,    // 메타데이터 (저장 시간, 플레이 시간)
                saveBytes,         // 실제 게임 데이터 (JSON 바이트)
                (commitStatus, committedGame) =>
                {
                    // 저장 완료 콜백
                });
        }
    });
```

## 저장 위치 및 형식

### 저장 위치
- **서비스**: Google Play Games 클라우드 저장소
- **파일명**: `"girls_evolution_save"` (고정)
- **계정**: Google Play Games에 로그인한 계정
- **접근**: 같은 Google 계정으로 로그인한 모든 기기에서 접근 가능

### 저장 형식
- **데이터 형식**: JSON (UTF-8 인코딩)
- **크기**: 게임 데이터에 따라 다름 (일반적으로 수 KB ~ 수십 KB)
- **암호화**: Google Play Games SDK가 자동으로 처리

## 저장되는 데이터

### SaveData 클래스에 포함된 정보

```csharp
public class SaveData
{
    // 플레이 통계
    public double totalPlayTimeSeconds;      // 총 플레이 시간
    public long totalGoldEarned;            // 총 획득 골드
    public int maxLevelReached;              // 최고 달성 레벨
    public int prestigeCount;                // 환생 횟수
    public int unlockedTierMask;            // 발견한 티어
    
    // 저장 정보
    public string saveTime;                  // 저장 시간
    
    // 게임 상태
    public int currentGold;                  // 현재 골드
    public int currentLevel;                 // 현재 레벨
    // ... 기타 게임 데이터
}
```

### 실제 저장 예시

```json
{
    "totalPlayTimeSeconds": 7200.5,
    "totalGoldEarned": 5000000,
    "maxLevelReached": 50,
    "prestigeCount": 5,
    "unlockedTierMask": 15,
    "saveTime": "2024-01-15 14:30:00",
    "currentGold": 100000,
    "currentLevel": 25,
    "girls": [...],
    "upgrades": [...],
    // ... 기타 매니저 데이터
}
```

## 저장 시점

### 자동 저장
1. **1분마다**: `Update()` 메서드에서 자동 저장
2. **앱 일시정지**: `OnApplicationPause(true)` 호출 시
3. **앱 종료**: `OnApplicationQuit()` 호출 시
4. **환생 완료**: 환생 시스템에서 호출
5. **최고 레벨 달성**: 레벨 시스템에서 호출

### 수동 저장
- 설정 패널의 "수동 저장" 버튼 클릭 시

## 오프라인 처리

### 오프라인 환경에서의 동작

```csharp
// DataSource.ReadCacheOrNetwork 사용
DataSource.ReadCacheOrNetwork
```

**동작 방식:**
1. **온라인**: 클라우드 서버에 직접 저장
2. **오프라인**: 로컬 캐시에 저장 → 온라인 복구 시 자동 동기화
3. **에러 처리**: 네트워크 오류 시 예외 발생하지 않음 (로그만 출력)

## 충돌 해결

### 충돌 상황
- 같은 계정으로 여러 기기에서 플레이
- 오프라인에서 플레이 후 온라인 복구
- 네트워크 지연으로 인한 동시 저장

### 해결 방법

```csharp
// 플레이 시간 비교
if (cloudData.totalPlayTimeSeconds > localData.totalPlayTimeSeconds)
{
    // 클라우드가 더 최신 → SaveConflictPanel 표시
    // 사용자가 선택: 클라우드 사용 또는 로컬 사용
}
```

## 저장 확인 방법

### Unity Console 로그

```
[CloudSaveManager] 클라우드 저장 성공
```

### Google Play Games 앱에서 확인
1. Google Play Games 앱 실행
2. 게임 선택
3. "저장된 게임" 메뉴 확인
4. `girls_evolution_save` 파일 확인

### 코드에서 확인

```csharp
// 저장 완료 이벤트 구독
CloudSaveManager.Instance.OnCloudSaveComplete += (success, error) =>
{
    if (success)
    {
        Debug.Log("클라우드 저장 성공!");
    }
    else
    {
        Debug.LogWarning($"클라우드 저장 실패: {error}");
    }
};
```

## 저장 데이터 구조 요약

```
게임 데이터 (SaveData 객체)
    ↓
JSON 문자열 변환 (JsonUtility.ToJson)
    ↓
UTF-8 바이트 배열 변환 (Encoding.UTF8.GetBytes)
    ↓
Google Play Games SavedGame API
    ↓
Google Play Games 클라우드 저장소
    ↓
파일명: "girls_evolution_save"
계정: Google Play Games 로그인 계정
```

## 참고사항

### 저장 제한
- **파일 크기**: Google Play Games 제한 (일반적으로 수 MB까지)
- **저장 빈도**: 너무 자주 저장하면 성능 저하 가능
- **동시 저장**: `IsSaving` 플래그로 중복 저장 방지

### 보안
- Google Play Games SDK가 자동으로 암호화 처리
- 사용자 인증 필요 (로그인 필수)
- 계정별로 독립적인 저장 공간

### 동기화
- 같은 Google 계정으로 로그인한 모든 기기에서 접근 가능
- 자동 동기화 (Google Play Games SDK가 처리)
- 오프라인에서 저장한 데이터도 온라인 복구 시 자동 동기화

## 결론

클라우드 저장은 다음과 같이 동작합니다:

1. **게임 데이터 수집** → SaveData 객체 생성
2. **JSON 변환** → 문자열로 직렬화
3. **바이트 변환** → UTF-8 인코딩
4. **Google Play Games API 호출** → 클라우드에 저장
5. **메타데이터 포함** → 저장 시간, 플레이 시간 등

**모든 과정은 Google Play Games SDK가 자동으로 처리하며, 개발자는 `SaveToCloud(data)`만 호출하면 됩니다.**
