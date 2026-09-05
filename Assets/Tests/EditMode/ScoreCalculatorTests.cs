using BallRolling.Gameplay.Logic;
using NUnit.Framework;

namespace BallRolling.Tests.EditMode
{
    public class ScoreCalculatorTests
    {
        [TestCase(59.9f, false, ExpectedResult = 2)]
        [TestCase(59.9f, true, ExpectedResult = 3)]
        [TestCase(60f, false, ExpectedResult = 2)]
        [TestCase(60f, true, ExpectedResult = 3)]
        [TestCase(60.1f, false, ExpectedResult = 1)]
        [TestCase(119.9f, false, ExpectedResult = 1)]
        [TestCase(120f, false, ExpectedResult = 1)]
        [TestCase(120f, true, ExpectedResult = 2)]
        [TestCase(120.1f, false, ExpectedResult = 0)]
        [TestCase(180f, false, ExpectedResult = 0)]
        public int Calculate_ReturnsTieredScore(float clearTime, bool hasItem)
        {
            return ScoreCalculator.Calculate(clearTime, hasItem);
        }

        [TestCase(0, "☆☆☆")]
        [TestCase(1, "★☆☆")]
        [TestCase(2, "★★☆")]
        [TestCase(3, "★★★")]
        [TestCase(5, "★★★")]
        [TestCase(-1, "☆☆☆")]
        public void RenderStars_ShowsCorrectStars(int score, string expected)
        {
            Assert.AreEqual(expected, ScoreCalculator.RenderStars(score));
        }
    }
}
