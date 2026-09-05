using System;
using UnityEngine;

namespace BallRolling.Gameplay.Logic
{
    /// <summary>迷路の1マス。各方向の壁の有無を保持する（true = 壁あり）。</summary>
    [Serializable]
    public struct Cell
    {
        public bool North;
        public bool South;
        public bool East;
        public bool West;
    }

    /// <summary>迷路データと入り口・出口を保持する。</summary>
    public class MazeModel
    {
        public int Width { get; }
        public int Height { get; }
        public Cell[] Cells { get; }
        public Vector2Int Entrance { get; }
        public Vector2Int Exit { get; }

        public MazeModel(int width, int height, Vector2Int entrance, Vector2Int exit)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException("width, height", "迷路サイズは1以上である必要があります");

            Width = width;
            Height = height;
            Cells = new Cell[width * height];
            Entrance = entrance;
            Exit = exit;

            for (var i = 0; i < Cells.Length; i++)
                Cells[i] = new Cell { North = true, South = true, East = true, West = true };
        }

        public Cell GetCell(int x, int y)
        {
            ValidateRange(x, y);
            return Cells[y * Width + x];
        }

        public void SetCell(int x, int y, Cell cell)
        {
            ValidateRange(x, y);
            Cells[y * Width + x] = cell;
        }

        public bool IsInside(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        private void ValidateRange(int x, int y)
        {
            if (!IsInside(x, y))
                throw new ArgumentOutOfRangeException($"({x}, {y})", "セル座標が迷路範囲外です");
        }
    }
}
