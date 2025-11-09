using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

// Classes to match the JSON structure
[System.Serializable]
public class Location
{
    public float x;
    public float y;
    public float z;
}

[System.Serializable]
public class TechnicianWs
{
    public string id;
    public Location location;
}

[System.Serializable]
public class TechnicianResponse
{
    public List<TechnicianWs> technicians;
    public int count;
    public string message;
}

public class MakeRequest : MonoBehaviour
{
    // Replace with your URL
    private string url = "https://hackutd-12-production.up.railway.app/technicians";

    void Start()
    {
        StartCoroutine(GetTechnicians());
    }

    public IEnumerator GetTechnicians()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            // Send the request and wait for a response
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error: " + request.error);
            }
            else
            {
                // Parse JSON response
                string json = request.downloadHandler.text;
                TechnicianResponse response = JsonUtility.FromJson<TechnicianResponse>(json);

                // Example: Log the first technician's info
                if (response.technicians != null && response.technicians.Count > 0)
                {
                    TechnicianWs firstTech = response.technicians[0];
                    Debug.Log("Technician ID: " + firstTech.id);
                    Debug.Log("Location: x=" + firstTech.location.x + " y=" + firstTech.location.y + " z=" + firstTech.location.z);
                }

                Debug.Log("Message from server: " + response.message);
            }
        }
    }
}
