using BallRolling.Gameplay.Logic;
using NUnit.Framework;

namespace BallRolling.Tests.EditMode
{
    public class GameLoopTests
    {
        [Test]
        public void InitialState_IsWaitingToStartWithFullTime()
        {
            var loop = new GameLoop();

            Assert.AreEqual(GamePhase.WaitingToStart, loop.Phase);
            Assert.AreEqual(GameLoop.DefaultTimeLimitSeconds, loop.RemainingSeconds);
            Assert.IsFalse(loop.CanRestart);
        }

        [Test]
        public void StartGame_TransitionsToPlayingWithFullTime()
        {
            var loop = new GameLoop();

            loop.StartGame();

            Assert.AreEqual(GamePhase.Playing, loop.Phase);
            Assert.AreEqual(GameLoop.DefaultTimeLimitSeconds, loop.RemainingSeconds);
        }

        [Test]
        public void Tick_DecreasesRemainingSeconds()
        {
            var loop = new GameLoop();
            loop.StartGame();

            loop.Tick(1f);
            loop.Tick(0.5f);

            Assert.AreEqual(GameLoop.DefaultTimeLimitSeconds - 1.5f, loop.RemainingSeconds, 0.0001f);
            Assert.AreEqual(GamePhase.Playing, loop.Phase);
        }

        [Test]
        public void Tick_IgnoredWhenNotPlaying()
        {
            var loop = new GameLoop();

            loop.Tick(10f);

            Assert.AreEqual(GamePhase.WaitingToStart, loop.Phase);
            Assert.AreEqual(GameLoop.DefaultTimeLimitSeconds, loop.RemainingSeconds);
        }

        [Test]
        public void Tick_ReachesZero_TransitionsToTimeUp()
        {
            var loop = new GameLoop();
            loop.StartGame();

            loop.Tick(GameLoop.DefaultTimeLimitSeconds);

            Assert.AreEqual(GamePhase.TimeUp, loop.Phase);
            Assert.AreEqual(0f, loop.RemainingSeconds);
            Assert.IsTrue(loop.CanRestart);
        }

        [Test]
        public void Tick_AfterTimeUp_RemainsZero()
        {
            var loop = new GameLoop();
            loop.StartGame();
            loop.Tick(GameLoop.DefaultTimeLimitSeconds);

            loop.Tick(30f);
            loop.Tick(5f);

            Assert.AreEqual(GamePhase.TimeUp, loop.Phase);
            Assert.AreEqual(0f, loop.RemainingSeconds);
        }

        [Test]
        public void StartGame_DuringPlaying_IsIgnored()
        {
            var loop = new GameLoop();
            loop.StartGame();
            loop.Tick(10f);

            loop.StartGame();

            Assert.AreEqual(GamePhase.Playing, loop.Phase);
            Assert.AreEqual(GameLoop.DefaultTimeLimitSeconds - 10f, loop.RemainingSeconds, 0.0001f);
        }

        [Test]
        public void NotifyGoal_DuringWaitingToStart_IsIgnored()
        {
            var loop = new GameLoop();

            loop.NotifyGoal();

            Assert.AreEqual(GamePhase.WaitingToStart, loop.Phase);
            Assert.IsFalse(loop.CanRestart);
        }

        [Test]
        public void NotifyGoal_TransitionsToGoal_AndKeepsElapsedTime()
        {
            var loop = new GameLoop();
            loop.StartGame();
            loop.Tick(72.5f);

            loop.NotifyGoal();

            Assert.AreEqual(GamePhase.Goal, loop.Phase);
            Assert.AreEqual(72.5f, loop.ElapsedSeconds, 0.0001f);
            Assert.IsTrue(loop.CanRestart);
        }

        [Test]
        public void Tick_DuringGoal_IsIgnored()
        {
            var loop = new GameLoop();
            loop.StartGame();
            loop.Tick(20f);
            loop.NotifyGoal();

            loop.Tick(5f);

            Assert.AreEqual(20f, loop.ElapsedSeconds, 0.0001f);
        }

        [Test]
        public void Restart_FromGoal_ReturnsToWaitingToStartWithFullTime()
        {
            var loop = new GameLoop();
            loop.StartGame();
            loop.Tick(45f);
            loop.NotifyGoal();

            loop.Restart();

            Assert.AreEqual(GamePhase.WaitingToStart, loop.Phase);
            Assert.AreEqual(GameLoop.DefaultTimeLimitSeconds, loop.RemainingSeconds);
            Assert.AreEqual(0f, loop.ElapsedSeconds, 0.0001f);
            Assert.IsFalse(loop.CanRestart);
        }

        [Test]
        public void Restart_FromTimeUp_ReturnsToWaitingToStartWithFullTime()
        {
            var loop = new GameLoop();
            loop.StartGame();
            loop.Tick(GameLoop.DefaultTimeLimitSeconds);

            loop.Restart();

            Assert.AreEqual(GamePhase.WaitingToStart, loop.Phase);
            Assert.AreEqual(GameLoop.DefaultTimeLimitSeconds, loop.RemainingSeconds);
            Assert.IsFalse(loop.CanRestart);
        }

        [Test]
        public void Restart_DuringWaitingToStart_IsIgnored()
        {
            var loop = new GameLoop();

            loop.Restart();

            Assert.AreEqual(GamePhase.WaitingToStart, loop.Phase);
        }

        [Test]
        public void Configure_RestoresRemainingWhenWaiting()
        {
            var loop = new GameLoop();

            loop.Configure(60f);

            Assert.AreEqual(60f, loop.RemainingSeconds);
        }

        [TestCase(180f, "03:00")]
        [TestCase(61.9f, "01:01")]
        [TestCase(0f, "00:00")]
        [TestCase(59.9f, "00:59")]
        [TestCase(-5f, "00:00")]
        public void FormatTime_ReturnsFloorMinutesSeconds(float seconds, string expected)
        {
            Assert.AreEqual(expected, GameLoop.FormatTime(seconds));
        }

        [TestCase(59.9f, 2)]
        [TestCase(60f, 2)]
        [TestCase(60.1f, 1)]
        [TestCase(119.9f, 1)]
        [TestCase(180f, 0)]
        public void Integration_ScoreCalculator_UsesElapsedTimeWithoutItem(float elapsed, int expectedScore)
        {
            var loop = new GameLoop();
            loop.Configure(GameLoop.DefaultTimeLimitSeconds);
            loop.StartGame();
            loop.Tick(elapsed);
            loop.NotifyGoal();

            var score = ScoreCalculator.Calculate(loop.ElapsedSeconds, hasItem: false);

            Assert.AreEqual(expectedScore, score);
        }
    }
}
