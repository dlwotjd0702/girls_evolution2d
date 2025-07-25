/*using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GirlsEvolution2D.MainSystem;

namespace GirlsEvolution2D.UI
{
    public class GPGSUIController : MonoBehaviour
    {
        [Header("UI 요소들")]
        [SerializeField] private Button signInButton;
        [SerializeField] private Button signOutButton;
        [SerializeField] private Button achievementsButton;
        [SerializeField] private Button leaderboardButton;
        [SerializeField] private TextMeshProUGUI userInfoText;
        [SerializeField] private GameObject gpgsPanel;
        
        [Header("설정")]
        [SerializeField] private bool autoSignIn = true;
        
        private void Start()
        {
            SetupUI();
            SetupEventListeners();
            
            if (autoSignIn && GPGSManager.Instance != null)
            {
                GPGSManager.Instance.SignIn();
            }
        }
        
        private void SetupUI()
        {
            if (signInButton != null)
                signInButton.onClick.AddListener(OnSignInClicked);
                
            if (signOutButton != null)
                signOutButton.onClick.AddListener(OnSignOutClicked);
                
            if (achievementsButton != null)
                achievementsButton.onClick.AddListener(OnAchievementsClicked);
                
            if (leaderboardButton != null)
                leaderboardButton.onClick.AddListener(OnLeaderboardClicked);
        }
        
        private void SetupEventListeners()
        {
            if (GPGSManager.Instance != null)
            {
                GPGSManager.Instance.OnAuthenticationChanged += OnAuthenticationChanged;
                GPGSManager.Instance.OnSignInSuccess += OnSignInSuccess;
                GPGSManager.Instance.OnSignInFailed += OnSignInFailed;
            }
        }
        
        private void OnDestroy()
        {
            if (GPGSManager.Instance != null)
            {
                GPGSManager.Instance.OnAuthenticationChanged -= OnAuthenticationChanged;
                GPGSManager.Instance.OnSignInSuccess -= OnSignInSuccess;
                GPGSManager.Instance.OnSignInFailed -= OnSignInFailed;
            }
        }
        
        private void OnSignInClicked()
        {
            if (GPGSManager.Instance != null)
                GPGSManager.Instance.SignIn();
        }
        
        private void OnSignOutClicked()
        {
            if (GPGSManager.Instance != null)
                GPGSManager.Instance.SignOut();
        }
        
        private void OnAchievementsClicked()
        {
            if (GPGSManager.Instance != null)
                GPGSManager.Instance.ShowAchievements();
        }
        
        private void OnLeaderboardClicked()
        {
            if (GPGSManager.Instance != null)
                GPGSManager.Instance.ShowLeaderboard();
        }
        
        private void OnAuthenticationChanged(bool isAuthenticated)
        {
            UpdateUI(isAuthenticated);
        }
        
        private void OnSignInSuccess()
        {
            Debug.Log("GPGS 로그인 성공!");
            UpdateUserInfo();
        }
        
        private void OnSignInFailed(string errorMessage)
        {
            Debug.LogWarning($"GPGS 로그인 실패: {errorMessage}");
        }
        
        private void UpdateUI(bool isAuthenticated)
        {
            if (signInButton != null)
                signInButton.gameObject.SetActive(!isAuthenticated);
                
            if (signOutButton != null)
                signOutButton.gameObject.SetActive(isAuthenticated);
                
            if (achievementsButton != null)
                achievementsButton.gameObject.SetActive(isAuthenticated);
                
            if (leaderboardButton != null)
                leaderboardButton.gameObject.SetActive(isAuthenticated);
                
            if (isAuthenticated)
            {
                UpdateUserInfo();
            }
            else
            {
                if (userInfoText != null)
                    userInfoText.text = "로그인되지 않음";
            }
        }
        
        private void UpdateUserInfo()
        {
            if (userInfoText != null && GPGSManager.Instance.IsAuthenticated)
            {
                string displayName = GPGSManager.Instance.GetUserDisplayName();
                string email = GPGSManager.Instance.GetUserEmail();
                
                userInfoText.text = $"사용자: {displayName}\n이메일: {email}";
            }
        }
        
        // 게임 내 업적 해금 메서드들
        public void UnlockFirstGirlAchievement()
        {
            if (GPGSManager.Instance != null)
                GPGSManager.Instance.UnlockAchievement("CgkI8JqQ8-4YEAIQAQ");
        }
        
        public void UnlockEvolutionAchievement()
        {
            if (GPGSManager.Instance != null)
                GPGSManager.Instance.UnlockAchievement("CgkI8JqQ8-4YEAIQAg");
        }
        
        public void UnlockGoldCollectorAchievement()
        {
            if (GPGSManager.Instance != null)
                GPGSManager.Instance.UnlockAchievement("CgkI8JqQ8-4YEAIQAw");
        }
        
        // 점수 제출 메서드들
        public void SubmitGoldScore(long goldAmount)
        {
            if (GPGSManager.Instance != null)
                GPGSManager.Instance.SubmitScore("CgkI8JqQ8-4YEAIQBA", goldAmount);
        }
        
        public void SubmitGirlsCountScore(int girlsCount)
        {
            if (GPGSManager.Instance != null)
                GPGSManager.Instance.SubmitScore("CgkI8JqQ8-4YEAIQBQ", girlsCount);
        }
        
        // 업적 진행도 업데이트
        public void UpdateEvolutionProgress(int currentEvolutions, int totalEvolutions)
        {
            if (GPGSManager.Instance != null)
                GPGSManager.Instance.IncrementAchievement("CgkI8JqQ8-4YEAIQAg", currentEvolutions, totalEvolutions);
        }
    }
} */