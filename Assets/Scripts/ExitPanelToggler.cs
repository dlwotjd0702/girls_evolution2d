
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

    // "예" 버튼에 연결해서 종료 및 저장
    public void ConfirmExit()
    {
        // 종료 전 저장
        SaveGame();
        
        // 저장 완료 후 종료 (약간의 지연을 주어 저장이 완료되도록)
        StartCoroutine(QuitAfterSave());
    }
    
    // 수동 저장 버튼용 메서드
    public void SaveGame()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame();
        }
        else
        {
            Debug.LogWarning("[ExitPanelToggler] SaveManager.Instance가 null입니다.");
        }
    }
    
    private System.Collections.IEnumerator QuitAfterSave()
    {
        yield return new WaitForSeconds(0.1f); // 저장 완료 대기
        Application.Quit();
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    // "아니오" 버튼에 연결해서 닫기
    public void Cancel() => Hide();
}