using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TierManager : MonoBehaviour, ISaveable
{
    [field: SerializeField, Range(0,3)]
    public int CurrentTierIndex { get; private set; } = 0;

    // 0층은 기본 오픈
    public readonly bool[] Unlocked = new bool[4] { true, false, false, false };

    public event Action<int> OnTierChanged;
    public event Action<int> OnTierUnlocked;

    [Header("UI")]
    [SerializeField] private Dropdown tierDropdown;     // 드롭다운(0~3 중 언락만, 현재층 제외)
    [SerializeField] private string captionDefault = "계층 이동"; // 캡션 텍스트
    private readonly List<int> _optionMap = new List<int>(4); // 옵션 인덱스 → tier 인덱스(0~3)

    void Awake()
    {
        if (tierDropdown != null)
        {
            tierDropdown.onValueChanged.RemoveAllListeners();
            tierDropdown.onValueChanged.AddListener(OnDropdownSelected);
        }
    }

    void Start()
    {
        RefreshCaptionOnly(); // 시작 시 캡션만 정리(옵션은 열 때마다 갱신)
    }

    // ─────────────────────────
    // 드롭다운 "열기 직전" 준비용 (EventTrigger: PointerDown 등에 연결)
    // ─────────────────────────
    public void PrepareDropdownForOpen()
    {
        if (tierDropdown == null) return;

        var opts = tierDropdown.options;
        opts.Clear();
        _optionMap.Clear();

        // 0~3 중 언락된 층만, 현재층 제외해서 옵션 구성
        for (int t = 0; t <= 3; t++)
        {
            if (!Unlocked[t]) continue;
            if (t == CurrentTierIndex) continue;

            opts.Add(new Dropdown.OptionData($"{t}층"));
            _optionMap.Add(t);
        }

        bool hasAny = _optionMap.Count > 0;
        tierDropdown.interactable = hasAny;
        tierDropdown.gameObject.SetActive(hasAny);

        // 드롭다운을 "버튼처럼" 쓸 거라 캡션은 고정 텍스트 유지
        tierDropdown.SetValueWithoutNotify(0);

        // 옵션이 전혀 없으면(= 언락된 다른 층이 없음) 드롭다운 숨김
        if (!hasAny)
            tierDropdown.gameObject.SetActive(false);
    }

    // 선택 시 호출: 매핑된 실제 tier로 전환
    private void OnDropdownSelected(int optionIndex)
    {
        if (optionIndex < 0 || optionIndex >= _optionMap.Count) return;
        int targetTier = _optionMap[optionIndex];
        SwitchTo(targetTier);

        // 선택 후 캡션만 다시 기본 텍스트로 유지
        RefreshCaptionOnly();
    }

    private void RefreshCaptionOnly()
    {
        if (tierDropdown == null) return;
        // Dropdown 캡션 라벨에 기본 텍스트(예: "계층 이동") 유지
        var caption = tierDropdown.captionText;
        if (caption != null) caption.text = captionDefault;
    }

    // ─────────────────────────
    // 계층 전환 / 언락
    // ─────────────────────────
    public void SwitchTo(int tierIndex)
    {
        if ((uint)tierIndex > 3u) return;
        if (!Unlocked[tierIndex]) return;
        if (CurrentTierIndex == tierIndex) return;

        CurrentTierIndex = tierIndex;
        OnTierChanged?.Invoke(CurrentTierIndex);
    }

    // 레벨 기반 자동 언락(9→1층, 17→2층, 25→3층)
    public bool TryUnlockByLevel(int level)
    {
        int t = TierRules.TierIndexFromLevel(level);
        bool changed = false;
        for (int i = 0; i <= t; i++)
        {
            if (!Unlocked[i])
            {
                Unlocked[i] = true;
                OnTierUnlocked?.Invoke(i);
                changed = true;
            }
        }
        // 드롭다운은 열 때마다 준비하므로 여기선 캡션만 유지하면 됨
        RefreshCaptionOnly();
        return changed;
    }

    // ─────────────────────────
    // 저장/복원
    // ─────────────────────────
    public void CollectSaveData(SaveData data)
    {
        data.currentTierIndex = CurrentTierIndex;
        data.unlockedTierMask =
            (Unlocked[0] ? 1 : 0) |
            (Unlocked[1] ? 2 : 0) |
            (Unlocked[2] ? 4 : 0) |
            (Unlocked[3] ? 8 : 0);
    }

    public void ApplyLoadedData(SaveData data)
    {
        CurrentTierIndex = Mathf.Clamp(data.currentTierIndex, 0, 3);

        for (int i = 0; i < 4; i++)
            Unlocked[i] = (data.unlockedTierMask & (1 << i)) != 0;

        Unlocked[0] = true; // 안전장치
        OnTierChanged?.Invoke(CurrentTierIndex);

        // 복원 직후에도 캡션만 유지(옵션은 열 때 새로)
        RefreshCaptionOnly();
    }
}
