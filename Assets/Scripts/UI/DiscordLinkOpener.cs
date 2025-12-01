using UnityEngine;

/// <summary>
/// 디스코드 링크 열기 유틸리티
/// 디스코드 초대 링크를 자동으로 엽니다.
/// </summary>
public static class DiscordLinkOpener
{
    /// <summary>
    /// 디스코드 초대 링크를 엽니다.
    /// </summary>
    /// <param name="inviteCode">디스코드 초대 코드 (예: "abc123") 또는 전체 URL</param>
    public static void OpenDiscordInvite(string inviteCode)
    {
        string url;
        
        // 전체 URL인지 확인
        if (inviteCode.StartsWith("http://") || inviteCode.StartsWith("https://"))
        {
            url = inviteCode;
        }
        else if (inviteCode.StartsWith("discord.gg/") || inviteCode.StartsWith("discord.com/invite/"))
        {
            url = $"https://{inviteCode}";
        }
        else
        {
            // 초대 코드만 있는 경우
            url = $"https://discord.gg/{inviteCode}";
        }
        
        // 플랫폼별 처리
#if UNITY_EDITOR
        // 에디터에서는 로그만 출력
        Debug.Log($"[DiscordLinkOpener] 디스코드 링크 열기: {url}");
        Debug.Log("[DiscordLinkOpener] 실제 빌드에서는 브라우저나 디스코드 앱이 열립니다.");
#elif UNITY_ANDROID
        // 안드로이드: Intent 사용하여 디스코드 앱 또는 브라우저 열기
        try
        {
            // 디스코드 앱이 설치되어 있으면 앱으로 열기 시도
            AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent");
            AndroidJavaObject intentObject = new AndroidJavaObject("android.content.Intent", intentClass.GetStatic<string>("ACTION_VIEW"));
            
            AndroidJavaClass uriClass = new AndroidJavaClass("android.net.Uri");
            AndroidJavaObject uriObject = uriClass.CallStatic<AndroidJavaObject>("parse", url);
            intentObject.Call<AndroidJavaObject>("setData", uriObject);
            
            // 디스코드 패키지명으로 앱 열기 시도
            intentObject.Call<AndroidJavaObject>("setPackage", "com.discord");
            
            AndroidJavaClass unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unity.GetStatic<AndroidJavaObject>("currentActivity");
            
            try
            {
                currentActivity.Call("startActivity", intentObject);
            }
            catch
            {
                // 디스코드 앱이 없으면 패키지 지정 없이 브라우저로 열기
                intentObject.Call<AndroidJavaObject>("setPackage", null);
                currentActivity.Call("startActivity", intentObject);
            }
        }
        catch
        {
            // 실패하면 일반 URL 열기
            Application.OpenURL(url);
        }
#elif UNITY_IOS
        // iOS: URL Scheme 사용
        // 디스코드 앱이 설치되어 있으면 앱으로, 없으면 Safari로 열림
        Application.OpenURL(url);
#else
        // PC 및 기타 플랫폼: 브라우저로 열기
        Application.OpenURL(url);
#endif
    }
    
    /// <summary>
    /// 기본 디스코드 초대 링크를 엽니다.
    /// </summary>
    public static void OpenDefaultDiscordInvite()
    {
        // 기본 초대 코드 설정 (인스펙터에서 설정 가능하도록 하려면 매개변수로 받기)
        string defaultInviteCode = "your-invite-code"; // TODO: 설정에서 가져오기
        OpenDiscordInvite(defaultInviteCode);
    }
}


