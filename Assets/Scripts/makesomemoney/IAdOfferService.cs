// Assets/Scripts/ads/IAdOfferService.cs
using System;

public interface IAdOfferService
{
    bool IsRewardedReady();
    void LoadRewarded();
    void ShowRewarded(Action onReward);

    event Action<bool> OnRewardedReadyChanged;
}