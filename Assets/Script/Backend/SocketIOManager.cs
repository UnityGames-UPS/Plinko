using Best.SocketIO;
using Best.SocketIO.Events;
using Newtonsoft.Json;
using PlinkoGame.Data;
using System;
using System.Collections;
using UnityEngine;

namespace PlinkoGame.Network
{
    /// <summary>
    /// Handles all Socket.IO communication - Unified structure with Slot game
    /// </summary>
    public class SocketIOManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private UIManager uiManager;
        [SerializeField] internal JSFunctCalls JSManager;

        [Header("Testing (Editor Only)")]
        [SerializeField] private string testToken;

        [Header("Blocker")]
        [SerializeField] private GameObject RaycastBlocker;

        [Header("Disconnect Settings")]
        [SerializeField] private float disconnectDelay = 60f;

        // Public data properties
        internal PlinkoGameData InitialData { get; private set; }
        internal PlinkoPlayer PlayerData { get; private set; }
        internal PlinkoResultPayload ResultData { get; private set; }
        internal bool IsResultReady { get; private set; }
        internal bool IsInitialized { get; private set; }

        // Socket configuration
        private SocketManager manager;
        private Socket gameSocket;
        private string SocketURI = null;
        private const string TestSocketURI = "https://devrealtime.dingdinghouse.com/";
        private string nameSpace = "playground";
        private string myAuth = null;

        // Connection state
        private bool isConnected = false;
        private bool hasEverConnected = false;
        private bool isExiting = false;

        // Ping/Pong system
        private float lastPongTime = 0f;
        private const float pingInterval = 2f;
        private bool waitingForPong = false;
        private int missedPongs = 0;
        private const int MaxMissedPongs = 15;
        private Coroutine PingRoutine;
        private Coroutine connectionTimeoutRoutine;
        private Coroutine initDataTimeoutRoutine;
        private Coroutine disconnectTimerCoroutine;

        private void Awake()
        {
            IsInitialized = false;
            IsResultReady = false;
        }

        private void Start()
        {
            if (!ValidateToken())
            {
                return;
            }

            OpenSocket();
        }

        // ============================================
        // VALIDATION
        // ============================================

        private bool ValidateToken()
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(testToken))
            {
                uiManager?.ShowErrorPopup("Test token is required in editor mode");
                if (RaycastBlocker != null)
                {
                    RaycastBlocker.SetActive(true);
                }
                return false;
            }

            if (testToken.Length < 10)
            {
                uiManager?.ShowErrorPopup("Invalid test token format");
                if (RaycastBlocker != null)
                {
                    RaycastBlocker.SetActive(true);
                }
                return false;
            }

            return true;
#else
            return true;
#endif
        }

        // ============================================
        // SOCKET CONNECTION
        // ============================================

        private void OpenSocket()
        {
            Debug.Log("[SOCKET] Opening Socket.IO connection...");

            SocketOptions options = new SocketOptions
            {
                AutoConnect = false,
                Reconnection = false,
                Timeout = TimeSpan.FromSeconds(3),
                ConnectWith = Best.SocketIO.Transports.TransportTypes.WebSocket
            };

#if UNITY_WEBGL && !UNITY_EDITOR
            JSManager.SendCustomMessage("authToken");
            StartCoroutine(WaitForAuthToken(options));
#else
            object authFunction(Best.SocketIO.SocketManager manager, Socket socket)
            {
                return new { token = testToken };
            }
            options.Auth = authFunction;
            SetupSocketManager(options);
#endif
        }

        private IEnumerator WaitForAuthToken(SocketOptions options)
        {
            float timeout = 10f;

            while (myAuth == null && timeout > 0)
            {
                Debug.Log("[AUTH] Waiting for auth token...");
                timeout -= Time.deltaTime;
                yield return null;
            }

            while (SocketURI == null && timeout > 0)
            {
                Debug.Log("[AUTH] Waiting for socket URI...");
                timeout -= Time.deltaTime;
                yield return null;
            }

            if (myAuth == null || SocketURI == null)
            {
                Debug.LogError("[AUTH] Authentication failed - timeout");
                uiManager?.ShowErrorPopup("Authentication failed. Please refresh.");
                if (RaycastBlocker != null)
                {
                    RaycastBlocker.SetActive(true);
                }
                yield break;
            }

            Debug.Log("[AUTH] Auth token received successfully");

            object authFunction(Best.SocketIO.SocketManager manager, Socket socket)
            {
                return new { token = myAuth };
            }
            options.Auth = authFunction;

            SetupSocketManager(options);
        }

        private void SetupSocketManager(SocketOptions options)
        {
            Debug.Log("[SOCKET] Setting up socket manager");

#if UNITY_EDITOR
            this.manager = new Best.SocketIO.SocketManager(new Uri(TestSocketURI), options);
            Debug.Log($"[SOCKET] Using TEST URI: {TestSocketURI}");
#else
            this.manager = new Best.SocketIO.SocketManager(new Uri(SocketURI), options);
            Debug.Log($"[SOCKET] Using PROD URI: {SocketURI}");
#endif

            if (string.IsNullOrEmpty(nameSpace))
            {
                gameSocket = this.manager.Socket;
                Debug.Log("[SOCKET] Using default namespace");
            }
            else
            {
                Debug.Log($"[SOCKET] Using namespace: {nameSpace}");
                gameSocket = this.manager.GetSocket("/" + nameSpace);
            }

            // Set subscriptions with debug logging
            gameSocket.On<ConnectResponse>(SocketIOEventTypes.Connect, (resp) => {
                Debug.Log("[SOCKET] Connect event received!");
                OnConnected(resp);
            });

            gameSocket.On(SocketIOEventTypes.Disconnect, () => {
                Debug.Log("[SOCKET] Disconnect event received!");
                OnDisconnected();
            });

            gameSocket.On<Error>(SocketIOEventTypes.Error, OnError);
            gameSocket.On<string>("game:init", OnInitData);
            gameSocket.On<string>("result", OnResult);
            gameSocket.On<string>("pong", OnPongReceived);
            gameSocket.On<string>("internalError", OnInternalError);
            gameSocket.On<string>("alert", OnAlert);
            gameSocket.On<string>("AnotherDevice", OnAnotherDevice);

            Debug.Log("[SOCKET] Event handlers registered");
            Debug.Log("[SOCKET] Opening connection...");
            manager.Open();
            Debug.Log("[SOCKET] Socket manager setup complete - waiting for connection...");

            connectionTimeoutRoutine = StartCoroutine(ConnectionTimeoutCheck());
        }

        private IEnumerator ConnectionTimeoutCheck()
        {
            float timeout = 10f;
            float elapsed = 0f;

            Debug.Log("[SOCKET] Starting connection timeout check (10s)...");

            while (!isConnected && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!isConnected)
            {
                Debug.LogError($"[SOCKET] Connection timeout after {elapsed:F1}s - OnConnected never called");
                uiManager?.ShowErrorPopup("Connection failed. Please check your network and try again.");
                if (RaycastBlocker != null)
                {
                    RaycastBlocker.SetActive(true);
                }
            }
            else
            {
                Debug.Log($"[SOCKET] Connected successfully after {elapsed:F1}s");
            }

            connectionTimeoutRoutine = null;
        }

        // ============================================
        // CONNECTION EVENTS
        // ============================================

        private void OnConnected(ConnectResponse resp)
        {
            Debug.Log("[SOCKET] Connected to server - OnConnected callback triggered");

            // Stop connection timeout
            if (connectionTimeoutRoutine != null)
            {
                StopCoroutine(connectionTimeoutRoutine);
                connectionTimeoutRoutine = null;
            }

            if (hasEverConnected)
            {
                uiManager?.CheckAndClosePopups();
            }

            isConnected = true;
            hasEverConnected = true;
            waitingForPong = false;
            missedPongs = 0;
            lastPongTime = Time.time;

            // Start init data timeout (server will send init automatically)
            initDataTimeoutRoutine = StartCoroutine(InitDataTimeoutCheck());

            SendPing();
        }

        private IEnumerator InitDataTimeoutCheck()
        {
            float timeout = 15f;
            float elapsed = 0f;

            Debug.Log("[SOCKET] Starting init data timeout check (15s)...");

            while (!IsInitialized && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!IsInitialized)
            {
                Debug.LogError($"[SOCKET] Init data timeout after {elapsed:F1}s - server did not send game data");
                uiManager?.ShowErrorPopup("Failed to load game data. Please refresh the page.");
                if (RaycastBlocker != null)
                {
                    RaycastBlocker.SetActive(true);
                }
            }
            else
            {
                Debug.Log($"[SOCKET] Init data received successfully after {elapsed:F1}s");
            }

            initDataTimeoutRoutine = null;
        }

        private void OnDisconnected()
        {
            Debug.LogWarning("[SOCKET] Disconnected from server");

            isConnected = false;
            ResetPingRoutine();

            if (!isExiting)
            {
                uiManager?.ShowDisconnectionPopup();
            }
        }

        private void OnPongReceived(string data)
        {
            Debug.Log("[PING] Pong received");
            waitingForPong = false;
            missedPongs = 0;
            lastPongTime = Time.time;
        }

        private void OnError(Error err)
        {
            Debug.LogError($"[SOCKET] Error: {err}");

            string errorMessage = err.ToString().ToLower();
            if (errorMessage.Contains("unauthorized") ||
                errorMessage.Contains("invalid token") ||
                errorMessage.Contains("expired") ||
                errorMessage.Contains("authentication") ||
                errorMessage.Contains("token"))
            {
                uiManager?.ShowErrorPopup("Invalid or expired token. Please refresh.");

                if (RaycastBlocker != null)
                {
                    RaycastBlocker.SetActive(true);
                }
            }
            else
            {
                uiManager?.ShowErrorPopup("Connection error occurred");
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            JSManager?.SendCustomMessage("error");
#endif
        }

        // ============================================
        // PING/PONG SYSTEM
        // ============================================

        private void SendPing()
        {
            ResetPingRoutine();
            PingRoutine = StartCoroutine(PingCheck());
        }

        private void ResetPingRoutine()
        {
            if (PingRoutine != null)
            {
                StopCoroutine(PingRoutine);
                PingRoutine = null;
            }
        }

        private IEnumerator PingCheck()
        {
            while (true)
            {
                if (missedPongs == 0)
                {
                    uiManager?.CheckAndClosePopups();
                }

                if (waitingForPong)
                {
                    missedPongs++;
                    Debug.LogWarning($"[PING] Pong missed #{missedPongs}/{MaxMissedPongs}");

                    if (missedPongs == 2)
                    {
                        uiManager?.ShowReconnectionPopup();
                    }

                    if (missedPongs >= MaxMissedPongs)
                    {
                        Debug.LogError("[PING] Connection lost - max pongs missed");
                        isConnected = false;
                        uiManager?.ShowDisconnectionPopup();
                        yield break;
                    }
                }

                waitingForPong = true;
                lastPongTime = Time.time;
                SendDataWithNamespace("ping");
                Debug.Log("[PING] Ping sent");

                yield return new WaitForSeconds(pingInterval);
            }
        }

        // ============================================
        // APPLICATION FOCUS - DISCONNECT TIMER
        // ============================================

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                disconnectTimerCoroutine = StartCoroutine(DisconnectTimer());
            }
            else
            {
                if (disconnectTimerCoroutine != null)
                {
                    StopCoroutine(disconnectTimerCoroutine);
                    disconnectTimerCoroutine = null;
                    Debug.Log("[FOCUS] Disconnect timer cancelled. App regained focus.");
                }
            }
        }

        private IEnumerator DisconnectTimer()
        {
            Debug.Log($"[FOCUS] App lost focus. Disconnect timer started for {disconnectDelay} seconds.");
            yield return new WaitForSeconds(disconnectDelay);
            Debug.Log("[FOCUS] Disconnect timer finished. Disconnecting due to prolonged inactivity.");

            // Disconnect the socket
            gameSocket?.Disconnect();

            // Show error popup to user
            uiManager?.ShowErrorPopup("You have been away from the game for too long. Please refresh to reconnect.");

            // Activate raycast blocker
            if (RaycastBlocker != null)
            {
                RaycastBlocker.SetActive(true);
            }
        }

        // ============================================
        // DATA EVENTS
        // ============================================

        private void OnInitData(string jsonData)
        {
            Debug.Log("[RESPONSE] Init data received");
            Debug.Log($"[JSON] Init Response: {jsonData}");
            ParseResponse(jsonData);
        }

        private void OnResult(string jsonData)
        {
            Debug.Log("[RESPONSE] Result data received");
            Debug.Log($"[JSON] Result Response: {jsonData}");
            ParseResponse(jsonData);
        }

        private void OnInternalError(string data)
        {
            Debug.LogError($"[SOCKET] Internal error: {data}");
            uiManager?.ShowErrorPopup("Server error occurred");
        }

        private void OnAlert(string data)
        {
            Debug.Log($"[SOCKET] Alert: {data}");
            // Handle alerts if needed
        }

        private void OnAnotherDevice(string data)
        {
            Debug.LogWarning($"[SOCKET] Another device login: {data}");
            uiManager?.ShowAnotherDevicePopup();
        }

        // ============================================
        // DATA PARSING
        // ============================================

        private void ParseResponse(string jsonData)
        {
            try
            {
                PlinkoRoot root = JsonConvert.DeserializeObject<PlinkoRoot>(jsonData);

                if (root == null)
                {
                    Debug.LogError("[PARSE] Failed to deserialize response");
                    return;
                }

                switch (root.id)
                {
                    case "initData":
                        HandleInitData(root);
                        break;

                    case "ResultData":
                        HandleResultData(root);
                        break;

                    default:
                        Debug.LogWarning($"[PARSE] Unknown response ID: {root.id}");
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PARSE] Error parsing response: {e.Message}");
                uiManager?.ShowErrorPopup("Failed to process server response");
            }
        }

        private void HandleInitData(PlinkoRoot root)
        {
            Debug.Log("[DATA] Processing init data");

            InitialData = root.gameData;
            PlayerData = root.player;

            // Stop init data timeout
            if (initDataTimeoutRoutine != null)
            {
                StopCoroutine(initDataTimeoutRoutine);
                initDataTimeoutRoutine = null;
            }

            if (!IsInitialized)
            {
                gameManager?.OnInitDataReceived();
                IsInitialized = true;

#if UNITY_WEBGL && !UNITY_EDITOR
                JSManager?.SendCustomMessage("OnEnter");
#endif
                if (RaycastBlocker != null)
                {
                    RaycastBlocker.SetActive(false);
                }
            }
            else
            {
                gameManager?.OnDataRefreshed();
            }
        }

        private void HandleResultData(PlinkoRoot root)
        {
            Debug.Log("[DATA] Processing result data");

            ResultData = root.payload;
            PlayerData = root.player;
            IsResultReady = true;

            gameManager?.OnResultReceived();
        }

        // ============================================
        // PUBLIC API
        // ============================================

        internal void ReceiveAuthToken(string jsonData)
        {
            Debug.Log($"[AUTH] Received auth data: {jsonData}");

            try
            {
                AuthTokenData data = JsonUtility.FromJson<AuthTokenData>(jsonData);
                SocketURI = data.socketURL;
                myAuth = data.cookie;
                nameSpace = data.nameSpace;

                if (string.IsNullOrEmpty(myAuth))
                {
                    Debug.LogError("[AUTH] Invalid authentication data");
                    uiManager?.ShowErrorPopup("Invalid authentication data");
                    if (RaycastBlocker != null)
                    {
                        RaycastBlocker.SetActive(true);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[AUTH] Error parsing auth data: {e.Message}");
                uiManager?.ShowErrorPopup("Authentication data format error");
                if (RaycastBlocker != null)
                {
                    RaycastBlocker.SetActive(true);
                }
            }
        }

        internal void SendBetRequest(int betIndex, int selectedRiskId, int selectedRowIndex)
        {
            IsResultReady = false;

            PlinkoBetRequest request = new PlinkoBetRequest();
            request.payload.betIndex = betIndex;
            request.payload.selectedRisk = selectedRiskId;
            request.payload.selectedRowIndex = selectedRowIndex;

            string json = JsonUtility.ToJson(request);
            Debug.Log("[REQUEST] Bet sent");
            Debug.Log($"[JSON] Bet Request: {json}");
            SendDataWithNamespace("request", json);
        }

        internal void ConsumeResult()
        {
            IsResultReady = false;
        }

        internal IEnumerator CloseSocket(bool showDisconnect = true)
        {
            isExiting = true;

            Debug.Log("[SOCKET] Closing socket...");

            if (RaycastBlocker != null)
            {
                RaycastBlocker.SetActive(true);
            }

            ResetPingRoutine();
            manager?.Close();
            manager = null;

            yield return new WaitForSeconds(0.5f);

            Debug.Log("[SOCKET] Socket closed");

#if UNITY_WEBGL && !UNITY_EDITOR
            JSManager?.SendCustomMessage("OnExit");
#endif
        }

        private void SendDataWithNamespace(string eventName, string json = null)
        {
            if (gameSocket != null && gameSocket.IsOpen)
            {
                if (json != null)
                {
                    gameSocket.Emit(eventName, json);
                }
                else
                {
                    gameSocket.Emit(eventName);
                }
            }
            else
            {
                Debug.LogWarning("[SOCKET] Socket is not connected");
            }
        }

        // ============================================
        // CLEANUP
        // ============================================

        private void OnDestroy()
        {
            ResetPingRoutine();

            if (disconnectTimerCoroutine != null)
            {
                StopCoroutine(disconnectTimerCoroutine);
                disconnectTimerCoroutine = null;
            }

            manager?.Close();
        }
    }
}