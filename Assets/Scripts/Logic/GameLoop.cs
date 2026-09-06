using UnityEngine;

namespace BallRolling.Gameplay.Logic
{
    /// <summary>ゲームの進行状態。</summary>
    public enum GamePhase
    {
        WaitingToStart,
        Playing,
        Goal,
        TimeUp
    }

    /// <summary>純粋C#のゲームループ状態マシン+タイマー。不正遷移は例外を投げず無視する。</summary>
    public class GameLoop
    {
        public const float DefaultTimeLimitSeconds = 180f;

        private float _timeLimitSeconds = DefaultTimeLimitSeconds;

        /// <summary>現在のフェーズ。</summary>
        public GamePhase Phase { get; private set; }

        /// <summary>残り時間（秒）。Playing中のみ減算される。</summary>
        public float RemainingSeconds { get; private set; }

        /// <summary>経過時間（秒）= 時間上限 - 残り時間。</summary>
        public float ElapsedSeconds => _timeLimitSeconds - RemainingSeconds;

        /// <summary>リスタート可能（Goal/TimeUp）かどうか。</summary>
        public bool CanRestart => Phase == GamePhase.Goal || Phase == GamePhase.TimeUp;

        public GameLoop()
        {
            RemainingSeconds = _timeLimitSeconds;
        }

        /// <summary>時間上限を設定する。待機中は残り時間も全回復させる。</summary>
        public void Configure(float timeLimitSeconds)
        {
            _timeLimitSeconds = Mathf.Max(0f, timeLimitSeconds);
            if (Phase == GamePhase.WaitingToStart)
                RemainingSeconds = _timeLimitSeconds;
        }

        /// <summary>WaitingToStart→Playing。他フェーズでは無視される。</summary>
        public void StartGame()
        {
            if (Phase != GamePhase.WaitingToStart)
                return;

            Phase = GamePhase.Playing;
            RemainingSeconds = _timeLimitSeconds;
        }

        /// <summary>Playing中のみ残り時間を減算する。0でTimeUpへ遷移し、以後のTickでは不変。</summary>
        public void Tick(float deltaTime)
        {
            if (Phase != GamePhase.Playing)
                return;

            RemainingSeconds = Mathf.Max(0f, RemainingSeconds - Mathf.Max(0f, deltaTime));
            if (RemainingSeconds <= 0f)
                Phase = GamePhase.TimeUp;
        }

        /// <summary>Playing→Goal。他フェーズでは無視される。</summary>
        public void NotifyGoal()
        {
            if (Phase != GamePhase.Playing)
                return;

            Phase = GamePhase.Goal;
        }

        /// <summary>Goal/TimeUp→WaitingToStart。残り時間を全回復させる。他フェーズでは無視される。</summary>
        public void Restart()
        {
            if (!CanRestart)
                return;

            Phase = GamePhase.WaitingToStart;
            RemainingSeconds = _timeLimitSeconds;
        }

        /// <summary>秒数を切り下げて "mm:ss" 形式にする（180→"03:00"、0→"00:00"）。</summary>
        public static string FormatTime(float seconds)
        {
            var total = Mathf.FloorToInt(Mathf.Max(0f, seconds));
            return $"{total / 60:00}:{total % 60:00}";
        }
    }
}
