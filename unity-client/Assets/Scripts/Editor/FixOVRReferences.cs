using UnityEngine;
using UnityEditor;

/// <summary>
/// Fixes broken OVR hand tracking references after copying objects between scenes
/// </summary>
public class FixOVRReferences : EditorWindow
{
    [MenuItem("Tools/Fix OVR Hand Tracking References")]
    public static void FixHandReferences()
    {
        // Find OVRCameraRig
        var cameraRig = GameObject.FindObjectOfType<OVRCameraRig>();
        if (cameraRig == null)
        {
            EditorUtility.DisplayDialog(
                "OVRCameraRig Not Found",
                "Could not find OVRCameraRig in the scene.\n\n" +
                "Please add one: GameObject → XR → Room-Scale OVR Rig",
                "OK"
            );
            return;
        }

        int fixedCount = 0;

        // Find all objects with Hand components
        var hands = GameObject.FindObjectsOfType<MonoBehaviour>();
        foreach (var component in hands)
        {
            var type = component.GetType();

            // Check if this is a Hand or HandRef component
            if (type.Name.Contains("Hand") || type.Name.Contains("Interactor"))
            {
                // Use reflection to find and set TrackingToWorldTransformer
                var field = type.GetField("_trackingToWorldTransformer",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

                if (field != null && field.GetValue(component) == null)
                {
                    // Try to find the transformer component on the camera rig
                    var transformer = cameraRig.GetComponentInChildren(
                        System.Type.GetType("Oculus.Interaction.Input.TrackingToWorldTransformerOVR, com.meta.xr.sdk.interaction.ovr"));

                    if (transformer != null)
                    {
                        field.SetValue(component, transformer);
                        EditorUtility.SetDirty(component);
                        fixedCount++;
                        Debug.Log($"Fixed reference on {component.gameObject.name}.{type.Name}");
                    }
                }
            }
        }

        if (fixedCount > 0)
        {
            EditorUtility.DisplayDialog(
                "Success",
                $"Fixed {fixedCount} broken OVR hand tracking reference(s).\n\n" +
                "Save your scene to keep the changes.",
                "OK"
            );
            Debug.Log($"✓ Fixed {fixedCount} OVR references");
        }
        else
        {
            EditorUtility.DisplayDialog(
                "No Issues Found",
                "All OVR hand tracking references appear to be intact.\n\n" +
                "If you're still seeing errors, try:\n" +
                "1. Delete copied hand objects\n" +
                "2. Add fresh OVRCameraRig: GameObject → XR → Room-Scale OVR Rig",
                "OK"
            );
        }
    }
}
