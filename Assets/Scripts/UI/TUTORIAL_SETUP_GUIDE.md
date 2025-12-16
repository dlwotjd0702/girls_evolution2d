# 튜토리얼 시스템 설정 가이드

이 문서는 게임의 튜토리얼 시스템을 Unity 에디터에서 설정하는 방법을 상세히 안내합니다.

## 📋 목차

- [개요](#-개요)
- [튜토리얼 UI 패널 생성](#-튜토리얼-ui-패널-생성)
- [TutorialManager 설정](#-tutorialmanager-설정)
- [튜토리얼 단계 설정](#-튜토리얼-단계-설정)
- [액션 타입 설명](#-액션-타입-설명)
- [테스트 및 디버깅](#-테스트-및-디버깅)

---

## 🎯 개요

튜토리얼 시스템은 첫 실행 시 게임 조작법을 단계별로 안내하는 시스템입니다.

### 주요 기능

- **단계별 가이드**: 순차적으로 게임 조작법 안내
- **액션 대기**: 사용자가 특정 액션을 수행할 때까지 대기
- **하이라이트**: 특정 UI 요소 강조 표시
- **오버레이**: 튜토리얼 중 다른 UI 상호작용 방지
- **스킵 기능**: 튜토리얼 건너뛰기
- **진행 상태 저장**: 튜토리얼 완료 여부 저장

---

## 🎨 튜토리얼 UI 패널 생성

### 1. 튜토리얼 패널 루트 생성

1. **Canvas 하위에 Panel 생성**
   - Hierarchy에서 Canvas 우클릭 → `UI` > `Panel`
   - 이름: `TutorialPanelRoot`
   - 초기 상태: **비활성화** (Inspector에서 체크 해제)

2. **패널 설정**
   - Anchor: `Stretch` (전체 화면)
   - 배경색: 반투명 검정 (선택사항, 오버레이와 별도로 사용 가능)

### 2. 튜토리얼 패널 구성 요소 생성

`TutorialPanelRoot` 하위에 다음 UI 요소들을 생성:

#### 2-1. 제목 텍스트 (`titleText`)

1. `TutorialPanelRoot` 우클릭 → `UI` > `Text - TextMeshPro`
2. 이름: `TitleText`
3. 설정:
   - **Rect Transform**: 상단 중앙 배치
   - **Font Size**: 24-32 (적절한 크기)
   - **Alignment**: 중앙 정렬
   - **Text**: 비워두기 (코드에서 설정)

#### 2-2. 메시지 텍스트 (`messageText`)

1. `TutorialPanelRoot` 우클릭 → `UI` > `Text - TextMeshPro`
2. 이름: `MessageText`
3. 설정:
   - **Rect Transform**: 중앙 배치
   - **Font Size**: 18-24
   - **Alignment**: 중앙 정렬
   - **Text**: 비워두기 (코드에서 설정)
   - **Text Area**: 충분한 크기로 설정 (여러 줄 표시)

#### 2-3. 다음 버튼 (`nextButton`)

1. `TutorialPanelRoot` 우클릭 → `UI` > `Button - TextMeshPro`
2. 이름: `NextButton`
3. 설정:
   - **Rect Transform**: 하단 중앙 배치
   - **Text**: "다음" 또는 "Next"
   - 버튼 크기 및 스타일은 디자인에 맞게 설정

#### 2-4. 스킵 버튼 (`skipButton`)

1. `TutorialPanelRoot` 우클릭 → `UI` > `Button - TextMeshPro`
2. 이름: `SkipButton`
3. 설정:
   - **Rect Transform**: 우측 상단 배치
   - **Text**: "건너뛰기" 또는 "Skip"
   - 작은 크기로 설정 (선택사항)

#### 2-5. 닫기 버튼 (`closeButton`)

1. `TutorialPanelRoot` 우클릭 → `UI` > `Button - TextMeshPro`
2. 이름: `CloseButton`
3. 설정:
   - **Rect Transform**: 하단 중앙 배치 (NextButton과 같은 위치 또는 옆)
   - **Text**: "확인" 또는 "Close"
   - **초기 상태**: 비활성화 (액션 대기 단계에서만 표시)

### 3. 오버레이 패널 생성 (`overlayPanel`)

1. **Canvas 하위에 별도 Panel 생성**
   - Hierarchy에서 Canvas 우클릭 → `UI` > `Panel`
   - 이름: `TutorialOverlayPanel`
   - 초기 상태: **비활성화**

2. **오버레이 설정**
   - Anchor: `Stretch` (전체 화면)
   - 배경색: 검정 (R:0, G:0, B:0, A:204) - 알파 0.8
   - **중요**: `TutorialPanelRoot`보다 **낮은 Canvas Order**에 배치
     - Canvas의 `Sort Order`를 조정하여 튜토리얼 패널이 오버레이 위에 표시되도록 설정

### 4. 하이라이트 오버레이 생성 (`highlightOverlay`) - 선택사항

특정 UI 요소를 강조하고 싶을 때 사용:

1. Canvas 하위에 별도 Panel 생성
2. 이름: `HighlightOverlay`
3. 설정:
   - 배경색: 반투명 (알파 낮게)
   - 초기 상태: 비활성화
   - 특정 UI 요소 주변에 표시되도록 설정

---

## ⚙️ TutorialManager 설정

### 1. GameObject 생성

1. **씬에 빈 GameObject 생성**
   - Hierarchy에서 우클릭 → `Create Empty`
   - 이름: `TutorialManager`

2. **TutorialManager 스크립트 추가**
   - `TutorialManager.cs` 스크립트를 드래그하여 추가
   - `DontDestroyOnLoad` 자동 적용됨

### 2. Inspector 필드 연결

`TutorialManager` GameObject를 선택하고 Inspector에서 다음 필드들을 연결:

#### Tutorial Panel 섹션

- **Tutorial Panel Root**: `TutorialPanelRoot` GameObject 드래그
- **Title Text**: `TitleText` TextMeshProUGUI 드래그
- **Message Text**: `MessageText` TextMeshProUGUI 드래그
- **Next Button**: `NextButton` Button 드래그
- **Skip Button**: `SkipButton` Button 드래그
- **Close Button**: `CloseButton` Button 드래그

#### Highlight Settings 섹션 (선택사항)

- **Highlight Overlay**: `HighlightOverlay` GameObject 드래그 (선택사항)
- **Highlight Target**: 비워두기 (코드에서 동적 설정)

#### Overlay Settings 섹션

- **Overlay Panel**: `TutorialOverlayPanel` GameObject 드래그
- **Overlay Image**: `TutorialOverlayPanel`의 Image 컴포넌트 (자동 감지되지만 수동 설정 가능)
- **Overlay Alpha**: `0.8` (기본값, 필요시 조정)

#### Tutorial Steps 섹션

- **Size**: 튜토리얼 단계 수 입력 (예: 7)
- 각 단계별 설정은 아래 섹션 참고

---

## 📝 튜토리얼 단계 설정

### 단계 구조

각 튜토리얼 단계는 다음 정보를 포함합니다:

- **Step Name**: 단계 이름 (디버깅용)
- **Title**: 제목 텍스트
- **Message**: 메시지 텍스트
- **Target Object Name**: 하이라이트할 GameObject 이름 (선택사항)
- **Wait For Action**: 사용자 액션 대기 여부
- **Action To Wait**: 대기할 액션 이름

### 단계 설정 예시

#### 예시 1: 환영 메시지 (액션 대기 없음)

```
Step Name: "Welcome"
Title: "게임에 오신 것을 환영합니다!"
Message: "이 게임은 캐릭터를 합성하여 레벨을 올리는 방치형 게임입니다.\n\n다음 버튼을 눌러 계속하세요."
Target Object Name: (비워두기)
Wait For Action: false
Action To Wait: (비워두기)
```

#### 예시 2: 소환 안내 (액션 대기)

```
Step Name: "Summon"
Title: "캐릭터 소환하기"
Message: "소환 버튼을 눌러 캐릭터를 소환하세요.\n\n2번 이상 소환해보세요!"
Target Object Name: "SummonButton"  // 실제 버튼 GameObject 이름
Wait For Action: true
Action To Wait: "SummonTwice"
```

#### 예시 3: 합성 안내 (액션 대기)

```
Step Name: "Merge"
Title: "캐릭터 합성하기"
Message: "같은 레벨의 캐릭터 2개를 드래그하여 합성하세요.\n\n합성하면 다음 레벨의 캐릭터가 생성됩니다!"
Target Object Name: (비워두기 또는 합성 관련 UI 이름)
Wait For Action: true
Action To Wait: "Merge"
```

#### 예시 4: 상점 안내 (액션 대기)

```
Step Name: "Shop"
Title: "상점 사용하기"
Message: "상점 버튼을 눌러 업그레이드를 구매할 수 있습니다.\n\n상점을 열어보세요!"
Target Object Name: "ShopButton"  // 실제 버튼 이름
Wait For Action: true
Action To Wait: "ShopOpen"
```

### 권장 튜토리얼 단계 순서

1. **환영 메시지** (Wait For Action: false)
2. **소환 안내** (Action: SummonTwice)
3. **합성 안내** (Action: Merge)
4. **상점 안내** (Action: ShopOpen)
5. **티어 이동 안내** (Action: TierSwitch) - 선택사항
6. **자동소환 안내** (Action: AutoSpawn) - 선택사항
7. **자동합성 안내** (Action: AutoMerge) - 선택사항

---

## 🎮 액션 타입 설명

### 지원하는 액션 타입

#### 1. `Summon` / `SummonTwice`
- **설명**: 소환 버튼 클릭 대기
- **Summon**: 1번 클릭 대기
- **SummonTwice**: 2번 이상 클릭 대기
- **대상**: `GirlFieldManager`의 소환 버튼

#### 2. `Merge`
- **설명**: 합성 완료 대기
- **동작**: 필드의 캐릭터 수가 줄어드는지 확인
- **참고**: DOTween 애니메이션이 일시정지됨

#### 3. `ShopOpen`
- **설명**: 상점 패널 열기 대기
- **대상**: `ShopPanelController`의 패널 활성화 확인

#### 4. `TierSwitch`
- **설명**: 티어 이동 대기
- **대상**: `TierManager`의 현재 티어 변경 확인

#### 5. `AutoSpawn`
- **설명**: 자동소환 활성화 대기
- **대상**: `SaveData.autoSpawnOn` 값 확인

#### 6. `AutoMerge`
- **설명**: 자동합성 활성화 대기
- **대상**: `SaveData.autoMergeOn` 값 확인

#### 7. `SummonPanel`
- **설명**: 소환 패널 열기 및 소환 대기
- **대상**: `SummonPanelController`의 패널 활성화 및 소환 확인

### 액션 대기 없이 진행

`Wait For Action`을 `false`로 설정하면:
- `NextButton`이 표시됨
- 사용자가 다음 버튼을 클릭하면 다음 단계로 진행
- 즉시 진행 가능

---

## 🧪 테스트 및 디버깅

### 튜토리얼 재시작하기

게임을 테스트할 때 튜토리얼을 다시 보려면:

1. **에디터에서**:
   - `EditorSaves/save.json` 파일에서 `tutorialCompleted`를 `false`로 변경
   - 또는 `SaveManager`의 `resetSaveOnStart` 옵션 활성화

2. **빌드에서**:
   - 세이브 파일 삭제 또는 `tutorialCompleted`를 `false`로 변경

### 디버그 로그 확인

Unity Console에서 다음 로그를 확인:

- `[TutorialManager] 튜토리얼 시작`
- `[TutorialManager] 튜토리얼 완료`
- `[TutorialManager] spawnButton을 찾을 수 없습니다. 필드 변화로 감지합니다.`

### 일반적인 문제 해결

#### 문제 1: 튜토리얼이 표시되지 않음

**원인**:
- `tutorialPanelRoot`가 Inspector에 연결되지 않음
- `tutorialSteps` 배열이 비어있음
- `tutorialCompleted`가 이미 `true`로 저장됨

**해결**:
1. Inspector에서 모든 필드가 올바르게 연결되었는지 확인
2. `tutorialSteps` 배열 크기가 1 이상인지 확인
3. 세이브 파일에서 `tutorialCompleted`를 `false`로 변경

#### 문제 2: 액션 대기에서 진행되지 않음

**원인**:
- `Action To Wait` 이름이 잘못됨
- 대상 GameObject 이름이 일치하지 않음
- 액션 감지 로직이 제대로 동작하지 않음

**해결**:
1. `Action To Wait` 이름이 정확한지 확인 (대소문자 구분)
2. `Target Object Name`이 실제 GameObject 이름과 일치하는지 확인
3. Unity Console에서 에러 로그 확인

#### 문제 3: 오버레이가 튜토리얼 패널을 가림

**원인**:
- Canvas Sort Order 설정 문제
- 오버레이 패널이 튜토리얼 패널보다 위에 배치됨

**해결**:
1. 튜토리얼 패널의 Canvas `Sort Order`를 오버레이보다 높게 설정
2. 또는 오버레이 패널을 별도 Canvas에 배치하고 `Sort Order`를 낮게 설정

#### 문제 4: 하이라이트가 작동하지 않음

**원인**:
- `Target Object Name`이 잘못됨
- `highlightOverlay`가 설정되지 않음

**해결**:
1. `Target Object Name`이 실제 GameObject 이름과 정확히 일치하는지 확인
2. `highlightOverlay`가 선택사항이므로 설정하지 않아도 됨 (오버레이만으로도 충분)

---

## 📚 추가 참고사항

### 저장 시스템 연동

튜토리얼 진행 상태는 `SaveData.tutorialCompleted` 필드에 저장됩니다:
- `false`: 튜토리얼 미완료 (다음 실행 시 튜토리얼 시작)
- `true`: 튜토리얼 완료 (다음 실행 시 튜토리얼 건너뜀)

### 다국어 지원

튜토리얼의 제목과 메시지는 `LocalizationManager`를 통해 다국어를 지원합니다:
- 한국어/영어 자동 전환
- `LocalizationManager.GetText()` 메서드 사용

### 성능 고려사항

- 튜토리얼은 첫 실행 시에만 표시되므로 성능 영향 최소
- 오버레이 패널은 필요할 때만 활성화
- 하이라이트 오버레이는 선택사항이므로 사용하지 않아도 됨

---

## ✅ 체크리스트

튜토리얼 시스템 설정 완료 체크리스트:

- [ ] 튜토리얼 패널 루트 생성 및 설정
- [ ] 제목/메시지/버튼 UI 요소 생성
- [ ] 오버레이 패널 생성 및 설정
- [ ] TutorialManager GameObject 생성 및 스크립트 추가
- [ ] Inspector에서 모든 필드 연결
- [ ] 튜토리얼 단계 배열 설정 (최소 1개 이상)
- [ ] 각 단계의 제목/메시지 입력
- [ ] 액션 대기 단계 설정 (필요한 경우)
- [ ] Target Object Name 설정 (하이라이트 필요한 경우)
- [ ] 게임 실행하여 튜토리얼 테스트
- [ ] 튜토리얼 완료 후 저장 확인
- [ ] 재실행 시 튜토리얼이 건너뛰어지는지 확인

---

**마지막 업데이트**: 2024년

