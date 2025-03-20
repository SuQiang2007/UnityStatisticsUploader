using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using System.Threading;

/// <summary>
/// This class can be modified at will according to your business logic.
/// Its role is solely to handle the interaction with your event tracking server.
/// In this example, I use Alibaba Cloud's event tracking server.
/// </summary>
public class USUConnector : BaseConnector
{
    private string _token;
    private bool _isLoggedIn;

    internal override void Initialize()
    {
        Debug.Log("Initializing USU Connector");
    }

    internal override bool IsConnectorAvailable()
    {
        return false;
    }

    internal override IEnumerator SendLogsToServer(List<Dictionary<string, object>> list, string filePath, Action<bool> callback)
    {
        throw new NotImplementedException();
    }

    internal override void DecorateData(Dictionary<string, object> data)
    {
        data.Add("id", Guid.NewGuid().ToString());
    }
}
