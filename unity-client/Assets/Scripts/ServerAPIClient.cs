using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;

public class ServerAPIClient : MonoBehaviour
{
    [Header("Server Configuration")]
    [Tooltip("Base URL of the FastAPI server")]
    public string serverBaseUrl = "http://localhost:8000";

    [Header("Debug Settings")]
    public bool debugMode = true;

    // Singleton instance
    private static ServerAPIClient _instance;
    public static ServerAPIClient Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ServerAPIClient>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("ServerAPIClient");
                    _instance = go.AddComponent<ServerAPIClient>();
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    #region Data Models
    [Serializable]
    public class JiraTicket
    {
        public string key;
        public string summary;
        public string description;
        public string status;
        public string priority;
        public string assignee;
        public string server_id;
        public string created;
        public string updated;
    }

    [Serializable]
    public class JiraTicketListResponse
    {
        public List<JiraTicket> tickets;
        public int count;
        public string message;
    }

    [Serializable]
    public class Server
    {
        public string id;
        public string name;
        public Location location;
    }

    [Serializable]
    public class Location
    {
        public float x;
        public float y;
        public float z;
    }

    [Serializable]
    public class ServerListResponse
    {
        public List<Server> servers;
        public int count;
        public string message;
    }
    #endregion

    #region API Calls

    /// <summary>
    /// Get all Jira tickets from the server
    /// </summary>
    public void GetAllTickets(Action<JiraTicketListResponse> onSuccess, Action<string> onError)
    {
        StartCoroutine(GetRequest<JiraTicketListResponse>("/items", onSuccess, onError));
    }

    /// <summary>
    /// Get a specific ticket by key
    /// </summary>
    public void GetTicket(string ticketKey, Action<JiraTicket> onSuccess, Action<string> onError)
    {
        StartCoroutine(GetRequest<JiraTicket>($"/items/{ticketKey}", onSuccess, onError));
    }

    /// <summary>
    /// Get all tickets associated with a specific server
    /// </summary>
    public void GetTicketsByServer(string serverId, Action<JiraTicketListResponse> onSuccess, Action<string> onError)
    {
        StartCoroutine(GetRequest<JiraTicketListResponse>($"/items/by-server/{serverId}", onSuccess, onError));
    }

    /// <summary>
    /// Get all servers
    /// </summary>
    public void GetAllServers(Action<ServerListResponse> onSuccess, Action<string> onError)
    {
        StartCoroutine(GetRequest<ServerListResponse>("/servers", onSuccess, onError));
    }

    /// <summary>
    /// Get a specific server by ID
    /// </summary>
    public void GetServer(string serverId, Action<Server> onSuccess, Action<string> onError)
    {
        StartCoroutine(GetRequest<Server>($"/servers/{serverId}", onSuccess, onError));
    }

    #endregion

    #region Generic Request Handlers

    private IEnumerator GetRequest<T>(string endpoint, Action<T> onSuccess, Action<string> onError)
    {
        string url = serverBaseUrl + endpoint;

        if (debugMode)
            Debug.Log($"[ServerAPIClient] GET Request to: {url}");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                if (debugMode)
                    Debug.Log($"[ServerAPIClient] Response: {request.downloadHandler.text}");

                try
                {
                    T response = JsonUtility.FromJson<T>(request.downloadHandler.text);
                    onSuccess?.Invoke(response);
                }
                catch (Exception e)
                {
                    string error = $"Failed to parse JSON: {e.Message}";
                    Debug.LogError($"[ServerAPIClient] {error}");
                    onError?.Invoke(error);
                }
            }
            else
            {
                string error = $"Request failed: {request.error}";
                Debug.LogError($"[ServerAPIClient] {error}");
                onError?.Invoke(error);
            }
        }
    }

    #endregion
}
