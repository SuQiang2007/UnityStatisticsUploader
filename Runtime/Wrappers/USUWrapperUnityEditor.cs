using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections;

// #if UNITY_EDITOR
/// <summary>
/// This should be used on only Editor and PC platform
/// But we use it on all platforms for now because we don't have enough time to do that.
/// </summary>
internal partial class USUWrapper : BaseWrapper
{
	private static readonly List<string> _logQueue = new List<string>();
	private static readonly string LOG_DIRECTORY;
	private static string _currentLogPath;
	private static bool _isRunning = true;
	private static bool _receiveLog = true;
	
	private static event Action FlushCalled;
	private static DateTime _lastFlushTime;

	private Coroutine _timerCoroutine;
	private Coroutine _writeCoroutine;

	private const string LOG_FOLDER = "usu_data";
	private const int AUTO_FLUSH_INTERVAL_SECONDS = 30;
	private const int MAX_COUNT_PER_FILE = 60;
	private const int CHECK_INTERVAL_S = 1;

	static USUWrapper()
	{
		LOG_DIRECTORY = Path.Combine(Application.persistentDataPath, LOG_FOLDER);
		Directory.CreateDirectory(LOG_DIRECTORY);
	}

	internal override void Initialize()
	{
		StartThread();
		StartAutoFlush();
	}

	internal override void SendStatistics(Dictionary<string, object> properties)
    {
	    if(!_receiveLog) return;
	    
        string logStr = GetLogStringFromDictionary(properties);
        _logQueue.Add(logStr.TrimEnd(',', ' '));
    }

	private string GetLogStringFromDictionary(Dictionary<string, object> properties)
	{
		return JsonConvert.SerializeObject(properties);
	}

	internal override void Flush()
	{
		Flush(true);
	}
	
	private void Flush(bool loop = false)
	{
		_receiveLog = loop;
		_isRunning = false;

		if (_currentLogPath != null)
		{
			if (!File.Exists(_currentLogPath))
			{
				Debug.LogWarning($"Log file does not exist: {_currentLogPath}");
				_currentLogPath = null;
			}
			else
			{
				using var writer = new StreamWriter(_currentLogPath, true);
				foreach (var logEntry in _logQueue)
				{
					writer.WriteLine(logEntry);
				}
				writer.Flush();
			}
		}
		
		// 启动协程
		MonoBehaviour monoBehaviour = GetMonoBehaviourInstance();
		if (monoBehaviour != null)
		{
			monoBehaviour.StartCoroutine(FlushCoroutine());
		}
		
		FlushCalled?.Invoke();
		
		if(!loop) return;

		_isRunning = true;
	}

	private IEnumerator FlushCoroutine()
	{
		string directory = Path.GetDirectoryName(_currentLogPath);
		if (directory == null) yield break;

		// 获取所有日志文件
		var logFiles = Directory.GetFiles(directory, "*.txt")
			.Where(f => Path.GetFileName(f).Length > 15)
			.Where(f => 
			{
				var fileName = Path.GetFileName(f);
				return fileName.Contains("_") && 
					   DateTime.TryParseExact(
						   fileName.Split('_')[0], 
						   "yyyyMMddHHmmss", 
						   null, 
						   System.Globalization.DateTimeStyles.None, 
						   out _
					   );
			})
			.OrderBy(f => Path.GetFileName(f).Split('_')[0])
			.ToList();

		foreach (var filePath in logFiles)
		{
			string[] lines = File.ReadAllLines(filePath);
			List<Dictionary<string, object>> data = new List<Dictionary<string, object>>();
			foreach (var line in lines)
			{
				data.Add(JsonConvert.DeserializeObject<Dictionary<string, object>>(line));
			}

			yield return GetMonoBehaviourInstance().StartCoroutine(USU.UsuConnector.SendLogsToServer(data, filePath, success =>
			{
				if (success)
				{
					File.Delete(filePath);
					Debug.Log($"Successfully processed and deleted log file: {filePath}");
				}
				else
				{
					Debug.LogWarning($"Failed to send logs from file: {filePath}");
				}
			}));
			
			// 每处理完一个文件暂停一帧，避免卡顿
			yield return null;
		}
	}

	private IEnumerator StartAutoFlushCoroutine()
	{
		_lastFlushTime = DateTime.Now;

		while (_isRunning)
		{
			var aaa  = (DateTime.Now - _lastFlushTime).TotalSeconds;
			if (aaa >= AUTO_FLUSH_INTERVAL_SECONDS)
			{
				_lastFlushTime = DateTime.Now;
				try
				{
					Flush(true);
				}
				catch (Exception ex)
				{
					Debug.LogError($"Auto flush failed: {ex.Message}");
				}
			}
			yield return new WaitForSeconds(CHECK_INTERVAL_S); // 转换为秒
		}
	}

	// 修改 StartAutoFlush 方法
	private void StartAutoFlush()
	{
		// 启动协程
		MonoBehaviour monoBehaviour = GetMonoBehaviourInstance();
		if (monoBehaviour != null && _timerCoroutine == null)
		{
			 _timerCoroutine = monoBehaviour.StartCoroutine(StartAutoFlushCoroutine());
		}
	}

	internal override void OnDestroy()
    {
	    try
	    {
		    GetMonoBehaviourInstance().StopCoroutine(_writeCoroutine);
		    GetMonoBehaviourInstance().StopCoroutine(_timerCoroutine);
		    _isRunning = false;
	    }
	    catch (Exception ex)
	    {
		    Debug.LogError($"Error in OnDestroy: {ex.Message}");
	    }
    }
    
	private IEnumerator WriteLogToFileCoroutine()
	{
		int currentEntryCount = 0;
		string currentFilePath = null;

		while (_isRunning || _logQueue.Count > 0)
		{
			try
			{
				// 当达到MAX_COUNT_PER_FILE条记录或者是第一次运行时，创建新文件
				if (currentEntryCount >= MAX_COUNT_PER_FILE || currentFilePath == null)
				{
					currentEntryCount = 0;
					// 生成时间戳_GUID格式的文件名
					string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
					string guid = Guid.NewGuid().ToString("N"); // N格式移除了破折号
					string fileName = $"{timestamp}_{guid}.txt";

					currentFilePath = Path.Combine(
						LOG_DIRECTORY,
						fileName
					);

					_currentLogPath = currentFilePath;
					// 确保目录存在
					Directory.CreateDirectory(Path.GetDirectoryName(currentFilePath) ?? string.Empty);
				}

				using (StreamWriter writer = new StreamWriter(currentFilePath, true))
				{
					if (_logQueue.Count > 0) // 检查 List 是否有日志
					{
						writer.WriteLine(_logQueue[0]); // 写入第一个日志
						_logQueue.RemoveAt(0); // 从 List 中移除已写入的日志
						currentEntryCount++;
					}
				}
			}
			catch (Exception ex)
			{
				Debug.LogError($"写入日志文件失败: {ex.Message}");
				currentFilePath = null; // 发生错误时重置文件路径，下次将创建新文件
			}

			yield return null; // 每次循环暂停一帧，避免卡顿
		}
	}

	// 修改 StartThread 方法
	private void StartThread()
	{
		string directory = Path.GetDirectoryName(LOG_DIRECTORY);
		Directory.CreateDirectory(directory);
		// 启动协程
		MonoBehaviour monoBehaviour = GetMonoBehaviourInstance();
		if (monoBehaviour != null && _writeCoroutine == null)
		{
			_writeCoroutine = monoBehaviour.StartCoroutine(WriteLogToFileCoroutine());
		}
	}

	// 添加获取 MonoBehaviour 实例的方法
	private MonoBehaviour GetMonoBehaviourInstance()
	{
		return USU.MonoBehaviour;
	}
}
// #endif