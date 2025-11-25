using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 골드 획득 팝업 풀
/// </summary>
public class GoldGainPopupPool : MonoBehaviour
{
    [SerializeField] private GoldGainPopup popupPrefab;
    [SerializeField] private int preloadCount = 12;

    private readonly Queue<GoldGainPopup> pool = new();

    void Awake()
    {
        if (popupPrefab == null)
        {
            Debug.LogError("[GoldGainPopupPool] popupPrefab이 설정되지 않았습니다.");
            enabled = false;
            return;
        }

        for (int i = 0; i < preloadCount; i++)
            CreateNewPopup();
    }

    private GoldGainPopup CreateNewPopup()
    {
        var popup = Instantiate(popupPrefab, transform);
        popup.gameObject.SetActive(false);
        popup.SetReleaseCallback(ReturnToPool);
        pool.Enqueue(popup);
        return popup;
    }

    private GoldGainPopup GetPopup()
    {
        if (pool.Count == 0)
            CreateNewPopup();

        var popup = pool.Dequeue();
        popup.gameObject.SetActive(true);
        popup.transform.SetParent(transform, false);
        return popup;
    }

    public void Show(Vector2 anchoredPosition, double amount, bool isClick)
    {
        var popup = GetPopup();
        popup.Play(anchoredPosition, amount, isClick);
    }

    private void ReturnToPool(GoldGainPopup popup)
    {
        if (popup == null) return;
        popup.Stop();
        popup.gameObject.SetActive(false);
        popup.transform.SetParent(transform, false);
        pool.Enqueue(popup);
    }
}


