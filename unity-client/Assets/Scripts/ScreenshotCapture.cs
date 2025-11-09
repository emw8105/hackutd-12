using UnityEngine;
using System.IO;
using System;

public class ScreenshotCapture : MonoBehaviour
{
    [Header("Screenshot Settings")]
    [Tooltip("Folder name to save screenshots (relative to project root)")]
    public string screenshotFolder = "Screenshots";

    [Tooltip("Screenshot filename prefix")]
    public string filenamePrefix = "Screenshot";

    [Tooltip("Enable debug logging")]
    public bool debugMode = true;

    [Header("Screenshot Quality")]
    [Tooltip("Super-sampling multiplier (1 = native resolution, 2 = 2x resolution, etc.)")]
    [Range(1, 4)]
    public int superSize = 1;

    private string screenshotPath;

    void Start()
    {
        // Create screenshots folder if it doesn't exist
        screenshotPath = Path.Combine(Application.dataPath, "..", screenshotFolder);

        if (!Directory.Exists(screenshotPath))
        {
            Directory.CreateDirectory(screenshotPath);
            if (debugMode)
                Debug.Log($"[ScreenshotCapture] Created screenshot folder: {screenshotPath}");
        }
    }

    /// <summary>
    /// Capture a screenshot with optional metadata in filename
    /// </summary>
    public void CaptureScreenshot(string metadata = "")
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string metadataSuffix = string.IsNullOrEmpty(metadata) ? "" : $"_{metadata}";
        string filename = $"{filenamePrefix}_{timestamp}{metadataSuffix}.png";
        string fullPath = Path.Combine(screenshotPath, filename);

        // Capture screenshot
        ScreenCapture.CaptureScreenshot(fullPath, superSize);

        if (debugMode)
        {
            Debug.Log("═══════════════════════════════════════════════════");
            Debug.Log($"<color=cyan>[ScreenshotCapture] 📸 SCREENSHOT CAPTURED</color>");
            Debug.Log($"<color=yellow>  File: {filename}</color>");
            Debug.Log($"<color=yellow>  Path: {fullPath}</color>");
            Debug.Log($"  Resolution: {Screen.width * superSize} x {Screen.height * superSize}");
            Debug.Log("═══════════════════════════════════════════════════");
        }
    }

    /// <summary>
    /// Capture screenshot for a specific server
    /// </summary>
    public void CaptureServerScreenshot(string serverId)
    {
        string sanitizedId = serverId.Replace("-", "_").Replace(" ", "_");
        CaptureScreenshot($"Server_{sanitizedId}");
    }

    /// <summary>
    /// Get the full path to the screenshots folder
    /// </summary>
    public string GetScreenshotFolder()
    {
        return screenshotPath;
    }

    /// <summary>
    /// Open the screenshots folder in file explorer
    /// </summary>
    [ContextMenu("Open Screenshots Folder")]
    public void OpenScreenshotsFolder()
    {
        if (Directory.Exists(screenshotPath))
        {
            System.Diagnostics.Process.Start(screenshotPath);
            Debug.Log($"[ScreenshotCapture] Opening folder: {screenshotPath}");
        }
        else
        {
            Debug.LogWarning($"[ScreenshotCapture] Screenshots folder doesn't exist: {screenshotPath}");
        }
    }

    /// <summary>
    /// Manual screenshot trigger (testing)
    /// </summary>
    [ContextMenu("Take Screenshot Now")]
    public void TakeScreenshotNow()
    {
        CaptureScreenshot("Manual");
    }
}