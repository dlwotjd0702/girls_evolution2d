using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 쿠폰 보상 데이터 구조
/// </summary>
[Serializable]
public class CouponReward
{
    public enum RewardType
    {
        Gold,
        Gem,
        PrestigePoint
    }

    [Header("쿠폰 정보")]
    [Tooltip("쿠폰 코드 (대소문자 구분 없음)")]
    public string code = "";
    
    [Header("보상 타입")]
    [Tooltip("보상 타입 선택")]
    public RewardType rewardType = RewardType.Gold;
    [HideInInspector] public bool isGoldReward = true;
    
    [Header("보상 금액")]
    [Tooltip("지급할 골드 또는 보석의 양")]
    public double rewardAmount = 0;
}

/// <summary>
/// 쿠폰 관리 클래스
/// - 인스펙터에서 쿠폰 코드와 보상 설정
/// - 쿠폰 검증 및 보상 지급
/// - 사용한 쿠폰 저장
/// </summary>
public class CouponManager : MonoBehaviour, ISaveable
{
    public static CouponManager Instance { get; private set; }
    
    [Header("쿠폰 데이터베이스")]
    [Tooltip("인스펙터에서 쿠폰 코드와 보상을 설정합니다")]
    [SerializeField] private List<CouponReward> couponDatabase = new List<CouponReward>();
    
    [Header("참조")]
    [SerializeField] private EconomyManager economy;
    [SerializeField] private PremiumCurrencyManager premiumCurrency;
    [SerializeField] private OfflineRewardPanel offlineRewardPanel;
    
    // 사용한 쿠폰 목록 (SaveData에서 로드)
    private HashSet<string> usedCoupons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    
    // 대기 중인 쿠폰 보상 (오프라인 보상 패널에 표시하기 위해)
    private CouponReward pendingReward = null;
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    void Start()
    {
        if (!economy) economy = FindObjectOfType<EconomyManager>(true);
        if (!premiumCurrency) premiumCurrency = FindObjectOfType<PremiumCurrencyManager>(true);
        if (!offlineRewardPanel) offlineRewardPanel = FindObjectOfType<OfflineRewardPanel>(true);
    }
    
    /// <summary>
    /// 쿠폰 코드를 검증하고 보상을 지급합니다
    /// </summary>
    /// <param name="code">입력한 쿠폰 코드</param>
    /// <returns>성공 여부</returns>
    public bool RedeemCoupon(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            Debug.LogWarning("[CouponManager] 쿠폰 코드가 비어있습니다.");
            return false;
        }
        
        // 대소문자 구분 없이 비교
        string normalizedCode = code.Trim();
        
        // 이미 사용한 쿠폰인지 확인
        if (usedCoupons.Contains(normalizedCode))
        {
            Debug.LogWarning($"[CouponManager] 이미 사용한 쿠폰입니다: {normalizedCode}");
            return false;
        }
        
        // 쿠폰 데이터베이스에서 찾기
        CouponReward reward = null;
        foreach (var coupon in couponDatabase)
        {
            if (string.Equals(coupon.code.Trim(), normalizedCode, StringComparison.OrdinalIgnoreCase))
            {
                reward = coupon;
                break;
            }
        }
        
        if (reward == null)
        {
            Debug.LogWarning($"[CouponManager] 유효하지 않은 쿠폰 코드입니다: {normalizedCode}");
            return false;
        }
        
        // 사용한 쿠폰으로 표시
        usedCoupons.Add(normalizedCode);
        
        // 보상이 있는지 확인
        bool hasReward = reward.rewardAmount > 0;
        
        if (hasReward)
        {
            var type = GetRewardType(reward);
            if (type == CouponReward.RewardType.PrestigePoint)
            {
                GrantRewardImmediate(reward);
            }
            else
            {
                // 오프라인 보상 패널에 표시하기 위해 보상 저장
                pendingReward = reward;

                // 오프라인 보상 패널이 있으면 표시, 없으면 즉시 지급
                if (offlineRewardPanel != null)
                {
                    ShowCouponReward(reward);
                }
                else
                {
                    GrantRewardImmediate(reward);
                }
            }
        }
        else
        {
            Debug.Log($"[CouponManager] 쿠폰이 사용되었지만 보상이 없습니다: {normalizedCode}");
        }
        
        // 세이브 데이터 저장
        SaveManager.Instance?.SaveGame();
        
        return true;
    }
    
    /// <summary>
    /// 쿠폰 보상을 오프라인 보상 패널에 표시
    /// </summary>
    void ShowCouponReward(CouponReward reward)
    {
        if (offlineRewardPanel == null) return;
        
        var type = GetRewardType(reward);
        // 골드 보상인 경우 Payload에 골드 금액 설정, 보석 보상인 경우 0
        double goldReward = type == CouponReward.RewardType.Gold ? reward.rewardAmount : 0;
        long gemReward = type == CouponReward.RewardType.Gem ? (long)reward.rewardAmount : 0;
        
        var payload = new OfflineRewardPanel.Payload
        {
            rawSeconds = 0,
            appliedSeconds = 0,
            perSecondIncome = 0,
            multiplier = 1.0,
            totalReward = goldReward
        };
        
        // 쿠폰 보상임을 표시하기 위해 durationText에 "쿠폰 보상" 표시
        offlineRewardPanel.ShowCouponReward(payload, reward);
    }
    
    /// <summary>
    /// 보상을 즉시 지급 (오프라인 보상 패널이 없을 때)
    /// </summary>
    void GrantRewardImmediate(CouponReward reward)
    {
        var type = GetRewardType(reward);
        if (type == CouponReward.RewardType.Gold && economy != null && reward.rewardAmount > 0)
        {
            economy.AddGold(reward.rewardAmount);
            Debug.Log($"[CouponManager] 골드 지급: {reward.rewardAmount}");
        }
        else if (type == CouponReward.RewardType.Gem && premiumCurrency != null && reward.rewardAmount > 0)
        {
            premiumCurrency.AddGems((long)reward.rewardAmount);
            Debug.Log($"[CouponManager] 보석 지급: {reward.rewardAmount}");
        }
        else if (type == CouponReward.RewardType.PrestigePoint && reward.rewardAmount > 0)
        {
            int amount = Mathf.Max(0, Mathf.RoundToInt((float)reward.rewardAmount));
            if (PrestigeManager.Instance != null && amount > 0)
            {
                PrestigeManager.Instance.AddPrestigePoints(amount);
                Debug.Log($"[CouponManager] 환생 포인트 지급: {amount}");
            }
        }
    }
    
    /// <summary>
    /// 쿠폰이 이미 사용되었는지 확인
    /// </summary>
    public bool IsCouponUsed(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        return usedCoupons.Contains(code.Trim());
    }
    
    // ===== Save / Load =====
    public void CollectSaveData(SaveData d)
    {
        d.usedCoupons = new List<string>(usedCoupons);
    }
    
    public void ApplyLoadedData(SaveData d)
    {
        usedCoupons.Clear();
        if (d.usedCoupons != null)
        {
            foreach (var code in d.usedCoupons)
            {
                if (!string.IsNullOrWhiteSpace(code))
                {
                    usedCoupons.Add(code.Trim());
                }
            }
        }
    }

    CouponReward.RewardType GetRewardType(CouponReward reward)
    {
        if (reward == null) return CouponReward.RewardType.Gold;
        return reward.rewardType;
    }
}

