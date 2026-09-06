using UnityEditor;
using UnityEngine;

namespace BallRolling.EditorTools
{
    /// <summary>ステップ4のゲームループを構築するウィンドウ（UI Canvas・GameController生成 → プレイ開始）。</summary>
    public class Step4_GameLoopSetupWindow : EditorWindow
    {
        private Step4_PlayParameters _parameters = new Step4_PlayParameters();
        private Vector2 _scrollPosition;
        private string _lastLog;

        [MenuItem("BallRolling/Step4 Game Loop")]
        public static void Open()
        {
            GetWindow<Step4_GameLoopSetupWindow>("Step4 Game Loop");
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.LabelField("ゲームループ", EditorStyles.boldLabel);
            _parameters.TimeLimitSeconds = EditorGUILayout.Slider("制限時間 (秒)", _parameters.TimeLimitSeconds, 30f, 600f);
            _parameters.BallSpawnHeight = EditorGUILayout.Slider("玉スポーン高さ", _parameters.BallSpawnHeight, 1f, 8f);
            _parameters.GoalFallLocalY = EditorGUILayout.Slider("ゴール判定Y (ローカル)", _parameters.GoalFallLocalY, -5f, -0.5f);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("UI表示", EditorStyles.boldLabel);
            _parameters.TimerFontSize = EditorGUILayout.IntSlider("タイマーフォントサイズ", _parameters.TimerFontSize, 20, 120);
            _parameters.MessageFontSize = EditorGUILayout.IntSlider("メッセージフォントサイズ", _parameters.MessageFontSize, 16, 100);
            _parameters.TimerColor = EditorGUILayout.ColorField("タイマー文字色", _parameters.TimerColor);
            _parameters.MessageColor = EditorGUILayout.ColorField("メッセージ文字色", _parameters.MessageColor);

            EditorGUILayout.Space();
            if (GUILayout.Button("ゲームループを構築", GUILayout.Height(32)))
            {
                var controllerObject = Step4_GameLoopSetup.Apply(_parameters);
                _lastLog = controllerObject != null
                    ? "構築完了。プレイモードでSpaceキー: 開始/リスタート、矢印キーで傾けます。"
                    : "失敗: MazeRoot または玉がありません（Step2→Step3 の順に実行して下さい）";
                Debug.Log($"[Step4] {_lastLog}");
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("プレイモードを開始", GUILayout.Height(28)))
            {
                EditorApplication.isPlaying = true;
            }

            EditorGUILayout.HelpBox(
                "手順: 1) Step2で迷路生成 2) Step3で玉スポーン 3) このボタンで構築 4) プレイモード開始 5) Spaceで開始\n" +
                "構築はUndo対応（Ctrl+Z）。終了後は Ctrl+S でシーンを保存して下さい。",
                MessageType.Info);

            if (!string.IsNullOrEmpty(_lastLog))
                EditorGUILayout.HelpBox(_lastLog, MessageType.None);

            EditorGUILayout.EndScrollView();
        }
    }
}
