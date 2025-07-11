using UnityEngine;

public class PrestigeUIController : MonoBehaviour
{
    public PrestigeManager prestigeManager;
    public GameObject prestigeButton;

    void Start()
    {
        // 환생 가능시 버튼 활성화
        prestigeManager.onPrestigeAvailable += () => { prestigeButton.SetActive(true); };
        prestigeButton.SetActive(false);
    }

    public void OnClickPrestige()
    {
        prestigeManager.DoPrestige();
        prestigeButton.SetActive(false);
    }
}