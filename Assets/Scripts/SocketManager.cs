using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NativeWebSocket;
using UnityEngine;

/// <summary>
/// This class is used to define the structure of the response that the server will send to the client.
/// </summary>
/// <remarks>
/// This class is used to convert a JSON response from the websocket server into a C# object which will be used to handle the response and manipulate the data in a type-safe manner.
/// </remarks>
[Serializable]
public class SocketResponse
{
    // Will always be present
    public string type;
    public int playerId;
    public int timestamp;

    // Presence depends on data type
    public string optionId;
    public string pollId;
    public string message;
    public List<Vote> results;

    public static SocketResponse CreateFromJson(string jsonString)
    {
        return JsonUtility.FromJson<SocketResponse>(jsonString);
    }
}

/// <summary>
/// This class is used to define a cutscene option that will be shown to the players in a poll.
/// </summary>
/// <remarks>
/// This class is used as a base type in the Marathon script to make sure the cutscene options are correctly defined inside the Unity Inspector.
/// It is also used to ensure type-safe requests to the server.
/// </remarks>
[Serializable]
public class Option
{
    [Tooltip("Name of the cutscene that the players will see.")]
    public string name;
    [Tooltip("Identifier for the animation. This should be identical to the name set in CutsceneManager.")]
    public string id;
}

/// <summary>
/// This class is used to define a vote that the player can make in a poll.
/// </summary>
/// <remarks>
/// This is used to make sure data from the server is correctly parsed and handled by converting the votes from a poll closure into an object.
/// </remarks>
[Serializable]
public class Vote
{
    public string optionId;
    public int votes = -1;
}

/// <summary>
/// This class is used to define the structure of the request that the client will send to the server.
/// </summary>
/// <remarks>
/// This class is used to convert a C# object into a JSON string that will be sent to the server via the websocket connection.
/// </remarks>
[Serializable]
public class SocketRequest
{
    public string type;
    public int timestamp = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public string message;
    public List<Option> options;
    public string title;
    public string id;
    public int endTime;
    public string pollId;
    public string heading;
    public string description;
    public string backgroundColor;
    public string textColor;
    public string reason;

    public string SaveToString()
    {
        return JsonUtility.ToJson(this);
    }
}

/// <summary>
/// This class is used to manage the websocket connection to the server.
/// </summary>
/// <remarks>
/// This script is the "brain" of the game. It manages the websocket connection to the server and handles the sending and receiving of messages.
/// All communication passes through here.
/// In this script, it instantiates a new websocket connection and sets up event listeners for the connection.
/// When a new message is received, it gets parsed to a C# object and then handled accordingly depending on the message type.
/// When a new poll is created by the Marathon script, it gets sent to the server here through the SendWebSocketPoll function.
/// </remarks>
public class SocketManager : MonoBehaviour
{
    [Tooltip("Enable this to connect to the development server. Disable this to connect to the production server.")]
    [SerializeField] private bool devServer = false;
    
    [Space]
    
    [Tooltip("Reference to the TimelinePlayer script. This should be on the CutsceneManager GameObject")]
    [SerializeField] private TimelinePlayer player;
    [Tooltip("Reference to the Marathon script. This should be on the SceneManager GameObject")]
    [SerializeField] private Marathon marathon;
    
    
    private string URL;
    private WebSocket _websocket;
    
    /// <summary>
    /// This method sets up the Websocket connection and event listeners.
    /// </summary>
    /// <remarks>
    /// This method sets up the Websocket connection and event listeners.
    /// The event listeners are set up to handle the different states of the connection and the messages that are received.
    /// When a message is received, it gets parsed to a C# object and then handled accordingly depending on the message type.
    /// When a message type message is received, it gets logged to the console. This is used for debugging purposes.
    /// When a message type voteClosure is received, it gets logged to the console and the cutscene is started with the cutscene id that has the most votes by calling the ReceiveResponse function in the Marathon script.
    /// When the connection is closed, it tries to reconnect after 5 seconds. This should help if there are any network issues or the server goes down.
    /// </remarks>
    async void Start()
    {
        if (devServer)
        {
            URL = "wss://DEVELOPMENTSERVER/";
        }
        else
        {
            URL = "wss://PRODUCTIONSERVER";
        }
        _websocket = new WebSocket(URL);

        _websocket.OnOpen += () =>
        {
            Debug.Log("Connection open!");
            SendWebSocketMessage("Unity connected");
        };

        _websocket.OnError += (e) => { Debug.Log("Error! " + e); };

        _websocket.OnClose += (e) =>
        {
            Debug.Log("Connection closed! Trying to reconnect in 5s");
            Invoke("Reconnect", 5.0f);
        };

        _websocket.OnMessage += (bytes) =>
        {
            // Debug.Log("OnMessage!");
            // Debug.Log(bytes);

            // getting the message as a string
            var message = Encoding.UTF8.GetString(bytes);
            // Parse message which is a JSON string
            var res = SocketResponse.CreateFromJson(message);

            Debug.Log(message);

            switch (res.type)
            {
                case "message":
                    if (res.message.Split('|')[0] == "playCutscene")
                    {
                        player.StartTimeline(res.message.Split('|')[1]);
                    } else if (res.message.ToLower() == "systemstart")
                    {
                        marathon.StartCutscene();
                    }

                    break;
                case "voteClosure":
                    var totalWinner = new Vote();
                    foreach (var result in res.results)
                    {
                        if (result.votes > totalWinner.votes)
                        {
                            totalWinner = result;
                        }
                    } 
                    marathon.ReceiveResponse(res.pollId, totalWinner.optionId);

                    Debug.Log(
                        $"WINNER for {res.pollId} is... {totalWinner.optionId} with {totalWinner.votes} votes!!");
                    break;
            }
        };
        // waiting for messages
        await _websocket.Connect();
    }

    /// <summary>
    /// This is code provided by the WebSocket-Sharp library to handle the message queue.
    /// </summary>
    /// <remarks>
    /// This is a preprocessor directive that checks if the game is running in WebGL. If it is, it will not run the code to dispatch the queue. If running in the Unity Editor or on a standalone build, it will run the code to dispatch the queue.
    /// </remarks>
    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        _websocket.DispatchMessageQueue();
#endif
    }

    /// <summary>
    /// Disables the websocket connection when the application is closed.
    /// </summary>
    private async void OnApplicationQuit()
    {
        await _websocket.Close();
    }

    /// <summary>
    /// When disconnected, this method is called every 5 seconds to reconnect to the server.
    /// </summary>
    void Reconnect()
    {
        _websocket.Connect();
    }

    /// <summary>
    /// This method sends a message to the server through the websocket connection.
    /// </summary>
    /// <remarks>
    /// This converts the message to a JSON string and sends it to the server through the websocket connection.
    /// </remarks>
    /// <param name="message">string. This is the message that will be broadcasted to the clients</param>
    public async void SendWebSocketMessage(string message)
    {
        if (_websocket.State == WebSocketState.Open)
        {
            // Sending bytes
            // await _websocket.Send(new byte[] { 10, 20, 30 });

            // Sending plain text
            var req = new SocketRequest();
            req.type = "message";
            req.message = message;

            await _websocket.SendText(req.SaveToString());
        }
    }

    /// <summary>
    /// This method sends a poll to the server through the websocket connection.
    /// </summary>
    /// <remarks>
    /// This takes in a poll name, options and a duration to send to the server.
    /// The server then receives this request and creates a poll for the players to participate in.
    /// Once the poll is complete, the server will send a voteClosure message to the clients with the results of the poll.
    /// </remarks>
    /// <param name="title">string. The name of the poll - typically a question.</param>
    /// <param name="options">List of Option. A list of the poll options - a cutscene id and name for each.</param>
    /// <param name="duration">int. How long the poll should last in seconds. Defaults to 30</param>
    public async void SendWebSocketPoll(string title, List<Option> options, int duration = 30)
    {
        var req = new SocketRequest();
        req.title = title;
        req.type = "poll";

        req.options = options;
        req.id = NameToId(title);
        // 30 is hardcoded for now because of some issues with unix time not syncing properly
        req.endTime = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 30;
        await _websocket.SendText(req.SaveToString());
    }

    /// <summary>
    /// This method converts a sentence case name to a lowercase dash-separated id.
    /// </summary>
    /// <param name="name">string. Can be any name, used for polls.</param>
    /// <returns>id - string. The converted name as a lowercase, dash-separated id</returns>
    public string NameToId(string name)
    {
        var id = name.ToLower().Replace(' ', '-');
        var allowed = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-";
        id = new string(id.Where(c => allowed.Contains(c)).ToArray());
        id = id.Trim('-');
        return id;
    }
}
