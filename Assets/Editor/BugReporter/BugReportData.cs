using System.IO;
using UnityEngine;
using UnityEditor;

namespace BugReporter
{
	/// <summary>
	/// バグ報告関連のデータ
	/// </summary>
	public class BugReportData : ScriptableObject
	{
		/// <summary>
		/// アセットパス
		/// </summary>
		private const string AssetPath = "Backlog/BugReport.asset";

		/// <summary>
		/// 現在の発生バージョン
		/// </summary>
		[Header("現在の発生バージョン")]
		public string CurrentVersion;
		/// <summary>
		/// デフォルトのチケットタイプ
		/// </summary>
		[Header("デフォルトのチケットタイプ")]
		public string TicketType = "バグ";
		/// <summary>
		/// デフォルトの優先度
		/// </summary>
		[Header("デフォルトの優先度")]
		public string Priority = "中";
		/// <summary>
		/// デフォルトのカテゴリ
		/// </summary>
		[Header("デフォルトのカテゴリ")]
		public string Category;
		/// <summary>
		/// デフォルトの担当者
		/// バグチケットの割り振りを行う人
		/// </summary>
		[Header("デフォルトの担当者")]
		public string Assignee;
		
		/// <summary>
		/// バグ報告関連のデータをロード
		/// </summary>
		public static BugReportData Load()
		{
			var asset = EditorGUIUtility.Load(AssetPath);
			if (!asset)
			{
				// 無かったら作成
				CreateAsset();
				asset = EditorGUIUtility.Load(AssetPath);
			}
			return asset as BugReportData;
		}
		
		/// <summary>
		/// アセット作成
		/// </summary>
		public static void CreateAsset()
		{
			var outputPath = "Assets/Editor Default Resources/" + AssetPath;

			var fullDirPath = Path.GetDirectoryName(Application.dataPath.Replace("Assets", outputPath));
			if (!Directory.Exists(fullDirPath))
			{
				Directory.CreateDirectory(fullDirPath);
			}
			
			AssetDatabase.CreateAsset(CreateInstance<BugReportData>(), outputPath);
			AssetDatabase.Refresh();
		}
		
		/// <summary>
		/// SerializedObjectで取得
		/// </summary>
		public static SerializedObject GetSerializedObject()
		{
			return new SerializedObject(Load());
		}
	}
}