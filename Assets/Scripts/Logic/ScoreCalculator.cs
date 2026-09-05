using UnityEngine;

namespace BallRolling.Gameplay.Logic
{
    /// <summary>クリア時間とアイテム取得から評点と星表示を計算する。</summary>
    public static class ScoreCalculator
    {
        public const int MaxScore = 3;
        private const float FirstTierSeconds = 60f;
        private const float SecondTierSeconds = 120f;

        /// <summary>評点 = 時間ボーナス（1分以内+2 / 2分以内+1 の排他）+ アイテム取得+1。</summary>
        public static int Calculate(float clearTimeSeconds, bool hasItem)
        {
            var timeScore = 0;
            if (clearTimeSeconds <= FirstTierSeconds)
                timeScore = 2;
            else if (clearTimeSeconds <= SecondTierSeconds)
                timeScore = 1;

            return timeScore + (hasItem ? 1 : 0);
        }

        /// <summary>評点を星文字列（★★☆ 等）で返す。</summary>
        public static string RenderStars(int score)
        {
            var clamped = Mathf.Clamp(score, 0, MaxScore);
            return new string('★', clamped) + new string('☆', MaxScore - clamped);
        }
    }
}
