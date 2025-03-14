using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeMove : MonoBehaviour
{
    [SerializeField] private float amplitude = 5f; // 移动幅度
    [SerializeField] private float speed = 2f;     // 移动速度
    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position; // 记住初始位置
    }

    void Update()
    {
        float newX = startPosition.x + Mathf.Sin(Time.time * speed) * amplitude;
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);
    }
}
