using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// This class can be modified at will according to your business logic.
/// Its role is solely to handle the interaction with your event tracking server.
/// In this example, I use Alibaba Cloud's event tracking server.
/// </summary>
public class USUConnector : BaseConnector
{
    private string _token;
    private bool _isLoggedIn;
    
    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    internal override void Initialize()
    {
        
    }

    internal override bool IsConnectorAvailable()
    {
        return false;
    }
    
    
    internal override async Task<bool> SendLogsToServerAsync(string logs)
    {
        if (!IsConnectorAvailable())
        {
            Debug.LogWarning("No USU connector available.");
            return false;
        }
        
        Debug.LogError($"上传到服务端一条日志：{logs}");
        return true;
        // try
        // {
        //     var content = new StringContent(
        //         logs,
        //         Encoding.UTF8,
        //         "application/json"
        //     );
        //
        //     var response = await _httpClient.PostAsync("https://your-api-endpoint/logs", content);
        //
        //     if (response.IsSuccessStatusCode)
        //     {
        //         var responseContent = await response.Content.ReadAsStringAsync();
        //         return true;
        //     }
        //     else
        //     {
        //         Debug.LogError($"Server returned error: {response.StatusCode}");
        //         return false;
        //     }
        // }
        // catch (Exception ex)
        // {
        //     Debug.LogError($"Error sending logs to server: {ex.Message}");
        //     return false;
        // }
    }
}
