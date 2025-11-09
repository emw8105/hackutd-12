using UnityEngine;

public class HeadLock : MonoBehaviour
{
    public Transform head;           // Assign your XR camera here

    void LateUpdate()
    {
        if (head == null) return;

        transform.position = head.position;
        transform.rotation = head.rotation;
    }
}