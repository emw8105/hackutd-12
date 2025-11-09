using UnityEngine;
using System.Text;
using System.Threading.Tasks;
using Meta.Net.NativeWebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UnityEngine.UI;
using System;
using TMPro;

public class TechnicianWS : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public string technicianId = string.Empty;
    public TextMeshProUGUI notification_description = null;
    public Text ticket_description = null;


    private WebSocket websocket;
    private Queue<Action> mainThreadActions = new Queue<Action>();

    async void Start()
    {
        websocket = new WebSocket("ws://127.0.0.1:8000/ws/technician");

        websocket.OnOpen += () =>
        {
            Debug.Log("Connection open!");
        };

        websocket.OnError += (e) =>
        {
            Debug.Log("Error! " + e);
        };

        websocket.OnClose += async (e) =>
        {
            Debug.Log("Connection closed! Reconnecting in 2 seconds...");
            await Task.Delay(2000);
            await websocket.Connect();
        };

        websocket.OnMessage += (bytes, offset, length) =>
        {
            string message = Encoding.UTF8.GetString(bytes);
            HandleServerMessage(message);
        };

        // Keep sending messages at every 0.3s
        InvokeRepeating("SendWebSocketMessage", 0.0f, 0.3f);

        // waiting for messages
        await websocket.Connect();
    }

    async void SendWebSocketMessage()
    {
        if (websocket.State == WebSocketState.Open)
        {
            var jsonData = new
            {
                event_type = "online",
                payload = new
                {
                    id = technicianId,
                    location = new
                    {
                        x = 1,
                        y = 2,
                        z = 3
                    }
                }
            };

            string jsonString = JsonConvert.SerializeObject(jsonData);
            await websocket.SendText(jsonString);
        }
    }

    private void HandleServerMessage(string message)
    {
        try
        {
            JObject obj = JObject.Parse(message);
            string eventType = obj["event_type"]?.ToString();

            if (eventType == "assignment")
            {
                JObject payloadObj = obj["payload"] as JObject;
                if (payloadObj != null)
                {
                    JiraTicket ticket = payloadObj.ToObject<JiraTicket>();
                    Debug.Log("Received assignment! Server ID: " + ticket.server_id);

                    // Queue the UI update to run on the main thread
                    EnqueueMainThreadAction(() => FillTicket(ticket));
                }
            }
            else
            {
                Debug.Log("Received unknown event type: " + eventType);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to parse server message: " + ex);
        }
    }

    private void EnqueueMainThreadAction(Action action)
    {
        lock (mainThreadActions)
        {
            mainThreadActions.Enqueue(action);
        }
    }

    private void FillTicket(JiraTicket ticket)
    {
        if (ticket_description != null)
        {
            string description = ticket.description;


            string formatted = string.Format(
                "Problem on server {0}. Priority: {1}. Do you accept?",
                            ticket.server_id ?? "N/A",
                            ticket.priority ?? "N/A"
                        );
            notification_description.SetText(formatted);

            // Format full ticket details
            string labels = (ticket.labels != null && ticket.labels.Count > 0)
                ? string.Join(", ", ticket.labels)
                : "N/A";

            string ticketDetails = string.Format(
                "<b>Ticket Key:</b> {0}\n" +
                "<b>Ticket ID:</b> {1}\n" +
                "<b>Server ID:</b> {2}\n\n" +
                "<b>Summary:</b> {3}\n\n" +
                "<b>Description:</b>\n{4}\n\n" +
                "<b>Status:</b> {5}\n" +
                "<b>Status ID:</b> {6}\n" +
                "<b>Priority:</b> {7}\n" +
                "<b>Issue Type:</b> {8}\n\n" +
                "<b>Assignee:</b> {9}\n" +
                "<b>Reporter:</b> {10}\n\n" +
                "<b>Project:</b> {11}\n" +
                "<b>Created:</b> {12}\n" +
                "<b>Updated:</b> {13}\n\n" +
                "<b>Labels:</b> {14}",
                ticket.key ?? "N/A",
                ticket.id ?? "N/A",
                ticket.server_id ?? "N/A",
                ticket.summary ?? "N/A",
                ticket.description ?? "N/A",
                ticket.status ?? "N/A",
                ticket.status_id ?? "N/A",
                ticket.priority ?? "N/A",
                ticket.issue_type ?? "N/A",
                ticket.assignee ?? "N/A",
                ticket.reporter ?? "N/A",
                ticket.project ?? "N/A",
                ticket.created ?? "N/A",
                ticket.updated ?? "N/A",
                labels
            );

            ticket_description.text = ticketDetails;

            Debug.Log("UI Updated with: " + description);
        }
        else
        {
            Debug.LogWarning("ticket_description is null!");
        }
    }


    void Update()
    {
        // Process all queued main thread actions
        lock (mainThreadActions)
        {
            while (mainThreadActions.Count > 0)
            {
                Action action = mainThreadActions.Dequeue();
                action?.Invoke();
            }
        }
    }

    private async void OnApplicationQuit()
    {
        if (websocket != null)
        {
            await websocket.Close();
        }
    }

    public class JiraTicket
    {
        public string key { get; set; }
        public string id { get; set; }
        public string summary { get; set; }
        public string description { get; set; }
        public string status { get; set; }
        public string status_id { get; set; }
        public string priority { get; set; }
        public string assignee { get; set; }
        public string reporter { get; set; }
        public string created { get; set; }
        public string updated { get; set; }
        public string project { get; set; }
        public string issue_type { get; set; }
        public List<string> labels { get; set; }
        public string server_id { get; set; }
    }
}