using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatisticsUploader : MonoBehaviour
{
    public static UsuConfigScriptable UsuConfig;
    public static USUConnector UsuConnector;
    private static USUWrapper _usuWrapper;
    //Init logic is here
    private void Awake()
    {
        UsuConnector = GetComponent<USUConnector>();
        _usuWrapper = new USUWrapper();
        _usuWrapper.Initialize();
        DontDestroyOnLoad(gameObject);
        
        Application.logMessageReceived += HandleLog;
        AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;
    }

    private void OnDestroy()
    {
        _usuWrapper.OnDestroy();
    }

    public static void SendStatistics(Dictionary<string, object> properties)
    {
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
        _usuWrapper.SendStatistics(errorData);
    }
}
