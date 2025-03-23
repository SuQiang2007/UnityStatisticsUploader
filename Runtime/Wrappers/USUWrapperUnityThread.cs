using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;
using System.Threading.Tasks;
using Newtonsoft.Json;

// #if UNITY_EDITOR
/// <summary>
/// This should be used on only Editor and PC platform
/// But we use it on all platforms for now because we don't have enough time to do that.
/// </summary>
internal class USUWrapperThread : BaseWrapper
{
	private readonly BlockingCollection<string> _logQueue = new(new ConcurrentQueue<string>());
	private Thread _logThread;
	private Thread _logTimerThread;
	private string _currentLogPath;
	private bool _isRunning = true;
	
	private event Action FlushCalled;
	private DateTime _lastFlushTime;
	private bool _forceFlush;
	
	private string LOG_DIRECTORY;
	private const string LOG_FOLDER = "usu_data";
	private const int AUTO_FLUSH_INTERVAL_SECONDS = 30;
	private const int CHECK_INTERVAL_MS = 1000;

	internal override void Initialize()
	{
		LOG_DIRECTORY = Path.Combine(Application.persistentDataPath, LOG_FOLDER);
		Directory.CreateDirectory(LOG_DIRECTORY);
		StartLogThread();
		StartAutoFlushThread();
	}

	internal override void SendStatistics(Dictionary<string, object> properties)
    {
        string logStr = GetLogStringFromDictionary(properties);
        _logQueue.Add(logStr.TrimEnd(',', ' '));
    }

	private string GetLogStringFromDictionary(Dictionary<string, object> properties)
	{
		return JsonConvert.SerializeObject(properties);
	}

	internal override void Flush()
	{
		_forceFlush = true;
	} 
	
	private void DoFlush()
	{
		_isRunning = false; // Stop the log thread
		_logThread?.Join(); // Wait for the log thread to finish

		// Save the log file if necessary
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
				while (_logQueue.TryTake(out var logEntry))
				{
					writer.WriteLine(logEntry);
				}
				writer.Flush();
			}
		}
		FlushAsync().Wait(); // 等待异步操作完成
		FlushCalled?.Invoke(); // 触发事件

		_isRunning = true;
		StartLogThread();
	}

	private async Task FlushAsync()
	{
		try
		{
			string directory = Path.GetDirectoryName(_currentLogPath);
			if (directory == null) return;

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
				try
				{
					Debug.Log($"Start upload file:{filePath}");
					string[] lines = await File.ReadAllLinesAsync(filePath);
					List<Dictionary<string, object>> data = new List<Dictionary<string, object>>();
					if (lines.Length == 0) // 检查是否有行
					{
						File.Delete(filePath);
						Debug.Log($"File is EMPTY and deleted log file: {Path.GetFileName(filePath)}");
						continue;
					}
					foreach (var line in lines)
					{
						data.Add(JsonConvert.DeserializeObject<Dictionary<string, object>>(line));
					}
					bool success = await USU.UsuConnector.SendLogsToServerAsync(data, filePath);
					
					if (success)
					{
						File.Delete(filePath);
						Debug.Log($"Successfully processed and deleted log file: {Path.GetFileName(filePath)}");
					}
					else
					{
						Debug.LogWarning($"Failed to send logs from file: {Path.GetFileName(filePath)}");
					}
				}
				catch (Exception ex)
				{
					Debug.LogError($"Error processing file {Path.GetFileName(filePath)}: {ex.Message}");
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogError($"Error in Flush: {ex.Message}");
		}
	}


	// 自动刷新的辅助方法
	private void StartAutoFlushThread()
	{
		if(_logTimerThread != null) return;
		_logTimerThread = new Thread(async () =>
		{
			_lastFlushTime = DateTime.Now;

			while (_isRunning)
			{
				if (_forceFlush || (DateTime.Now - _lastFlushTime).TotalSeconds >= AUTO_FLUSH_INTERVAL_SECONDS)
				{
					try
					{
						DoFlush();
					}
					catch (Exception ex)
					{
						Debug.LogError($"Auto flush failed: {ex.Message}");
					}
				}
				await Task.Delay(CHECK_INTERVAL_MS);
			}
		})
		{
			IsBackground = true,
			Name = "AutoFlushThread" // 添加线程名称便于调试
		};
		_logTimerThread.Start();

		FlushCalled += () =>
		{
			_forceFlush = false;
			_lastFlushTime = DateTime.Now;
		};
	}

	internal override void OnDestroy()
    {
	    try
	    {
		    _isRunning = false;
		    _logTimerThread?.Join(1000);
		    _logThread?.Join(1000);
	    }
	    catch (Exception ex)
	    {
		    Debug.LogError($"Error in OnDestroy: {ex.Message}");
	    }
    }

	private void WriteLogToFile()
	{
		const int maxEntriesPerFile = 50;
		int currentEntryCount = 0;
		string currentFilePath = null;
		
		while (_isRunning || _logQueue.Count > 0)
		{
			try
			{
				// 当达到50条记录或者是第一次运行时，创建新文件
				if (currentEntryCount >= maxEntriesPerFile || currentFilePath == null)
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
					Directory.CreateDirectory(Path.GetDirectoryName(_currentLogPath));
				}

				using (StreamWriter writer = new StreamWriter(currentFilePath, true))
				{
					if (_logQueue.TryTake(out string log, 500)) // 尝试获取日志，最多等待 500ms
					{
						writer.WriteLine(log);
						writer.Flush();
						currentEntryCount++;
					}
				}
			}
			catch (Exception ex)
			{
				Debug.LogError($"写入日志文件失败: {ex.Message}");
				Thread.Sleep(1000); // 发生错误时等待一秒再继续
				currentFilePath = null; // 发生错误时重置文件路径，下次将创建新文件
			}
		}
	}

	private void StartLogThread()
	{
		_logThread = new Thread(WriteLogToFile) { IsBackground = true };
		_logThread.Start();
	}
}
// #endif