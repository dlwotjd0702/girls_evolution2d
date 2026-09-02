using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class AndroidReleaseBuilder
{
    const string SummaryPath = "Logs/android-build-summary.txt";
    const string DevelopmentSummaryPath = "Logs/android-development-build-summary.txt";
    const string VerificationSummaryPath = "Logs/android-verification-build-summary.txt";
    const string NextReleaseVersionName = "3.1.5";
    const int NextReleaseVersionCode = 22;

    public static bool IsVerificationBuild { get; private set; }

    [MenuItem("Tools/Girls Evolution/Prepare 3.1.5 (Code 22) %#&u")]
    public static void PrepareNextReleaseVersion()
    {
        PlayerSettings.bundleVersion = NextReleaseVersionName;
        PlayerSettings.Android.bundleVersionCode = NextReleaseVersionCode;
        AssetDatabase.SaveAssets();
        Debug.Log($"[AndroidReleaseBuilder] Prepared Android release {NextReleaseVersionName} (code {NextReleaseVersionCode}).");
    }

    [MenuItem("Tools/Girls Evolution/Build Android Release AAB %&b")]
    public static void BuildReleaseAab()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new BuildFailedException("Android release build cannot start in Play Mode.");

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();
        if (scenes.Length == 0)
            throw new BuildFailedException("No enabled scenes are configured in Build Settings.");

        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string outputDirectory = Path.Combine(projectRoot, "Builds", "Android");
        Directory.CreateDirectory(outputDirectory);
        Directory.CreateDirectory(Path.Combine(projectRoot, "Logs"));

        string fileName = $"girls-evolution2d-v{PlayerSettings.bundleVersion}-code{PlayerSettings.Android.bundleVersionCode}.aab";
        string outputPath = Path.Combine(outputDirectory, fileName);
        EditorUserBuildSettings.buildAppBundle = true;

        BuildReport report = null;
        try
        {
            report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            });

            WriteSummary(projectRoot, outputPath, report.summary, null);
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"Android AAB build failed: {report.summary.result}");

            Debug.Log($"[AndroidReleaseBuilder] AAB build succeeded: {outputPath}");
        }
        catch (Exception exception)
        {
            WriteSummary(projectRoot, outputPath, report?.summary, exception);
            throw;
        }
    }

    [MenuItem("Tools/Girls Evolution/Build Android Development APK %&k")]
    public static void BuildDevelopmentApk()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new BuildFailedException("Android development build cannot start in Play Mode.");

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();
        if (scenes.Length == 0)
            throw new BuildFailedException("No enabled scenes are configured in Build Settings.");

        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string outputDirectory = Path.Combine(projectRoot, "Builds", "Android");
        Directory.CreateDirectory(outputDirectory);
        Directory.CreateDirectory(Path.Combine(projectRoot, "Logs"));
        string outputPath = Path.Combine(outputDirectory,
            $"girls-evolution2d-v{PlayerSettings.bundleVersion}-code{PlayerSettings.Android.bundleVersionCode}-development.apk");

        bool previousBuildAppBundle = EditorUserBuildSettings.buildAppBundle;
        bool previousUseCustomKeystore = PlayerSettings.Android.useCustomKeystore;
        BuildReport report = null;
        try
        {
            EditorUserBuildSettings.buildAppBundle = false;
            PlayerSettings.Android.useCustomKeystore = false;
            report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            });

            WriteSummary(projectRoot, DevelopmentSummaryPath, outputPath, report.summary, null);
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"Android development APK build failed: {report.summary.result}");

            Debug.Log($"[AndroidReleaseBuilder] Development APK build succeeded: {outputPath}");
        }
        catch (Exception exception)
        {
            WriteSummary(projectRoot, DevelopmentSummaryPath, outputPath, report?.summary, exception);
            throw;
        }
        finally
        {
            EditorUserBuildSettings.buildAppBundle = previousBuildAppBundle;
            PlayerSettings.Android.useCustomKeystore = previousUseCustomKeystore;
            AssetDatabase.SaveAssets();
        }
    }

    [MenuItem("Tools/Girls Evolution/Build Android Verification AAB %#&v")]
    public static void BuildVerificationAab()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new BuildFailedException("Android verification build cannot start in Play Mode.");

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();
        if (scenes.Length == 0)
            throw new BuildFailedException("No enabled scenes are configured in Build Settings.");

        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string outputDirectory = Path.Combine(projectRoot, "Builds", "Android");
        Directory.CreateDirectory(outputDirectory);
        Directory.CreateDirectory(Path.Combine(projectRoot, "Logs"));
        string outputPath = Path.Combine(outputDirectory,
            $"girls-evolution2d-v{PlayerSettings.bundleVersion}-code{PlayerSettings.Android.bundleVersionCode}-verification-debug-signed.aab");

        bool previousBuildAppBundle = EditorUserBuildSettings.buildAppBundle;
        bool previousUseCustomKeystore = PlayerSettings.Android.useCustomKeystore;
        BuildReport report = null;
        try
        {
            IsVerificationBuild = true;
            EditorUserBuildSettings.buildAppBundle = true;
            PlayerSettings.Android.useCustomKeystore = false;
            report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            });

            WriteSummary(projectRoot, VerificationSummaryPath, outputPath, report.summary, null);
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"Android verification AAB build failed: {report.summary.result}");

            Debug.Log($"[AndroidReleaseBuilder] Verification AAB build succeeded: {outputPath}");
        }
        catch (Exception exception)
        {
            WriteSummary(projectRoot, VerificationSummaryPath, outputPath, report?.summary, exception);
            throw;
        }
        finally
        {
            IsVerificationBuild = false;
            EditorUserBuildSettings.buildAppBundle = previousBuildAppBundle;
            PlayerSettings.Android.useCustomKeystore = previousUseCustomKeystore;
            AssetDatabase.SaveAssets();
        }
    }

    static void WriteSummary(string projectRoot, string outputPath, BuildSummary? summary, Exception exception)
    {
        WriteSummary(projectRoot, SummaryPath, outputPath, summary, exception);
    }

    static void WriteSummary(string projectRoot, string summaryPath, string outputPath, BuildSummary? summary, Exception exception)
    {
        string result = summary.HasValue ? summary.Value.result.ToString() : "Exception";
        string size = summary.HasValue ? summary.Value.totalSize.ToString() : "0";
        string duration = summary.HasValue ? summary.Value.totalTime.ToString() : "n/a";
        string errors = summary.HasValue ? summary.Value.totalErrors.ToString() : "n/a";
        string warnings = summary.HasValue ? summary.Value.totalWarnings.ToString() : "n/a";
        string details = exception == null ? "" : $"{Environment.NewLine}Exception: {exception}";
        File.WriteAllText(Path.Combine(projectRoot, summaryPath),
            $"Result: {result}{Environment.NewLine}" +
            $"Output: {outputPath}{Environment.NewLine}" +
            $"Size: {size}{Environment.NewLine}" +
            $"Duration: {duration}{Environment.NewLine}" +
            $"Errors: {errors}{Environment.NewLine}" +
            $"Warnings: {warnings}{details}{Environment.NewLine}");
    }
}
