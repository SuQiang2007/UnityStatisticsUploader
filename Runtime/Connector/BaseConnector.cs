using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public abstract class BaseConnector : MonoBehaviour
{
    internal abstract void Initialize();

    internal abstract bool IsConnectorAvailable();

    internal abstract IEnumerator SendLogsToServer(List<Dictionary<string, object>> list, string filePath, Action<bool> callback);

    //Every developer will have their own data rules(such as some public attributes like id, timestamp ex), you can apply them with override DecorateData
    internal abstract void DecorateData(Dictionary<string, object> data);
    
    protected MonoBehaviour GetMonoBehaviourInstance()
    {
        return StatisticsUploader.MonoBehaviour;
    }
}
