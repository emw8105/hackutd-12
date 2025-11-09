using UnityEngine;

/// <summary>
/// Displays a QR code texture on a GameObject (plane, cube face, etc.)
/// Attach this to any object you want to show a QR code on
/// </summary>
public class QRCodeDisplay : MonoBehaviour
{
    [Header("QR Code Settings")]
    [Tooltip("The QR code texture to display")]
    public Texture2D qrCodeTexture;

    [Tooltip("Server ID this QR code represents")]
    public string serverId;

    [Header("Display Settings")]
    [Tooltip("Material to apply the QR code to (leave empty to auto-create)")]
    public Material qrMaterial;

    [Tooltip("Scale of the QR code display")]
    public Vector3 displayScale = new Vector3(0.5f, 0.5f, 0.01f);

    void Start()
    {
        SetupQRCodeDisplay();
    }

    void SetupQRCodeDisplay()
    {
        // Get or create renderer
        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogError($"[QRCodeDisplay] No Renderer found on {gameObject.name}! Add a MeshRenderer component.");
            return;
        }

        // Create material with Unlit shader (no lighting needed, always visible)
        if (qrMaterial == null)
        {
            qrMaterial = new Material(Shader.Find("Unlit/Texture"));
        }

        if (qrCodeTexture != null)
        {
            qrMaterial.mainTexture = qrCodeTexture;
        }

        // Apply material
        renderer.material = qrMaterial;

        // Disable shadows (QR codes don't need them)
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        // Set scale
        transform.localScale = displayScale;

        if (qrCodeTexture != null)
        {
            Debug.Log($"[QRCodeDisplay] QR Code for server '{serverId}' displayed on {gameObject.name} using Unlit/Texture shader");
        }
        else
        {
            Debug.LogWarning($"[QRCodeDisplay] No QR code texture assigned to {gameObject.name}");
        }
    }

    /// <summary>
    /// Update the QR code at runtime
    /// </summary>
    public void SetQRCode(Texture2D newTexture, string newServerId)
    {
        qrCodeTexture = newTexture;
        serverId = newServerId;
        SetupQRCodeDisplay();
    }

    /// <summary>
    /// Get the server ID this QR code represents
    /// </summary>
    public string GetServerId()
    {
        return serverId;
    }

    // Visual helper in Scene view
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}
