using System;

public interface IAdOfferService
{
    bool IsReady(string placement = null);
    void ShowRewarded(string placement, Action onRewarded, Action onClosed = null);
}