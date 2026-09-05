using System.Collections.Generic;
using UnityEngine;

namespace BallRolling.Gameplay.Logic
{
    /// <summary>再帰的バックトラッカーで完全迷路（全セル到達可能）を生成する。</summary>
    public static class MazeGenerator
    {
        /// <summary>指定サイズ・シードで迷路を生成する。全セルが接続された完全迷路を保証する。</summary>
        public static MazeModel Generate(int width, int height, int seed)
        {
            var maze = new MazeModel(width, height, new Vector2Int(0, 0), new Vector2Int(width - 1, height - 1));
            var random = new System.Random(seed);

            var visited = new bool[width, height];
            var stack = new Stack<Vector2Int>();
            var start = new Vector2Int(random.Next(width), random.Next(height));
            stack.Push(start);
            visited[start.x, start.y] = true;

            while (stack.Count > 0)
            {
                var current = stack.Peek();
                var neighbors = FindUnvisitedNeighbors(current, visited);
                if (neighbors.Count == 0)
                {
                    stack.Pop();
                    continue;
                }

                var next = neighbors[random.Next(neighbors.Count)];
                RemoveWall(maze, current, next);
                visited[next.x, next.y] = true;
                stack.Push(next);
            }

            return maze;
        }

        /// <summary>壁を3方向持つセル（行き止まり）を返す。</summary>
        public static List<Vector2Int> FindDeadEnds(MazeModel maze)
        {
            var deadEnds = new List<Vector2Int>();
            for (var y = 0; y < maze.Height; y++)
            {
                for (var x = 0; x < maze.Width; x++)
                {
                    if (CountWalls(maze.GetCell(x, y)) == 3)
                        deadEnds.Add(new Vector2Int(x, y));
                }
            }
            return deadEnds;
        }

        private static int CountWalls(Cell cell)
        {
            return (cell.North ? 1 : 0) + (cell.South ? 1 : 0) + (cell.East ? 1 : 0) + (cell.West ? 1 : 0);
        }

        private static void RemoveWall(MazeModel maze, Vector2Int from, Vector2Int to)
        {
            var fromCell = maze.GetCell(from.x, from.y);
            var toCell = maze.GetCell(to.x, to.y);
            var delta = to - from;

            if (delta == Vector2Int.up)
            {
                fromCell.North = false;
                toCell.South = false;
            }
            else if (delta == Vector2Int.down)
            {
                fromCell.South = false;
                toCell.North = false;
            }
            else if (delta == Vector2Int.right)
            {
                fromCell.East = false;
                toCell.West = false;
            }
            else if (delta == Vector2Int.left)
            {
                fromCell.West = false;
                toCell.East = false;
            }

            maze.SetCell(from.x, from.y, fromCell);
            maze.SetCell(to.x, to.y, toCell);
        }

        private static List<Vector2Int> FindUnvisitedNeighbors(Vector2Int cell, bool[,] visited)
        {
            var width = visited.GetLength(0);
            var height = visited.GetLength(1);
            var result = new List<Vector2Int>(4);

            if (cell.y + 1 < height && !visited[cell.x, cell.y + 1])
                result.Add(new Vector2Int(cell.x, cell.y + 1));
            if (cell.y - 1 >= 0 && !visited[cell.x, cell.y - 1])
                result.Add(new Vector2Int(cell.x, cell.y - 1));
            if (cell.x + 1 < width && !visited[cell.x + 1, cell.y])
                result.Add(new Vector2Int(cell.x + 1, cell.y));
            if (cell.x - 1 >= 0 && !visited[cell.x - 1, cell.y])
                result.Add(new Vector2Int(cell.x - 1, cell.y));

            return result;
        }
    }
}
