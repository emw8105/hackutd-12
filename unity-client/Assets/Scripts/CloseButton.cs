using UnityEngine;
using UnityEngine.UI; 

public class CloseButton : MonoBehaviour
{
    [Header("Assign your Panel GameObject here")]
    public GameObject Panel;
    public Button Button; // Changed to Button type instead of GameObject

    void Start()
    {
        // Attach the listener when the script starts
        if (Button != null)
        {
            Button.onClick.AddListener(Close);
        }
        else
        {
            Debug.LogWarning("ClosePanel: No button assigned!");
        }
    }

    public void Close()
    {
        if (Panel != null)
        {
            Panel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("ClosePanel: No panel assigned!");
        }
    }

    // Clean up listener when destroyed
    void OnDestroy()
    {
        if (Button != null)
        {
            Button.onClick.RemoveListener(Close);
        }
    }
}
