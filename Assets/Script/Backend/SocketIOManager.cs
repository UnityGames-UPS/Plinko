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
    /// Handles all Socket.IO communication for Plinko game
    /// </summary>
    public class SocketIOManager : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private UIManager uiManager;
        [SerializeField] internal JSFunctCalls JSManager;

        [Header("Testing (Editor Only)")]
        [SerializeField] private string testToken;

        [Header("Blocker")]
        [SerializeField] private GameObject RaycastBlocker;

        [Header("Settings")]
        [SerializeField] private float disconnectDelay = 60f;
        #endregion

        #region Public Properties
        internal PlinkoGameData InitialData { get; private set; }
        internal PlinkoPlayer PlayerData { get; private set; }
        internal PlinkoResultPayload ResultData { get; private set; }
        internal bool IsResultReady { get; private set; }
        internal bool IsInitialized { get; private set; }
        #endregion

        #region Private Fields
        private SocketManager manager;
        private Socket gameSocket;
        private string SocketURI = null;
        private const string TestSocketURI = "https://devrealtime.dingdinghouse.com/";
        private string nameSpace = "playground";
        private string myAuth = null;

        private bool isConnected = false;
        private bool hasEverConnected = false;
        private bool isExiting = false;
        private bool isWaitingForInitData = false;

        private float lastPongTime = 0f;
        private const float pingInterval = 2f;
        private bool waitingForPong = false;
        private int missedPongs = 0;
        private const int MaxMissedPongs = 5;

        private bool hasFocus = true;
        private float focusLostTime = 0f;
        private const float maxBackgroundTime = 120f; // 2 minutes

        private Coroutine PingRoutine;
        private Coroutine initTimeoutRoutine;
        private Coroutine disconnectTimerCoroutine;
        private Coroutine focusCheckRoutine;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            IsInitialized = false;
            IsResultReady = false;
        }

        private void Start()
        {
            if (!ValidateToken()) return;
            OpenSocket();
        }

        private void OnDestroy()
        {
            Debug.Log("[CLEANUP] SocketIOManager destroying");
            CleanupRoutines();
            manager?.Close();
            manager = null;
        }

        private void OnApplicationFocus(bool focus)
        {
            hasFocus = focus;

            if (!focus)
            {
                focusLostTime = Time.time;
                Debug.Log("[FOCUS] Application lost focus");

                if (focusCheckRoutine == null)
                {
                    focusCheckRoutine = StartCoroutine(FocusTimeoutCheck());
                }
            }
            else
            {
                Debug.Log("[FOCUS] Application gained focus");

                if (focusCheckRoutine != null)
                {
                    StopCoroutine(focusCheckRoutine);
                    focusCheckRoutine = null;
                }
            }
        }
        #endregion

        #region Validation
        private bool ValidateToken()
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(testToken) || testToken.Length < 10)
            {
                Debug.LogError("[VALIDATION] Invalid test token");
                ShowErrorAndBlock("Test token is required in editor mode");
                return false;
            }
            Debug.Log("[VALIDATION] Token validated");
            return true;
#else
            return true;
#endif
        }
        #endregion

        #region Socket Connection
        private void OpenSocket()
        {
            Debug.Log("[SOCKET] Opening connection");
            RaycastBlocker?.SetActive(true);

            SocketOptions options = new SocketOptions
            {
                AutoConnect = false,
                Reconnection = false,
                Timeout = TimeSpan.FromSeconds(5),
                ConnectWith = Best.SocketIO.Transports.TransportTypes.WebSocket
            };

#if UNITY_WEBGL && !UNITY_EDITOR
            JSManager.SendCustomMessage("authToken");
            StartCoroutine(WaitForAuthToken(options));
#else
            options.Auth = (Best.SocketIO.SocketManager manager, Socket socket) => new { token = testToken };
            SetupSocketManager(options);
#endif
        }

        private IEnumerator WaitForAuthToken(SocketOptions options)
        {
            float timeout = 15f;
            float elapsed = 0f;

            while (myAuth == null && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (myAuth == null)
            {
                Debug.LogError("[AUTH] Token timeout");
                ShowErrorAndBlock("Authentication failed. Please refresh the page.");
                yield break;
            }

            elapsed = 0f;
            while (SocketURI == null && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (SocketURI == null)
            {
                Debug.LogError("[AUTH] URI timeout");
                ShowErrorAndBlock("Connection configuration failed. Please refresh.");
                yield break;
            }

            Debug.Log($"[AUTH] Authenticated: {myAuth.Substring(0, Math.Min(20, myAuth.Length))}...");
            options.Auth = (Best.SocketIO.SocketManager manager, Socket socket) => new { token = myAuth };
            SetupSocketManager(options);
        }

        private void SetupSocketManager(SocketOptions options)
        {
            Debug.Log("[SOCKET] Setting up manager");

#if UNITY_EDITOR
            this.manager = new Best.SocketIO.SocketManager(new Uri(TestSocketURI), options);
#else
            this.manager = new Best.SocketIO.SocketManager(new Uri(SocketURI), options);
#endif

            gameSocket = string.IsNullOrEmpty(nameSpace) ?
                this.manager.Socket :
                this.manager.GetSocket("/" + nameSpace);

            RegisterEventHandlers();
            manager.Open();

            initTimeoutRoutine = StartCoroutine(ConnectionAndInitTimeout());
        }

        private void RegisterEventHandlers()
        {
            gameSocket.On<ConnectResponse>(SocketIOEventTypes.Connect, OnConnected);
            gameSocket.On(SocketIOEventTypes.Disconnect, OnDisconnected);
            gameSocket.On<Error>(SocketIOEventTypes.Error, OnError);
            gameSocket.On<string>("game:init", OnInitData);
            gameSocket.On<string>("result", OnResult);
            gameSocket.On<string>("pong", OnPongReceived);
            gameSocket.On<string>("internalError", OnInternalError);
            gameSocket.On<string>("alert", OnAlert);
            gameSocket.On<string>("AnotherDevice", OnAnotherDevice);
        }

        private IEnumerator ConnectionAndInitTimeout()
        {
            float connectionTimeout = 15f;
            float initTimeout = 10f;
            float elapsed = 0f;

            // Wait for connection
            while (!isConnected && elapsed < connectionTimeout && !isExiting)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!isConnected && !isExiting)
            {
                Debug.LogError("[SOCKET] Connection timeout");
                ShowErrorAndBlock("Connection failed. Please check your network.");
                yield break;
            }

            // Wait for init data
            elapsed = 0f;
            while (isWaitingForInitData && elapsed < initTimeout && !isExiting)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (isWaitingForInitData && !isExiting)
            {
                Debug.LogError("[INIT] Init data timeout");
                ShowErrorAndBlock("Failed to receive game data. Please refresh.");
            }

            initTimeoutRoutine = null;
        }
        #endregion

        #region Connection Events
        private void OnConnected(ConnectResponse resp)
        {
            Debug.Log("[CONNECTION] Connected");

            if (initTimeoutRoutine != null)
            {
                StopCoroutine(initTimeoutRoutine);
                initTimeoutRoutine = null;
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

            SendPing();

            if (!IsInitialized)
            {
                isWaitingForInitData = true;
                initTimeoutRoutine = StartCoroutine(ConnectionAndInitTimeout());
            }
        }

        private void OnDisconnected()
        {
            if (isExiting)
            {
                Debug.Log("[CONNECTION] Disconnected (intentional)");
                return;
            }

            Debug.LogWarning("[CONNECTION] Disconnected");
            isConnected = false;
            ResetPingRoutine();
            uiManager?.ShowDisconnectionPopup();

            if (disconnectTimerCoroutine == null)
            {
                disconnectTimerCoroutine = StartCoroutine(DisconnectTimer());
            }
        }

        private void OnError(Error err)
        {
            Debug.LogError($"[SOCKET] Error: {err}");
#if UNITY_WEBGL && !UNITY_EDITOR
            JSManager?.SendCustomMessage("error");
#endif
            uiManager?.ShowErrorPopup("Connection error occurred");
        }
        #endregion

        #region Ping/Pong System
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
            while (isConnected && !isExiting)
            {
                if (missedPongs == 0 && hasEverConnected)
                {
                    uiManager?.CheckAndClosePopups();
                }

                if (waitingForPong)
                {
                    missedPongs++;
                    Debug.LogWarning($"[PING] Missed pong #{missedPongs}/{MaxMissedPongs}");

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
                SendDataWithNamespace("ping");
                yield return new WaitForSeconds(pingInterval);
            }
        }

        private void OnPongReceived(string data)
        {
            waitingForPong = false;
            missedPongs = 0;
            lastPongTime = Time.time;

            if (hasEverConnected)
            {
                uiManager?.CheckAndClosePopups();
            }
        }
        #endregion

        #region Disconnect Timer
        private IEnumerator DisconnectTimer()
        {
            Debug.Log($"[DISCONNECT] Timer started ({disconnectDelay}s)");
            float elapsed = 0f;

            while (elapsed < disconnectDelay && !isConnected && !isExiting)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!isConnected && !isExiting)
            {
                Debug.LogError("[DISCONNECT] Timeout - forcing exit");
                ShowErrorAndBlock("You have been away too long. Please refresh.");
            }

            disconnectTimerCoroutine = null;
        }
        #endregion

        #region Focus Check
        private IEnumerator FocusTimeoutCheck()
        {
            Debug.Log($"[FOCUS] Starting background timeout check ({maxBackgroundTime}s)");

            while (!hasFocus && !isExiting)
            {
                float timeInBackground = Time.time - focusLostTime;

                if (timeInBackground >= maxBackgroundTime)
                {
                    Debug.LogError("[FOCUS] App in background too long - disconnecting");

                    // Disconnect socket
                    isConnected = false;
                    ResetPingRoutine();
                    manager?.Close();

                    ShowErrorAndBlock("Game timed out due to inactivity. Please refresh.");
                    focusCheckRoutine = null;
                    yield break;
                }

                yield return new WaitForSeconds(1f);
            }

            Debug.Log("[FOCUS] Focus regained or game exiting");
            focusCheckRoutine = null;
        }
        #endregion

        #region Data Events
        private void OnInitData(string jsonData)
        {
            Debug.Log("[DATA] Init data received");
            isWaitingForInitData = false;

            if (initTimeoutRoutine != null)
            {
                StopCoroutine(initTimeoutRoutine);
                initTimeoutRoutine = null;
            }

            ParseResponse(jsonData);
        }

        private void OnResult(string jsonData)
        {
            Debug.Log("[DATA] Result received");
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
        }

        private void OnAnotherDevice(string data)
        {
            Debug.LogWarning($"[SOCKET] Another device: {data}");
            uiManager?.ShowAnotherDevicePopup();
        }
        #endregion

        #region Data Parsing
        private void ParseResponse(string jsonData)
        {
            try
            {
                PlinkoRoot root = JsonConvert.DeserializeObject<PlinkoRoot>(jsonData);

                if (root == null)
                {
                    Debug.LogError("[PARSE] Null response");
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
                        Debug.LogWarning($"[PARSE] Unknown ID: {root.id}");
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PARSE] Error: {e.Message}");
                uiManager?.ShowErrorPopup("Failed to process server response");
            }
        }

        private void HandleInitData(PlinkoRoot root)
        {
            Debug.Log("[HANDLER] Processing init data");

            InitialData = root.gameData;
            PlayerData = root.player;

            if (!IsInitialized)
            {
                gameManager?.OnInitDataReceived();
                IsInitialized = true;

#if UNITY_WEBGL && !UNITY_EDITOR
                Debug.Log("[PLATFORM] Sending OnEnter");
                JSManager?.SendCustomMessage("OnEnter");
#endif

                RaycastBlocker?.SetActive(false);
                Debug.Log("[UI] Game ready");
            }
            else
            {
                gameManager?.OnDataRefreshed();
            }
        }

        private void HandleResultData(PlinkoRoot root)
        {
            Debug.Log("[HANDLER] Processing result");

            ResultData = root.payload;
            PlayerData = root.player;
            IsResultReady = true;

            gameManager?.OnResultReceived();
        }
        #endregion

        #region Public API
        internal void ReceiveAuthToken(string jsonData)
        {
            Debug.Log("[AUTH] Received auth data");

            try
            {
                AuthTokenData data = JsonUtility.FromJson<AuthTokenData>(jsonData);
                SocketURI = data.socketURL;
                myAuth = data.cookie;
                nameSpace = data.nameSpace;

                if (string.IsNullOrEmpty(myAuth))
                {
                    Debug.LogError("[AUTH] Empty token");
                    ShowErrorAndBlock("Invalid authentication data");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[AUTH] Parse error: {e.Message}");
                ShowErrorAndBlock("Authentication data format error");
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
            Debug.Log("[REQUEST] Sending bet");
            SendDataWithNamespace("request", json);
        }

        internal void ConsumeResult()
        {
            IsResultReady = false;
        }

        internal IEnumerator CloseSocket()
        {
            isExiting = true;
            Debug.Log("[SOCKET] Closing");

            RaycastBlocker?.SetActive(true);
            CleanupRoutines();

            manager?.Close();
            manager = null;

            yield return new WaitForSeconds(0.5f);

#if UNITY_WEBGL && !UNITY_EDITOR
            Debug.Log("[PLATFORM] Sending OnExit");
            JSManager?.SendCustomMessage("OnExit");
#endif
        }
        #endregion

        #region Private Helpers
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
                Debug.LogWarning($"[EMIT] Socket not connected for '{eventName}'");
            }
        }

        private void ShowErrorAndBlock(string message)
        {
            uiManager?.ShowErrorPopup(message);
            RaycastBlocker?.SetActive(true);
        }

        private void CleanupRoutines()
        {
            ResetPingRoutine();

            if (initTimeoutRoutine != null)
            {
                StopCoroutine(initTimeoutRoutine);
                initTimeoutRoutine = null;
            }

            if (disconnectTimerCoroutine != null)
            {
                StopCoroutine(disconnectTimerCoroutine);
                disconnectTimerCoroutine = null;
            }

            if (focusCheckRoutine != null)
            {
                StopCoroutine(focusCheckRoutine);
                focusCheckRoutine = null;
            }
        }
        #endregion
    }
}