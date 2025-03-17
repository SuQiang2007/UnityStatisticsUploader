using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;

// #if UNITY_EDITOR
/// <summary>
/// This should be used on only Editor and PC platform
/// But we use it on all platforms for now because we don't have enough time to do that.
/// </summary>
internal partial class USUWrapper : BaseWrapper
{
	private static readonly BlockingCollection<string> _logQueue = new(new ConcurrentQueue<string>());
	private static readonly string LOG_DIRECTORY;
	private static Thread _logThread;
	private static Thread _logTimerThread;
	private static string _currentLogPath;
	private static bool _isRunning = true;
	private static bool _receiveLog = true;
	
	private static event Action FlushCalled;
	private static DateTime _lastFlushTime;


	private const string LOG_FOLDER = "usu_data";
	private const int AUTO_FLUSH_INTERVAL_SECONDS = 30;
	private const int CHECK_INTERVAL_MS = 1000;

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
		
		if(!loop) return;

		_isRunning = true;
		_logThread = new Thread(WriteLogToFile) { IsBackground = true };
		_logThread.Start();
	}

	private async Task FlushAsync()
	{
		try
		{
			string directory = Path.GetDirectoryName(_currentLogPath);
			if (directory == null) return;

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
				try
				{
					// string logEntries = await File.ReadAllTextAsync(filePath);
					// if (logEntries.Length == 0) 
					// {
					// 	File.Delete(filePath);
					// 	continue;
					// }
					Debug.Log($"Start upload file:{filePath}");
					string[] lines = await File.ReadAllLinesAsync(filePath);
					List<Dictionary<string, object>> data = new List<Dictionary<string, object>>();
					foreach (var line in lines)
					{
						data.Add(JsonConvert.DeserializeObject<Dictionary<string, object>>(line));
					}
					bool success = await StatisticsUploader.UsuConnector.SendLogsToServerAsync(data);
					
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
	private void StartAutoFlush()
	{
		if(_logTimerThread != null) return;
		_logTimerThread = new Thread(async () =>
		{
			_lastFlushTime = DateTime.Now;

			while (_isRunning)
			{
				if ((DateTime.Now - _lastFlushTime).TotalSeconds >= AUTO_FLUSH_INTERVAL_SECONDS)
				{
					try
					{
						Flush(true);
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

		FlushCalled += () => _lastFlushTime = DateTime.Now;
	}

	internal override void OnDestroy()
    {
	    try
	    {
		    _isRunning = false;
		    _logTimerThread?.Join(1000);
		    _logThread?.Join(1000);
		    // var timeout = Task.WhenAny(Task.Delay(3000), Task.Run(() =>
		    // {
			//     try
			//     {
			// 	    // 异步刷新
			// 	    var flushTask = Task.Run(() => Flush());
			// 	    if (!flushTask.Wait(2000)) // 给flush操作2秒超时
			// 	    {
			// 		    Debug.LogWarning("Final flush timed out");
			// 	    }
			//     }
			//     catch (Exception ex)
			//     {
			// 	    Debug.LogError($"Final flush failed: {ex.Message}");
			//     }
		    // }));
		    // timeout.Wait(); 
	    }
	    catch (Exception ex)
	    {
		    Debug.LogError($"Error in OnDestroy: {ex.Message}");
	    }
    }
    
	void StartThread()
	{
		string directory = Path.Combine(Application.persistentDataPath, LOG_FOLDER);
		Directory.CreateDirectory(directory); // 确保目录存在
		_logThread = new Thread(WriteLogToFile) { IsBackground = true };
		_logThread.Start();
	}

	private static void WriteLogToFile()
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
					Directory.CreateDirectory(Path.GetDirectoryName(currentFilePath));
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
}
// #endif