using System;
using System.Linq;
using UnityEngine;

/// <summary>Online-only bonuses. Never changes prices, prestige, or offline estimates.</summary>
public class IncomeActivityManager : MonoBehaviour, ISaveable
{
    public static IncomeActivityManager Instance { get; private set; }
    [Min(1)] public int clicksPerSecond = 3;
    [Min(0.1f)] public float sustainSeconds = 3;
    [Range(0, 2)] public float feverBonus = 0.5f;
    [Min(1)] public int boostSeconds = 600;
    [Range(1, 5)] public float adMultiplier = 2;
    public FeverMeter Fever { get; } = new FeverMeter();
    long boostUntilUtc;
    int requestVersion;
    IAdOfferService adService;
    public bool RequestPending { get; private set; }
    public static long UtcNow => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    public long BoostSecondsRemaining => Math.Max(0, boostUntilUtc - UtcNow);
    public double OnlineMultiplier => GetOnlineMultiplier(UtcNow);
    public double GetOnlineMultiplier(long now) => (boostUntilUtc > now ? adMultiplier : 1)
        * (1 + (Fever.IsActive ? feverBonus : 0));

    void Awake() { Instance = this; }
    void OnDestroy() { requestVersion++; if (Instance == this) Instance = null; }
    void Update()
    {
        Fever.RequiredClicksPerSecond = clicksPerSecond;
        Fever.ChargeSeconds = sustainSeconds;
        Fever.Tick(Time.realtimeSinceStartupAsDouble, Time.unscaledDeltaTime);
    }
    void OnApplicationFocus(bool focused) { if (!focused) Fever.Reset(); }
    void OnApplicationPause(bool paused) { if (paused) Fever.Reset(); }
    public void RecordCharacterClick() => Fever.RecordClick(Time.realtimeSinceStartupAsDouble);

    public IAdOfferService FindAdService()
    {
        if (adService is UnityEngine.Object provider && !provider) adService = null;
        return adService ?? (adService = FindObjectsOfType<MonoBehaviour>(true).OfType<IAdOfferService>().FirstOrDefault());
    }
    public bool CanRequestBoost => !RequestPending && BoostSecondsRemaining == 0
        && ((PremiumCurrencyManager.Instance && PremiumCurrencyManager.Instance.AdsRemoved)
            || (FindAdService()?.IsRewardedReady() ?? false));

    public void RequestBoost()
    {
        RequestBoost(FindAdService(), PremiumCurrencyManager.Instance && PremiumCurrencyManager.Instance.AdsRemoved);
    }

    // An ad-free entitlement uses the same capped duration without fabricating an ad view.
    public bool RequestBoost(IAdOfferService ads, bool adFree)
    {
        if (RequestPending || BoostSecondsRemaining > 0) return false;
        if (!adFree && (ads == null || !ads.IsRewardedReady())) return false;
        int request = ++requestVersion;
        RequestPending = true;
        bool granted = false;
        Action reward = () =>
        {
            if (!this || request != requestVersion || granted) return;
            granted = true;
            boostUntilUtc = UtcNow + boostSeconds;
            RequestPending = false;
            SaveManager.Instance?.SaveGame();
        };
        if (adFree) { reward(); return true; }
        if (!(ads is IAdOfferWithCompletion completion))
        {
            RequestPending = false;
            return false; // A provider must report cancellation so the button cannot get stuck.
        }
        try
        {
            completion.ShowRewarded(reward, () => { if (this && request == requestVersion) RequestPending = false; });
        }
        catch (Exception error) { RequestPending = false; Debug.LogException(error); return false; }
        return true;
    }

    public void CollectSaveData(SaveData data) { data.incomeBoostUntilUtc = boostUntilUtc; }
    public void ApplyLoadedData(SaveData data)
    {
        requestVersion++; RequestPending = false; Fever.Reset();
        // A loaded clock cannot grant more than one configured session of bonus time.
        boostUntilUtc = Math.Max(0, Math.Min(data.incomeBoostUntilUtc, UtcNow + boostSeconds));
    }
}
