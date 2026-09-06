using UnityEditor;
using UnityEngine;

namespace BallRolling.EditorTools
{
    /// <summary>ステップ5のアイテム環境を構築するウィンドウ（Ballタグ・ItemIndicator・GameController配線）。</summary>
    public class Step5_ItemSetupWindow : EditorWindow
    {
        private Step5_PlayParameters _parameters = new Step5_PlayParameters();
        private Vector2 _scrollPosition;
        private string _lastLog;

        [MenuItem("BallRolling/Step5 Item")]
        public static void Open()
        {
            GetWindow<Step5_ItemSetupWindow>("Step5 Item");
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.LabelField("アイテム", EditorStyles.boldLabel);
            _parameters.ItemSize = EditorGUILayout.Slider("アイテムサイズ", _parameters.ItemSize, 0.1f, 0.8f);
            _parameters.ItemLocalY = EditorGUILayout.Slider("アイテム高さ (床上面=0)", _parameters.ItemLocalY, 0.1f, 0.9f);
            _parameters.ItemRotationSpeedDegrees = EditorGUILayout.Slider("回転速度 (度/秒)", _parameters.ItemRotationSpeedDegrees, 0f, 360f);
            _parameters.ItemColor = EditorGUILayout.ColorField("アイテムの色", _parameters.ItemColor);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("取得表示 (ItemIndicator)", EditorStyles.boldLabel);
            _parameters.ItemIndicatorFontSize = EditorGUILayout.IntSlider("フォントサイズ", _parameters.ItemIndicatorFontSize, 16, 100);
            _parameters.ItemIndicatorColor = EditorGUILayout.ColorField("文字色", _parameters.ItemIndicatorColor);
            _parameters.ItemIndicatorText = EditorGUILayout.TextField("取得時の文言", _parameters.ItemIndicatorText);

            EditorGUILayout.Space();
            if (GUILayout.Button("アイテム表示を構築", GUILayout.Height(32)))
            {
                var controllerObject = Step5_ItemSetup.Apply(_parameters);
                _lastLog = controllerObject != null
                    ? "構築完了。行き止まりのアイテムを取得すると画面左上に表示され、★評価へ加算されます。"
                    : "失敗: MazeRoot / 玉 / UICanvas / GameController のいずれかがありません（Step2→Step3→Step4 の順に実行して下さい）";
                Debug.Log($"[Step5] {_lastLog}");
            }

            EditorGUILayout.HelpBox(
                "手順: 1) Step2で迷路生成 2) Step3で玉スポーン 3) Step4でゲームループ構築 4) このボタンで構築 5) プレイモードでSpace開始\n" +
                "注意: シーン内迷路へのアイテム配置は Step2 Maze Builder の再実行で行われます（Step5はタグ・UI・配線のみ）。\n" +
                "構築はUndo対応（Ctrl+Z）。終了後は Ctrl+S でシーンを保存して下さい。",
                MessageType.Info);

            if (!string.IsNullOrEmpty(_lastLog))
                EditorGUILayout.HelpBox(_lastLog, MessageType.None);

            EditorGUILayout.EndScrollView();
        }
    }
}
