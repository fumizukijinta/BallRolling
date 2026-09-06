using System.Collections.Generic;
using BallRolling.Gameplay.Logic;
using NUnit.Framework;
using UnityEngine;

namespace BallRolling.Tests.EditMode
{
    public class ItemPlacerTests
    {
        [Test]
        public void FindReachableDeadEnds_ExcludesEntranceAndExitFromCandidates()
        {
            // 入り口・出口が行き止まりであっても候補から除外され、到達可能な行き止まりのみ返る
            var maze = CreateMaze(3, 2, new Vector2Int(0, 0), new Vector2Int(2, 1));
            Open(maze, new Vector2Int(0, 0), new Vector2Int(1, 0)); // 入り口 → 通路
            Open(maze, new Vector2Int(1, 0), new Vector2Int(2, 0)); // 通路 → 到達可能な行き止まり
            Open(maze, new Vector2Int(1, 0), new Vector2Int(1, 1));
            Open(maze, new Vector2Int(1, 1), new Vector2Int(2, 1)); // 通路 → 出口

            var deadEnds = ItemPlacer.FindReachableDeadEnds(maze);

            Assert.AreEqual(1, deadEnds.Count);
            Assert.Contains(new Vector2Int(2, 0), deadEnds);
            CollectionAssert.DoesNotContain(deadEnds, new Vector2Int(0, 0), "入り口は候補から除外される");
            CollectionAssert.DoesNotContain(deadEnds, new Vector2Int(2, 1), "出口は候補から除外される");
        }

        [Test]
        public void FindReachableDeadEnds_ExcludesDeadEndOnlyReachableThroughExit()
        {
            // 出口より奥の行き止まり（出口を通らないと到達不能）は候補から除外される
            var maze = CreateMaze(3, 1, new Vector2Int(0, 0), new Vector2Int(1, 0));
            Open(maze, new Vector2Int(0, 0), new Vector2Int(1, 0)); // 入り口 → 出口
            Open(maze, new Vector2Int(1, 0), new Vector2Int(2, 0)); // 出口の奥 → 行き止まり

            // 通常の行き止まり判定では (2,0) が候補に挙がる
            var allDeadEnds = MazeGenerator.FindDeadEnds(maze);
            CollectionAssert.Contains(allDeadEnds, new Vector2Int(2, 0), "前提: 出口の奥のセルは行き止まり");

            // 出口を通行不能とした到達探索では除外される
            var reachable = ItemPlacer.FindReachableDeadEnds(maze);
            Assert.AreEqual(0, reachable.Count);
            CollectionAssert.DoesNotContain(reachable, new Vector2Int(2, 0));
        }

        [Test]
        public void SelectCell_ReturnsNullWhenNoReachableDeadEnds()
        {
            // 到達可能な行き止まりが無い（出口の奥にしか行き止まりが無い）→ null
            var maze = CreateMaze(3, 1, new Vector2Int(0, 0), new Vector2Int(1, 0));
            Open(maze, new Vector2Int(0, 0), new Vector2Int(1, 0));
            Open(maze, new Vector2Int(1, 0), new Vector2Int(2, 0));

            var selected = ItemPlacer.SelectCell(maze, new System.Random(7));

            Assert.IsFalse(selected.HasValue);
        }

        [Test]
        public void SelectCell_SameSeedReturnsSameCell()
        {
            // seed 再現性: 同 seed なら同セル
            for (var seed = 0; seed < 20; seed++)
            {
                var maze = MazeGenerator.Generate(10, 10, seed);
                var first = ItemPlacer.SelectCell(maze, new System.Random(seed));
                var second = ItemPlacer.SelectCell(maze, new System.Random(seed));

                Assert.IsTrue(first.HasValue, "seed=" + seed);
                Assert.IsTrue(second.HasValue, "seed=" + seed);
                Assert.AreEqual(first.Value, second.Value, "seed=" + seed);
            }
        }

        [Test]
        public void SelectCell_ReturnsOnlyCandidateWhenSingleCandidate()
        {
            // 候補1つ → 常にそのセル（出口(1,1)は行き止まりだが除外、(0,1) のみが候補）
            var maze = CreateMaze(2, 2, new Vector2Int(0, 0), new Vector2Int(1, 1));
            Open(maze, new Vector2Int(0, 0), new Vector2Int(1, 0));
            Open(maze, new Vector2Int(1, 0), new Vector2Int(1, 1));
            Open(maze, new Vector2Int(0, 0), new Vector2Int(0, 1));

            for (var i = 0; i < 10; i++)
            {
                var selected = ItemPlacer.SelectCell(maze, new System.Random(i));
                Assert.IsTrue(selected.HasValue);
                Assert.AreEqual(new Vector2Int(0, 1), selected.Value);
            }
        }

        [Test]
        public void SelectCell_ReturnsCellWithinReachableDeadEnds_AcrossManySeeds()
        {
            // 結果は必ず FindReachableDeadEnds の要素（＝入り口から出口を通らず到達可能）で、入り口・出口は常に除外
            for (var seed = 0; seed < 50; seed++)
            {
                var maze = MazeGenerator.Generate(10, 10, seed);
                var expected = ItemPlacer.FindReachableDeadEnds(maze);
                var selected = ItemPlacer.SelectCell(maze, new System.Random(seed));

                if (expected.Count == 0)
                {
                    Assert.IsFalse(selected.HasValue, "seed=" + seed);
                    continue;
                }

                Assert.IsTrue(selected.HasValue, "seed=" + seed);
                CollectionAssert.Contains(expected, selected.Value, "seed=" + seed);
                Assert.AreNotEqual(maze.Entrance, selected.Value, "seed=" + seed);
                Assert.AreNotEqual(maze.Exit, selected.Value, "seed=" + seed);
            }
        }

        private static MazeModel CreateMaze(int width, int height, Vector2Int entrance, Vector2Int exit)
        {
            // 全壁の迷路を返す（通路は Open で明示的に開ける）
            return new MazeModel(width, height, entrance, exit);
        }

        private static void Open(MazeModel maze, Vector2Int a, Vector2Int b)
        {
            var cellA = maze.GetCell(a.x, a.y);
            var cellB = maze.GetCell(b.x, b.y);
            var delta = b - a;

            if (delta == Vector2Int.up)
            {
                cellA.North = false;
                cellB.South = false;
            }
            else if (delta == Vector2Int.down)
            {
                cellA.South = false;
                cellB.North = false;
            }
            else if (delta == Vector2Int.right)
            {
                cellA.East = false;
                cellB.West = false;
            }
            else if (delta == Vector2Int.left)
            {
                cellA.West = false;
                cellB.East = false;
            }

            maze.SetCell(a.x, a.y, cellA);
            maze.SetCell(b.x, b.y, cellB);
        }
    }
}
