# Girls Evolution 2D – Pre 성능/구조 점검 TODO 목록

실제 기능 변경/최적화에 들어가기 전, 코드 플로우를 검수하면서 발견한 **성능·구조 개선 후보** 리스트입니다.  
우선순위는 `P1(중요)` → `P2(권장)` → `P3(선택)` 순서입니다.

---

## 1. 드래그/합성 관련

- **[P1] `GirlMergeManager.UpdateMergeHighlight` 거리 계산 최적화**
  - 현재: 드래그 중일 때마다 `fieldManager.girlList` 전체를 순회하며 `Vector2.Distance` 호출.
  - 문제:
    - 캐릭터 수가 늘어나면 드래그 한 번에 O(N) 연산, 드래그 프레임 수까지 곱해져 프레임 드랍 가능성.
  - 개선 아이디어:
    - 가까운 레벨만 대상으로 삼고, 일정 거리 밖이면 early-out.
    - 필요 시 셀 단위 그리드/슬롯 구조를 만들어 “같은 슬롯/주변 슬롯만 검사”하는 구조 도입.

- **[P2] `GirlMergeManager.TryAutoMerge` 이중 루프 개선**
  - 현재: `girlList`를 2중 for 루프로 스캔하여 가장 가까운 페어를 찾음(O(N²)).
  - 문제:
    - 캐릭 수가 많은 환경에서 자동 합성이 켜져 있으면 간헐적인 스파이크 가능.
  - 개선 아이디어:
    - 레벨별 리스트(예: `Dictionary<int, List<GirlCharacter>>`)를 유지해서 같은 레벨끼리만 순회.
    - 또는 N이 특정 값(예: 50~80)을 넘으면 “첫 번째로 발견한 페어”만 사용해 탐색 조기 종료.

---

## 2. 점프/이동 루프 (`GirlCharacter`)

- **[P2] `JumpBounceLoop` 다수 코루틴 동시 실행 점검**
  - 현재: 필드에 있는 각 캐릭터마다 `JumpBounceLoop` 코루틴이 돌며, `WaitForSeconds` + 상태 체크를 반복.
  - 문제:
    - 캐릭터 수가 매우 많을 경우, 코루틴 오버헤드 + GC(코루틴 내부 캡처, 클로저 등)로 프레임 드랍 가능.
  - 개선 아이디어:
    - “글로벌 타이머” 또는 `GirlFieldManager` 쪽 Update에서 타이밍을 관리하고, 각 캐릭터에 이벤트로 점프/바운스를 요청하는 구조로 변경.
    - 최소한, 코루틴 내부에서 박스/할당이 없는지(캡처된 변수, 람다 사용 여부) 재점검.

- **[P3] 점프 방향 확률 계산의 분기/수학 연산**
  - 현재: 각 점프마다 `Mathf.InverseLerp`, `Mathf.Lerp`, `Random.value` 등을 호출.
  - 문제:
    - 개별 프레임 퍼포먼스에는 큰 문제는 아니지만, 고성능 틱 환경에서는 수학 연산 최적화 여지가 있음.
  - 개선 아이디어:
    - 필요 시 `nx/ny`를 미리 정규화된 범위로 캐싱하거나, 선형 보간 대신 간단한 piece-wise 규칙으로 대체.

---

## 3. 세이브/로드 & 리플렉션

- **[P1] `GirlMergeManager`의 리플렉션 캐시 사용 위치 재점검**
  - 현재: `GetLegacyTwoStepChanceSafe`, `GetShopTwoStepChanceSafe`에서 리플렉션 기반으로 외부 매니저(`LegacyRankManager`, `PrestigeShopManager`)에 접근.
  - 장점:
    - 외부 시스템 없이도 컴파일/실행 가능.
  - 잠재 이슈:
    - `MergeRoutine`는 자주 호출될 수 있는 경로이므로, 리플렉션 캐시가 **정말 1회만** 셋업되는지 검증 필요.
  - 개선 아이디어:
    - 최초 1회 `FindTypeByName`/`GetMethod` 실행이 확실히 끝난 후에는, 이후에는 단순 delegate 호출로 구성.
    - 필요하다면, 리플렉션 대신 인터페이스/직접 참조로 전환한 “빌드 전용 설정” 옵션 제공.

- **[P2] `LegacyRankManager` / `PremiumCurrencyManager` 리플렉션/필드 접근**
  - 현재: SaveData와의 호환성을 위해 리플렉션 유틸(`TrySetInt`, `TryGetInt`, `TrySetLong`) 사용.
  - 문제:
    - 호출 빈도는 크지 않지만, 구조상 다소 복잡하고 디버깅 비용 증가.
  - 개선 아이디어:
    - 장기적으로는 SaveData에 정식 필드로 편입하고, 리플렉션 경로는 `dataVersion` 기준으로 완전 제거하는 마이그레이션 고려.

- **[P2] `SaveManager`의 `FindObjectsOfType<MonoBehaviour>(true)` 호출 비용**
  - 현재: `SaveGame` / `LoadGame` 시마다 씬 내 **모든 `ISaveable`**을 탐색.
  - 문제:
    - 오브젝트 수가 많으면 저장 시 프레임 스파이크 유발 가능(특히 자동 세이브 타이밍).
  - 개선 아이디어:
    - `ISaveable` 구현체가 자신을 `SaveManager`에 등록/해제하는 구조(리스트 캐싱)로 변경.
    - 또는 “핵심 매니저만 세이브”하는 구조로 제한(필요 없는 컴포넌트 제외).

---

## 4. UI/도감/Addressables

- **[P2] `EncyclopediaPanelController.BuildSlots` 슬롯 재생성 비용**
  - 현재: 패널 열릴 때마다 기존 슬롯 오브젝트를 전부 `Destroy` 후, 25개를 새로 Instantiate.
  - 문제:
    - 자주 여닫는 환경에서는 GC/할당/오브젝트 파괴 비용이 누적될 수 있음.
  - 개선 아이디어:
    - 슬롯들을 최초 1회만 생성하고, 이후에는 활성/비활성 및 데이터만 갱신하는 구조로 변경.
    - `slots` 딕셔너리 유지, `Refresh()`에서 데이터만 갈아끼우기.

- **[P3] `EncyclopediaPanelController`의 `string.Join` 로그**
  - 현재: 언락되지 않은 슬롯 클릭 시 `string.Join(", ", discoveredLevels)`로 디버그 로그 출력.
  - 문제:
    - 개발/디버깅 용도이므로, 실제 빌드에서는 비활성화하거나 로그 레벨/샘플링을 줄이는 것이 바람직.

- **[P3] OfflineRewardPanel / Ad 연동**
  - 현재: `FindObjectOfType<EconomyManager>(true)`를 `Awake()`에서 한 번만 사용 → 빈도는 낮음.
  - 개선 아이디어:
    - `GameSystem.Instance`를 통해 EconomyManager를 DI하는 방식으로 통일(일관성을 위해).

---

## 5. Addressables & 데이터 로딩

- **[P2] `GirlDataManager.LoadAsync` 예외/취소 토큰 처리**
  - 현재: CSV/TSV 파싱은 잘 캡슐화되어 있으며, Addressables 핸들도 finally 블록에서 Release.
  - 개선 아이디어:
    - 대규모 데이터로 확장될 경우를 대비해, `LoadAsync` 진행 중 씬 전환/종료 시 CancellationToken을 실제로 전달/사용하는 구조(현재는 기본값).

---

## 6. 기타 구조 개선 후보

- **[P2] `GirlFieldManager`의 Update/타이머 분리 검토**
  - Idle 수익, 자동 소환/합성, UI 업데이트, 발견 처리 등 다양한 역할을 한 클래스가 담당.
  - 개선 아이디어:
    - “수익 루프”, “자동 소환/합성”, “발견/연출”을 서브 컴포넌트로 나누어 책임 분리.
    - 이 과정에서 타이머/업데이트 루프도 각 역할 단위로 최소화 가능.

- **[P3] 디버그 로그 레벨링**
  - `Debug.Log`, `Debug.LogWarning`, `Debug.LogError`가 여러 곳에서 사용되고 있음.
  - 릴리즈 빌드에서 불필요한 로그 호출을 줄이기 위해, 간단한 래퍼(예: `Log.Info/Warning/Error` + 빌드 플래그)를 통한 제어 고려.

---

이 문서는 **사전 검토용(pre)** TODO이므로, 실제로 손댈 항목을 결정한 뒤에는  

