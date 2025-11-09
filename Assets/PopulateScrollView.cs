using UnityEngine;
using UnityEngine.UI;

public class PopulateScrollView : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public GameObject itemPrefab;      // The UI element (button, panel, etc.) to spawn
    public Transform contentTransform;  // The "Content" GameObject inside Scroll View

    void Start()
    {
        
    }

    public void PopulateItems()
    {
        // Loop 20 times to create 20 items
        for (int i = 1; i <= 20; i++)
        {
            AddItem("Item " + i);
        }

        // Force layout to rebuild so Scroll View updates correctly
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)contentTransform);
    }

    void AddItem(string itemName)
    {
        // 1. Instantiate new item as child of Content
        GameObject newItem = Instantiate(itemPrefab, contentTransform);

        // 2. Optional: Set the text on the item if it has a Text component
        Text textComp = newItem.GetComponentInChildren<Text>();
        if (textComp != null)
        {
            textComp.text = itemName;
        }
    }
}