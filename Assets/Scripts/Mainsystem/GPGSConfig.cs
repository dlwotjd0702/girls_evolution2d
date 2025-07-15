using UnityEngine;

namespace GirlsEvolution2D.MainSystem
{
    [CreateAssetMenu(fileName = "GPGSConfig", menuName = "Girls Evolution 2D/GPGS Config")]
    public class GPGSConfig : ScriptableObject
    {
        [Header("Google Play Games Services 설정")]
        [SerializeField] private string webClientId = "";
        [SerializeField] private bool enableDebugLog = true;
        [SerializeField] private bool autoSignIn = true;
        
        [Header("업적 ID들")]
        [SerializeField] private string firstGirlAchievementId = "CgkI8JqQ8-4YEAIQAQ";
        [SerializeField] private string evolutionAchievementId = "CgkI8JqQ8-4YEAIQAg";
        [SerializeField] private string goldCollectorAchievementId = "CgkI8JqQ8-4YEAIQAw";
        
        [Header("리더보드 ID들")]
        [SerializeField] private string goldLeaderboardId = "CgkI8JqQ8-4YEAIQBA";
        [SerializeField] private string girlsCountLeaderboardId = "CgkI8JqQ8-4YEAIQBQ";
        
        // 프로퍼티들
        public string WebClientId => webClientId;
        public bool EnableDebugLog => enableDebugLog;
        public bool AutoSignIn => autoSignIn;
        
        // 업적 ID들
        public string FirstGirlAchievementId => firstGirlAchievementId;
        public string EvolutionAchievementId => evolutionAchievementId;
        public string GoldCollectorAchievementId => goldCollectorAchievementId;
        
        // 리더보드 ID들
        public string GoldLeaderboardId => goldLeaderboardId;
        public string GirlsCountLeaderboardId => girlsCountLeaderboardId;
        
        // 설정 검증
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(webClientId);
        }
        
        // 개발용 기본 설정
        public void SetDefaultConfig()
        {
            webClientId = "YOUR_WEB_CLIENT_ID_HERE";
            enableDebugLog = true;
            autoSignIn = true;
            
            Debug.Log("GPGS 기본 설정이 적용되었습니다. Web Client ID를 설정해주세요.");
        }
    }
} 