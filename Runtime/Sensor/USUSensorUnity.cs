using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Used to monotor Game's life cycle.
/// 
/// This should be used on only Unity Editor and PC platforms
/// But we use it on android for now cause we don't have enough time.
/// </summary>
public class USUSensorUnity : MonoBehaviour
{
    private void Awake()
    {
        if(USU.ReceiveErrorLog) Application.logMessageReceived += USU.HandleLog;
        if(USU.ReceiveExceptionLog) AppDomain.CurrentDomain.UnhandledException += USU.HandleUnhandledException;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnApplicationFocus(bool focus)
    {
        // StatisticsUploader.SendStatistics();
    }

    private void OnApplicationPause(bool isPause)
    {
        USU.Flush();
    }
    
    void OnDestroy()
    {
        Application.logMessageReceived -= USU.HandleLog;
        AppDomain.CurrentDomain.UnhandledException -= USU.HandleUnhandledException;
    }
}
