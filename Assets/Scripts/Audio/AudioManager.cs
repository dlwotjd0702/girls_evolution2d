using UnityEngine;

/// <summary>
/// 오디오 매니저
/// BGM 및 효과음 재생 관리
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    
    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;  // 단일 AudioSource (BGM + SFX 모두 사용)
    
    [Header("BGM")]
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] private bool playBGMOnStart = true;
    [SerializeField] private bool loopBGM = true;
    
    [Header("SFX Clips")]
    [SerializeField] private AudioClip clickSFX;           // 클릭 효과음
    [SerializeField] private AudioClip mergeSFX;           // 합성 효과음
    [SerializeField] private AudioClip discoverySFX;       // 새로운 단계 발견 효과음
    [SerializeField] private AudioClip prestigeSFX;        // 환생 효과음
    
    [Header("Volume Settings")]
    [SerializeField] private float masterVolume = 1f;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // AudioSource 자동 생성
        if (audioSource == null)
        {
            GameObject audioObj = new GameObject("Audio Source");
            audioObj.transform.SetParent(transform);
            audioSource = audioObj.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        
        // 볼륨 설정 불러오기
        LoadVolumeSettings();
        ApplyVolumes();
    }
    
    private void Start()
    {
        if (playBGMOnStart)
        {
            PlayBGM(); // bgmClip이 null이면 내부에서 처리
        }
    }
    
    /// <summary>
    /// 볼륨 설정 불러오기 (PlayerPrefs)
    /// </summary>
    void LoadVolumeSettings()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
    }
    
    /// <summary>
    /// 볼륨 적용
    /// </summary>
    void ApplyVolumes()
    {
        if (audioSource != null)
        {
            audioSource.volume = masterVolume;
        }
    }
    
    /// <summary>
    /// 마스터 볼륨 업데이트 (VolumeSettingsPanel에서 호출)
    /// </summary>
    public void UpdateMasterVolume(float volume)
    {
        masterVolume = volume;
        ApplyVolumes();
    }
    
    // ========== BGM ==========
    
    /// <summary>
    /// BGM 재생
    /// </summary>
    public void PlayBGM()
    {
        if (audioSource == null) return;
        if (bgmClip == null) return; // BGM 클립이 없으면 재생하지 않음
        
        try
        {
            if (audioSource.clip != bgmClip || !audioSource.isPlaying)
            {
                audioSource.clip = bgmClip;
                audioSource.loop = loopBGM;
                audioSource.Play();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[AudioManager] BGM 재생 실패: {e.Message}");
        }
    }
    
    /// <summary>
    /// BGM 정지
    /// </summary>
    public void StopBGM()
    {
        if (audioSource != null)
        {
            try
            {
                audioSource.Stop();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[AudioManager] BGM 정지 실패: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// BGM 일시정지
    /// </summary>
    public void PauseBGM()
    {
        if (audioSource != null)
        {
            try
            {
                audioSource.Pause();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[AudioManager] BGM 일시정지 실패: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// BGM 재개
    /// </summary>
    public void ResumeBGM()
    {
        if (audioSource != null)
        {
            try
            {
                audioSource.UnPause();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[AudioManager] BGM 재개 실패: {e.Message}");
            }
        }
    }
    
    // ========== SFX ==========
    
    /// <summary>
    /// 클릭 효과음 재생
    /// </summary>
    public void PlayClickSFX()
    {
        PlaySFX(clickSFX);
    }
    
    /// <summary>
    /// 합성 효과음 재생
    /// </summary>
    public void PlayMergeSFX()
    {
        PlaySFX(mergeSFX);
    }
    
    /// <summary>
    /// 새로운 단계 발견 효과음 재생
    /// </summary>
    public void PlayDiscoverySFX()
    {
        PlaySFX(discoverySFX);
    }
    
    /// <summary>
    /// 환생 효과음 재생
    /// </summary>
    public void PlayPrestigeSFX()
    {
        PlaySFX(prestigeSFX);
    }
    
    /// <summary>
    /// 효과음 재생 (일반)
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return; // 클립이 없으면 재생하지 않음
        if (audioSource == null) return;
        
        try
        {
            // PlayOneShot을 사용하여 BGM과 동시 재생 가능
            audioSource.PlayOneShot(clip);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[AudioManager] 효과음 재생 실패: {e.Message}");
        }
    }
    
    /// <summary>
    /// 효과음 재생 (볼륨 조절 가능)
    /// </summary>
    public void PlaySFX(AudioClip clip, float volumeScale)
    {
        if (clip == null) return; // 클립이 없으면 재생하지 않음
        if (audioSource == null) return;
        
        try
        {
            audioSource.PlayOneShot(clip, volumeScale);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[AudioManager] 효과음 재생 실패: {e.Message}");
        }
    }
    
    // ========== 유틸리티 ==========
    
    /// <summary>
    /// 모든 오디오 정지
    /// </summary>
    public void StopAll()
    {
        if (audioSource != null)
        {
            try
            {
                audioSource.Stop();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[AudioManager] 오디오 정지 실패: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// BGM 클립 변경
    /// </summary>
    public void SetBGMClip(AudioClip clip)
    {
        bgmClip = clip;
        if (audioSource != null)
        {
            try
            {
                bool wasPlaying = audioSource.isPlaying;
                audioSource.clip = clip;
                if (wasPlaying && clip != null)
                {
                    audioSource.loop = loopBGM;
                    audioSource.Play();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[AudioManager] BGM 클립 변경 실패: {e.Message}");
            }
        }
    }
}

