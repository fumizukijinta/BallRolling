using System.IO;
using BallRolling.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BallRolling.EditorTools
{
    /// <summary>ステップ5のアイテム取得環境構築（Ballタグ登録・ItemIndicator生成・GameController配線）。Undo対応・シーンのダーティ登録を行う。</summary>
    public static class Step5_ItemSetup
    {
        public const string ItemIndicatorTextName = "ItemIndicatorText";
        public const string BallTagName = "Ball";
        private const string MaterialFolder = "Assets/Art/Materials";
        private const string ItemMaterialName = "ItemGold";

        /// <summary>Ballタグの登録・玉への設定・ItemIndicator生成・GameControllerへの配線を行う。前提オブジェクト不足の場合はnullを返す。</summary>
        public static GameObject Apply(Step5_PlayParameters p)
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
                Debug.LogError("玉が見つかりません。先に Step3 Play Setup でプレイ環境を構築して下さい。");
                return null;
            }

            var canvas = GameObject.Find(Step4_GameLoopSetup.CanvasName);
            if (canvas == null)
            {
                Debug.LogError("UICanvas が見つかりません。先に Step4 Game Loop でゲームループを構築して下さい。");
                return null;
            }

            var controller = Object.FindFirstObjectByType<GameController>();
            if (controller == null)
            {
                Debug.LogError("GameController が見つかりません。先に Step4 Game Loop でゲームループを構築して下さい。");
                return null;
            }

            // タグ登録前に tag を設定すると例外が出るため、必ず登録を先に行う
            RegisterBallTag();

            Undo.RecordObject(ball, "Step5: Set Ball Tag");
            ball.tag = BallTagName;
            EditorUtility.SetDirty(ball);

            var indicatorText = CreateItemIndicatorText(canvas.transform, p);
            var itemMaterial = GetOrCreateItemMaterial(p);

            var serializedController = new SerializedObject(controller);
            serializedController.FindProperty("_itemIndicatorText").objectReferenceValue = indicatorText;
            serializedController.FindProperty("_itemSize").floatValue = p.ItemSize;
            serializedController.FindProperty("_itemLocalY").floatValue = p.ItemLocalY;
            serializedController.FindProperty("_itemRotationSpeedDegrees").floatValue = p.ItemRotationSpeedDegrees;
            serializedController.FindProperty("_itemMaterial").objectReferenceValue = itemMaterial;
            serializedController.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = controller.gameObject;
            return controller.gameObject;
        }

        /// <summary>BallタグがTagManagerに未登録なら登録する。他のStep（玉の再生成など）からも再利用可能。</summary>
        public static void RegisterBallTag()
        {
            var tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
            var tagManager = new SerializedObject(tagManagerAsset);
            var tags = tagManager.FindProperty("tags");
            for (var i = 0; i < tags.arraySize; i++)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue == BallTagName)
                    return; // 登録済み
            }

            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = BallTagName;
            tagManager.ApplyModifiedProperties();
            EditorUtility.SetDirty(tagManagerAsset);
        }

        private static Text CreateItemIndicatorText(Transform canvasTransform, Step5_PlayParameters p)
        {
            var oldText = GameObject.Find(ItemIndicatorTextName);
            if (oldText != null)
                Undo.DestroyObjectImmediate(oldText);

            var textObject = new GameObject(ItemIndicatorTextName);
            textObject.transform.SetParent(canvasTransform, false);
            Undo.RegisterCreatedObjectUndo(textObject, "Step5: Create ItemIndicatorText");

            // Unity 6 では "Arial.ttf" は使えず、組み込みフォントは LegacyRuntime.ttf を使う
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = p.ItemIndicatorFontSize;
            text.color = p.ItemIndicatorColor;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = string.Empty; // 待機中・未取得は空文字

            var rectTransform = text.rectTransform;
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = new Vector2(20f, -20f);
            rectTransform.sizeDelta = new Vector2(600f, 80f);

            EditorUtility.SetDirty(textObject);
            return text;
        }

        private static Material GetOrCreateItemMaterial(Step5_PlayParameters p)
        {
            var path = $"{MaterialFolder}/{ItemMaterialName}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Directory.CreateDirectory(MaterialFolder);
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = p.ItemColor;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }
    }
}
