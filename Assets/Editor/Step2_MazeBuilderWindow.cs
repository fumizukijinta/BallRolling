using BallRolling.Gameplay.Logic;
using UnityEditor;
using UnityEngine;

namespace BallRolling.EditorTools
{
    /// <summary>迷路生成パラメータを調整し、ボタンでシーンに迷路を生成するウィンドウ。</summary>
    public class Step2_MazeBuilderWindow : EditorWindow
    {
        private Step2_MazeBuildParameters _parameters = new Step2_MazeBuildParameters();
        private Vector2 _scrollPosition;
        private string _lastBuildLog;

        [MenuItem("BallRolling/Step2 Maze Builder")]
        public static void Open()
        {
            GetWindow<Step2_MazeBuilderWindow>("Step2 Maze Builder");
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.LabelField("迷路生成パラメータ", EditorStyles.boldLabel);
            _parameters.Width = EditorGUILayout.IntField("幅（セル数）", _parameters.Width);
            _parameters.Height = EditorGUILayout.IntField("高さ（セル数）", _parameters.Height);
            _parameters.IsRandomSeed = EditorGUILayout.Toggle("ランダムシード", _parameters.IsRandomSeed);

            using (new EditorGUI.DisabledScope(_parameters.IsRandomSeed))
            {
                _parameters.Seed = EditorGUILayout.IntField("シード（固定）", _parameters.Seed);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("形状", EditorStyles.boldLabel);
            _parameters.CellSize = EditorGUILayout.Slider("セルサイズ", _parameters.CellSize, 0.5f, 3f);
            _parameters.WallHeight = EditorGUILayout.Slider("壁の高さ", _parameters.WallHeight, 0.8f, 2.5f);
            _parameters.WallThickness = EditorGUILayout.Slider("壁の厚さ", _parameters.WallThickness, 0.05f, 0.5f);
            _parameters.FloorThickness = EditorGUILayout.Slider("床の厚さ", _parameters.FloorThickness, 0.05f, 1f);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("色", EditorStyles.boldLabel);
            _parameters.WallColor = EditorGUILayout.ColorField("壁", _parameters.WallColor);
            _parameters.FloorColor = EditorGUILayout.ColorField("床", _parameters.FloorColor);
            _parameters.EntranceColor = EditorGUILayout.ColorField("入り口", _parameters.EntranceColor);
            _parameters.ExitColor = EditorGUILayout.ColorField("出口", _parameters.ExitColor);

            EditorGUILayout.Space();
            if (GUILayout.Button("迷路を生成", GUILayout.Height(32)))
            {
                GenerateMaze();
            }

            EditorGUILayout.HelpBox(
                "生成・削除はUndo対応（Ctrl+Zで戻せます）。生成後は Ctrl+S でシーンを保存して下さい。",
                MessageType.Info);

            if (!string.IsNullOrEmpty(_lastBuildLog))
                EditorGUILayout.HelpBox(_lastBuildLog, MessageType.None);

            EditorGUILayout.EndScrollView();
        }

        private void GenerateMaze()
        {
            var width = Mathf.Max(2, _parameters.Width);
            var height = Mathf.Max(2, _parameters.Height);
            var seed = _parameters.ResolveSeed();

            var maze = MazeGenerator.Generate(width, height, seed);
            Step2_SceneMazeBuilder.Build(maze, _parameters);
            _lastBuildLog = $"生成完了: {width}x{height} / seed={seed}\n入り口={maze.Entrance} 出口={maze.Exit}（seedを固定すれば再現できます）";
            Debug.Log($"[Step2] {_lastBuildLog.Replace("\n", " ")}");
        }
    }
}
