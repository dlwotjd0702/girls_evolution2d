using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AutoAutomationController : MonoBehaviour
{
    public enum AutoType { AutoMerge, AutoSpawn }
    public enum PurchaseCurrency { Gold, Gem } // 인스펙터에서 선택

    [Header("Config")]
    public AutoType type;
    [Tooltip("업그레이드 결제 통화 선택 (Gold/Gem). Gem은 Economy에 TryBuy...WithGems(...) API가 있을 때만 실제 구매 시도.")]
    public PurchaseCurrency purchaseCurrency = PurchaseCurrency.Gold;

    [Header("Refs")]
    public EconomyManager economy;
    [Tooltip("선택: 보석 부족/골드 부족 시 띄울 패널(광고/상점 유도)")]
    public InsufficientFundsPanel insufficientPanel;

    // 선택: 환생 상점 영구 단축 표시용(PrestigeShopManager 페이사드)
    private object _prestigeShop;            // PrestigeShopManager.Instance
    private MethodInfo _miGetAutoMergeMul;   // double GetAutoMergeIntervalMul()
    private MethodInfo _miGetAutoSpawnMul;   // double GetAutoSpawnIntervalMul()

    [Header("Texts (TMP)")]
    public TextMeshProUGUI combinedLabel; // "자동 합성(3.2s) • -15%"
    public TextMeshProUGUI levelText;     // "Lv. x / y"
    public TextMeshProUGUI costText;      // 다음 비용(N0), MAX면 "-"

    [Header("Buy/Upgrade (Icon Only)")]
    public Button buyOrUpgradeButton;
    public Image  buyIconTarget;
    public Sprite buyIconSprite;      // Lv0
    public Sprite upgradeIconSprite;  // 1..Cap-1
    public Sprite maxIconSprite;      // Cap

    [Header("Toggle (Single Button + Icon)")]
    public Button toggleButton;       // 한 개 버튼으로 온/오프
    public Image  toggleIconTarget;   // 온/오프 이미지 교체
    public Sprite toggleOnSprite;
    public Sprite toggleOffSprite;

    [Header("Reason Label (Fallback)")]
    public TextMeshProUGUI reasonLabel;
    public float reasonShowSeconds = 1.15f;
    Coroutine _reasonRoutine;

    void Awake()
    {
        if (!economy) economy = FindObjectOfType<EconomyManager>();

        if (buyOrUpgradeButton) buyOrUpgradeButton.onClick.AddListener(OnClickBuyOrUpgrade);
        if (toggleButton)       toggleButton.onClick.AddListener(OnClickToggle);

        if (reasonLabel) reasonLabel.gameObject.SetActive(false);

        CachePrestigeShop();
    }

    void OnEnable()
    {
        if (economy!=null)
        {
            economy.OnGoldChanged    += HandleGoldChanged;
            economy.OnUpgradeChanged += HandleUpgradeChanged;
        }
        Refresh();
    }
    void OnDisable()
    {
        if (economy!=null)
        {
            economy.OnGoldChanged    -= HandleGoldChanged;
            economy.OnUpgradeChanged -= HandleUpgradeChanged;
        }
    }
    void HandleGoldChanged(double _) => Refresh();
    void HandleUpgradeChanged() => Refresh();

    public void Refresh()
    {
        if (!economy) return;

        int  lv  = (type==AutoType.AutoMerge) ? economy.GetAutoMergeUpgradeLevel()
                                              : economy.GetAutoSpawnUpgradeLevel();
        int  cap = (type==AutoType.AutoMerge) ? economy.GetAutoMergeCap()
                                              : economy.GetAutoSpawnCap();
        bool on  = (type==AutoType.AutoMerge) ? economy.IsAutoMergeOn()
                                              : economy.IsAutoSpawnOn();

        // ── 표시 값(현재 간격 + 환생 영구 단축 %) ──
        string title = (type==AutoType.AutoMerge) ? "자동 합성" : "자동 소환";
        string value;
        if (lv <= 0) value = "잠김";
        else
        {
            float iv = (type==AutoType.AutoMerge) ? economy.GetAutoMergeInterval()
                                                  : economy.GetAutoSpawnInterval();
            value = (iv >= float.MaxValue*0.5f) ? "-" : $"{iv:0.0}s";
        }

        string perm = GetPrestigeAutoReducePercentText();
        if (combinedLabel) combinedLabel.text = string.IsNullOrEmpty(perm)
            ? $"{title}({value})"
            : $"{title}({value}) • {perm}";

        // 레벨/코스트
        if (levelText) levelText.text = FormatLvCap(lv, cap);
        double nextCost = (type==AutoType.AutoMerge) ? economy.GetAutoMergeNextCost()
                                                     : economy.GetAutoSpawnNextCost();
        if (costText) costText.text = double.IsInfinity(nextCost) ? "-" : $"{nextCost:N0}";

        // 아이콘 & 상호작용
        if (buyIconTarget)
        {
            if      (lv <= 0)        buyIconTarget.sprite = buyIconSprite;
            else if (lv >= cap)      buyIconTarget.sprite = maxIconSprite;
            else                     buyIconTarget.sprite = upgradeIconSprite;
        }
        if (buyOrUpgradeButton) buyOrUpgradeButton.interactable = (lv < cap);

        if (toggleButton)      toggleButton.gameObject.SetActive(lv > 0);
        if (toggleIconTarget)  toggleIconTarget.sprite = on ? toggleOnSprite : toggleOffSprite;
    }

    void OnClickBuyOrUpgrade()
    {
        if (!economy) { ShowReason("시스템 미준비"); return; }

        int lv  = (type==AutoType.AutoMerge) ? economy.GetAutoMergeUpgradeLevel()
                                             : economy.GetAutoSpawnUpgradeLevel();
        int cap = (type==AutoType.AutoMerge) ? economy.GetAutoMergeCap()
                                             : economy.GetAutoSpawnCap();
        if (lv >= cap) { ShowReason("최대 레벨입니다."); return; }

        double needGold = (type==AutoType.AutoMerge) ? economy.GetAutoMergeNextCost()
                                                     : economy.GetAutoSpawnNextCost();

        // ── 통화 분기 ──
        if (purchaseCurrency == PurchaseCurrency.Gold)
        {
            bool ok = (type==AutoType.AutoMerge) ? economy.TryBuyAutoMergeUpgrade()
                                                 : economy.TryBuyAutoSpawnUpgrade();

            if (!ok)
            {
                // 골드 부족 → 패널로 유도(광고/보석 상점)
                if (insufficientPanel) insufficientPanel.ShowForGoldShortage(needGold, economy.GetGold());
                else
                {
                    if (economy.GetGold() < needGold) ShowReason("골드가 부족합니다.");
                    else ShowReason("구매가 불가합니다.");
                }
                return;
            }
        }
        else // PurchaseCurrency.Gem
        {
            // 1) Economy에 보석 구매 API가 있으면 시도 (리플렉션)
            string method = (type==AutoType.AutoMerge) ? "TryBuyAutoMergeWithGems" : "TryBuyAutoSpawnWithGems";
            try
            {
                var mi = economy.GetType().GetMethod(method, BindingFlags.Public|BindingFlags.Instance);
                if (mi != null && mi.ReturnType == typeof(bool))
                {
                    bool ok = (bool)mi.Invoke(economy, null);
                    if (!ok)
                    {
                        // 보석 부족 → 보석 상점 유도 패널
                        if (insufficientPanel)
                        {
                            long have = GetCurrentGems();
                            long need = EstimateGemsFromGold(needGold); // 추정치(메시지용)
                            insufficientPanel.ShowForGemShortage(need, have);
                        }
                        else ShowReason("보석이 부족합니다.");
                        return;
                    }
                }
                else
                {
                    // 보석 구매 API 미구현 → 상점 열기 유도
                    if (insufficientPanel)
                    {
                        long have = GetCurrentGems();
                        long need = EstimateGemsFromGold(needGold);
                        insufficientPanel.ShowForGemShortage(need, have);
                    }
                    else ShowReason("보석 구매 경로가 없습니다.");
                    return;
                }
            }
            catch
            {
                ShowReason("보석 구매 실패");
                return;
            }
        }

        HideReason();
        Refresh();
    }

    void OnClickToggle()
    {
        if (!economy) { ShowReason("시스템 미준비"); return; }

        int lv = (type==AutoType.AutoMerge) ? economy.GetAutoMergeUpgradeLevel()
                                            : economy.GetAutoSpawnUpgradeLevel();
        if (lv <= 0) { ShowReason("먼저 구매로 언락하세요."); return; }

        bool on = (type==AutoType.AutoMerge) ? economy.IsAutoMergeOn()
                                             : economy.IsAutoSpawnOn();

        if (type==AutoType.AutoMerge) economy.SetAutoMergeOn(!on);
        else                          economy.SetAutoSpawnOn(!on);

        HideReason();
        Refresh();
    }

    // "Lv. 현재 / 최대" 포맷
    string FormatLvCap(int lv, int cap)
    {
        if (cap <= 0) return $"Lv. {lv}";
        lv = Mathf.Clamp(lv, 0, cap);
        return $"Lv.{lv}/{cap}";
    }

    // ───────────────────── Prestige 연동(표시 전용) ─────────────────────
    void CachePrestigeShop()
    {
        try
        {
            var t = FindTypeByName("PrestigeShopManager");
            if (t == null) return;
            var pi = t.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            _prestigeShop = pi?.GetValue(null, null);
            _miGetAutoMergeMul = t.GetMethod("GetAutoMergeIntervalMul", BindingFlags.Public | BindingFlags.Instance);
            _miGetAutoSpawnMul = t.GetMethod("GetAutoSpawnIntervalMul", BindingFlags.Public | BindingFlags.Instance);
        }
        catch { }
    }

    string GetPrestigeAutoReducePercentText()
    {
        try
        {
            if (_prestigeShop == null) return "";
            double mul =
                (type==AutoType.AutoMerge)
                ? Convert.ToDouble(_miGetAutoMergeMul?.Invoke(_prestigeShop, null) ?? 1.0)
                : Convert.ToDouble(_miGetAutoSpawnMul?.Invoke(_prestigeShop, null) ?? 1.0);

            double reducePct = (1.0 - Mathf.Clamp01((float)mul)) * 100.0;
            if (reducePct <= 0.0001) return "";
            return $"-{reducePct:0.#}%";
        }
        catch { return ""; }
    }

    static Type FindTypeByName(string name)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(name);
            if (t != null) return t;
            try
            {
                foreach (var tt in asm.GetTypes())
                    if (tt.Name == name) return tt;
            }
            catch { }
        }
        return null;
    }

    // ───────────────────── Gem/Panel helpers ─────────────────────
    long GetCurrentGems()
    {
        try
        {
            //var pcm = PremiumCurrencyManager.Instance ?? FindObjectOfType<PremiumCurrencyManager>(true);
            //if (pcm != null) return pcm.GetGems();
        } catch {}
        // Economy에 GetGems()가 있다면 폴백
        try
        {
            var t = economy?.GetType();
            var mi = t?.GetMethod("GetGems", BindingFlags.Public|BindingFlags.Instance);
            if (mi != null) return Convert.ToInt64(mi.Invoke(economy, null));
        } catch {}
        return 0;
    }

    // 추정용(표시만): 골드 → 보석 환산. 실제 결제는 Economy의 TryBuy...WithGems()에 위임.
    [Tooltip("표시용 환산(골드→보석) 추정. 실제 결제엔 사용하지 않음.")]
    public double goldToGemFactorForHint = 0.01;
    long EstimateGemsFromGold(double goldCost)
    {
        double f = Math.Max(1e-6, goldToGemFactorForHint);
        return (long)Math.Max(1, Math.Ceiling(goldCost * f));
    }

    // Reason helpers (패널 없을 땐 라벨 fallback)
    void ShowReason(string msg)
    {
        if (insufficientPanel && !string.IsNullOrEmpty(msg))
        {
            // 골드/보석 부족 상황이 아니면 범용 메시지로
            insufficientPanel.ShowGeneric("안내", msg);
            return;
        }
        if (!reasonLabel) return;
        HideReason();
        reasonLabel.text = msg;
        reasonLabel.gameObject.SetActive(true);
        _reasonRoutine = StartCoroutine(HideAfter(reasonShowSeconds));
    }
    System.Collections.IEnumerator HideAfter(float sec){ yield return new WaitForSecondsRealtime(sec); HideReason(); }
    void HideReason(){ if(!reasonLabel) return; if(_reasonRoutine!=null){ StopCoroutine(_reasonRoutine); _reasonRoutine=null; } reasonLabel.text=""; reasonLabel.gameObject.SetActive(false); }
}
