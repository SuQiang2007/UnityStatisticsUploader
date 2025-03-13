using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseWrapper
{
    public abstract void Initialize();
    public abstract void SendStatistics(Dictionary<string, object> properties);

    public abstract void Flush();
    public abstract void OnDestroy();
}
