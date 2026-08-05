using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// JavaScript bridge for WebGL communication
/// WebGL iframe host bridge version
/// </summary>
public class JSFunctCalls : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern void SendPostMessage(string message);

    [DllImport("__Internal")]
    private static extern void RegisterVisibilityChangeListener(string gameObjectName);

    [DllImport("__Internal")]
    private static extern void RegisterResizeListener(string gameObjectName, string methodName);

    [DllImport("__Internal")]
    private static extern void RegisterTokenListener(string gameObjectName, string methodName);

    private void Start()
    {
        RegisterDimensionsListener();
    }

    internal void SendCustomMessage(string message)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.Log($"[JS] Sending message to platform: {message}");
        SendPostMessage(message);
#else
        Debug.Log($"[JS] Would send message (editor mode): {message}");
#endif
    }

    internal void RegisterVisibilityListener(string gameObjectName)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.Log($"[JS] Registering visibility change listener on '{gameObjectName}'");
        RegisterVisibilityChangeListener(gameObjectName);
#else
        Debug.Log("[JS] Visibility listener not registered (editor mode)");
#endif
    }

    // Self-contained resize bridge: the page drives <OC_GO>.<OC_METHOD>("width,height") on its own resize.
    internal void RegisterDimensionsListener(string gameObjectName = "OC", string methodName = "SwitchDisplay")
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.Log($"[JS] Registering resize listener for '{gameObjectName}.{methodName}'");
        RegisterResizeListener(gameObjectName, methodName);
#else
        Debug.Log($"[JS] Resize listener not registered ('{gameObjectName}.{methodName}', editor mode)");
#endif
    }

    // Inbound auth: routes the host's "TokenReceived" message to gameObjectName.methodName(json).
    internal void RegisterAuthTokenListener(string gameObjectName, string methodName = "ReceiveAuthToken")
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.Log($"[JS] Registering auth token listener for '{gameObjectName}.{methodName}'");
        RegisterTokenListener(gameObjectName, methodName);
#else
        Debug.Log($"[JS] Token listener not registered ('{gameObjectName}.{methodName}', editor mode)");
#endif
    }
}