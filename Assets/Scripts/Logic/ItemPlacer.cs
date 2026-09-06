using System.Collections.Generic;
using UnityEngine;

namespace BallRolling.Gameplay.Logic
{
    /// <summary>迷路の行き止まりからアイテム配置セルを選ぶ純粋ロジック（乱数は注入でテスト再現性を確保）。</summary>
    public static class ItemPlacer
    {
        /// <summary>出口セルを通行不能とした到達探索（BFS）で、入り口から到達可能な行き止まりのリストを返す。入り口・出口自体は候補から除外する。</summary>
        public static List<Vector2Int> FindReachableDeadEnds(MazeModel maze)
        {
            var visited = new bool[maze.Width, maze.Height];
            var queue = new Queue<Vector2Int>();
            visited[maze.Entrance.x, maze.Entrance.y] = true;
            queue.Enqueue(maze.Entrance);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var neighbor in FindOpenNeighbors(maze, current))
                {
                    if (visited[neighbor.x, neighbor.y])
                        continue;
                    visited[neighbor.x, neighbor.y] = true;
                    queue.Enqueue(neighbor);
                }
            }

            var deadEnds = new List<Vector2Int>();
            for (var y = 0; y < maze.Height; y++)
            {
                for (var x = 0; x < maze.Width; x++)
                {
                    if (!visited[x, y])
                        continue;

                    var position = new Vector2Int(x, y);
                    // 入り口セルは玉の落下地点で天井穴があり、出口セルは床が無くアイテムが落下するため配置不可
                    if (position == maze.Entrance || position == maze.Exit)
                        continue;

                    if (CountWalls(maze.GetCell(x, y)) == 3)
                        deadEnds.Add(position);
                }
            }
            return deadEnds;
        }

        /// <summary>到達可能な行き止まりから乱数で1つ選ぶ。候補が空なら null（アイテムなしでゲーム継続）。</summary>
        public static Vector2Int? SelectCell(MazeModel maze, System.Random random)
        {
            var candidates = FindReachableDeadEnds(maze);
            if (candidates.Count == 0)
                return null;

            return candidates[random.Next(candidates.Count)];
        }

        /// <summary>壁の無い方向かつ迷路内の隣接セルを返す。出口セルは床が穴で通過＝ゴール落下のため流入・流出とも通行不能とする。</summary>
        private static List<Vector2Int> FindOpenNeighbors(MazeModel maze, Vector2Int position)
        {
            var cell = maze.GetCell(position.x, position.y);
            var result = new List<Vector2Int>(4);

            if (!cell.North)
                TryAddNeighbor(result, maze, position + Vector2Int.up);
            if (!cell.South)
                TryAddNeighbor(result, maze, position + Vector2Int.down);
            if (!cell.East)
                TryAddNeighbor(result, maze, position + Vector2Int.right);
            if (!cell.West)
                TryAddNeighbor(result, maze, position + Vector2Int.left);

            return result;
        }

        private static void TryAddNeighbor(List<Vector2Int> result, MazeModel maze, Vector2Int neighbor)
        {
            if (neighbor == maze.Exit)
                return; // 出口への流入を遮断（出口からの流出は出口をキューに入れないため生じない）

            if (maze.IsInside(neighbor.x, neighbor.y))
                result.Add(neighbor);
        }

        private static int CountWalls(Cell cell)
        {
            return (cell.North ? 1 : 0) + (cell.South ? 1 : 0) + (cell.East ? 1 : 0) + (cell.West ? 1 : 0);
        }
    }
}
