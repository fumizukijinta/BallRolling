using System;
using UnityEngine;

namespace BallRolling.EditorTools
{
    /// <summary>ステップ4のゲームループ構築パラメータ（Step4_GameLoopSetupWindow で編集する）。</summary>
    [Serializable]
    public class Step4_PlayParameters
    {
        public float TimeLimitSeconds = 180f;
        public float BallSpawnHeight = 3f;
        public float GoalFallLocalY = -1.5f;
        public int TimerFontSize = 60;
        public int MessageFontSize = 48;
        public Color TimerColor = Color.white;
        public Color MessageColor = new Color(1f, 0.85f, 0.2f);
    }
}
