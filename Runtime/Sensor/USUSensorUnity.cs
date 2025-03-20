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
        if(StatisticsUploader.ReceiveErrorLog) Application.logMessageReceived += StatisticsUploader.HandleLog;
        if(StatisticsUploader.ReceiveExceptionLog) AppDomain.CurrentDomain.UnhandledException += StatisticsUploader.HandleUnhandledException;
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
        StatisticsUploader.Flush();
    }
    
    void OnDestroy()
    {
        Application.logMessageReceived -= StatisticsUploader.HandleLog;
        AppDomain.CurrentDomain.UnhandledException -= StatisticsUploader.HandleUnhandledException;
    }
}
