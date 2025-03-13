using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "USUConfig", menuName = "ScriptableObjects/USUConfig")]
public class UsuConfigScriptable : ScriptableObject
{
    [Header("USU Configuration")]
    [Tooltip("缓存文件名称，用于存储待上传的统计数据")]
    public string CacheFileName = "usu_cache.json";
}
