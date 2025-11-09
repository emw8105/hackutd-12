using UnityEngine;
using UnityEngine.UI;

public class PanelController : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public Button acceptButton;
    public Button closeButton;
    public GameObject notifPanel;
    public GameObject panel;

    void Start()
    {
        // Make sure panel is hidden at start
        if (panel != null)
        {
            panel.SetActive(false);
        }

        // Add listeners to buttons
        if (acceptButton != null)
        {
            acceptButton.onClick.AddListener(OnAcceptClicked);
        }
        else
        {
            Debug.LogWarning("Accept Button is not assigned!");
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseClicked);
        }
        else
        {
            Debug.LogWarning("Close Button is not assigned!");
        }
    }

    void OnAcceptClicked()
    {
        Debug.Log("Accept button clicked!");

        if (panel != null)
        {
            panel.SetActive(true);
            notifPanel.SetActive(false);    
            Debug.Log("Panel opened");
        }
        else
        {
            Debug.LogWarning("Panel is not assigned!");
        }
    }

    void OnCloseClicked()
    {
        Debug.Log("Close button clicked!");

        if (panel != null)
        {
            panel.SetActive(false);
            notifPanel.SetActive(false);
            Debug.Log("Panel closed");
        }
        else
        {
            Debug.LogWarning("Panel is not assigned!");
        }
    }

    void OnDestroy()
    {
        // Clean up listeners when object is destroyed
        if (acceptButton != null)
        {
            acceptButton.onClick.RemoveListener(OnAcceptClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(OnCloseClicked);
        }
    }
}