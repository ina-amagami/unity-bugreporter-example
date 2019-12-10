using UnityEditor;
using UnityEngine;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif

namespace BugReporter
{
	/// <summary>
	/// バグ報告用の設定ファイルをProjectSettingsから編集できるようにする
	/// </summary>
	public class BugReportSettings : SettingsProvider
	{
		private const string Path = "Project/BugReport";

		public BugReportSettings(string path, SettingsScope scope) : base(path, scope) { }
		
		/// <summary>
		/// ProjectSettingsに項目追加
		/// </summary>
		[SettingsProvider]
		private static SettingsProvider Create()
		{
			var provider = new BugReportSettings(Path, SettingsScope.Project)
			{
				// 検索対象のキーワード登録（SerializedObjectから自動で取得）
				keywords = GetSearchKeywordsFromSerializedObject(BugReportData.GetSerializedObject())
			};
			return provider;
		}
		
		private static SerializedObject so;
		
		public override void OnActivate(string searchContext, VisualElement rootElement)
		{
			// 設定ファイル取得
			so = BugReportData.GetSerializedObject();
		}
		
		/// <summary>
		/// 他の設定項目と比べて左側の余白が無いので、GUIScopeを作って付ける
		/// </summary>
		internal class GUIScope : GUI.Scope
		{
			public GUIScope()
			{
				GUILayout.BeginHorizontal();
				GUILayout.Space(10f);
				GUILayout.BeginVertical();
			}

			protected override void CloseScope()
			{
				GUILayout.EndVertical();
				GUILayout.EndHorizontal();
			}
		}
		
		public override void OnGUI(string searchContext)
		{
			using (new GUIScope())
			{
				// プロパティの表示
				var iterator = so.GetIterator();
				EditorGUI.BeginChangeCheck();
				while (iterator.NextVisible(true))
				{
					bool isScript = iterator.name.Equals("m_Script");
					if (isScript) { GUI.enabled = false; }
				
					EditorGUILayout.PropertyField(iterator);
				
					if (isScript) { GUI.enabled = true; }
				}
				if (EditorGUI.EndChangeCheck())
				{
					so.ApplyModifiedProperties();
				}
			}
		}
	}
}