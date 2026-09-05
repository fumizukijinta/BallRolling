using UnityEditor;
using UnityEngine;

namespace BallRolling.EditorTools
{
    /// <summary>ステップ3のプレイ環境を構築するウィンドウ（玉・傾け・カメラ設定 → プレイ開始）。</summary>
    public class Step3_PlaySetupWindow : EditorWindow
    {
        private Step3_PlayParameters _parameters = new Step3_PlayParameters();
        private Vector2 _scrollPosition;
        private string _lastLog;

        [MenuItem("BallRolling/Step3 Play Setup")]
        public static void Open()
        {
            GetWindow<Step3_PlaySetupWindow>("Step3 Play Setup");
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.LabelField("玉の物理", EditorStyles.boldLabel);
            _parameters.GravityScale = EditorGUILayout.Slider("重力スケール (1=標準)", _parameters.GravityScale, 1f, 4f);
            _parameters.BallSize = EditorGUILayout.Slider("玉のサイズ", _parameters.BallSize, 0.3f, 1f);
            _parameters.BallMass = EditorGUILayout.Slider("質量", _parameters.BallMass, 0.1f, 5f);
            _parameters.BallDrag = EditorGUILayout.Slider("空気抵抗 (Drag)", _parameters.BallDrag, 0f, 2f);
            _parameters.BallAngularDrag = EditorGUILayout.Slider("回転抵抗", _parameters.BallAngularDrag, 0f, 2f);
            _parameters.Friction = EditorGUILayout.Slider("摩擦", _parameters.Friction, 0f, 1f);
            _parameters.Bounciness = EditorGUILayout.Slider("跳ね返り", _parameters.Bounciness, 0f, 1f);
            _parameters.BallColor = EditorGUILayout.ColorField("玉の色", _parameters.BallColor);
            _parameters.BallSpawnHeight = EditorGUILayout.Slider("スポーン高さ", _parameters.BallSpawnHeight, 1f, 8f);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("ボードの傾き", EditorStyles.boldLabel);
            _parameters.MaxTiltAngle = EditorGUILayout.Slider("最大傾き角 (度)", _parameters.MaxTiltAngle, 5f, 30f);
            _parameters.TiltSpeed = EditorGUILayout.Slider("傾き速度", _parameters.TiltSpeed, 1f, 15f);

            EditorGUILayout.Space();
            if (GUILayout.Button("玉をスポーン＆プレイ環境を設定", GUILayout.Height(32)))
            {
                var ball = Step3_PlaySetup.Apply(_parameters);
                _lastLog = ball != null
                    ? "セットアップ完了。プレイモードを開始すると矢印キーで傾けられます。"
                    : "失敗: MazeRoot がありません（Step2で迷路を生成して下さい）";
                Debug.Log($"[Step3] {_lastLog}");
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("プレイモードを開始", GUILayout.Height(28)))
            {
                EditorApplication.isPlaying = true;
            }

            EditorGUILayout.HelpBox(
                "手順: 1) このボタンでセットアップ 2) プレイモード開始 3) 矢印キー（上下左右）でボードを傾ける\n" +
                "セットアップはUndo対応（Ctrl+Z）。終了後は Ctrl+S でシーンを保存して下さい。",
                MessageType.Info);

            if (!string.IsNullOrEmpty(_lastLog))
                EditorGUILayout.HelpBox(_lastLog, MessageType.None);

            EditorGUILayout.EndScrollView();
        }
    }
}
