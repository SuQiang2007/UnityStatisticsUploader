using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public abstract class BaseConnector : MonoBehaviour
{
    internal abstract void Initialize();

    internal abstract bool IsConnectorAvailable();

    internal abstract Task<bool> SendLogsToServerAsync(List<Dictionary<string, object>> list);
}