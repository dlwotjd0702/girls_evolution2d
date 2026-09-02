using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>Stops a non-development Android build when known test/release-critical values remain.</summary>
public sealed class AndroidReleasePreflight : IPreprocessBuildWithReport
{
    const string MobileAdsSettingsPath = "Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset";
    const string IngameScenePath = "Assets/Scenes/Ingame.unity";
    const string IntroScenePath = "Assets/Scenes/Intro.unity";
    const string GpgsSettingsPath = "ProjectSettings/GooglePlayGameSettings.txt";
    const string GoogleTestAppId = "ca-app-pub-3940256099942544~3347511713";
    const string GoogleTestRewardedId = "ca-app-pub-3940256099942544/5224354917";

    public int callbackOrder => -1000;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android) return;
        if ((report.summary.options & BuildOptions.Development) != 0) return;

        ValidateAdMobAppId();
        ValidateAndroidIdentity();
        ValidateAndroidRuntime();
        ValidateBuildScenes();
        ValidateReleaseSceneSettings();
        ValidateGooglePlayGamesSettings();
        if (!AndroidReleaseBuilder.IsVerificationBuild)
            ValidateSigningFile();
    }

    static void ValidateAdMobAppId()
    {
        var asset = AssetDatabase.LoadMainAssetAtPath(MobileAdsSettingsPath);
        if (!asset) throw new BuildFailedException($"Google Mobile Ads 설정을 찾을 수 없습니다: {MobileAdsSettingsPath}");

        var property = new SerializedObject(asset).FindProperty("adMobAndroidAppId");
        string appId = property?.stringValue?.Trim();
        if (string.IsNullOrEmpty(appId) || appId == GoogleTestAppId)
        {
            throw new BuildFailedException(
                "출시 빌드가 차단되었습니다. Google Mobile Ads Android App ID가 비어 있거나 공식 테스트 ID입니다. " +
                "Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset에 실제 AdMob 앱 ID를 입력하세요.");
        }
    }

    static void ValidateAndroidIdentity()
    {
        string identifier = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
        if (string.IsNullOrWhiteSpace(identifier) || identifier.Contains("example"))
            throw new BuildFailedException($"출시용 Android 패키지명이 유효하지 않습니다: '{identifier}'");

        if (PlayerSettings.Android.bundleVersionCode <= 0)
            throw new BuildFailedException("Android bundleVersionCode는 1 이상이어야 합니다.");
    }

    static void ValidateAndroidRuntime()
    {
        if (PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) != ScriptingImplementation.IL2CPP)
            throw new BuildFailedException("출시 Android 빌드는 IL2CPP를 사용해야 합니다.");

        if ((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) == 0)
            throw new BuildFailedException("Google Play 출시 빌드에는 ARM64 아키텍처가 포함되어야 합니다.");
    }

    static void ValidateBuildScenes()
    {
        var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();
        if (scenes.Length < 2 || scenes[0].path != IntroScenePath || scenes[1].path != IngameScenePath)
            throw new BuildFailedException("Build Settings의 활성 씬 순서는 Intro, Ingame이어야 합니다.");

        foreach (var scene in scenes)
            if (!File.Exists(scene.path))
                throw new BuildFailedException($"Build Settings 씬 파일을 찾을 수 없습니다: {scene.path}");
    }

    static void ValidateReleaseSceneSettings()
    {
        string scene = File.ReadAllText(IngameScenePath);
        if (Regex.IsMatch(scene, @"(?m)^\s*resetSaveOnStart:\s*1\s*$"))
            throw new BuildFailedException("출시 씬에서 resetSaveOnStart가 켜져 있어 사용자 저장 데이터가 삭제될 수 있습니다.");
        if (!Regex.IsMatch(scene, @"(?m)^\s*enableCloudSave:\s*1\s*$")
            || !Regex.IsMatch(scene, @"(?m)^\s*syncOnStart:\s*1\s*$"))
            throw new BuildFailedException("출시 씬의 클라우드 저장 또는 시작 동기화가 꺼져 있습니다.");

        var rewarded = Regex.Match(scene, @"(?m)^\s*rewardedAdUnitId:\s*(\S+)\s*$");
        if (!rewarded.Success || rewarded.Groups[1].Value == GoogleTestRewardedId)
            throw new BuildFailedException("출시 씬의 Android 보상형 광고 단위 ID가 비어 있거나 공식 테스트 ID입니다.");
    }

    static void ValidateGooglePlayGamesSettings()
    {
        if (!File.Exists(GpgsSettingsPath))
            throw new BuildFailedException($"Google Play Games 설정을 찾을 수 없습니다: {GpgsSettingsPath}");

        string settings = File.ReadAllText(GpgsSettingsPath);
        var appId = Regex.Match(settings, @"(?m)^proj\.AppId=(\d+)\s*$");
        var clientId = Regex.Match(settings, @"(?m)^and\.ClientId=(\d+)-.+\.apps\.googleusercontent\.com\s*$");
        if (!appId.Success || !clientId.Success || appId.Groups[1].Value != clientId.Groups[1].Value)
            throw new BuildFailedException("Google Play Games App ID와 Web Client ID 설정이 없거나 서로 일치하지 않습니다.");
    }

    static void ValidateSigningFile()
    {
        if (!PlayerSettings.Android.useCustomKeystore)
            throw new BuildFailedException("출시 Android 빌드에는 Custom Keystore를 사용해야 합니다.");

        string configured = PlayerSettings.Android.keystoreName;
        if (string.IsNullOrWhiteSpace(configured))
            throw new BuildFailedException("Android Keystore 경로가 비어 있습니다.");

        string path = configured.StartsWith("{inproject}: ")
            ? Path.Combine(Directory.GetCurrentDirectory(), configured.Substring("{inproject}: ".Length))
            : configured;
        if (!File.Exists(path))
            throw new BuildFailedException($"Android Keystore 파일을 찾을 수 없습니다: {path}");
    }
}
