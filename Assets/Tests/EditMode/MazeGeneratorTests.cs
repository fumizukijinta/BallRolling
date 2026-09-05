using System.Collections.Generic;
using BallRolling.Gameplay.Logic;
using NUnit.Framework;
using UnityEngine;

namespace BallRolling.Tests.EditMode
{
    public class MazeGeneratorTests
    {
        private const int Size = 10;

        [Test]
        public void Generate_ReturnsRequestedSize()
        {
            var maze = MazeGenerator.Generate(Size, Size, seed: 1);
            Assert.AreEqual(Size, maze.Width);
            Assert.AreEqual(Size, maze.Height);
        }

        [Test]
        public void Generate_AllCellsReachableFromEntrance()
        {
            var maze = MazeGenerator.Generate(Size, Size, seed: 42);
            Assert.IsTrue(IsAllReachable(maze), "全セルが入り口から到達可能であること");
        }

        [Test]
        public void Generate_WallsSymmetricBetweenNeighbors()
        {
            var maze = MazeGenerator.Generate(Size, Size, seed: 7);

            for (var y = 0; y < maze.Height; y++)
            {
                for (var x = 0; x < maze.Width; x++)
                {
                    var cell = maze.GetCell(x, y);
                    if (x + 1 < maze.Width)
                    {
                        var east = maze.GetCell(x + 1, y);
                        Assert.AreEqual(cell.East, east.West, $"({x},{y})のEast壁と({x + 1},{y})のWest壁が不一致");
                    }
                    if (y + 1 < maze.Height)
                    {
                        var north = maze.GetCell(x, y + 1);
                        Assert.AreEqual(cell.North, north.South, $"({x},{y})のNorth壁と({x},{y + 1})のSouth壁が不一致");
                    }
                }
            }
        }

        [Test]
        public void Generate_SameSeedProducesSameMaze()
        {
            var a = MazeGenerator.Generate(Size, Size, 123);
            var b = MazeGenerator.Generate(Size, Size, 123);
            CollectionAssert.AreEqual(a.Cells, b.Cells);
        }

        [Test]
        public void Generate_DifferentSeedProducesDifferentMaze()
        {
            var a = MazeGenerator.Generate(Size, Size, 1);
            var b = MazeGenerator.Generate(Size, Size, 2);
            CollectionAssert.AreNotEqual(a.Cells, b.Cells);
        }

        [Test]
        public void FindDeadEnds_ReturnsAtLeastOneDeadEnd()
        {
            var maze = MazeGenerator.Generate(Size, Size, seed: 5);
            Assert.Greater(MazeGenerator.FindDeadEnds(maze).Count, 0, "完全迷路には行き止まりが存在する");
        }

        [Test]
        public void FindDeadEnds_MayIncludeExit_CellStructureIsNeutral()
        {
            // 出口セルが行き止まりになることは許容する。入り口/出口の除外は
            // アイテム配置の段階（配置候補から除く）で扱う（design.md 参照）。
            var maze = MazeGenerator.Generate(Size, Size, seed: 5);
            var deadEnds = MazeGenerator.FindDeadEnds(maze);
            CollectionAssert.IsNotEmpty(deadEnds);
        }

        private static bool IsAllReachable(MazeModel maze)
        {
            var visited = new bool[maze.Width, maze.Height];
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(maze.Entrance);
            visited[maze.Entrance.x, maze.Entrance.y] = true;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var cell = maze.GetCell(current.x, current.y);

                if (!cell.North && maze.IsInside(current.x, current.y + 1) && !visited[current.x, current.y + 1])
                {
                    visited[current.x, current.y + 1] = true;
                    queue.Enqueue(new Vector2Int(current.x, current.y + 1));
                }
                if (!cell.South && maze.IsInside(current.x, current.y - 1) && !visited[current.x, current.y - 1])
                {
                    visited[current.x, current.y - 1] = true;
                    queue.Enqueue(new Vector2Int(current.x, current.y - 1));
                }
                if (!cell.East && maze.IsInside(current.x + 1, current.y) && !visited[current.x + 1, current.y])
                {
                    visited[current.x + 1, current.y] = true;
                    queue.Enqueue(new Vector2Int(current.x + 1, current.y));
                }
                if (!cell.West && maze.IsInside(current.x - 1, current.y) && !visited[current.x - 1, current.y])
                {
                    visited[current.x - 1, current.y] = true;
                    queue.Enqueue(new Vector2Int(current.x - 1, current.y));
                }
            }

            for (var y = 0; y < maze.Height; y++)
                for (var x = 0; x < maze.Width; x++)
                    if (!visited[x, y])
                        return false;

            return true;
        }
    }
}
