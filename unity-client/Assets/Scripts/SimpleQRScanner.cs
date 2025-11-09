using UnityEngine;
using UnityEngine.Events;
using System;

public class SimpleQRScanner : MonoBehaviour
{
    [Header("Auto-Scan Simulation")]
    [Tooltip("Server IDs available for testing - simulates looking at different servers")]
    public string[] testServerIds = { "1-01-2-03", "1-01-2-04" };

    [Tooltip("Currently selected server index (simulates which server you're looking at)")]
    public int selectedServerIndex = 0;

    [Header("Scanning Settings")]
    [Tooltip("Enable debug logging")]
    public bool debugMode = true;

    [Tooltip("Cooldown between scans (seconds)")]
    public float scanCooldown = 2f;

    [Tooltip("Auto-scan when activated (simulates instant camera scan)")]
    public bool autoScanOnActivation = true;

    [Header("Visual Feedback")]
    [Tooltip("UI Panel to show when scanning is active")]
    public GameObject scanningPanel;

    [Tooltip("Optional target reticle/crosshair")]
    public GameObject scanReticle;

    // Events
    [Serializable]
    public class QRCodeEvent : UnityEvent<string> { }

    [Header("Events")]
    public QRCodeEvent OnQRCodeScanned;

    // Private variables
    private bool isScanning = false;
    private float lastScanTime = 0f;
    private string lastScannedCode = "";

    void Start()
    {
        if (scanningPanel != null)
            scanningPanel.SetActive(false);
        if (scanReticle != null)
            scanReticle.SetActive(false);

        if (debugMode)
        {
            Debug.Log("═══════════════════════════════════════════════════");
            Debug.Log("[SimpleQRScanner] QR Scanner Ready (AUTO-SCAN MODE)");
            Debug.Log("WORKFLOW:");
            Debug.Log("  1. Make peace sign gesture ✌️");
            Debug.Log("  2. Scanner auto-scans immediately!");
            Debug.Log("\nTESTING CONTROLS (Editor only):");
            Debug.Log("  [/]      = Cycle which server you're 'looking at'");
            Debug.Log($"\nAvailable Servers: {string.Join(", ", testServerIds)}");
            Debug.Log($"Currently looking at: {(testServerIds.Length > 0 ? testServerIds[selectedServerIndex] : "none")}");
            Debug.Log("═══════════════════════════════════════════════════");
        }
    }

    void Update()
    {
        if (!isScanning)
            return;

        // Testing controls - cycle through servers (simulates looking at different servers)
        HandleTestingControls();
    }

    /// <summary>
    /// Handle testing controls for cycling through servers (Editor only)
    /// </summary>
    private void HandleTestingControls()
    {
        // [ and ] = Cycle through servers (simulates turning to look at different servers)
        if (Input.GetKeyDown(KeyCode.LeftBracket))
        {
            selectedServerIndex = (selectedServerIndex - 1 + testServerIds.Length) % testServerIds.Length;
            if (debugMode)
                Debug.Log($"<color=cyan>[SimpleQRScanner] Now looking at: {testServerIds[selectedServerIndex]}</color>");
        }
        if (Input.GetKeyDown(KeyCode.RightBracket))
        {
            selectedServerIndex = (selectedServerIndex + 1) % testServerIds.Length;
            if (debugMode)
                Debug.Log($"<color=cyan>[SimpleQRScanner] Now looking at: {testServerIds[selectedServerIndex]}</color>");
        }
    }

    /// <summary>
    /// Simulate scanning a QR code with the given data
    /// </summary>
    public void SimulateScan(string qrData)
    {
        if (string.IsNullOrEmpty(qrData))
        {
            Debug.LogWarning("[SimpleQRScanner] Cannot scan empty data");
            return;
        }

        // Check cooldown
        if (Time.time - lastScanTime < scanCooldown)
        {
            if (debugMode)
                Debug.Log($"[SimpleQRScanner] Cooldown active ({scanCooldown - (Time.time - lastScanTime):F1}s remaining)");
            return;
        }

        // Check duplicate
        if (qrData == lastScannedCode && Time.time - lastScanTime < scanCooldown * 2)
        {
            if (debugMode)
                Debug.Log("[SimpleQRScanner] Duplicate scan ignored");
            return;
        }

        lastScannedCode = qrData;
        lastScanTime = Time.time;

        if (debugMode)
        {
            Debug.Log("═══════════════════════════════════════════════════");
            Debug.Log($"<color=green>[SimpleQRScanner] ✓ QR CODE SCANNED</color>");
            Debug.Log($"<color=cyan>  Server ID: {qrData}</color>");
            Debug.Log("═══════════════════════════════════════════════════");
        }

        // Trigger event
        OnQRCodeScanned?.Invoke(qrData);
    }

    /// <summary>
    /// Start scanning for QR codes
    /// </summary>
    public void StartScanning()
    {
        if (isScanning)
        {
            if (debugMode)
                Debug.Log("[SimpleQRScanner] Already scanning");
            return;
        }

        isScanning = true;

        if (scanningPanel != null)
            scanningPanel.SetActive(true);
        if (scanReticle != null)
            scanReticle.SetActive(true);

        if (debugMode)
        {
            Debug.Log("═══════════════════════════════════════════════════");
            Debug.Log("<color=yellow>[SimpleQRScanner] SCANNER ACTIVATED ✌️</color>");
            Debug.Log($"Looking at: {(testServerIds.Length > 0 ? testServerIds[selectedServerIndex] : "none")}");
            Debug.Log("═══════════════════════════════════════════════════");
        }

        // Auto-scan immediately when activated (simulates instant camera scan)
        if (autoScanOnActivation && testServerIds.Length > 0 && selectedServerIndex < testServerIds.Length)
        {
            // Small delay to simulate camera processing
            Invoke("PerformAutoScan", 0.3f);
        }
    }

    /// <summary>
    /// Perform the automatic scan when activated
    /// </summary>
    private void PerformAutoScan()
    {
        if (isScanning && testServerIds.Length > 0 && selectedServerIndex < testServerIds.Length)
        {
            SimulateScan(testServerIds[selectedServerIndex]);

            // Auto-stop after scanning (peace sign gesture toggles, so it will turn off)
            Invoke("StopScanning", 0.5f);
        }
    }

    /// <summary>
    /// Stop scanning for QR codes
    /// </summary>
    public void StopScanning()
    {
        if (!isScanning)
            return;

        isScanning = false;

        if (scanningPanel != null)
            scanningPanel.SetActive(false);
        if (scanReticle != null)
            scanReticle.SetActive(false);

        if (debugMode)
            Debug.Log("[SimpleQRScanner] Scanner stopped");
    }

    /// <summary>
    /// Toggle scanning on/off
    /// </summary>
    public void ToggleScanning()
    {
        if (isScanning)
            StopScanning();
        else
            StartScanning();
    }

    /// <summary>
    /// Get the current scanning state
    /// </summary>
    public bool IsScanning()
    {
        return isScanning;
    }

    // Context menu commands for testing in Editor
    [ContextMenu("Start Scanning")]
    private void StartScanningMenu()
    {
        StartScanning();
    }

    [ContextMenu("Stop Scanning")]
    private void StopScanningMenu()
    {
        StopScanning();
    }

    [ContextMenu("Simulate Scan - Server A")]
    private void ScanServerA()
    {
        SimulateScan("rack-a-001");
    }

    [ContextMenu("Simulate Scan - Server B")]
    private void ScanServerB()
    {
        SimulateScan("rack-b-002");
    }
}
