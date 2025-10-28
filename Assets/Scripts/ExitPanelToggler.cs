// ExitPanelToggler.cs
using UnityEngine;

public class ExitPanelToggler : MonoBehaviour
{
    [Header("패널 루트 (네가 만든 GameObject 할당)")]
    public GameObject panelRoot;

    [Header("동작 옵션")]
    public bool toggleWithBackKey = true;   // 뒤로가기로 토글할지
    public bool closeIfOpenOnBack = true;   // 열려있을 때 뒤로가면 닫기

    public bool IsOpen => panelRoot && panelRoot.activeSelf;

    void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    void Update()
    {
        if (!toggleWithBackKey) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!IsOpen) Show();
            else if (closeIfOpenOnBack) Hide();
        }
    }

    // --- 외부/버튼에서 호출할 메서드들 ---
    public void Show()
    {
        if (panelRoot == null) return;
        panelRoot.SetActive(true);
    }

    public void Hide()
    {
        if (panelRoot == null) return;
        panelRoot.SetActive(false);
    }

    public void Toggle()
    {
        if (IsOpen) Hide(); else Show();
    }

    // "예" 버튼에 연결해서 종료
    public void ConfirmExit()
    {
        Application.Quit();
    }

    // "아니오" 버튼에 연결해서 닫기
    public void Cancel() => Hide();
}