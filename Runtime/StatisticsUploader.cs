using System;
using System.Collections.Generic;
using UnityEngine;

public class StatisticsUploader : MonoBehaviour
{
    public static BaseConnector UsuConnector;
    private static USUWrapper _usuWrapper;
    
    //You can set these parameters anywhere
    public static bool ReceiveErrorLog = true;
    public static bool ReceiveExceptionLog = true;
    
    [HideInInspector]
    public static MonoBehaviour MonoBehaviour;

    public static void DynamicInitUsu<T>(Transform parent = null) where T : Component
    {
        GameObject usuGo = new GameObject("USUGameObject");
        if(parent != null) usuGo.transform.parent = parent;
        DontDestroyOnLoad(usuGo);
        usuGo.AddComponent<T>();
        usuGo.AddComponent<StatisticsUploader>();
        usuGo.AddComponent<USUSensorUnity>();
    }
    
    //Init logic is here
    private void Awake()
    {
        MonoBehaviour = this;
        
        UsuConnector = GetComponent<BaseConnector>();
        UsuConnector.Initialize();
        
        _usuWrapper = new USUWrapper();
        _usuWrapper.Initialize();
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        _usuWrapper.OnDestroy();
    }

    public static void SendStatistics(Dictionary<string, object> properties)
    {
        UsuConnector.DecorateData(properties);
        _usuWrapper.SendStatistics(properties);
    }

    public static void Flush()
    {
        _usuWrapper.Flush();
    }
    
    
    
    
    public static void HandleLog(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error)
        {
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
        var errorData = new Dictionary<string, object>
        {
            { "ErrorMessage", e.ToString() },
            { "StackTrace", "" }
        };
        UsuConnector.DecorateData(errorData);
        _usuWrapper.SendStatistics(errorData);
    }
}
