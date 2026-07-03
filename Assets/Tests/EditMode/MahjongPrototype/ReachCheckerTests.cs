using MahjongPrototype.Tests.TestSupport.Features.Reach;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class ReachCheckerTests
    {
        [Test]
        public void CheckReach_ReturnsCandidatesForReadyHand()
        {
            ReachCheckerTestDriver driver = ReachCheckerTestDriver.Create();
            object result = driver.CheckReach(
                "1m 2m 3m 2p 3p 4p 7s 8s 9s E E E 5m",
                "6m");

            Assert.That(driver.CanReach(result), Is.True);
            Assert.That(driver.CandidateCount(result), Is.GreaterThan(0));
        }

        [Test]
        public void CheckReach_ReturnsMultipleCandidates()
        {
            ReachCheckerTestDriver driver = ReachCheckerTestDriver.Create();
            object result = driver.CheckReach(
                "1m 2m 3m 2p 3p 4p 7s 8s 9s E E E 5m",
                "6m");

            Assert.That(driver.CandidateCount(result), Is.GreaterThanOrEqualTo(2));
            Assert.That(driver.FindCandidate(result, "Hand", "5m"), Is.Not.Null);
            Assert.That(driver.FindCandidate(result, "DrawnTile", "6m"), Is.Not.Null);
        }

        [Test]
        public void CheckReach_ReturnsDrawnTileCandidateForTsumogiriReach()
        {
            ReachCheckerTestDriver driver = ReachCheckerTestDriver.Create();
            object result = driver.CheckReach(
                "1m 2m 3m 2p 3p 4p 7s 8s 9s E E E 5m",
                "6m");

            object candidate = driver.FindCandidate(result, "DrawnTile", "6m");

            Assert.That(candidate, Is.Not.Null);
            Assert.That(driver.CandidateHandIndex(candidate), Is.EqualTo(-1));
        }

        [Test]
        public void CheckReach_ReturnsHandIndexForHandDiscardCandidate()
        {
            ReachCheckerTestDriver driver = ReachCheckerTestDriver.Create();
            object result = driver.CheckReach(
                "1m 2m 3m 2p 3p 4p 7s 8s 9s E E E 5m",
                "6m");

            object candidate = driver.FindCandidate(result, "Hand", "5m");

            Assert.That(candidate, Is.Not.Null);
            Assert.That(driver.CandidateHandIndex(candidate), Is.EqualTo(12));
        }

        [Test]
        public void CheckReach_ReturnsNotReadyForNonReadyHand()
        {
            ReachCheckerTestDriver driver = ReachCheckerTestDriver.Create();
            object result = driver.CheckReach(
                "1m 4m 7m 2p 5p 8p 3s 6s 9s E S W N",
                "P");

            Assert.That(driver.CanReach(result), Is.False);
            Assert.That(driver.CandidateCount(result), Is.EqualTo(0));
        }

        [Test]
        public void CheckReach_ReturnsNotReadyForInvalidInput()
        {
            ReachCheckerTestDriver driver = ReachCheckerTestDriver.Create();
            object shortHandResult = driver.CheckReach("1m 2m 3m", "4m");
            object missingDrawnTileResult = driver.CheckReachWithInvalidDrawnTile(
                "1m 2m 3m 2p 3p 4p 7s 8s 9s E E E 5m");

            Assert.That(driver.CanReach(shortHandResult), Is.False);
            Assert.That(driver.CandidateCount(shortHandResult), Is.EqualTo(0));
            Assert.That(driver.CanReach(missingDrawnTileResult), Is.False);
            Assert.That(driver.CandidateCount(missingDrawnTileResult), Is.EqualTo(0));
        }
    }
}
