using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 쿠폰 입력 패널 컨트롤러
/// - 쿠폰 코드 입력 UI
/// - 확인 버튼으로 쿠폰 사용
/// </summary>
public class CouponPanelController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI titleText; // 타이틀 텍스트
    [SerializeField] private TMP_InputField codeInputField; // 쿠폰 코드 입력 필드
    [SerializeField] private Button confirmButton; // 확인 버튼
    [SerializeField] private Button closeButton; // 닫기 버튼
    
    [Header("Reason Label")]
    [SerializeField] private TextMeshProUGUI reasonLabel;
    [SerializeField] private float reasonShowSeconds = 1.5f;
    private Coroutine _reasonRoutine;
    
    [Header("참조")]
    [SerializeField] private CouponManager couponManager;
    
    void Awake()
    {
        if (!couponManager) couponManager = FindObjectOfType<CouponManager>(true);
        BindButtons();
        if (panelRoot) panelRoot.SetActive(false);
        if (reasonLabel != null) reasonLabel.gameObject.SetActive(false);
    }
    
    void OnEnable()
    {
        if (codeInputField != null)
        {
            codeInputField.text = "";
            codeInputField.Select();
            codeInputField.ActivateInputField();
        }
    }
    
    void BindButtons()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnClickConfirm);
        }
        
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(OnClickClose);
        }
        
        // 입력 필드에서 Enter 키로도 확인 가능
        if (codeInputField != null)
        {
            codeInputField.onSubmit.RemoveAllListeners();
            codeInputField.onSubmit.AddListener(_ => OnClickConfirm());
        }
    }
    
    /// <summary>
    /// 쿠폰 패널 열기
    /// </summary>
    public void Show()
    {
        if (panelRoot) panelRoot.SetActive(true);
        if (codeInputField != null)
        {
            codeInputField.text = "";
            codeInputField.Select();
            codeInputField.ActivateInputField();
        }
        HideReasonImmediate();
    }
    
    /// <summary>
    /// 쿠폰 패널 닫기
    /// </summary>
    public void Hide()
    {
        if (panelRoot) panelRoot.SetActive(false);
        HideReasonImmediate();
    }
    
    void OnClickConfirm()
    {
        if (couponManager == null)
        {
            ShowReasonTemp(LocalizationManager.GetText(
                "쿠폰 시스템을 찾을 수 없습니다.",
                "Coupon system not found."
            ));
            return;
        }
        
        string code = codeInputField != null ? codeInputField.text : "";
        
        if (string.IsNullOrWhiteSpace(code))
        {
            ShowReasonTemp(LocalizationManager.GetText(
                "쿠폰 코드를 입력해주세요.",
                "Please enter a coupon code."
            ));
            return;
        }
        
        bool success = couponManager.RedeemCoupon(code);
        
        if (success)
        {
            // 성공 시 "사용되었다" 문구 표시
            ShowReasonTemp(LocalizationManager.GetText(
                "쿠폰이 사용되었습니다!",
                "Coupon redeemed successfully!"
            ));
            
            // 입력 필드 초기화
            if (codeInputField != null)
            {
                codeInputField.text = "";
            }
            
            // 1초 후 패널 닫기
            Invoke(nameof(Hide), 1f);
        }
        else
        {
            // 실패 시 오류 메시지 표시
            ShowReasonTemp(LocalizationManager.GetText(
                "유효하지 않은 쿠폰 코드이거나\n이미 사용한 쿠폰입니다.",
                "Invalid coupon code or\nalready used coupon."
            ));
            
            // 입력 필드만 초기화 (재시도 가능하도록)
            if (codeInputField != null)
            {
                codeInputField.text = "";
                codeInputField.Select();
                codeInputField.ActivateInputField();
            }
        }
    }
    
    void OnClickClose()
    {
        Hide();
    }
    
    // Reason helpers
    void ShowReasonTemp(string msg)
    {
        if (reasonLabel == null) return;
        HideReasonImmediate();
        reasonLabel.text = msg;
        reasonLabel.gameObject.SetActive(true);
        _reasonRoutine = StartCoroutine(HideReasonAfter(reasonShowSeconds));
    }
    
    System.Collections.IEnumerator HideReasonAfter(float sec)
    {
        yield return new WaitForSecondsRealtime(sec);
        HideReasonImmediate();
    }
    
    void HideReasonImmediate()
    {
        if (reasonLabel == null) return;
        if (_reasonRoutine != null)
        {
            StopCoroutine(_reasonRoutine);
            _reasonRoutine = null;
        }
        reasonLabel.text = "";
        reasonLabel.gameObject.SetActive(false);
    }
}

