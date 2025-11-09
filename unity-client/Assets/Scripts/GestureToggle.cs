using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GestureToggle : MonoBehaviour
{
    [Header("Hand Data Sources")]
    public OVRHand leftHandData;
    public OVRHand rightHandData;

    [Header("Target Objects")]
    [Tooltip("Target object for the rock-and-roll gesture (right hand)")]
    public GameObject rockAndRollTarget;
    [Tooltip("Target object for the peace sign gesture (right hand)")]
    public GameObject peaceSignTarget;

    [Header("QR Code Scanning")]
    [Tooltip("Simple QR Code scanner component (uses keyboard for testing)")]
    public SimpleQRScanner simpleQrScanner;
    [Tooltip("Enable QR scanning when peace sign modal opens")]
    public bool enableQRScanOnPeaceSign = true;

    [Header("Gesture Settings")]
    [Tooltip("How long the gesture must be held before triggering (in seconds)")]
    public float gestureHoldTime = 0.1f;
    [Tooltip("Cooldown time after a gesture is triggered (in seconds)")]
    public float gestureCooldown = 1.0f;
    [Tooltip("Enable debug logging to see gesture detection values")]
    public bool debugMode = false;

    private bool rockGestureTriggered = false;
    private bool peaceGestureTriggered = false;
    private float rockGestureHoldTimer = 0f;
    private float peaceGestureHoldTimer = 0f;
    private float rockCooldownTimer = 0f;
    private float peaceCooldownTimer = 0f;
    private string currentScannedServerId = null;

    void Start()
    {
        // Subscribe to QR code scan events
        if (simpleQrScanner != null)
        {
            simpleQrScanner.OnQRCodeScanned.AddListener(OnQRCodeDetected);
        }

        // Ensure cubes are visible at start with neutral color
        if (rockAndRollTarget != null)
        {
            rockAndRollTarget.SetActive(true);
            SetCubeColor(rockAndRollTarget, Color.gray); // Neutral gray
        }

        if (peaceSignTarget != null)
        {
            peaceSignTarget.SetActive(true);
            SetCubeColor(peaceSignTarget, Color.gray); // Neutral gray
        }
    }

    void Update()
    {
        if (rightHandData == null)
        {
            Debug.LogWarning("Right Hand Data Source not assigned!");
            return;
        }

        // Update cooldown timers
        if (rockCooldownTimer > 0f)
            rockCooldownTimer -= Time.deltaTime;
        if (peaceCooldownTimer > 0f)
            peaceCooldownTimer -= Time.deltaTime;

        // Check for rock-and-roll gesture (index + pinky up)
        if (rightHandData.IsTracked && rockCooldownTimer <= 0f && DetectRockAndRoll(rightHandData))
        {
            rockGestureHoldTimer += Time.deltaTime;

            if (rockGestureHoldTimer >= gestureHoldTime && !rockGestureTriggered)
            {
                rockGestureTriggered = true;
                ToggleRockAndRollTarget();
                rockCooldownTimer = gestureCooldown;
                Debug.Log($"[GestureToggle] Rock-and-roll gesture held for {rockGestureHoldTimer:F2}s - Triggered!");
            }
        }
        else
        {
            rockGestureHoldTimer = 0f;
            rockGestureTriggered = false;
        }

        // Check for peace sign gesture (index + middle up)
        if (rightHandData.IsTracked && peaceCooldownTimer <= 0f && DetectPeaceSign(rightHandData))
        {
            peaceGestureHoldTimer += Time.deltaTime;

            if (peaceGestureHoldTimer >= gestureHoldTime && !peaceGestureTriggered)
            {
                peaceGestureTriggered = true;
                TogglePeaceSignTarget();
                peaceCooldownTimer = gestureCooldown;
                Debug.Log($"[GestureToggle] Peace sign gesture held for {peaceGestureHoldTimer:F2}s - Triggered!");
            }
        }
        else
        {
            peaceGestureHoldTimer = 0f;
            peaceGestureTriggered = false;
        }
    }

    private bool DetectRockAndRoll(OVRHand hand)
    {
        // Rock and roll: Index and pinky extended, middle and ring curled
        float indexStrength = hand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
        float middleStrength = hand.GetFingerPinchStrength(OVRHand.HandFinger.Middle);
        float ringStrength = hand.GetFingerPinchStrength(OVRHand.HandFinger.Ring);
        float pinkyStrength = hand.GetFingerPinchStrength(OVRHand.HandFinger.Pinky);

        // Index and pinky should be extended (low pinch strength)
        bool indexExtended = indexStrength < 0.5f;
        bool pinkyExtended = pinkyStrength < 0.5f;

        // Middle and ring should be curled (high pinch strength)
        bool middleCurled = middleStrength > 0.5f;
        bool ringCurled = ringStrength > 0.5f;

        bool gestureDetected = indexExtended && pinkyExtended && middleCurled && ringCurled;

        // Debug output
        if (debugMode && Time.frameCount % 30 == 0)
        {
            Debug.Log($"[GestureToggle] Index:{indexStrength:F2}({indexExtended}) Middle:{middleStrength:F2}({middleCurled}) " +
                     $"Ring:{ringStrength:F2}({ringCurled}) Pinky:{pinkyStrength:F2}({pinkyExtended}) | Result:{gestureDetected}");
        }

        return gestureDetected;
    }

    private bool DetectPeaceSign(OVRHand hand)
    {
        // Peace sign: Index and middle extended, ring and pinky curled
        float indexStrength = hand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
        float middleStrength = hand.GetFingerPinchStrength(OVRHand.HandFinger.Middle);
        float ringStrength = hand.GetFingerPinchStrength(OVRHand.HandFinger.Ring);
        float pinkyStrength = hand.GetFingerPinchStrength(OVRHand.HandFinger.Pinky);

        // Index and middle should be extended (low pinch strength)
        bool indexExtended = indexStrength < 0.6f;
        bool middleExtended = middleStrength < 0.6f;

        // Ring and pinky should be curled (high pinch strength)
        bool ringCurled = ringStrength > 0.4f;
        bool pinkyCurled = pinkyStrength > 0.4f;

        bool gestureDetected = indexExtended && middleExtended && ringCurled && pinkyCurled;

        // Debug output
        if (debugMode && Time.frameCount % 30 == 0)
        {
            Debug.Log($"[GestureToggle PEACE] Index:{indexStrength:F2}({indexExtended}) Middle:{middleStrength:F2}({middleExtended}) " +
                     $"Ring:{ringStrength:F2}({ringCurled}) Pinky:{pinkyStrength:F2}({pinkyCurled}) | Result:{gestureDetected}");
        }

        return gestureDetected;
    }

    private void ToggleRockAndRollTarget()
    {
        Debug.Log("[GestureToggle] Rock-and-roll gesture triggered - Fetching tickets...");
        FetchAndDisplayTickets();
    }

    private void TogglePeaceSignTarget()
    {
        Debug.Log("[GestureToggle] Peace sign gesture triggered - Starting QR scan...");

        if (enableQRScanOnPeaceSign && simpleQrScanner != null)
        {
            Debug.Log("[GestureToggle] Starting QR scanner for server detection...");
            simpleQrScanner.StartScanning();
        }
        else
        {
            // Fallback: Show all servers if QR scanner not available
            FetchAndDisplayServers();
        }
    }

    /// <summary>
    /// Called when a QR code is detected by the scanner
    /// </summary>
    private void OnQRCodeDetected(string qrData)
    {
        Debug.Log($"[GestureToggle] QR Code scanned: {qrData}");

        string serverId = ParseServerIdFromQR(qrData);

        if (!string.IsNullOrEmpty(serverId))
        {
            currentScannedServerId = serverId;
            FetchAndDisplaySpecificServer(serverId);
        }
        else
        {
            Debug.LogWarning($"[GestureToggle] Could not parse server ID from QR code: {qrData}");
        }
    }

    /// <summary>
    /// Parse server ID from QR code data
    /// Supports plain text (server ID directly) or JSON format
    /// </summary>
    private string ParseServerIdFromQR(string qrData)
    {
        if (string.IsNullOrEmpty(qrData))
            return null;

        // Try parsing as JSON first
        try
        {
            // Simple JSON parsing for {"id":"rack-a-001"} format
            if (qrData.Contains("{") && qrData.Contains("\"id\""))
            {
                var jsonData = JsonUtility.FromJson<QRServerData>(qrData);
                if (!string.IsNullOrEmpty(jsonData.id))
                    return jsonData.id;
            }
        }
        catch
        {
            // Not JSON, continue to plain text parsing
        }

        // Assume plain text is the server ID
        return qrData.Trim();
    }

    [System.Serializable]
    private class QRServerData
    {
        public string id;
        public string type;
    }

    /// <summary>
    /// Fetch and display information for a specific server by ID
    /// </summary>
    private void FetchAndDisplaySpecificServer(string serverId)
    {
        Debug.Log($"[GestureToggle] Fetching data for server: {serverId}");

        ServerAPIClient.Instance.GetServer(serverId,
            onSuccess: (server) =>
            {
                Debug.Log($"[GestureToggle] ✓ SUCCESS: Server found: {server.id} - {server.name}");
                Debug.Log($"  Location: ({server.location.x}, {server.location.y}, {server.location.z})");

                // Update the display with this specific server
                UpdatePeaceSignDisplayWithServer(server);

                // Optionally fetch tickets for this server
                FetchTicketsForServer(serverId);
            },
            onError: (error) =>
            {
                Debug.LogError($"[GestureToggle] ✗ FAILED to fetch server {serverId}: {error}");
                // Set cube to RED on error
                SetCubeColor(peaceSignTarget, Color.red);
            }
        );
    }

    /// <summary>
    /// Fetch tickets associated with a specific server
    /// </summary>
    private void FetchTicketsForServer(string serverId)
    {
        ServerAPIClient.Instance.GetTicketsByServer(serverId,
            onSuccess: (response) =>
            {
                Debug.Log($"[GestureToggle] Found {response.count} ticket(s) for server {serverId}");

                foreach (var ticket in response.tickets)
                {
                    Debug.Log($"  - {ticket.key}: {ticket.summary} ({ticket.status})");
                }

                // Update UI with ticket information
                UpdatePeaceSignDisplayWithTickets(response);
            },
            onError: (error) =>
            {
                Debug.LogWarning($"[GestureToggle] No tickets found for server {serverId}: {error}");
            }
        );
    }

    // Example: Fetch all tickets when rock-and-roll gesture is triggered
    private void FetchAndDisplayTickets()
    {
        ServerAPIClient.Instance.GetAllTickets(
            onSuccess: (response) =>
            {
                Debug.Log($"[GestureToggle] ✓ SUCCESS: Received {response.count} tickets from server");

                // Example: Update the display with ticket information
                foreach (var ticket in response.tickets)
                {
                    Debug.Log($"Ticket: {ticket.key} - {ticket.summary} - Status: {ticket.status}");
                }

                // Update cube to GREEN on success
                UpdateRockAndRollDisplay(response);
            },
            onError: (error) =>
            {
                Debug.LogError($"[GestureToggle] ✗ FAILED to fetch tickets: {error}");
                // Set cube to RED on error
                SetCubeColor(rockAndRollTarget, Color.red);
            }
        );
    }

    // Example: Fetch all servers when peace sign gesture is triggered
    private void FetchAndDisplayServers()
    {
        ServerAPIClient.Instance.GetAllServers(
            onSuccess: (response) =>
            {
                Debug.Log($"[GestureToggle] ✓ SUCCESS: Received {response.count} servers from server");

                foreach (var server in response.servers)
                {
                    Debug.Log($"Server: {server.id} - {server.name} at ({server.location.x}, {server.location.y}, {server.location.z})");
                }

                // Update cube to GREEN on success
                UpdatePeaceSignDisplay(response);
            },
            onError: (error) =>
            {
                Debug.LogError($"[GestureToggle] ✗ FAILED to fetch servers: {error}");
                // Set cube to RED on error
                SetCubeColor(peaceSignTarget, Color.red);
            }
        );
    }

    // Placeholder methods - customize these to update your displays
    private void UpdateRockAndRollDisplay(ServerAPIClient.JiraTicketListResponse ticketData)
    {
        // Set cube to GREEN on successful API call
        SetCubeColor(rockAndRollTarget, Color.green);

        Debug.Log($"[GestureToggle] Rock-and-roll cube set to GREEN - {ticketData.count} tickets loaded");
    }

    private void UpdatePeaceSignDisplay(ServerAPIClient.ServerListResponse serverData)
    {
        // Set cube to GREEN on successful API call
        SetCubeColor(peaceSignTarget, Color.green);

        Debug.Log($"[GestureToggle] Peace sign cube set to GREEN - {serverData.count} servers loaded");
    }

    /// <summary>
    /// Update display with specific server information from QR scan
    /// </summary>
    private void UpdatePeaceSignDisplayWithServer(ServerAPIClient.Server server)
    {
        // Set cube to GREEN on successful server fetch
        SetCubeColor(peaceSignTarget, Color.green);

        Debug.Log($"[GestureToggle] Peace sign cube set to GREEN - Server {server.id} loaded");
    }

    /// <summary>
    /// Update display with tickets for the scanned server
    /// </summary>
    private void UpdatePeaceSignDisplayWithTickets(ServerAPIClient.JiraTicketListResponse ticketData)
    {
        // Cube is already green from server fetch
        // Could add additional visual feedback based on ticket count

        Debug.Log($"[GestureToggle] Tickets loaded for server - Count: {ticketData.count}");
    }

    /// <summary>
    /// Helper method to set cube color
    /// </summary>
    private void SetCubeColor(GameObject target, Color color)
    {
        if (target == null)
            return;

        var renderer = target.GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            renderer.material.color = color;
        }
    }
}
