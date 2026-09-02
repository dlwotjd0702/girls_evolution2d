using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class StoreListingCapture
{
    private const string OutputFolder = "Docs/StoreListing/2026-08-31/fresh";

    [MenuItem("Tools/Store Listing/Capture Game View %#g")]
    public static void CaptureGameView()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("[StoreListingCapture] Enter Play Mode before capturing.");
            return;
        }

        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
        {
            Debug.LogError("[StoreListingCapture] Could not resolve the project root.");
            return;
        }

        string outputDirectory = Path.Combine(projectRoot, OutputFolder);
        Directory.CreateDirectory(outputDirectory);

        int index = 1;
        string outputPath;
        do
        {
            outputPath = Path.Combine(outputDirectory, $"{index:00}-capture.png");
            index++;
        }
        while (File.Exists(outputPath));

        ScreenCapture.CaptureScreenshot(outputPath, 1);
        Debug.Log($"[StoreListingCapture] Captured: {outputPath}");
    }
}
