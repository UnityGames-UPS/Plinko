using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Newtonsoft.Json;
using Best.SocketIO;
using Best.SocketIO.Events;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;

public class SocketIOManager : MonoBehaviour
{

    internal GameData initialData = null;
    internal GameData resultData = null;
    internal PlayerData playerdata = null;
    [SerializeField] internal List<string> bonusdata = null;
    internal bool isResultdone = false;
    internal List<List<int>> LineData = null;
    // protected string nameSpace="game"; //BackendChanges
    protected string nameSpace = ""; //BackendChanges
    private Socket gameSocket; //BackendChanges

    private SocketManager manager;

    protected string SocketURI = null;
    // protected string TestSocketURI = "https://game-crm-rtp-backend.onrender.com/";
    protected string TestSocketURI = "http://localhost:5001/";

    [SerializeField]
    private string testToken;

    protected string gameID = "pl-base";
    // protected string gameID = "";

    internal bool isLoaded = false;

    internal bool SetInit = false;

    private const int maxReconnectionAttempts = 6;
    private readonly TimeSpan reconnectionDelay = TimeSpan.FromSeconds(10);
    [SerializeField]
    GameManager gameManager;

    private void Awake()
    {
        //Debug.unityLogger.logEnabled = false;
        isLoaded = false;
        SetInit = false;
#if UNITY_WEBGL && !UNITY_EDITOR
    Application.ExternalEval(@"
      if(window.ReactNativeWebView){
        window.ReactNativeWebView.postMessage('This is the new version of the game 2');
      }
    ");
#endif
    }

    private void Start()
    {
        OpenSocket();
    }

    void ReceiveAuthToken(string jsonData)
    {
        Debug.Log("Received data: " + jsonData);

        // Parse the JSON data
        var data = JsonUtility.FromJson<AuthTokenData>(jsonData);

        // Proceed with connecting to the server using myAuth and socketURL
        SocketURI = data.socketURL;
        myAuth = data.cookie;
        nameSpace = data.nameSpace; //BackendChanges
    }


    string myAuth = null;

    private void OpenSocket()
    {
        //Create and setup SocketOptions
        SocketOptions options = new SocketOptions();
        options.ReconnectionAttempts = maxReconnectionAttempts;
        options.ReconnectionDelay = reconnectionDelay;
        options.Reconnection = true;
        options.ConnectWith = Best.SocketIO.Transports.TransportTypes.WebSocket; //BackendChanges

        Application.ExternalCall("window.parent.postMessage", "authToken", "*");

#if UNITY_WEBGL && !UNITY_EDITOR //BackendChanges Start
        Application.ExternalEval(@" 
        (function (){
        
          if(window.ReactNativeWebView){
             try {
            if (!window.ReactNativeWebView || typeof window.ReactNativeWebView.postMessage !== 'function') {
                window.ReactNativeWebView.postMessage('ReactNativeWebView is not available.');
                console.error('ReactNativeWebView is not available.');
                return;
            }

            if (typeof window.ReactNativeWebView.injectedObjectJson !== 'function') {
                window.ReactNativeWebView.postMessage('ReactNativeWebView.injectedObjectJson is not a function.');
                console.error('ReactNativeWebView.injectedObjectJson is not a function.');
                return;
            }

            var injectedObj = JSON.parse(window.ReactNativeWebView.injectedObjectJson())
            window.ReactNativeWebView.postMessage('Injected obj : ' + injectedObj);

            if (!injectedObj || typeof injectedObj !== 'object') {
                window.ReactNativeWebView.postMessage('Injected object is invalid.');
                console.error('Injected object is invalid.');
                return;
            }

            // Expected properties: 'socketURL' and 'token'
            if (typeof injectedObj.socketURL !== 'string' || typeof injectedObj.token !== 'string') {
                window.ReactNativeWebView.postMessage('Injected object properties are invalid.');
                console.error('Injected object properties are invalid.');
                return;
            }

            var combinedData = JSON.stringify({
                socketURL: injectedObj.socketURL.trim(),
                cookie: injectedObj.token.trim(),
                nameSpace: injectedObj.nameSpace?.trim()
            });

            window.ReactNativeWebView.postMessage('authToken');

            // Send data to Unity, ensuring 'SendMessage' is available
            if (typeof SendMessage === 'function') {
                SendMessage('SocketManager', 'ReceiveAuthToken', combinedData);
            } else {
                window.ReactNativeWebView.postMessage('SendMessage function is not available.');
                console.error('SendMessage function is not available.');
            }
        } catch (error) {
            window.ReactNativeWebView.postMessage(JSON.stringify(error));
            console.error('An error occurred:', error);
        }
          }else{
              window.addEventListener('message', function(event) {
                  if (event.data.type === 'authToken') {
                      var combinedData = JSON.stringify({
                          cookie: event.data.cookie,
                          socketURL: event.data.socketURL,
                          nameSpace: event.data?.nameSpace ? event.data.nameSpace : ''
                      });
                      // Send the combined data to Unity
                      SendMessage('SocketManager', 'ReceiveAuthToken', combinedData);
                  }});
          }
        })()
                  "); 
        StartCoroutine(WaitForAuthToken(options)); 
#else //BackendChanges Finish
        Func<SocketManager, Socket, object> authFunction = (manager, socket) =>
        {
            return new
            {
                token = testToken,
                gameId = gameID
            };
        };
        options.Auth = authFunction;
        // Proceed with connecting to the server
        SetupSocketManager(options);
#endif
    }


    private IEnumerator WaitForAuthToken(SocketOptions options)
    {
        // Wait until myAuth is not null
        while (myAuth == null)
        {
            Debug.Log("My Auth is null");
            yield return null;
        }
        while (SocketURI == null)
        {
            Debug.Log("My Socket is null");
            yield return null;
        }
        Debug.Log("My Auth is not null");

        // Once myAuth is set, configure the authFunction
        Func<SocketManager, Socket, object> authFunction = (manager, socket) =>
        {
            return new
            {
                token = myAuth,
                gameId = gameID
            };
        };
        options.Auth = authFunction;

        Debug.Log("Auth function configured with token: " + myAuth);

        // Proceed with connecting to the server
        SetupSocketManager(options);
    }

    private void SetupSocketManager(SocketOptions options)
    {
        // Create and setup SocketManager
#if UNITY_EDITOR
        this.manager = new SocketManager(new Uri(TestSocketURI), options);
#else
    this.manager = new SocketManager(new Uri(SocketURI), options);
#endif

        if (string.IsNullOrEmpty(nameSpace))
        {  //BackendChanges Start
            gameSocket = this.manager.Socket;
        }
        else
        {
            print("nameSpace: " + nameSpace);
            gameSocket = this.manager.GetSocket("/" + nameSpace);
        }
        // Set subscriptions
        gameSocket.On<ConnectResponse>(SocketIOEventTypes.Connect, OnConnected);
        gameSocket.On<string>(SocketIOEventTypes.Disconnect, OnDisconnected);
        gameSocket.On<string>(SocketIOEventTypes.Error, OnError);
        gameSocket.On<string>("Message", OnListenEvent);
        gameSocket.On<bool>("socketState", OnSocketState);
        gameSocket.On<string>("internalError", OnSocketError);
        gameSocket.On<string>("alert", OnSocketAlert);
        gameSocket.On<string>("AnotherDevice", OnSocketOtherDevice); //BackendChanges Finish
                                                                     // Start connecting to the server
    }

    // Connected event handler implementation
    void OnConnected(ConnectResponse resp)
    {
        Debug.Log("Connected!");
        SendPing();
#if UNITY_WEBGL && !UNITY_EDITOR
    Application.ExternalEval(@"
      if(window.ReactNativeWebView){
        window.ReactNativeWebView.postMessage('Game Socket Connected');
      }
    ");
#endif
    }

    private void OnDisconnected(string response)
    {
        Debug.Log("Disconnected from the server");
        StopAllCoroutines();
      
#if UNITY_WEBGL && !UNITY_EDITOR
        Application.ExternalEval(@"
          if(window.ReactNativeWebView){
            window.ReactNativeWebView.postMessage('Game Socket OnDisconnected');
          }
        ");
#endif
    }

    private void OnError(string response)
    {
        Debug.LogError("Error: " + response);
#if UNITY_WEBGL && !UNITY_EDITOR
    Application.ExternalEval(@"
      if(window.ReactNativeWebView){
        window.ReactNativeWebView.postMessage('Game Socket OnError');
      }
    ");
#endif
    }

    private void OnListenEvent(string data)
    {
        ParseResponse(data);
#if UNITY_WEBGL && !UNITY_EDITOR
    Application.ExternalEval(@"
      if(window.ReactNativeWebView){
        window.ReactNativeWebView.postMessage('Game Socket OnListenEvent');
      }
    ");
#endif
    }

    private void OnSocketState(bool state)
    {
        Debug.Log("my state is " + state);
    }

    private void OnSocketError(string data)
    {
        Debug.Log("Received error with data: " + data);
#if UNITY_WEBGL && !UNITY_EDITOR
    Application.ExternalEval(@"
      if(window.ReactNativeWebView){
        window.ReactNativeWebView.postMessage('Game Socket OnSocketError');
      }
    ");
#endif
    }

    private void OnSocketAlert(string data)
    {
        Debug.Log("Received alert with data: " + data);
#if UNITY_WEBGL && !UNITY_EDITOR
    Application.ExternalEval(@"
      if(window.ReactNativeWebView){
        window.ReactNativeWebView.postMessage('Game Socket Alert');
      }
    ");
#endif
    }

    private void OnSocketOtherDevice(string data)
    {
        Debug.Log("Received Device Error with data: " + data);
       
#if UNITY_WEBGL && !UNITY_EDITOR
    Application.ExternalEval(@"
      if(window.ReactNativeWebView){
        window.ReactNativeWebView.postMessage('Game Socket OnSocketOtherDevice');
      }
    ");
#endif
    }

    private void SendPing()
    {
        InvokeRepeating("AliveRequest", 0f, 3f);
    }

    private void AliveRequest()
    {
        SendDataWithNamespace("YES I AM ALIVE");
    }

    private void SendDataWithNamespace(string eventName, string json = null)
    {
        // Send the message
        if (gameSocket != null && gameSocket.IsOpen) //BackendChanges
        {
            if (json != null)
            {
                gameSocket.Emit(eventName, json);
                Debug.Log("JSON data sent: " + json);
            }
            else
            {
                gameSocket.Emit(eventName);
            }
        }
        else
        {
            Debug.LogWarning("Socket is not connected.");
        }
    }

    internal void CloseSocket()
    {
        SendDataWithNamespace("EXIT");

    }

    internal void ReactNativeCallOnFailedToConnect() //BackendChanges
    {
#if UNITY_WEBGL && !UNITY_EDITOR
    Application.ExternalEval(@"
      if(window.ReactNativeWebView){
        window.ReactNativeWebView.postMessage('onExit');
      }
    ");
#endif
    }

    private void ParseResponse(string jsonObject)
    {
        Debug.Log(jsonObject);
        Root myData = JsonConvert.DeserializeObject<Root>(jsonObject);

        string id = myData.id;

        switch (id)
        {
            case "InitData":
                {
                    initialData = myData.message.GameData;
                    playerdata = myData.message.PlayerData;
                    if (!SetInit)
                    {
                        SetInit = true;
                        
                        setInitialData(initialData.Bets);
                    }
                    else
                    {
                        RefreshUI();
                    }
                    break;
                }
            case "ResultData":
                {
                    //Debug.Log(jsonObject);
                    resultData = myData.message.GameData;
                    playerdata = myData.message.PlayerData;
                    isResultdone = true;
                    break;
                }
            case "ExitUser":
                {
                    if (gameSocket != null) //BackendChanges
                    {
                        Debug.Log("Dispose my Socket");
                        this.manager.Close();
                    }
                    Application.ExternalCall("window.parent.postMessage", "onExit", "*");
#if UNITY_WEBGL && !UNITY_EDITOR //BackendChanges
    Application.ExternalEval(@"
      if(window.ReactNativeWebView){
        window.ReactNativeWebView.postMessage('onExit');
      }
    ");
#endif
                    break;
                }
        }
    }

    private void RefreshUI()
    {
       
    }

    private void setInitialData(List<double> slotPop)
    {
       
        isLoaded = true;
        gameManager.setInitialUI();
        Application.ExternalCall("window.parent.postMessage", "OnEnter", "*");
#if UNITY_WEBGL && !UNITY_EDITOR //BackendChanges
    Application.ExternalEval(@"
      if(window.ReactNativeWebView){
        window.ReactNativeWebView.postMessage('OnEnter');
      }
    ");
#endif
    }

    internal void AccumulateResult(double currBet,int row,int risk)
    {
        isResultdone = false;
        MessageData message = new MessageData();
        message.data = new BetData();
        message.data.currentBet = currBet;
        message.data.spins = 1;
        message.data.rows = row;
        message.data.risk = risk;
        message.id = "SPIN";

        // Serialize message data to JSON
        string json = JsonUtility.ToJson(message);
        SendDataWithNamespace("message", json);
    }

    private List<string> RemoveQuotes(List<string> stringList)
    {
        for (int i = 0; i < stringList.Count; i++)
        {
            stringList[i] = stringList[i].Replace("\"", ""); // Remove inverted commas
        }
        return stringList;
    }

    private List<string> ConvertListListIntToListString(List<List<int>> listOfLists)
    {
        List<string> resultList = new List<string>();

        foreach (List<int> innerList in listOfLists)
        {
            // Convert each integer in the inner list to string
            List<string> stringList = new List<string>();
            foreach (int number in innerList)
            {
                stringList.Add(number.ToString());
            }

            // Join the string representation of integers with ","
            string joinedString = string.Join(",", stringList.ToArray()).Trim();
            resultList.Add(joinedString);
        }

        return resultList;
    }

    private List<string> ConvertListOfListsToStrings(List<List<string>> inputList)
    {
        List<string> outputList = new List<string>();

        foreach (List<string> row in inputList)
        {
            string concatenatedString = string.Join(",", row);
            outputList.Add(concatenatedString);
        }

        return outputList;
    }

    private List<string> TransformAndRemoveRecurring(List<List<string>> originalList)
    {
        // Flattened list
        List<string> flattenedList = new List<string>();
        foreach (List<string> sublist in originalList)
        {
            flattenedList.AddRange(sublist);
        }

        // Remove recurring elements
        HashSet<string> uniqueElements = new HashSet<string>(flattenedList);

        // Transformed list
        List<string> transformedList = new List<string>();
        foreach (string element in uniqueElements)
        {
            transformedList.Add(element.Replace(",", ""));
        }

        return transformedList;
    }
}

[Serializable]
public class BetData
{
    public double currentBet;
    public int rows;
    public int risk;
    public double spins;
}

[Serializable]
public class MessageData
{
    public BetData data;
    public string id;
}

public class GameData
{
    public List<double> Bets { get; set; }
    public List<string> rows { get; set; }
    public List<string> risk { get; set; }
    public int ballPosition;
    public string selectedMultiplier;
    public List<List<List<double>>> multiplier { get; set; }
}

public class Message
{
    public GameData GameData { get; set; }
    public PlayerData PlayerData { get; set; }
}

public class PlayerData
{
    public double Balance { get; set; }
    public double currentWining;
}

public class Root
{
    public string id { get; set; }
    public Message message { get; set; }
    public string username { get; set; }
}
[Serializable]
public class AuthTokenData
{
    public string cookie;
    public string socketURL;
    public string nameSpace; //BackendChanges
}


