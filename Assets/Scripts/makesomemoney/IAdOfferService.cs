// Assets/Scripts/ads/IAdOfferService.cs
using System;
using UnityEngine;

public interface IAdOfferService
{
    bool IsRewardedReady();
    void LoadRewarded();
    void ShowRewarded(Action onReward);

    event Action<bool> OnRewardedReadyChanged;
}

public interface IAdOfferWithCompletion : IAdOfferService
{
    void ShowRewarded(Action onReward, Action onFinished);
}




