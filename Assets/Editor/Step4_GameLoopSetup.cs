using BallRolling.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BallRolling.EditorTools
{
    /// <summary>ステップ4のUI Canvas・GameController構築。Undo対応・シーンのダーティ登録を行う。</summary>
    public static class Step4_GameLoopSetup
    {
        public const string CanvasName = "UICanvas";
        public const string TimerTextName = "TimerText";
        public const string MessageTextName = "MessageText";
        private const string MaterialFolder = "Assets/Art/Materials";

        /// <summary>UI CanvasとGameControllerを生成して配線する。MazeRoot/Ballが不足の場合はnullを返す。</summary>
        public static GameObject Apply(Step4_PlayParameters p)
        {
            var root = GameObject.Find(Step2_SceneMazeBuilder.RootName);
            if (root == null)
            {
                Debug.LogError("MazeRoot が見つかりません。先に Step2 Maze Builder で迷路を生成して下さい。");
                return null;
            }

            var ball = root.transform.Find(Step3_PlaySetup.BallName);
            if (ball == null)
            {
                Debug.LogError("玉が見つかりません。先に Step3 Play Setup でプレイ環境を構築して下さい（玉の自動生成は行いません）。");
                return null;
            }

            var canvas = CreateCanvas(p);
            var timerText = CreateText(canvas.transform, TimerTextName, p.TimerFontSize, p.TimerColor);
            var messageText = CreateText(canvas.transform, MessageTextName, p.MessageFontSize, p.MessageColor);
            AnchorTimerText(timerText.rectTransform);
            AnchorMessageText(messageText.rectTransform);

            var controllerObject = CreateGameController(root.transform, ball.gameObject, timerText, messageText, p);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = controllerObject;
            return controllerObject;
        }

        private static Canvas CreateCanvas(Step4_PlayParameters p)
        {
            var oldCanvas = GameObject.Find(CanvasName);
            if (oldCanvas != null)
                Undo.DestroyObjectImmediate(oldCanvas);

            var canvasObject = new GameObject(CanvasName);
            Undo.RegisterCreatedObjectUndo(canvasObject, "Step4: Create UI Canvas");

            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static Text CreateText(Transform parent, string name, int fontSize, Color color)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);

            // Unity 6 では "Arial.ttf" は使えず、組み込みフォントは LegacyRuntime.ttf を使う
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void AnchorTimerText(RectTransform rectTransform)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = new Vector2(0f, -20f);
            rectTransform.sizeDelta = new Vector2(800f, 140f);
        }

        private static void AnchorMessageText(RectTransform rectTransform)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(1600f, 400f);
        }

        private static GameObject CreateGameController(Transform root, GameObject ball, Text timerText, Text messageText, Step4_PlayParameters p)
        {
            var oldController = GameObject.Find(nameof(GameController));
            if (oldController != null)
                Undo.DestroyObjectImmediate(oldController);

            var controllerObject = new GameObject(nameof(GameController));
            Undo.RegisterCreatedObjectUndo(controllerObject, "Step4: Create GameController");
            var controller = controllerObject.AddComponent<GameController>();

            var serializedController = new SerializedObject(controller);
            serializedController.FindProperty("_mazeRoot").objectReferenceValue = root;
            serializedController.FindProperty("_ball").objectReferenceValue = ball;
            serializedController.FindProperty("_timerText").objectReferenceValue = timerText;
            serializedController.FindProperty("_messageText").objectReferenceValue = messageText;
            serializedController.FindProperty("_timeLimitSeconds").floatValue = p.TimeLimitSeconds;
            serializedController.FindProperty("_ballSpawnHeight").floatValue = p.BallSpawnHeight;
            serializedController.FindProperty("_goalFallLocalY").floatValue = p.GoalFallLocalY;
            serializedController.FindProperty("_wallMaterial").objectReferenceValue = LoadMaterial("MazeWall");
            serializedController.FindProperty("_floorMaterial").objectReferenceValue = LoadMaterial("MazeFloor");
            serializedController.FindProperty("_entranceMarkerMaterial").objectReferenceValue = LoadMaterial("EntranceMarker");
            serializedController.FindProperty("_exitMarkerMaterial").objectReferenceValue = LoadMaterial("ExitMarker");
            serializedController.ApplyModifiedProperties();

            EditorUtility.SetDirty(controller);
            return controllerObject;
        }

        private static Material LoadMaterial(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/{name}.mat");
        }
    }
}
