using System;
using UnityEngine;

/// <summary>
/// 이메일 보내기 유틸리티
/// 기본 이메일 클라이언트를 열어서 메일을 보낼 수 있도록 합니다.
/// </summary>
public static class EmailSender
{
    /// <summary>
    /// 이메일 클라이언트를 열어서 메일을 보냅니다.
    /// </summary>
    /// <param name="to">받는 사람 이메일 주소</param>
    /// <param name="subject">메일 제목</param>
    /// <param name="body">메일 본문</param>
    public static void SendEmail(string to, string subject, string body)
    {
        string email = $"mailto:{to}?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(body)}";
        
        // 플랫폼별 처리
#if UNITY_EDITOR
        // 에디터에서는 로그만 출력
        Debug.Log($"[EmailSender] 이메일 보내기:\n받는 사람: {to}\n제목: {subject}\n본문: {body}");
        Debug.Log($"[EmailSender] 실제 빌드에서는 {email} 링크가 열립니다.");
#elif UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
        // PC 플랫폼: mailto: 프로토콜 사용
        Application.OpenURL(email);
#elif UNITY_ANDROID
        // 안드로이드: Intent 사용
        AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent");
        AndroidJavaObject intentObject = new AndroidJavaObject("android.content.Intent", intentClass.GetStatic<string>("ACTION_SENDTO"));
        intentObject.Call<AndroidJavaObject>("setType", "text/plain");
        intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_EMAIL"), new string[] { to });
        intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_SUBJECT"), subject);
        intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), body);
        intentObject.Call<AndroidJavaObject>("setData", new AndroidJavaClass("android.net.Uri").CallStatic<AndroidJavaObject>("parse", $"mailto:{to}"));
        
        AndroidJavaClass unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject currentActivity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        currentActivity.Call("startActivity", intentObject);
#elif UNITY_IOS
        // iOS: mailto: 프로토콜 사용
        Application.OpenURL(email);
#else
        // 기타 플랫폼: mailto: 프로토콜 사용
        Application.OpenURL(email);
#endif
    }
    
    /// <summary>
    /// 기본 설정으로 이메일을 보냅니다.
    /// </summary>
    /// <param name="subject">메일 제목</param>
    /// <param name="body">메일 본문</param>
    public static void SendEmailToDefault(string subject, string body)
    {
        // 기본 이메일 주소 설정 (인스펙터에서 설정 가능하도록 하려면 매개변수로 받기)
        string defaultEmail = "your-email@example.com"; // TODO: 설정에서 가져오기
        SendEmail(defaultEmail, subject, body);
    }
}

