using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.IO;
using NBacklog.DataTypes;
using System.Threading.Tasks;

namespace Backlog.BugReporter
{
	/// <summary>
	/// Backlogにバグ報告チケットを追加する
	/// </summary>
	public class BugReportWizard : ScriptableWizard
	{
		#region ウィザードの作成
		
		private const string MenuPath = "Backlog/バグ報告";
		
		/// <summary>
		/// BacklogAPI
		/// </summary>
		private static readonly BacklogAPI m_BacklogAPI = new BacklogAPI();
		private static BacklogAPI.ProjectData ProjectData => m_BacklogAPI.Data;
		
		[MenuItem(MenuPath, validate = true)]
		static bool OpenValidate()
		{
			return EditorApplication.isPlaying;
		}

		[MenuItem(MenuPath)]
		static void Open()
		{
			try
			{
				EditorUtility.DisplayProgressBar("Backlog", "プロジェクト情報をロード中です...", 0f);
				m_BacklogAPI.LoadProjectInfo(OpenWizard);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}
		}

		private static void OpenWizard()
		{
			string header = $"{m_BacklogAPI.Space.Name}:{m_BacklogAPI.Project.Name}:バグ報告";
			var wizard = DisplayWizard<BugReportWizard>(header, "バグ報告する");
			wizard.minSize = new Vector2(640f, 440f);
		}
		
		#endregion

		// 種別は固定
		private TicketType ticketType;

		// プルダウンで選択するもの
		private int priorityIndex;
		private List<string> priorityDropDown;

		private int versionIndex;
		private List<string> versionDropDown;

		private int categoryIndex;
		private List<string> categoryPullDown;

		private int assigneeIndex;
		private List<string> assigneePullDown;

		private void OnEnable()
		{
			var defaultValues = BugReportData.Load();

			// 種別（固定）
			ticketType = ProjectData.TicketTypes.FirstOrDefault(x => x.Name == defaultValues.TicketType);

			// 優先度
			priorityDropDown = ProjectData.Priorities.Select(x => x.Name).ToList();
			priorityIndex = priorityDropDown.FindIndex(x => x.Equals(defaultValues.Priority));

			// 発生バージョン
			versionDropDown = ProjectData.Milestones.Select(x => x.Name).ToList();
			versionIndex = versionDropDown.FindIndex(x => x.Equals(defaultValues.CurrentVersion));

			// カテゴリ
			categoryPullDown = ProjectData.Categories.Select(x => x.Name).ToList();
			categoryIndex = categoryPullDown.FindIndex(x => x.Equals(defaultValues.Category));

			// 担当者
			assigneePullDown = ProjectData.Users.Select(x => x.Name).ToList();
			assigneeIndex = assigneePullDown.FindIndex(x => x.Equals(defaultValues.Assignee));
		}
		
		// 設定項目
		private string ticketTitle;
		private string content;
		private string howTo;
		private bool isCaptureScreenShot = true;
		private bool isSendLog = true;
		
		private string searchText;

		protected override bool DrawWizardGUI()
		{
			if (m_BacklogAPI.Project == null || ticketType == null)
			{
				// 初期化失敗
				Close();
				return false;
			}

			EditorGUILayout.LabelField("タイトル（必須）");
			ticketTitle = EditorGUILayout.TextField(ticketTitle);
			if (string.IsNullOrEmpty(ticketTitle))
			{
				Color cCache = GUI.contentColor;
				GUI.contentColor = Color.red;
				EditorGUILayout.LabelField("タイトルを入力して下さい");
				GUI.contentColor = cCache;
			}
			EditorGUILayout.Space();

			const float minHeight = 42f;
			EditorGUILayout.LabelField("バグ内容");
			content = EditorGUILayout.TextArea(content, GUILayout.MinHeight(minHeight));
			EditorGUILayout.Space();

			EditorGUILayout.LabelField("再現方法");
			howTo = EditorGUILayout.TextArea(howTo, GUILayout.MinHeight(minHeight));
			EditorGUILayout.Space();

			using (new EditorGUILayout.HorizontalScope())
			{
				priorityIndex = EditorGUILayout.Popup("優先度", priorityIndex, priorityDropDown.ToArray());
				versionIndex = EditorGUILayout.Popup("発生バージョン", versionIndex, versionDropDown.ToArray());
			}
			EditorGUILayout.Space();

			using (new EditorGUILayout.HorizontalScope())
			{
				categoryIndex = EditorGUILayout.Popup("カテゴリ", categoryIndex, categoryPullDown.ToArray());
				assigneeIndex = EditorGUILayout.Popup("担当者", assigneeIndex, assigneePullDown.ToArray());
			}
			EditorGUILayout.Space();

			using (new EditorGUILayout.HorizontalScope())
			{
				isCaptureScreenShot = EditorGUILayout.Toggle("スクリーンショットを送る", isCaptureScreenShot);
				isSendLog = EditorGUILayout.Toggle("ログを送る", isSendLog);
			}
			EditorGUILayout.Space();

			// キーワードバグ検索機能
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("キーワードバグ検索（ブラウザで開きます）");
			using (new EditorGUILayout.HorizontalScope())
			{
				searchText = GUILayout.TextField(searchText, "SearchTextField", GUILayout.Width(200));
				GUI.enabled = !string.IsNullOrEmpty(searchText);
				if (GUILayout.Button("Clear", "SearchCancelButton"))
				{
					searchText = string.Empty;
				}
				GUI.enabled = true;
				if (GUILayout.Button("検索", GUILayout.Width(60f)))
				{
					string searchURL = "https://{0}.{1}/find/{2}?condition.projectId={3}&condition.issueTypeId={4}&condition.statusId=1&condition.statusId=2&condition.statusId=3&condition.limit=20&condition.offset=0&condition.query={5}&condition.sort=UPDATED&condition.order=false&condition.simpleSearch=false&condition.allOver=false";
					var uri = new Uri(string.Format(
						searchURL,
						m_BacklogAPI.Space.Key,
						m_BacklogAPI.APIData.Domain,
						m_BacklogAPI.Project.Key,
						m_BacklogAPI.Project.Id,
						ticketType.Id.ToString(),
						searchText));
					Application.OpenURL(uri.AbsoluteUri);
				}
			}
			EditorGUILayout.Space();

			return true;
		}

		private void OnWizardUpdate()
		{
			isValid = !string.IsNullOrEmpty(ticketTitle);
		}

		private async void OnWizardCreate()
		{
			EditorUtility.DisplayProgressBar("Backlog", "準備中です...", 0f);

			//-- 詳細作成
			string desc = "";

			// バグ内容
			if (!string.IsNullOrEmpty(content))
			{
				desc += $"【バグ内容】\n{content}\n\n";
			}

			// 再現方法
			if (!string.IsNullOrEmpty(howTo))
			{
				desc += $"【再現方法】\n{howTo}\n\n";
			}

			// 発生OS
			desc += "【環境】\n";
#if UNITY_EDITOR_OSX
			desc += "発生OS:Mac\n";
#elif UNITY_EDITOR_WIN
			desc += "発生OS:Windows\n";
			#endif

			// ログ
			if (isSendLog)
			{
				desc += "\n【ログ】\n";
				desc += LogRecorder.GetBacklogLogText();
			}
			
			//-- 添付ファイルの作成
			var attachments = new List<Attachment>();

			// 画面スクショ
			string screenShotName = string.Empty;
			if (isCaptureScreenShot)
			{
				EditorUtility.DisplayProgressBar("Backlog", "スクリーンショットを添付しています...", 0.25f);

				screenShotName = $"capture_{DateTime.Now.ToString("yyyy_MM_dd_H-mm-ss")}.jpg";
				ScreenCapture.CaptureScreenshot(screenShotName);
				string path = $"{Application.dataPath}/../{screenShotName}";

				// 撮影完了待ち
				bool isPaused = EditorApplication.isPaused;
				EditorApplication.isPaused = false;
				
				var startTime = DateTime.Now;
				var timeLimit = new TimeSpan(0,0,5);
				while (!File.Exists(path))
				{
					await Task.Delay(500);
					if (DateTime.Now - startTime > timeLimit)
					{
						// タイムアウト
						EditorUtility.ClearProgressBar();
						EditorUtility.DisplayDialog("エラー", "スクリーンショットの撮影に失敗しました", "OK");
						return;
					}
				}
				
				EditorApplication.isPaused = isPaused;
				
				try
				{
					var attachment = m_BacklogAPI.AddAttachment(path);
					if (attachment == null)
					{
						EditorUtility.ClearProgressBar();
						EditorUtility.DisplayDialog("エラー", "スクリーンショットの添付に失敗しました", "OK");
						return;
					}
					attachments.Add(attachment);
				}
				catch (Exception e)
				{
					EditorUtility.ClearProgressBar();
					Debug.LogException(e);
				}
				finally
				{
					File.Delete(path);
				}
			}

			//-- チケット作成
			EditorUtility.DisplayProgressBar("Backlog", "バグ報告チケットを追加しています...", 0.75f);

			// 優先度
			string priorityName = priorityDropDown[priorityIndex];
			var priority = ProjectData.Priorities.FirstOrDefault((x) => x.Name == priorityName);

			// バージョン
			string versionName = versionDropDown[versionIndex];
			var version = ProjectData.Milestones.FirstOrDefault((x) => x.Name == versionName);

			// カテゴリ
			string categoryName = categoryPullDown[categoryIndex];
			var category = ProjectData.Categories.FirstOrDefault((x) => x.Name == categoryName);

			// 担当者
			string assigneeName = assigneePullDown[assigneeIndex];
			var assignee = ProjectData.Users.FirstOrDefault((x) => x.Name == assigneeName);		

			var ticket = new Ticket(ticketTitle, ticketType, priority);
			ticket.Description = desc;
			ticket.Versions = new[] { version };
			ticket.Categories = new[] { category };
			ticket.Assignee = assignee;
			if (attachments.Count > 0)
			{
				ticket.Attachments = attachments.ToArray();
			}

			try
			{
				var result = m_BacklogAPI.AddTicket(ticket);
				if (result == null)
				{
					EditorUtility.ClearProgressBar();
					EditorUtility.DisplayDialog("エラー", "チケット作成に失敗しました", "OK");
					return;
				}

				// サムネをを加える
				// [注意] カスタム属性があるプロジェクトの場合、カスタム属性の対応を入れないと更新に失敗する
				if (isCaptureScreenShot)
				{
					EditorUtility.DisplayProgressBar("Backlog", "チケットに情報を追加しています...", 0.9f);

					// 一度スペースに追加した添付ファイルは、実際にチケットが発行されると削除され新しくIDが振られるので、
					// このタイミングでないと付け加えられない
					int thumbnailId = result.Attachments.First(x => x.Name.Equals(screenShotName)).Id;
					result.Description = $"#thumbnail({thumbnailId.ToString()})\n\n" + desc;
					result = m_BacklogAPI.UpdateTicket(result);

					if (result == null)
					{
						EditorUtility.ClearProgressBar();
						EditorUtility.DisplayDialog("エラー", "情報の追加に失敗しました", "OK");
						return;
					}
				}

				EditorUtility.ClearProgressBar();
				if (EditorUtility.DisplayDialog("Backlog", " バグ報告が完了しました", "チケットを開く", "閉じる"))
				{
					m_BacklogAPI.OpenBacklogTicket(result);
				}
			}
			catch (Exception e)
			{
				EditorUtility.ClearProgressBar();
				Debug.LogException(e);
			}
		}
	}
}
