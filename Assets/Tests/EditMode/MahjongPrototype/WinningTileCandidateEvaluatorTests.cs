using MahjongPrototype.Tests.TestSupport.Features.Win;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class WinningTileCandidateEvaluatorTests
    {
        [Test]
        public void EvaluateCurrentHand_EnumeratesSingleRyanmenAndMultiWaitsInTypeOrder()
        {
            WinningTileCandidateEvaluatorTestDriver driver =
                WinningTileCandidateEvaluatorTestDriver.Create();

            object singleState = driver.CreateGameState();
            driver.AddHand(
                singleState,
                "East",
                "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C");
            Assert.That(
                driver.TileCodes(driver.EvaluateCurrent(singleState)),
                Is.EqualTo(new[] { "C" }));

            object ryanmenState = driver.CreateGameState();
            driver.AddHand(
                ryanmenState,
                "East",
                "1p 2p 3p 1s 2s 3s E E E P P 4m 5m");
            Assert.That(
                driver.TileCodes(driver.EvaluateCurrent(ryanmenState)),
                Is.EqualTo(new[] { "3m", "6m" }));

            object multiWaitState = driver.CreateGameState();
            driver.AddHand(
                multiWaitState,
                "East",
                "1m 1m 1m 2m 3m 4m 5m 6m 7m 8m 9m 9m 9m");
            Assert.That(
                driver.TileCodes(driver.EvaluateCurrent(multiWaitState)),
                Is.EqualTo(new[]
                {
                    "1m", "2m", "3m", "4m", "5m",
                    "6m", "7m", "8m", "9m"
                }));
        }

        [Test]
        public void EvaluateCurrentHand_EnumeratesSevenPairsAndThirteenOrphans()
        {
            WinningTileCandidateEvaluatorTestDriver driver =
                WinningTileCandidateEvaluatorTestDriver.Create();

            object sevenPairsState = driver.CreateGameState();
            driver.AddHand(
                sevenPairsState,
                "East",
                "1m 1m 2m 2m 3p 3p 4p 4p 5s 5s E E C");
            Assert.That(
                driver.TileCodes(driver.EvaluateCurrent(sevenPairsState)),
                Is.EqualTo(new[] { "C" }));

            object orphansState = driver.CreateGameState();
            driver.AddHand(
                orphansState,
                "East",
                "1m 9m 1p 9p 1s 9s E S W N P F C");
            Assert.That(
                driver.TileCodes(driver.EvaluateCurrent(orphansState)),
                Is.EqualTo(new[]
                {
                    "1m", "9m", "1p", "9p", "1s", "9s",
                    "E", "S", "W", "N", "P", "F", "C"
                }));
        }

        [Test]
        public void EvaluateCurrentHand_WithOpenMeldUsesThirteenTileEquivalentCount()
        {
            WinningTileCandidateEvaluatorTestDriver driver =
                WinningTileCandidateEvaluatorTestDriver.Create();
            object gameState = driver.CreateGameState();
            driver.AddHand(
                gameState,
                "East",
                "1m 2m 3m 1p 2p 3p E E E C");
            object sourceDiscard = driver.AddDiscard(gameState, "South", "5s", 1);
            driver.AddPonMeld(
                gameState,
                "East",
                "South",
                "5s",
                sourceDiscard);

            Assert.That(
                driver.TileCodes(driver.EvaluateCurrent(gameState)),
                Is.EqualTo(new[] { "C" }));
        }

        [Test]
        public void EvaluateCurrentHand_NonTenpaiReturnsEmpty()
        {
            WinningTileCandidateEvaluatorTestDriver driver =
                WinningTileCandidateEvaluatorTestDriver.Create();
            object gameState = driver.CreateGameState();
            driver.AddHand(
                gameState,
                "East",
                "1m 4m 7m 2p 5p 8p 3s 6s 9s E S W N");

            Assert.That(driver.Count(driver.EvaluateCurrent(gameState)), Is.Zero);
        }

        [Test]
        public void EvaluateAfterDiscard_ReportsDifferentWaitsForReachCandidates()
        {
            WinningTileCandidateEvaluatorTestDriver driver =
                WinningTileCandidateEvaluatorTestDriver.Create();
            const string hand =
                "1m 2m 3m 2p 3p 4p 7s 8s 9s E E E 5m";
            object gameState = driver.CreateGameState();
            driver.AddHand(gameState, "East", hand);
            driver.SetDrawnTile(gameState, "East", "6m");
            object reachResult = driver.CheckReach(gameState, hand, "6m");

            object evaluations = driver.EvaluateReachCandidates(
                gameState,
                driver.ReachCandidates(reachResult));
            object handDiscard = driver.FindReachEvaluation(evaluations, "Hand", "5m");
            object drawnDiscard = driver.FindReachEvaluation(evaluations, "DrawnTile", "6m");

            Assert.That(handDiscard, Is.Not.Null);
            Assert.That(drawnDiscard, Is.Not.Null);
            Assert.That(
                driver.TileCodes(driver.WinningTiles(handDiscard)),
                Is.EqualTo(new[] { "6m" }));
            Assert.That(
                driver.TileCodes(driver.WinningTiles(drawnDiscard)),
                Is.EqualTo(new[] { "5m" }));
        }

        [Test]
        public void GroupReachCandidates_CombinesDiscardChoicesWithIdenticalWaits()
        {
            WinningTileCandidateEvaluatorTestDriver driver =
                WinningTileCandidateEvaluatorTestDriver.Create();
            object gameState = driver.CreateGameState();
            driver.AddHand(
                gameState,
                "East",
                "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C");
            driver.SetDrawnTile(gameState, "East", "1m");
            object candidates = driver.CreateReachCandidateList(
                driver.CreateReachCandidate("Hand", 0, "1m"),
                driver.CreateReachCandidate("DrawnTile", -1, "1m"));

            object groups = driver.GroupReachCandidates(gameState, candidates);

            Assert.That(driver.Count(groups), Is.EqualTo(1));
            object group = driver.Item(groups, 0);
            Assert.That(driver.GroupDiscardCandidateCount(group), Is.EqualTo(2));
            Assert.That(
                driver.TileCodes(driver.WinningTiles(group)),
                Is.EqualTo(new[] { "C" }));
        }

        [Test]
        public void VisibleRemainingCount_SubtractsOnlyLocalHandDrawDiscardsAndPublicMelds()
        {
            WinningTileCandidateEvaluatorTestDriver driver =
                WinningTileCandidateEvaluatorTestDriver.Create();
            object gameState = driver.CreateGameState();
            driver.AddHand(gameState, "East", "5m");
            driver.SetDrawnTile(gameState, "East", "5m");
            driver.AddDiscard(gameState, "South", "5m", 1);
            object chiSource = driver.AddDiscard(gameState, "South", "4m", 2);
            driver.AddChiMeld(
                gameState,
                "West",
                "South",
                "4m 5m 6m",
                "4m",
                chiSource);

            Assert.That(
                driver.CountVisibleRemaining(gameState, "East", "5m"),
                Is.Zero);
        }

        [Test]
        public void VisibleRemainingCount_ClaimedDiscardIsCountedOnceUsingSourceDiscardId()
        {
            WinningTileCandidateEvaluatorTestDriver driver =
                WinningTileCandidateEvaluatorTestDriver.Create();
            object gameState = driver.CreateGameState();
            object sourceDiscard = driver.AddDiscard(gameState, "South", "5m", 1);
            driver.AddPonMeld(
                gameState,
                "West",
                "South",
                "5m",
                sourceDiscard);

            Assert.That(
                driver.CountVisibleRemaining(gameState, "East", "5m"),
                Is.EqualTo(1));
        }

        [Test]
        public void VisibleRemainingCount_IgnoresOpponentConcealedTilesAndWallSnapshots()
        {
            WinningTileCandidateEvaluatorTestDriver driver =
                WinningTileCandidateEvaluatorTestDriver.Create();
            object firstState = driver.CreateGameState(12345);
            driver.AddHand(firstState, "South", "5m 5m 5m 5m");
            driver.SetDrawnTile(firstState, "West", "5m");
            object secondState = driver.CreateGameState(98765);

            Assert.That(
                driver.CountVisibleRemaining(firstState, "East", "5m"),
                Is.EqualTo(4));
            Assert.That(
                driver.CountVisibleRemaining(secondState, "East", "5m"),
                Is.EqualTo(4));
        }

        [Test]
        public void EvaluateCurrentHand_KeepsWinningCandidateWhenVisibleRemainingIsZero()
        {
            WinningTileCandidateEvaluatorTestDriver driver =
                WinningTileCandidateEvaluatorTestDriver.Create();
            object gameState = driver.CreateGameState();
            driver.AddHand(
                gameState,
                "East",
                "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C");
            driver.AddDiscard(gameState, "South", "C", 1);
            driver.AddDiscard(gameState, "West", "C", 2);
            driver.AddDiscard(gameState, "North", "C", 3);

            object candidates = driver.EvaluateCurrent(gameState);

            Assert.That(driver.TileCodes(candidates), Is.EqualTo(new[] { "C" }));
            Assert.That(driver.RemainingCount(candidates, "C"), Is.Zero);
        }
    }
}
