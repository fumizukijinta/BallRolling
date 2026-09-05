using System;
using UnityEngine;

namespace BallRolling.EditorTools
{
    /// <summary>迷路生成の調整可能パラメータ（Step2_MazeBuilderWindow で編集する）。</summary>
    [Serializable]
    public class Step2_MazeBuildParameters
    {
        public int Width = 10;
        public int Height = 10;
        public int Seed = 42;
        public bool IsRandomSeed = true;
        public float CellSize = 1f;
        public float WallHeight = 1.2f;
        public float WallThickness = 0.2f;
        public float FloorThickness = 0.5f;
        public Color WallColor = new Color(0.75f, 0.75f, 0.8f);
        public Color FloorColor = new Color(0.55f, 0.6f, 0.65f);
        public Color EntranceColor = new Color(0.2f, 0.85f, 0.3f);
        public Color ExitColor = new Color(0.9f, 0.25f, 0.25f);

        public int ResolveSeed()
        {
            return IsRandomSeed ? UnityEngine.Random.Range(int.MinValue, int.MaxValue) : Seed;
        }
    }
}
