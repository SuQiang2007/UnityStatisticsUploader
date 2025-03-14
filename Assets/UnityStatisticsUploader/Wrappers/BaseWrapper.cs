using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal abstract class BaseWrapper
{
    internal abstract void Initialize();
    internal abstract void SendStatistics(Dictionary<string, object> properties);

    internal abstract void Flush();
    internal abstract void OnDestroy();
}
