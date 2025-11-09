using UnityEngine;
using UnityEngine.Events;
using System;

public class SimpleQRScanner : MonoBehaviour
{
    [Header("Raycast Detection")]
    [Tooltip("Camera to use for raycasting (usually Main Camera or Center Eye Anchor)")]
    public Camera scanCamera;

    [Tooltip("Maximum distance to detect QR codes")]
    public float maxScanDistance = 10f;

    [Header("Auto-Scan Simulation (Fallback)")]
    [Tooltip("Server IDs available for testing - simulates looking at different servers")]
    public string[] testServerIds = { "1-01-2-03", "1-01-2-04" };

    [Tooltip("Currently selected server index (simulates which server you're looking at)")]
    public int selectedServerIndex = 0;

    [Header("Scanning Settings")]
    [Tooltip("Enable debug logging")]
    public bool debugMode = true;

    [Tooltip("Cooldown between scans (seconds)")]
    public float scanCooldown = 2f;

    [Tooltip("Auto-scan when activated (uses raycast to detect QR code)")]
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
        // Auto-find camera if not assigned
        if (scanCamera == null)
        {
            scanCamera = Camera.main;
            if (scanCamera == null)
            {
                Debug.LogWarning("[SimpleQRScanner] No camera assigned and Camera.main not found. Will use fallback scanning.");
            }
        }

        if (scanningPanel != null)
            scanningPanel.SetActive(false);
        if (scanReticle != null)
            scanReticle.SetActive(false);

        if (debugMode)
        {
            Debug.Log("═══════════════════════════════════════════════════");
            Debug.Log("[SimpleQRScanner] QR Scanner Ready (RAYCAST MODE)");
            Debug.Log("WORKFLOW:");
            Debug.Log("  1. Look at a QR code");
            Debug.Log("  2. Make peace sign gesture ✌️");
            Debug.Log("  3. Scanner auto-scans what you're looking at!");
            Debug.Log("\nTESTING CONTROLS (Editor only):");
            Debug.Log("  [/]      = Cycle which server you're 'looking at' (fallback)");
            Debug.Log($"\nScan Camera: {(scanCamera != null ? scanCamera.name : "NOT ASSIGNED")}");
            Debug.Log($"Max Scan Distance: {maxScanDistance}m");
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
    /// Uses raycast to detect which QR code the user is looking at
    /// </summary>
    private void PerformAutoScan()
    {
        if (!isScanning)
            return;

        // Try raycast detection first
        string detectedServerId = DetectQRCodeWithRaycast();

        if (!string.IsNullOrEmpty(detectedServerId))
        {
            // Successfully detected a QR code with raycast
            SimulateScan(detectedServerId);
        }
        else
        {
            // Fallback to testServerIds if raycast didn't detect anything
            if (testServerIds.Length > 0 && selectedServerIndex < testServerIds.Length)
            {
                Debug.LogWarning("[SimpleQRScanner] No QR code detected with raycast, using fallback test server");
                SimulateScan(testServerIds[selectedServerIndex]);
            }
            else
            {
                Debug.LogWarning("[SimpleQRScanner] No QR code detected and no fallback servers available");
            }
        }

        // Auto-stop after scanning
        Invoke("StopScanning", 0.5f);
    }

    /// <summary>
    /// Use raycast to detect which QR code GameObject the camera is looking at
    /// </summary>
    private string DetectQRCodeWithRaycast()
    {
        if (scanCamera == null)
        {
            Debug.LogWarning("[SimpleQRScanner] No scan camera assigned for raycast detection");
            return null;
        }

        // Cast a ray from the center of the camera
        Ray ray = new Ray(scanCamera.transform.position, scanCamera.transform.forward);
        RaycastHit hit;

        if (debugMode)
        {
            Debug.Log($"[SimpleQRScanner] Raycasting from {scanCamera.name} position {scanCamera.transform.position} direction {scanCamera.transform.forward}");
            // Draw debug ray in scene view
            Debug.DrawRay(ray.origin, ray.direction * maxScanDistance, Color.green, 2f);
        }

        if (Physics.Raycast(ray, out hit, maxScanDistance))
        {
            if (debugMode)
            {
                Debug.Log($"[SimpleQRScanner] Raycast hit: {hit.collider.gameObject.name} at distance {hit.distance:F2}m");
            }

            // Check if the hit object is a QR code display
            QRCodeDisplay qrDisplay = hit.collider.GetComponent<QRCodeDisplay>();
            if (qrDisplay != null)
            {
                string serverId = qrDisplay.GetServerId();
                if (debugMode)
                {
                    Debug.Log($"<color=green>[SimpleQRScanner] ✓ Detected QR Code: {serverId}</color>");
                }
                return serverId;
            }
            else
            {
                if (debugMode)
                {
                    Debug.Log($"[SimpleQRScanner] Hit object '{hit.collider.gameObject.name}' has no QRCodeDisplay component");
                }
            }
        }
        else
        {
            if (debugMode)
            {
                Debug.Log("[SimpleQRScanner] Raycast didn't hit anything within range");
            }
        }

        return null;
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
        SimulateScan("1-01-2-03");
    }

    [ContextMenu("Simulate Scan - Server B")]
    private void ScanServerB()
    {
        SimulateScan("1-01-2-04");
    }
}
