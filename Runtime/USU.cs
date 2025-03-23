using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public class USU : MonoBehaviour
{
    public static BaseConnector UsuConnector;
    private static BaseWrapper _usuWrapper;
    
    //You can set these parameters anywhere
    public static bool ReceiveErrorLog = true;
    public static bool ReceiveExceptionLog = true;
    public static bool UsuEnabled = true;
    public static bool SendLog = true;
    
    [HideInInspector]
    public static MonoBehaviour MonoBehaviour;

    
    public static void DynamicInitUsu<T1,T2>(Transform parent = null) where T1 : Component where T2 : Component
    {
        GameObject usuGo = new GameObject("USUGameObject");
        if(parent != null) usuGo.transform.parent = parent;
        DontDestroyOnLoad(usuGo);
        usuGo.AddComponent<T1>();
        usuGo.AddComponent<T2>();
        usuGo.AddComponent<USU>();
        usuGo.AddComponent<USUSensorUnity>();
    }
    
    //Init logic is here
    private void Awake()
    {
        MonoBehaviour = this;
        if (!UsuEnabled)
        {
            LogWarning("Usu Enabled is false, USU will not be executed at runtime.");
            return;
        }
        
        Log("Connector Initialized");
        UsuConnector = GetComponent<BaseConnector>();
        UsuConnector.Initialize();
        
        Log("Logic wrapper Initialized");
        _usuWrapper = GetComponent<BaseWrapper>();
        _usuWrapper.Initialize();
        
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        Log("======== Destroyed ========");
        _usuWrapper.OnDestroy();
    }

    public static void SendStatistics(Dictionary<string, object> properties)
    {
        if(!UsuEnabled) return;
        Log($"Sending Statistics :{JsonConvert.SerializeObject(properties)}");
        UsuConnector.DecorateData(properties);
        _usuWrapper.SendStatistics(properties);
    }

    public static void Flush()
    {
        if(!UsuEnabled) return;
        Log("Flush");
        _usuWrapper.Flush();
    }
    
    
    
    
    public static void HandleLog(string condition, string stackTrace, LogType type)
    {
        if(!UsuEnabled) return;
        if (type == LogType.Exception || type == LogType.Error)
        {
            Log("Handling Log");
            var errorData = new Dictionary<string, object>
            {
                { "ErrorMessage", condition },
                { "StackTrace", stackTrace }
            };
            UsuConnector.DecorateData(errorData);
            _usuWrapper.SendStatistics(errorData);
        }
    }

    public static void HandleUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if(!UsuEnabled) return;
        Log("Handling Unhandled Exception");
        var errorData = new Dictionary<string, object>
        {
            { "ErrorMessage", e.ToString() },
            { "StackTrace", "" }
        };
        UsuConnector.DecorateData(errorData);
        _usuWrapper.SendStatistics(errorData);
    }

    internal static void Log(string content)
    {
        if(SendLog) Debug.Log($"USU:{content}");
    }

    internal static void LogError(string content)
    {
        if(SendLog) Debug.LogError($"USU:{content}");
    }

    internal static void LogWarning(string content)
    {
        if(SendLog) Debug.LogWarning($"USU:{content}");
    }
}
