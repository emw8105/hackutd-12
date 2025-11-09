using UnityEngine;

/// <summary>
/// Simple test script to verify server connection on scene start.
/// Attach this to any GameObject to test the API connection.
/// Remove after testing is complete.
/// </summary>
public class ServerConnectionTest : MonoBehaviour
{
    [Header("Test Settings")]
    [Tooltip("Automatically test connection when scene starts")]
    public bool testOnStart = true;

    [Tooltip("Test interval in seconds (0 = only test once)")]
    public float testInterval = 0f;

    private float nextTestTime = 0f;
    private bool hasTestedOnce = false;

    void Start()
    {
        if (testOnStart)
        {
            Debug.Log("[ServerTest] Starting connection test...");
            TestServerConnection();
        }
    }

    void Update()
    {
        // Periodic testing if interval is set
        if (testInterval > 0 && Time.time >= nextTestTime)
        {
            TestServerConnection();
            nextTestTime = Time.time + testInterval;
        }
    }

    /// <summary>
    /// Test connection by fetching servers from the API
    /// </summary>
    [ContextMenu("Test Server Connection")]
    public void TestServerConnection()
    {
        Debug.Log($"[ServerTest] Testing connection to: {ServerAPIClient.Instance.serverBaseUrl}");

        // Test 1: Get all servers
        ServerAPIClient.Instance.GetAllServers(
            onSuccess: (response) =>
            {
                Debug.Log($"<color=green>✓ SERVER CONNECTED!</color> Received {response.count} servers");

                if (response.servers != null)
                {
                    foreach (var server in response.servers)
                    {
                        Debug.Log($"  • Server: {server.id} ({server.name}) at position ({server.location.x}, {server.location.y}, {server.location.z})");
                    }
                }

                hasTestedOnce = true;
            },
            onError: (error) =>
            {
                Debug.LogError($"<color=red>✗ SERVER CONNECTION FAILED!</color>\n{error}");
                Debug.LogError("Make sure your FastAPI server is running at http://localhost:8000");
                Debug.LogError("Run: cd server && python -m uvicorn main:app --reload --host 127.0.0.1 --port 8000");
            }
        );
    }
}
