using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class SendStatisticsAlways : MonoBehaviour
{
    private float _timer = 0f;
    public float sendInterval = 1f;
    private USUWrapper _usuWrapper;
    
    // Start is called before the first frame update
    void Start()
    {
        USU.DynamicInitUsu<USUConnector, USUWrapperThread>();
    }

    // Update is called once per frame
    void Update()
    {
        _timer += Time.deltaTime;
        
        if (_timer >= sendInterval)
        {
            _timer = 0f; // 重置计时器
            
            // 构造测试数据
            var testData = new Dictionary<string, object>
            {
                { "timestamp", System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
                { "eventType", "test_log" },
                { "gameTime", Time.time },
                { "frameCount", Time.frameCount },
                { "randomValue", Random.Range(0, 100) }
            };
            
            // 发送统计数据
            USU.SendStatistics(testData);
        }
    }
}
