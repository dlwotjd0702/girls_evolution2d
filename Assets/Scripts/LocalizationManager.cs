using UnityEngine;

/// <summary>
/// 다국어 지원 매니저
/// SaveData에 언어 설정을 저장하고, 최초 실행 시에만 시스템 언어를 사용합니다.
/// 이후에는 SaveData에서 언어 설정을 불러와 사용합니다.
/// </summary>
[DefaultExecutionOrder(-201)] // SaveManager(-200)보다 먼저 실행
public class LocalizationManager : MonoBehaviour, ISaveable
{
    public static LocalizationManager Instance { get; private set; }
    
    private static bool _isKorean = false;
    private static bool _isInitialized = false;
    
    /// <summary>
    /// 현재 한국어 모드인지 확인합니다.
    /// </summary>
    public static bool IsKorean
    {
        get
        {
            // 초기화되지 않았으면 시스템 언어로 임시 설정
            // (SaveManager의 LoadGame이 완료되면 ApplyLoadedData에서 정확한 값으로 업데이트됨)
            if (!_isInitialized)
            {
                _isKorean = Application.systemLanguage == SystemLanguage.Korean;
                _isInitialized = true;
            }
            return _isKorean;
        }
    }
    
    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    private void Start()
    {
        // SaveManager가 로드한 후에 언어 초기화
        // SaveManager의 LoadGame에서 ISaveable.ApplyLoadedData를 호출하므로
        // 여기서는 별도 초기화가 필요 없음
    }
    
    /// <summary>
    /// 언어를 수동으로 변경합니다. (설정 패널에서 호출)
    /// </summary>
    public static void SetLanguage(bool isKorean)
    {
        _isKorean = isKorean;
        _isInitialized = true;
        
        // SaveData에 저장
        if (Instance != null && SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame();
        }
    }
    
    /// <summary>
    /// 한국어일 때는 koreanText를, 그 외에는 englishText를 반환합니다.
    /// </summary>
    public static string GetText(string koreanText, string englishText)
    {
        return IsKorean ? koreanText : englishText;
    }
    
    /// <summary>
    /// 한국어일 때는 koreanText를, 그 외에는 englishText를 반환합니다.
    /// null이거나 빈 문자열인 경우 기본값을 반환합니다.
    /// </summary>
    public static string GetText(string koreanText, string englishText, string defaultText)
    {
        if (IsKorean)
        {
            return string.IsNullOrEmpty(koreanText) ? defaultText : koreanText;
        }
        return string.IsNullOrEmpty(englishText) ? defaultText : englishText;
    }
    
    // ISaveable 구현
    public void CollectSaveData(SaveData data)
    {
        if (data == null) return;
        // languageCode: 0=시스템 언어 미설정(최초 실행), 1=한국어, 2=영어
        data.languageCode = _isKorean ? 1 : 2;
    }
    
    public void ApplyLoadedData(SaveData data)
    {
        if (data == null)
        {
            // SaveData가 null이면 최초 실행으로 간주하여 시스템 언어 사용
            _isKorean = Application.systemLanguage == SystemLanguage.Korean;
            _isInitialized = true;
            return;
        }
        
        // languageCode가 0이면 최초 실행으로 간주하여 시스템 언어 사용
        if (data.languageCode == 0)
        {
            _isKorean = Application.systemLanguage == SystemLanguage.Korean;
        }
        else
        {
            // 저장된 언어 설정 사용 (1=한국어, 2=영어)
            _isKorean = (data.languageCode == 1);
        }
        
        _isInitialized = true;
    }
}

