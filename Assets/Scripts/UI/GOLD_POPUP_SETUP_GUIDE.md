# 골드 팝업 풀링 셋업 가이드

## 개요

캐릭터를 클릭하거나 자동 수익이 발생할 때 골드 획득 팝업이 표시됩니다. 이 시스템은 오브젝트 풀링을 사용하여 성능을 최적화합니다.

---

## 1. 골드 팝업 프리팹 생성

### 1-1. 골드 팝업 UI 생성

1. **Canvas 하위에 빈 GameObject 생성**
   - Hierarchy에서 Canvas 하위에 빈 GameObject 생성
   - 이름: `GoldGainPopup` (프리팹으로 만들 예정)

2. **RectTransform 설정**
   - Anchor: Middle-Center
   - Width: 200, Height: 60 (원하는 크기로 조정)
   - Pivot: (0.5, 0.5)

3. **Image 컴포넌트 추가 (선택사항)**
   - 배경 이미지가 필요하면 Image 컴포넌트 추가
   - 색상: 반투명 검정 또는 원하는 색상

4. **TextMeshProUGUI 추가**
   - `GoldGainPopup` 하위에 `TextMeshProUGUI` 생성
   - 이름: `AmountText`
   - 텍스트: "+1000" (임시)
   - 폰트 크기: 24~32 (원하는 크기)
   - 정렬: Center, Middle
   - 색상: 노란색 계열 (예: RGBA 255, 217, 51, 255)

5. **CanvasGroup 컴포넌트 추가**
   - `GoldGainPopup`에 `CanvasGroup` 컴포넌트 추가
   - Alpha 페이드 아웃 애니메이션에 사용

### 1-2. GoldGainPopup 컴포넌트 설정

1. **GoldGainPopup 스크립트 추가**
   - `GoldGainPopup` 오브젝트 선택
   - Inspector에서 `Add Component` → `GoldGainPopup` 추가

2. **Inspector 필드 연결**
   - `Amount Text`: 위에서 생성한 `AmountText` 드래그
   - `Move Distance`: 팝업이 위로 이동할 거리 (기본 90)
   - `Duration`: 애니메이션 지속 시간 (기본 0.65초)
   - `Bounce Color`: 자동 수익 팝업 색상 (기본: RGBA 255, 217, 51, 255)
   - `Click Color`: 클릭 수익 팝업 색상 (기본: RGBA 255, 242, 115, 255)

### 1-3. 프리팹으로 저장

1. **프리팹 생성**
   - `GoldGainPopup` 오브젝트를 `Assets/Prefabs` 폴더로 드래그
   - 프리팹 이름: `GoldGainPopup`

2. **씬에서 제거**
   - 씬에 있는 `GoldGainPopup` 오브젝트는 삭제 (프리팹만 사용)

---

## 2. 골드 팝업 풀 셋업

### 2-1. 골드 팝업 풀 오브젝트 생성

1. **Canvas 하위에 빈 GameObject 생성**
   - Hierarchy에서 Canvas 하위에 빈 GameObject 생성
   - 이름: `GoldGainPopupPool`

2. **RectTransform 설정**
   - Anchor: Stretch-Stretch
   - Left, Right, Top, Bottom: 0
   - 이 오브젝트는 팝업들을 관리하는 컨테이너 역할

### 2-2. GoldGainPopupPool 컴포넌트 설정

1. **GoldGainPopupPool 스크립트 추가**
   - `GoldGainPopupPool` 오브젝트 선택
   - Inspector에서 `Add Component` → `GoldGainPopupPool` 추가

2. **Inspector 필드 연결**
   - `Popup Prefab`: 위에서 만든 `GoldGainPopup` 프리팹 드래그
   - `Preload Count`: 미리 생성할 팝업 개수 (기본 12개)
     - 동시에 표시될 팝업 개수보다 약간 많게 설정 권장

---

## 3. GirlFieldManager 연결

### 3-1. GirlFieldManager Inspector 설정

1. **GirlFieldManager 오브젝트 선택**
   - 씬에서 `GirlFieldManager` 오브젝트 찾기

2. **Gold Popup 필드 연결**
   - `Gold Popup Pool`: 위에서 만든 `GoldGainPopupPool` 컴포넌트 드래그
   - `Gold Popup Offset`: 팝업이 캐릭터 위에서 얼마나 떨어져 표시될지 (기본: X=0, Y=120)

---

## 4. 동작 방식

### 4-1. 클릭 시 골드 팝업 표시

```
사용자가 캐릭터 클릭
  └─> GirlCharacter.OnPointerClick()
      └─> GirlMergeManager.AddIncomeGold(girl, isClick: true)
          └─> GirlFieldManager.ShowGoldPopup(girl, amount, isClick: true)
              └─> GoldGainPopupPool.Show(position, amount, isClick: true)
                  └─> GoldGainPopup.Play() - 위로 이동 + 페이드 아웃
```

### 4-2. 자동 수익 시 골드 팝업 표시

```
1초마다 자동 수익 지급
  └─> GirlFieldManager.Update() - Idle 타이머
      └─> EconomyManager.AddGold(perSec)
          └─> (선택사항) 골드 팝업 표시
```

---

## 5. 커스터마이징

### 5-1. 팝업 애니메이션 조정

**GoldGainPopup.cs Inspector:**
- `Move Distance`: 위로 이동할 거리 (픽셀 단위)
- `Duration`: 애니메이션 지속 시간 (초)
- `Bounce Color`: 자동 수익 팝업 색상
- `Click Color`: 클릭 수익 팝업 색상

### 5-2. 풀 크기 조정

**GoldGainPopupPool.cs Inspector:**
- `Preload Count`: 미리 생성할 팝업 개수
  - 너무 적으면: 동시에 많은 팝업이 필요할 때 새로 생성되어 성능 저하
  - 너무 많으면: 메모리 사용량 증가
  - 권장: 10~20개

### 5-3. 팝업 위치 조정

**GirlFieldManager.cs Inspector:**
- `Gold Popup Offset`: 캐릭터 위치 기준 오프셋
  - X: 좌우 오프셋 (0 = 중앙)
  - Y: 위로 떨어질 거리 (양수)

---

## 6. 테스트 체크리스트

- [ ] 골드 팝업 프리팹이 올바르게 생성되었는지 확인
- [ ] GoldGainPopupPool이 프리팹을 참조하고 있는지 확인
- [ ] GirlFieldManager에 GoldGainPopupPool이 연결되어 있는지 확인
- [ ] 캐릭터 클릭 시 골드 팝업이 표시되는지 확인
- [ ] 팝업이 위로 이동하며 페이드 아웃되는지 확인
- [ ] 클릭 팝업과 자동 수익 팝업 색상이 다른지 확인
- [ ] 여러 캐릭터를 빠르게 클릭해도 팝업이 정상적으로 표시되는지 확인
- [ ] 팝업이 사라진 후 풀로 반환되는지 확인 (성능)

---

## 7. 트러블슈팅

### 문제: 팝업이 표시되지 않음
- **해결**: 
  - GoldGainPopupPool의 `Popup Prefab` 필드가 올바르게 연결되었는지 확인
  - GirlFieldManager의 `Gold Popup Pool` 필드가 올바르게 연결되었는지 확인
  - GoldGainPopup 프리팹에 `GoldGainPopup` 컴포넌트가 추가되어 있는지 확인

### 문제: 팝업이 캐릭터 위치에 표시되지 않음
- **해결**: 
  - GirlFieldManager의 `Gold Popup Offset` 값을 조정
  - 캔버스 스케일 모드 확인 (Scale With Screen Size 권장)

### 문제: 팝업이 너무 많이 생성되어 성능 저하
- **해결**: 
  - GoldGainPopupPool의 `Preload Count`를 늘려서 미리 생성
  - 팝업이 풀로 제대로 반환되는지 확인 (GoldGainPopup.Play()의 콜백 확인)

### 문제: 팝업 텍스트가 보이지 않음
- **해결**: 
  - GoldGainPopup의 `Amount Text` 필드가 올바르게 연결되었는지 확인
  - TextMeshProUGUI의 폰트가 설정되어 있는지 확인
  - 텍스트 색상이 배경과 구분되는지 확인

---

## 8. 성능 최적화 팁

1. **풀 크기 최적화**
   - 동시에 표시될 팝업 개수를 예상하여 `Preload Count` 설정
   - 일반적으로 10~15개면 충분

2. **애니메이션 최적화**
   - DOTween을 사용하므로 성능이 좋음
   - 너무 많은 팝업이 동시에 표시되면 `Duration`을 짧게 조정

3. **메모리 관리**
   - 팝업이 풀로 반환되면 자동으로 비활성화되어 메모리 사용량 최소화
   - 풀링 시스템이 자동으로 관리하므로 별도 처리 불필요

