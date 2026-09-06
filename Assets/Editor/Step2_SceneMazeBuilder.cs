using System.IO;
using BallRolling.Gameplay;
using BallRolling.Gameplay.Logic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BallRolling.EditorTools
{
    /// <summary>迷路をシーン上に生成するエディター用ビルダー。Undo対応・シーンのダーティ登録を行う。</summary>
    public static class Step2_SceneMazeBuilder
    {
        public const string RootName = "MazeRoot";
        private const string MaterialFolder = "Assets/Art/Materials";
        private const float DefaultItemSize = 0.3f;
        private const float DefaultItemLocalY = 0.35f;
        private static readonly Color DefaultItemColor = new Color(1f, 0.8f, 0.1f);

        /// <summary>迷路を生成してシーンに配置する。既存のMazeRootはUndo可能な形で削除される。</summary>
        public static GameObject Build(MazeModel maze, Step2_MazeBuildParameters parameters)
        {
            var oldRoot = GameObject.Find(RootName);
            if (oldRoot != null)
            {
                DetachCameraFrom(oldRoot.transform); // MazeRoot削除でカメラが消えないよう先に退避
                Undo.DestroyObjectImmediate(oldRoot);
            }

            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Step2: Generate Maze");

            BuildFloor(maze, parameters, root.transform);
            BuildWalls(maze, parameters, root.transform);
            BuildCeiling(maze, parameters, root.transform);
            BuildMarkers(maze, parameters, root.transform);
            BuildItem(maze, parameters, root.transform);
            FrameCamera(maze, parameters);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = root;
            return root;
        }

        private static void BuildFloor(MazeModel maze, Step2_MazeBuildParameters p, Transform root)
        {
            var floorParent = new GameObject("Floor");
            floorParent.transform.SetParent(root, false);
            var material = GetOrCreateMaterial("MazeFloor", p.FloorColor);

            for (var y = 0; y < maze.Height; y++)
            {
                for (var x = 0; x < maze.Width; x++)
                {
                    var position = new Vector2Int(x, y);
                    if (position == maze.Exit)
                        continue; // 出口セルのみ穴にする（入り口は床あり=玉が着地して迷路に入る）

                    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.name = $"Floor_{x}_{y}";
                    cube.transform.SetParent(floorParent.transform, false);
                    cube.transform.localPosition = new Vector3(
                        (x + 0.5f) * p.CellSize, -p.FloorThickness / 2f, (y + 0.5f) * p.CellSize);
                    cube.transform.localScale = new Vector3(p.CellSize, p.FloorThickness, p.CellSize);
                    cube.GetComponent<Renderer>().sharedMaterial = material;
                }
            }
        }

        private static void BuildWalls(MazeModel maze, Step2_MazeBuildParameters p, Transform root)
        {
            var wallsParent = new GameObject("Walls");
            wallsParent.transform.SetParent(root, false);
            var material = GetOrCreateMaterial("MazeWall", p.WallColor);

            for (var y = 0; y < maze.Height; y++)
            {
                for (var x = 0; x < maze.Width; x++)
                {
                    var cell = maze.GetCell(x, y);
                    if (cell.North)
                        CreateWall(wallsParent.transform, material, p,
                            new Vector3((x + 0.5f) * p.CellSize, p.WallHeight / 2f, (y + 1f) * p.CellSize),
                            isVertical: false, $"Wall_N_{x}_{y}");
                    if (cell.East)
                        CreateWall(wallsParent.transform, material, p,
                            new Vector3((x + 1f) * p.CellSize, p.WallHeight / 2f, (y + 0.5f) * p.CellSize),
                            isVertical: true, $"Wall_E_{x}_{y}");
                    if (y == 0 && cell.South)
                        CreateWall(wallsParent.transform, material, p,
                            new Vector3((x + 0.5f) * p.CellSize, p.WallHeight / 2f, 0f),
                            isVertical: false, $"Wall_S_{x}_0");
                    if (x == 0 && cell.West)
                        CreateWall(wallsParent.transform, material, p,
                            new Vector3(0f, p.WallHeight / 2f, (y + 0.5f) * p.CellSize),
                            isVertical: true, $"Wall_W_0_{y}");
                }
            }
        }

        private static void CreateWall(Transform parent, Material material, Step2_MazeBuildParameters p, Vector3 position, bool isVertical, string name)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent, false);
            // 下端を床に0.1だけめり込ませ、壁と床の接縫（すり抜けやすい縁）を消す
            wall.transform.localPosition = position - new Vector3(0f, 0.1f, 0f);
            wall.transform.localScale = isVertical
                ? new Vector3(p.WallThickness, p.WallHeight, p.CellSize + p.WallThickness)
                : new Vector3(p.CellSize + p.WallThickness, p.WallHeight, p.WallThickness);
            wall.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void BuildCeiling(MazeModel maze, Step2_MazeBuildParameters p, Transform root)
        {
            // 透明な天井（コライダーのみ）: 跳ねた玉が壁を越えられないよう下部を壁上端より低く抑える。
            // 入り口セルの上だけ穴を開け、玉が上空から落下して入れるようにする。
            var ceilingParent = new GameObject("Ceiling");
            ceilingParent.transform.SetParent(root, false);

            for (var y = 0; y < maze.Height; y++)
            {
                for (var x = 0; x < maze.Width; x++)
                {
                    if (x == maze.Entrance.x && y == maze.Entrance.y)
                        continue; // 入り口セルの上は落下経路として開ける

                    var tile = new GameObject($"Ceiling_{x}_{y}");
                    tile.transform.SetParent(ceilingParent.transform, false);
                    tile.transform.localPosition = new Vector3(
                        (x + 0.5f) * p.CellSize, p.WallHeight - 0.2f, (y + 0.5f) * p.CellSize);
                    var box = tile.AddComponent<BoxCollider>();
                    box.size = new Vector3(p.CellSize, 0.2f, p.CellSize);
                }
            }
        }

        private static void BuildMarkers(MazeModel maze, Step2_MazeBuildParameters p, Transform root)
        {
            var markersParent = new GameObject("Markers");
            markersParent.transform.SetParent(root, false);

            CreateMarker(markersParent.transform, maze.Entrance, p, "EntranceMarker", p.EntranceColor);
            CreateMarker(markersParent.transform, maze.Exit, p, "ExitMarker", p.ExitColor);
        }

        private static void CreateMarker(Transform parent, Vector2Int cell, Step2_MazeBuildParameters p, string name, Color color)
        {
            var material = GetOrCreateMaterial(name, color);
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = name;
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = new Vector3(
                (cell.x + 0.5f) * p.CellSize, -1.2f, (cell.y + 0.5f) * p.CellSize);
            marker.transform.localScale = new Vector3(p.CellSize, 0.05f, p.CellSize);
            marker.GetComponent<Renderer>().sharedMaterial = material;
            // 視認用マーカーのため当たり判定は持たせない
            Object.DestroyImmediate(marker.GetComponent<Collider>());
        }

        /// <summary>行き止まりにアイテムを1つ生成する（生成ロジックは MazeBuilder.BuildItem と共有。Undo登録のみこちらで行う）。</summary>
        private static void BuildItem(MazeModel maze, Step2_MazeBuildParameters p, Transform root)
        {
            var geometry = new MazeGeometry
            {
                CellSize = p.CellSize,
                WallHeight = p.WallHeight,
                WallThickness = p.WallThickness,
                FloorThickness = p.FloorThickness
            };
            var material = GetOrCreateMaterial("ItemGold", DefaultItemColor);
            var builder = new MazeBuilder();
            // アイテムのシードは迷路シードと独立の乱数（要件: ランダム配置）
            var item = builder.BuildItem(maze, geometry, root, DefaultItemSize, DefaultItemLocalY,
                material, UnityEngine.Random.Range(int.MinValue, int.MaxValue));
            if (item != null)
                Undo.RegisterCreatedObjectUndo(item.transform.parent.gameObject, "Step2: Build Item");
        }

        private static void DetachCameraFrom(Transform root)
        {
            var camera = root.GetComponentInChildren<Camera>();
            if (camera != null)
                camera.transform.SetParent(null, true); // ワールド位置維持で親から外す
        }

        private static void FrameCamera(MazeModel maze, Step2_MazeBuildParameters p)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                Undo.RegisterCreatedObjectUndo(cameraObject, "Step2: Create Main Camera");
                camera = cameraObject.GetComponent<Camera>();
            }

            Undo.RecordObject(camera.transform, "Step2: Frame Maze Camera");
            var centerX = maze.Width * p.CellSize / 2f;
            var centerZ = maze.Height * p.CellSize / 2f;
            var height = Mathf.Max(maze.Width, maze.Height) * p.CellSize * 1.1f;
            camera.transform.position = new Vector3(centerX, height, centerZ);
            camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            EditorUtility.SetDirty(camera.transform);
        }

        private static Material GetOrCreateMaterial(string name, Color color)
        {
            var path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Directory.CreateDirectory(MaterialFolder);
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }
    }
}
