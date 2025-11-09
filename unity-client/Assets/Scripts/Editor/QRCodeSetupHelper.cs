using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to quickly create QR code displays in the scene
/// </summary>
public class QRCodeSetupHelper : EditorWindow
{
    [MenuItem("Tools/Setup QR Codes in Scene")]
    public static void ShowWindow()
    {
        GetWindow<QRCodeSetupHelper>("QR Code Setup");
    }

    void OnGUI()
    {
        GUILayout.Label("QR Code Scene Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "This will create two planes in your scene with QR codes attached:\n\n" +
            "• Server 1 (1-01-2-03) at position (2, 1.5, 5)\n" +
            "• Server 2 (1-01-2-04) at position (-2, 1.5, 5)\n\n" +
            "Each will face the camera and display the QR code.",
            MessageType.Info
        );

        GUILayout.Space(10);

        if (GUILayout.Button("Create QR Code Objects", GUILayout.Height(40)))
        {
            CreateQRCodeObjects();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Remove All QR Code Objects", GUILayout.Height(30)))
        {
            RemoveQRCodeObjects();
        }
    }

    void CreateQRCodeObjects()
    {
        // Load QR code textures
        Texture2D qrServer1 = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/QRCodes/server_1_01_2_03.png");
        Texture2D qrServer2 = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/QRCodes/server_1_01_2_04.png");

        if (qrServer1 == null || qrServer2 == null)
        {
            EditorUtility.DisplayDialog(
                "QR Codes Not Found",
                "Could not find QR code images in Assets/QRCodes/\n\n" +
                "Make sure the QR codes have been copied to:\n" +
                "Assets/QRCodes/server_1_01_2_03.png\n" +
                "Assets/QRCodes/server_1_01_2_04.png",
                "OK"
            );
            return;
        }

        // Find an existing cube to clone its exact setup
        GameObject existingCube = GameObject.Find("[BuildingBlock] Cube");
        if (existingCube == null)
        {
            EditorUtility.DisplayDialog(
                "Reference Cube Not Found",
                "Could not find the '[BuildingBlock] Cube' object in the scene.\n\n" +
                "This is needed as a reference for the correct rendering setup.",
                "OK"
            );
            return;
        }

        // Create Server 1 QR Code by cloning the cube
        CreateQRCodeFromCube(existingCube, "QR_Server_1", qrServer1, "1-01-2-03", new Vector3(2, 1.5f, 2));

        // Create Server 2 QR Code by cloning the cube
        CreateQRCodeFromCube(existingCube, "QR_Server_2", qrServer2, "1-01-2-04", new Vector3(-2, 1.5f, 2));

        Debug.Log("✓ QR Code objects created by cloning working cube!");
        EditorUtility.DisplayDialog(
            "Success!",
            "QR Code objects have been created by cloning the working cube.\n\n" +
            "They should now be visible when you press Play!\n\n" +
            "Look for QR_Server_1 and QR_Server_2 in the Hierarchy.",
            "OK"
        );
    }

    void CreateQRCodeFromCube(GameObject referenceCube, string name, Texture2D texture, string serverId, Vector3 position)
    {
        // Check if already exists
        GameObject existing = GameObject.Find(name);
        if (existing != null)
        {
            Debug.Log($"Removing existing {name}");
            DestroyImmediate(existing);
        }

        // Clone the working cube
        GameObject qrObject = Instantiate(referenceCube);
        qrObject.name = name;
        qrObject.transform.position = position;
        qrObject.transform.rotation = Quaternion.identity;
        qrObject.transform.localScale = new Vector3(1f, 1f, 0.1f); // Flat like a sign
        qrObject.transform.SetParent(null);

        // Replace cube mesh with quad mesh
        MeshFilter meshFilter = qrObject.GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            meshFilter.mesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        }

        // Update material texture
        MeshRenderer renderer = qrObject.GetComponent<MeshRenderer>();
        if (renderer != null && renderer.sharedMaterial != null)
        {
            // Create a new material instance based on the cube's material
            Material newMat = new Material(renderer.sharedMaterial);
            newMat.mainTexture = texture;
            renderer.material = newMat;
        }

        // Add QRCodeDisplay component for reference
        QRCodeDisplay qrDisplay = qrObject.GetComponent<QRCodeDisplay>();
        if (qrDisplay == null)
        {
            qrDisplay = qrObject.AddComponent<QRCodeDisplay>();
        }
        qrDisplay.qrCodeTexture = texture;
        qrDisplay.serverId = serverId;

        Debug.Log($"✓ Created {name} by cloning cube at {position} with server ID: {serverId}");
    }

    void CreateQRCodePlane(string name, Texture2D texture, string serverId, Vector3 position, Transform parent)
    {
        // Check if already exists
        GameObject existing = GameObject.Find(name);
        if (existing != null)
        {
            Debug.Log($"Updating existing {name}");
            DestroyImmediate(existing);
        }

        // Create quad instead of plane (better for vertical surfaces)
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = name;

        // CRITICAL: Set layer to Default (0) - same as the working cubes
        quad.layer = 0;

        quad.transform.position = position;
        quad.transform.rotation = Quaternion.Euler(0, 180, 0); // Face camera (forward)
        quad.transform.localScale = new Vector3(1f, 1f, 1f); // 1x1 meter

        // Don't parent it - make it a root object like the cubes
        quad.transform.SetParent(null);

        // Remove the MeshCollider (QR codes don't need physics)
        MeshCollider collider = quad.GetComponent<MeshCollider>();
        if (collider != null)
        {
            DestroyImmediate(collider);
        }

        // Add QRCodeDisplay component
        QRCodeDisplay qrDisplay = quad.AddComponent<QRCodeDisplay>();
        qrDisplay.qrCodeTexture = texture;
        qrDisplay.serverId = serverId;

        // Create material with Unlit shader (always visible, no lighting needed)
        Material mat = new Material(Shader.Find("Unlit/Texture"));
        mat.mainTexture = texture;

        // Make sure texture settings are correct
        if (texture != null)
        {
            // Ensure texture is readable and has proper import settings
            string texturePath = AssetDatabase.GetAssetPath(texture);
            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer != null)
            {
                bool needsReimport = false;
                if (!importer.isReadable)
                {
                    importer.isReadable = true;
                    needsReimport = true;
                }
                if (importer.textureType != TextureImporterType.Default)
                {
                    importer.textureType = TextureImporterType.Default;
                    needsReimport = true;
                }
                if (needsReimport)
                {
                    AssetDatabase.ImportAsset(texturePath);
                }
            }
        }

        MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
        renderer.material = mat;

        // Match the cube's rendering settings
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        Debug.Log($"✓ Created {name} at {position} with server ID: {serverId} (Root object, Layer 0, Unlit/Texture shader)");
    }
    void RemoveQRCodeObjects()
    {
        // Find individual QR objects (they're now root objects, not under a parent)
        GameObject qr1 = GameObject.Find("QR_Server_1");
        GameObject qr2 = GameObject.Find("QR_Server_2");
        GameObject qrParent = GameObject.Find("QRCodes"); // Old parent if it exists

        bool foundAny = false;

        if (qr1 != null || qr2 != null || qrParent != null)
        {
            if (EditorUtility.DisplayDialog(
                "Remove QR Codes",
                "Are you sure you want to remove all QR code objects from the scene?",
                "Yes", "Cancel"))
            {
                if (qr1 != null) { DestroyImmediate(qr1); foundAny = true; }
                if (qr2 != null) { DestroyImmediate(qr2); foundAny = true; }
                if (qrParent != null) { DestroyImmediate(qrParent); foundAny = true; }

                Debug.Log("✓ QR Code objects removed");
            }
        }
        else
        {
            EditorUtility.DisplayDialog("Not Found", "No QR code objects found in scene.", "OK");
        }
    }
}
