using UnityEngine;

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

    [Header("Gesture Settings")]
    [Tooltip("How long the gesture must be held before triggering (in seconds)")]
    public float gestureHoldTime = 0.1f;
    [Tooltip("Cooldown time after a gesture is triggered (in seconds)")]
    public float gestureCooldown = 1.0f;
    [Tooltip("Enable debug logging to see gesture detection values")]
    public bool debugMode = false;

    private bool rockGestureTriggered = false;
    private bool peaceGestureTriggered = false;
    private bool rockTargetVisible = true;
    private bool peaceTargetVisible = true;
    private float rockGestureHoldTimer = 0f;
    private float peaceGestureHoldTimer = 0f;
    private float rockCooldownTimer = 0f;
    private float peaceCooldownTimer = 0f;

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
        bool indexExtended = indexStrength < 0.5f;
        bool middleExtended = middleStrength < 0.5f;

        // Ring and pinky should be curled (high pinch strength)
        bool ringCurled = ringStrength > 0.5f;
        bool pinkyCurled = pinkyStrength > 0.5f;

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
        rockTargetVisible = !rockTargetVisible;
        if (rockAndRollTarget != null)
        {
            rockAndRollTarget.SetActive(rockTargetVisible);
            Debug.Log($"[GestureToggle] Rock-and-roll target toggled {(rockTargetVisible ? "ON" : "OFF")}");
        }
    }

    private void TogglePeaceSignTarget()
    {
        peaceTargetVisible = !peaceTargetVisible;
        if (peaceSignTarget != null)
        {
            peaceSignTarget.SetActive(peaceTargetVisible);
            Debug.Log($"[GestureToggle] Peace sign target toggled {(peaceTargetVisible ? "ON" : "OFF")}");
        }
    }
}
