using System;
using UnityEngine;

namespace BallRolling.EditorTools
{
    /// <summary>ステップ3のプレイ環境パラメータ（玉の物理とボードの傾き）。</summary>
    [Serializable]
    public class Step3_PlayParameters
    {
        public float BallSize = 0.8f;
        public float BallMass = 1f;
        public float BallDrag = 0.1f;
        public float BallAngularDrag = 0.05f;
        public float Friction = 0.6f;
        public float Bounciness = 0.1f;
        public Color BallColor = new Color(0.2f, 0.45f, 0.95f);
        public float MaxTiltAngle = 15f;
        public float TiltSpeed = 5f;
        public float BallSpawnHeight = 3f;
    }
}
