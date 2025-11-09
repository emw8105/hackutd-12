using UnityEngine;

public class GestureToggle : MonoBehaviour
{
    [Header("Hand Data Sources")]
    public OVRHand leftHandData;
    public OVRHand rightHandData;

    [Header("Target Object")]
    public GameObject targetObject;

    private bool gestureTriggered = false;
    private bool isVisible = true;

    void Update()
    {
        if (rightHandData == null)
        {
            Debug.LogWarning("Right Hand Data Source not assigned!");
            return;
        }

        // Check if the user is pinching with the right hand
        if (rightHandData.IsTracked)
        {
            float pinchStrength = rightHandData.GetFingerPinchStrength(OVRHand.HandFinger.Index);

            // Trigger on strong pinch
            if (pinchStrength > 0.9f && !gestureTriggered)
            {
                gestureTriggered = true;
                ToggleTarget();
            }
            else if (pinchStrength < 0.5f)
            {
                // Reset trigger once the user releases
                gestureTriggered = false;
            }
        }
    }

    private void ToggleTarget()
    {
        isVisible = !isVisible;
        if (targetObject != null)
        {
            targetObject.SetActive(isVisible);
            Debug.Log($"[GestureToggle] Toggled {(isVisible ? "ON" : "OFF")}");
        }
    }
}
