using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class RequestButtonHookup : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public XRSimpleInteractable pokeButton;      // Your PokeButton
    public MakeRequest request;  // The script that populates the scroll view

    void OnEnable()
    {
        pokeButton.selectEntered.AddListener(OnPokeButtonPressed);
    }

    void OnDisable()
    {
        pokeButton.selectEntered.RemoveListener(OnPokeButtonPressed);
    }

    void OnPokeButtonPressed(SelectEnterEventArgs args)
    {
        request.GetTechnicians(); // Call your scroll view method
    }
}