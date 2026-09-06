using BallRolling.Gameplay.Logic;
using UnityEngine;

namespace BallRolling.Gameplay
{
    /// <summary>迷路のジオメトリ寸法（Step2_MazeBuildParameters と同値を GameController 経由で受け取る）。</summary>
    public struct MazeGeometry
    {
        public float CellSize;
        public float WallHeight;
        public float WallThickness;
        public float FloorThickness;
    }

    /// <summary>迷路の構成マテリアル。未設定のものは GameController 側でフォールバック生成される前提。</summary>
    public struct MazeMaterials
    {
        public Material Wall;
        public Material Floor;
        public Material EntranceMarker;
        public Material ExitMarker;
        public Material Item;
    }

    /// <summary>ランタイム用の迷路ビルダー。配置・寸法は Step2_SceneMazeBuilder と完全一致させる（出口セル床なし・入り口セルのみ天井穴・壁下端0.1めり込み）。</summary>
    public class MazeBuilder
    {
        private const float CeilingThickness = 0.2f;
        private const float WallSinkOffset = 0.1f;
        private const float MarkerThickness = 0.05f;
        private const float MarkerLocalY = -1.2f;

        /// <summary>root直下の Floor/Walls/Ceiling/Markers/Item を破棄する（カメラ・玉など他の子は影響しない）。</summary>
        public void Clear(Transform root)
        {
            if (root == null)
                return;

            var parentNames = new[] { "Floor", "Walls", "Ceiling", "Markers", "Item" };
            foreach (var parentName in parentNames)
            {
                var child = root.Find(parentName);
                if (child != null)
                    Object.Destroy(child.gameObject);
            }
        }

        /// <summary>迷路を root 直下に Floor/Walls/Ceiling/Markers として生成する。</summary>
        public void Build(MazeModel maze, MazeGeometry geometry, Transform root, MazeMaterials materials)
        {
            BuildFloor(maze, geometry, root, materials.Floor);
            BuildWalls(maze, geometry, root, materials.Wall);
            BuildCeiling(maze, geometry, root);
            BuildMarkers(maze, geometry, root, materials.EntranceMarker, materials.ExitMarker);
        }

        /// <summary>行き止まりから1箇所選び回転Cube+トリガーのアイテムを生成する。候補なし/失敗時は null。</summary>
        public GameObject BuildItem(MazeModel maze, MazeGeometry geometry, Transform root,
            float itemSize, float itemLocalY, Material material, int seed)
        {
            var cell = ItemPlacer.SelectCell(maze, new System.Random(seed));
            if (cell == null)
            {
                Debug.LogWarning("ItemPlacer: 配置可能な行き止まりがないためアイテムを配置しません（ゲームは継続します）。");
                return null;
            }

            var itemParent = new GameObject("Item");
            itemParent.transform.SetParent(root, false);

            var item = GameObject.CreatePrimitive(PrimitiveType.Cube);
            item.name = "ItemPickup";
            item.transform.SetParent(itemParent.transform, false);
            item.transform.localPosition = new Vector3(
                (cell.Value.x + 0.5f) * geometry.CellSize, itemLocalY, (cell.Value.y + 0.5f) * geometry.CellSize);
            item.transform.localScale = new Vector3(itemSize, itemSize, itemSize);
            item.GetComponent<Renderer>().sharedMaterial = material;
            // 玉を物理的に妨げないトリガー。Rigidbodyは持たせない（静的トリガーへの玉側侵入で判定する）
            item.GetComponent<Collider>().isTrigger = true;
            item.AddComponent<ItemPickup>();
            return item;
        }

        private static void BuildFloor(MazeModel maze, MazeGeometry g, Transform root, Material material)
        {
            var floorParent = new GameObject("Floor");
            floorParent.transform.SetParent(root, false);

            for (var y = 0; y < maze.Height; y++)
            {
                for (var x = 0; x < maze.Width; x++)
                {
                    if (new Vector2Int(x, y) == maze.Exit)
                        continue; // 出口セルのみ穴にする（入り口は床あり=玉が着地して迷路に入る）

                    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.name = $"Floor_{x}_{y}";
                    cube.transform.SetParent(floorParent.transform, false);
                    cube.transform.localPosition = new Vector3(
                        (x + 0.5f) * g.CellSize, -g.FloorThickness / 2f, (y + 0.5f) * g.CellSize);
                    cube.transform.localScale = new Vector3(g.CellSize, g.FloorThickness, g.CellSize);
                    cube.GetComponent<Renderer>().sharedMaterial = material;
                }
            }
        }

        private static void BuildWalls(MazeModel maze, MazeGeometry g, Transform root, Material material)
        {
            var wallsParent = new GameObject("Walls");
            wallsParent.transform.SetParent(root, false);

            for (var y = 0; y < maze.Height; y++)
            {
                for (var x = 0; x < maze.Width; x++)
                {
                    var cell = maze.GetCell(x, y);
                    if (cell.North)
                        CreateWall(wallsParent.transform, material, g,
                            new Vector3((x + 0.5f) * g.CellSize, g.WallHeight / 2f, (y + 1f) * g.CellSize),
                            isVertical: false, $"Wall_N_{x}_{y}");
                    if (cell.East)
                        CreateWall(wallsParent.transform, material, g,
                            new Vector3((x + 1f) * g.CellSize, g.WallHeight / 2f, (y + 0.5f) * g.CellSize),
                            isVertical: true, $"Wall_E_{x}_{y}");
                    if (y == 0 && cell.South)
                        CreateWall(wallsParent.transform, material, g,
                            new Vector3((x + 0.5f) * g.CellSize, g.WallHeight / 2f, 0f),
                            isVertical: false, $"Wall_S_{x}_0");
                    if (x == 0 && cell.West)
                        CreateWall(wallsParent.transform, material, g,
                            new Vector3(0f, g.WallHeight / 2f, (y + 0.5f) * g.CellSize),
                            isVertical: true, $"Wall_W_0_{y}");
                }
            }
        }

        private static void CreateWall(Transform parent, Material material, MazeGeometry g, Vector3 position, bool isVertical, string name)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent, false);
            // 下端を床に0.1だけめり込ませ、壁と床の接縫（すり抜けやすい縁）を消す
            wall.transform.localPosition = position - new Vector3(0f, WallSinkOffset, 0f);
            wall.transform.localScale = isVertical
                ? new Vector3(g.WallThickness, g.WallHeight, g.CellSize + g.WallThickness)
                : new Vector3(g.CellSize + g.WallThickness, g.WallHeight, g.WallThickness);
            wall.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void BuildCeiling(MazeModel maze, MazeGeometry g, Transform root)
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
                        (x + 0.5f) * g.CellSize, g.WallHeight - CeilingThickness, (y + 0.5f) * g.CellSize);
                    var box = tile.AddComponent<BoxCollider>();
                    box.size = new Vector3(g.CellSize, CeilingThickness, g.CellSize);
                }
            }
        }

        private static void BuildMarkers(MazeModel maze, MazeGeometry g, Transform root, Material entranceMaterial, Material exitMaterial)
        {
            var markersParent = new GameObject("Markers");
            markersParent.transform.SetParent(root, false);

            CreateMarker(markersParent.transform, maze.Entrance, g, "EntranceMarker", entranceMaterial);
            CreateMarker(markersParent.transform, maze.Exit, g, "ExitMarker", exitMaterial);
        }

        private static void CreateMarker(Transform parent, Vector2Int cell, MazeGeometry g, string name, Material material)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = name;
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = new Vector3(
                (cell.x + 0.5f) * g.CellSize, MarkerLocalY, (cell.y + 0.5f) * g.CellSize);
            marker.transform.localScale = new Vector3(g.CellSize, MarkerThickness, g.CellSize);
            marker.GetComponent<Renderer>().sharedMaterial = material;
            // 視認用マーカーのため当たり判定は持たせない
            Object.Destroy(marker.GetComponent<Collider>());
        }
    }
}
