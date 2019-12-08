using System.Collections.Generic;
using UnityEngine;
using System;

namespace Backlog.BugReporter
{
	public static class LogRecorder
	{
		/// <summary>
		/// ログデータ
		/// </summary>
		private struct LogData
		{
			/// <summary>
			/// メッセージ
			/// </summary>
			public string Message;
			/// <summary>
			/// スタックトレース
			/// </summary>
			public string StackTrace;
			/// <summary>
			/// ログタイプ
			/// </summary>
			public LogType Type;
			/// <summary>
			/// 最終発生日時
			/// </summary>
			public DateTime LastDate;
		}

		// 最大保持件数（重複は排除した後）
		private const int maxLog = 32;

		// ログリスト
		private static List<LogData> logList = new List<LogData>(maxLog);
		
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] 
		private static void RegisterReceiveLog()
		{
			Application.logMessageReceived += ReceiveLog;
		}

		private static void ReceiveLog(string logMessage, string logStackTrace, LogType logType)
		{
			// 通常のログは無視
			if (logType == LogType.Log)
			{
				return;
			}

			// 既に追加されているものと同じなら最終発生時間だけ更新
			for (int i = 0; i < logList.Count; ++i)
			{
				var log = logList[i];
				if (log.Type == logType &&
					log.Message.Equals(logMessage) &&
					log.StackTrace.Equals(logStackTrace))
				{
					log.LastDate = DateTime.Now;
					logList[i] = log;
					return;
				}
			}
			
			if (logList.Count >= maxLog)
			{
				// 発生時間が最も古いものを取り除く
				SortListByLastDate();
				logList.RemoveAt(logList.Count - 1);
			}

			logList.Add(new LogData
			{
				Message = logMessage,
				StackTrace = logStackTrace,
				Type = logType,
				LastDate = DateTime.Now
			});
		}

		// 最終発生時間でソート
		private static void SortListByLastDate()
		{
			logList.Sort((x, y) => DateTime.Compare(y.LastDate, x.LastDate));
		}
		
		/// <summary>
		/// バグ報告に追加する用に整形して受け取る
		/// </summary>
		public static string GetBacklogLogText()
		{
			SortListByLastDate();
			
			string text = "";
			foreach (var log in logList)
			{
				switch (log.Type)
				{
					case LogType.Error:
					case LogType.Exception:
					case LogType.Assert:
						text += "''&color(#ff0000) { ";
						break;
					case LogType.Warning:
						text += "''&color(#bbbb00) { ";
						break;
					default:
						continue;
				}
				text += $"{log.Type.ToString()}''";
				text += " }\n";
				text += $"''LastDate'' {log.LastDate}\n";
				text += $"''Message'' {log.Message}\n";
				text += $"''StackTrace'' {log.StackTrace}\n";
			}
			return text;
		}
	}
}