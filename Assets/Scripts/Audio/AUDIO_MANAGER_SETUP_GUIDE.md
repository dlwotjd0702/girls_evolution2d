# 오디오 매니저 셋업 가이드

## 개요

오디오 매니저는 게임의 모든 사운드를 관리합니다. BGM과 효과음(클릭, 합성, 발견, 환생)을 재생합니다.

---

## 1. AudioManager 오브젝트 생성

### 1-1. 씬에 AudioManager 오브젝트 생성

1. **Hierarchy에서 빈 GameObject 생성**
   - Hierarchy 창에서 우클릭 → `Create Empty`
   - 이름: `AudioManager`

2. **AudioManager 컴포넌트 추가**
   - `AudioManager` 오브젝트 선택
   - Inspector 창에서 `Add Component` 클릭
   - `AudioManager` 스크립트 추가

3. **DontDestroyOnLoad 설정**
   - `AudioManager` 오브젝트 선택
   - Inspector에서 `DontDestroyOnLoad` 체크
   - 또는 코드에서 자동으로 처리됨 (이미 구현됨)

---

## 2. AudioClip 할당

### 2-1. 오디오 파일 준비

1. **오디오 파일 준비**
   - BGM: 배경음악 파일 (WAV, MP3, OGG 등)
   - 클릭 효과음: 클릭 시 재생할 효과음
   - 합성 효과음: 합성 시 재생할 효과음
   - 발견 효과음: 새로운 단계 발견 시 재생할 효과음
   - 환생 효과음: 환생 시 재생할 효과음

2. **오디오 파일 Import 설정 (Unity 에디터)**
   - Project 창에서 오디오 파일 선택
   - Inspector에서 설정:
     - **BGM**: Load Type = Streaming (긴 파일), Compression Format = Vorbis
     - **효과음**: Load Type = Decompress On Load (짧은 파일), Compression Format = PCM 또는 Vorbis

### 2-2. Inspector에서 AudioClip 할당

1. **AudioManager 오브젝트 선택**

2. **BGM 설정**
   - `BGM Clip`: 배경음악 AudioClip 드래그 (없어도 오류 없음)
   - `Play BGM On Start`: 게임 시작 시 자동 재생 여부 (기본: true)
   - `Loop BGM`: BGM 반복 재생 여부 (기본: true)

3. **SFX Clips 설정**
   - `Click SFX`: 클릭 효과음 AudioClip 드래그 (없어도 오류 없음)
   - `Merge SFX`: 합성 효과음 AudioClip 드래그 (없어도 오류 없음)
   - `Discovery SFX`: 발견 효과음 AudioClip 드래그 (없어도 오류 없음)
   - `Prestige SFX`: 환생 효과음 AudioClip 드래그 (없어도 오류 없음)
   - **참고**: 모든 AudioClip은 선택사항입니다. 없으면 해당 효과음이 재생되지 않을 뿐 오류는 발생하지 않습니다.

4. **Volume Settings (선택사항)**
   - `Master Volume`: 전체 볼륨 (기본: 1.0)
   - **참고**: 실제 볼륨은 PlayerPrefs에서 불러오므로 여기서 설정한 값은 초기값입니다.

---

## 3. AudioSource 자동 생성 (선택사항)

### 3-1. 수동으로 AudioSource 생성 (선택사항)

AudioManager는 AudioSource가 없으면 자동으로 생성하지만, 수동으로 생성할 수도 있습니다:

1. **Audio Source 생성**
   - `AudioManager` 하위에 빈 GameObject 생성
   - 이름: `Audio Source`
   - `AudioSource` 컴포넌트 추가
   - `Play On Awake`: 체크 해제
   - `Volume`: 1.0 (볼륨은 설정 패널에서 조절)

2. **Inspector에서 연결**
   - AudioManager의 `Audio Source` 필드에 `Audio Source` 드래그

**참고**: 단일 AudioSource를 사용하여 BGM과 효과음을 모두 재생합니다. PlayOneShot을 사용하므로 BGM과 효과음이 동시에 재생 가능합니다.

---

## 4. 볼륨 설정 연동

### 4-1. VolumeSettingsPanel과 연동

AudioManager는 `VolumeSettingsPanel`에서 설정한 볼륨을 자동으로 적용합니다.

1. **VolumeSettingsPanel 셋업**
   - 설정 패널의 볼륨 설정에서 볼륨을 조절하면
   - `AudioManager.UpdateMasterVolume()`이 자동으로 호출되어
   - 모든 오디오에 볼륨이 적용됩니다.

2. **PlayerPrefs 저장**
   - 볼륨 설정은 `PlayerPrefs`에 저장됩니다:
     - `MasterVolume`: 전체 볼륨 (0~1)
   - 게임 재시작 시에도 설정이 유지됩니다.
   - AudioManager는 단일 AudioSource를 사용하므로 볼륨 조절이 간단합니다.

---

## 5. 효과음 재생 위치

### 5-1. 자동으로 재생되는 위치

다음 상황에서 효과음이 자동으로 재생됩니다:

1. **클릭 효과음**
   - 위치: `GirlMergeManager.AddIncomeGold()` (isClick = true일 때)
   - 캐릭터를 클릭하여 골드를 획득할 때

2. **합성 효과음**
   - 위치: `GirlMergeManager.MergeRoutine()`
   - 두 캐릭터를 합성할 때

3. **발견 효과음**
   - 위치: `GirlFieldManager.PlayDiscoveryOnce()`
   - 새로운 단계의 캐릭터를 처음 발견할 때

4. **환생 효과음**
   - 위치: `PrestigeManager.DoPrestige()`
   - 환생을 실행할 때

5. **BGM**
   - 위치: `AudioManager.Start()` (playBGMOnStart = true일 때)
   - 게임 시작 시 자동 재생

---

## 6. 수동으로 효과음 재생

### 6-1. 코드에서 직접 호출

```csharp
// 클릭 효과음
if (AudioManager.Instance != null)
{
    AudioManager.Instance.PlayClickSFX();
}

// 합성 효과음
AudioManager.Instance?.PlayMergeSFX();

// 발견 효과음
AudioManager.Instance?.PlayDiscoverySFX();

// 환생 효과음
AudioManager.Instance?.PlayPrestigeSFX();

// BGM 재생
AudioManager.Instance?.PlayBGM();

// BGM 정지
AudioManager.Instance?.StopBGM();
```

### 6-2. 일반 효과음 재생

```csharp
// AudioClip을 직접 재생
AudioClip myClip = ...;
AudioManager.Instance?.PlaySFX(myClip);

// 볼륨 조절하여 재생
AudioManager.Instance?.PlaySFX(myClip, 0.5f); // 50% 볼륨
```

---

## 7. 테스트 체크리스트

### AudioManager 기본 테스트
- [ ] AudioManager 오브젝트가 씬에 있는지 확인
- [ ] AudioManager 컴포넌트가 추가되어 있는지 확인
- [ ] 모든 AudioClip이 할당되어 있는지 확인
- [ ] 게임 시작 시 BGM이 재생되는지 확인
- [ ] BGM이 반복 재생되는지 확인

### 효과음 테스트
- [ ] 캐릭터 클릭 시 클릭 효과음이 재생되는지 확인
- [ ] 캐릭터 합성 시 합성 효과음이 재생되는지 확인
- [ ] 새로운 단계 발견 시 발견 효과음이 재생되는지 확인
- [ ] 환생 시 환생 효과음이 재생되는지 확인

### 볼륨 테스트
- [ ] 볼륨 설정 패널에서 볼륨 조절 시 오디오 볼륨이 변경되는지 확인
- [ ] 게임 재시작 후 볼륨 설정이 유지되는지 확인

---

## 8. 트러블슈팅

### 문제: BGM이 재생되지 않음
- **해결**: 
  - `BGM Clip`이 할당되어 있는지 확인
  - `Play BGM On Start`가 체크되어 있는지 확인
  - AudioSource의 `Volume`이 0이 아닌지 확인
  - AudioManager가 씬에 있는지 확인

### 문제: 효과음이 재생되지 않음
- **해결**: 
  - 해당 효과음의 AudioClip이 할당되어 있는지 확인
  - AudioManager.Instance가 null이 아닌지 확인
  - AudioSource의 `Volume`이 0이 아닌지 확인

### 문제: 볼륨이 적용되지 않음
- **해결**: 
  - VolumeSettingsPanel이 AudioManager를 참조하고 있는지 확인
  - PlayerPrefs에 볼륨 값이 저장되어 있는지 확인
  - AudioManager의 `UpdateMasterVolume()` 메서드가 호출되는지 확인

### 문제: 효과음이 겹쳐서 재생되지 않음
- **해결**: 
  - PlayOneShot을 사용하므로 BGM과 효과음이 동시에 재생 가능합니다
  - 효과음이 너무 빠르게 연속 재생되면 일부가 들리지 않을 수 있으나, 이는 정상 동작입니다

### 문제: 씬 전환 시 오디오가 끊김
- **해결**: 
  - AudioManager가 `DontDestroyOnLoad`로 설정되어 있는지 확인
  - 씬 전환 시 AudioManager가 파괴되지 않도록 확인

---

## 9. 성능 최적화 팁

1. **오디오 파일 최적화**
   - 효과음: 짧게 (1초 이하), PCM 또는 Vorbis 압축
   - BGM: Vorbis 압축, 적절한 비트레이트 (128kbps 정도)

2. **AudioSource 관리**
   - AudioManager는 싱글톤이므로 씬에 하나만 존재
   - DontDestroyOnLoad로 씬 전환 시에도 유지
   - 단일 AudioSource를 사용하여 메모리 효율적

3. **메모리 관리**
   - 효과음은 `PlayOneShot`을 사용하여 메모리 효율적
   - BGM은 Streaming으로 로드하여 메모리 사용량 최소화

---

## 10. 추가 기능

### 10-1. BGM 전환

```csharp
// 다른 BGM으로 변경
AudioClip newBGM = ...;
AudioManager.Instance?.SetBGMClip(newBGM);
```

### 10-2. 모든 오디오 정지

```csharp
// 모든 오디오 정지 (일시정지 등)
AudioManager.Instance?.StopAll();
```

### 10-3. BGM 일시정지/재개

```csharp
// BGM 일시정지
AudioManager.Instance?.PauseBGM();

// BGM 재개
AudioManager.Instance?.ResumeBGM();
```

---

## 11. 실행 순서 확인

AudioManager는 다른 시스템보다 먼저 초기화되어야 합니다.

- 현재는 특별한 실행 순서 설정이 없지만, 필요시 `[DefaultExecutionOrder(-300)]` 등을 추가할 수 있습니다.
- SaveManager(-200)보다 먼저 실행되도록 설정 권장

