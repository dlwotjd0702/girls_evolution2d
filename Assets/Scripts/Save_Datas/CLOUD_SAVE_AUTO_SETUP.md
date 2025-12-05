# 클라우드 자동 저장 설정 가이드

## ❌ SDK 설정만으로는 자동 저장 안 됩니다!

Google Play Games SDK 설정만 완료해도 **자동으로 클라우드에 저장되지 않습니다.**

## ✅ 자동 저장을 위한 필수 조건

코드를 보면 (`SaveManager.cs` 164번 줄):

```csharp
// 클라우드 저장 (옵션)
if (enableCloudSave && autoCloudSave && CloudSaveManager.Instance != null)
{
    CloudSaveManager.Instance.SaveToCloud(data);
}
```

**3가지 조건이 모두 필요합니다:**

### 1. ✅ Google Play Games SDK 설정
- `Window` > `Google Play Games` > `Setup`
- Android Resources XML 붙여넣기
- **이미 업적 설정 시 완료했다면 건너뛰기**

### 2. ✅ CloudSaveManager GameObject 생성
- Hierarchy에서 우클릭 → `Create Empty`
- 이름: `CloudSaveManager`
- `CloudSaveManager.cs` 스크립트 추가
- **이 GameObject가 없으면 `CloudSaveManager.Instance`가 null이 되어 저장 안 됨**

### 3. ✅ SaveManager Inspector 설정
- `SaveManager` GameObject 선택
- Inspector에서 다음 옵션 체크:
  ```
  Cloud Save Settings:
  ✅ Enable Cloud Save: 체크 (enableCloudSave = true)
  ✅ Auto Cloud Save: 체크 (autoCloudSave = true)
  ✅ Sync On Start: 체크 (선택사항, 시작 시 동기화)
  ```

## 자동 저장 동작 시점

다음 상황에서 자동으로 클라우드에 저장됩니다:

1. **1분마다 자동 저장** (`Update()` 메서드)
2. **앱 일시정지 시** (`OnApplicationPause`)
3. **앱 종료 시** (`OnApplicationQuit`)
4. **환생 완료 시**
5. **최고 레벨 달성 시**

**조건**: 위의 3가지 조건이 모두 충족되어야 함

## 설정 체크리스트

### SDK 설정 (업적 설정 시 이미 완료)
- [x] Google Play Games SDK 설치
- [x] Android Resources XML 설정
- [x] SHA-1 인증서 지문 등록

### Unity 에디터 설정
- [ ] CloudSaveManager GameObject 생성
- [ ] SaveManager GameObject 확인
- [ ] SaveManager Inspector 설정:
  - [ ] `Enable Cloud Save`: ✅ 체크
  - [ ] `Auto Cloud Save`: ✅ 체크
  - [ ] `Sync On Start`: ✅ 체크 (선택사항)

## 설정 방법 (간단 요약)

### 1단계: CloudSaveManager GameObject 생성
```
Hierarchy → 우클릭 → Create Empty
이름: CloudSaveManager
CloudSaveManager.cs 스크립트 추가
```

### 2단계: SaveManager 옵션 체크
```
SaveManager GameObject 선택
Inspector → Cloud Save Settings:
✅ Enable Cloud Save
✅ Auto Cloud Save
✅ Sync On Start (선택사항)
```

## 결론

**SDK 설정만으로는 안 됩니다!**

필요한 것:
1. ✅ Google Play Games SDK 설정 (업적 설정 시 완료)
2. ✅ CloudSaveManager GameObject 생성
3. ✅ SaveManager Inspector 옵션 체크

**이 3가지를 모두 완료해야 자동으로 클라우드에 저장됩니다.**

## 참고

- **로컬 저장**: 항상 자동 (옵션과 무관)
- **클라우드 저장**: 위 3가지 조건이 모두 필요
- **오프라인**: 클라우드 저장 실패해도 로컬 저장은 정상 작동
