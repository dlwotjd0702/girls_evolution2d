# girls_evolution2d

## 프로젝트 소개

**girls_evolution2d**는 Unity 기반의 2D 방치형/진화형 게임 프로젝트입니다. 다양한 소녀 캐릭터의 진화, 골드 수집, 업그레이드, 프레스티지 등 핵심 시스템을 구현하고 있습니다.

## 폴더 구조 및 주요 스크립트

```
Scripts/
  ├─ UI/                # 게임 UI 관리 (GameUIManager, PrestigeUIController 등)
  ├─ Save_Datas/        # 세이브 데이터 관리 (SaveManager, SaveData)
  ├─ Mainsystem/        # 핵심 시스템 (PrestigeManager, UpgradeManager, GirlMergeManager 등)
  ├─ Interface/         # 인터페이스(ISaveable)
  ├─ Gold/              # 골드 관련 시스템 (IdleGoldManager, ClickGoldManager 등)
  └─ Girl/              # 캐릭터, 진화 데이터 (GirlCharacter, GirlFieldManager 등)
```

## 실행 방법

1. Unity Hub에서 본 프로젝트 폴더를 열어주세요.
2. `SampleScene.unity`를 실행하면 기본 게임 시스템을 확인할 수 있습니다.
3. 2D 프로젝트로 세팅되어 있으니, 2D 환경에서 개발 및 테스트가 가능합니다.

## 개발/기여 가이드

- 모든 스크립트는 C#으로 작성되어 있습니다.
- 2D 전용 컴포넌트(SpriteRenderer, Rigidbody2D 등)를 사용해주세요.
- 새로운 기능 추가 시, 관련 폴더에 스크립트를 분류해 주세요.
- 커밋 메시지는 명확하게 작성해 주세요.

## 참고사항

- 본 프로젝트는 3D에서 2D로 전환된 상태입니다.
- 추가 문의나 요청 사항은 이슈로 등록하거나, 직접 연락해 주세요. 