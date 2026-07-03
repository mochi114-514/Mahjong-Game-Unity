using MahjongPrototype.Tests.TestSupport.Features.Furiten;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class FuritenEvaluatorTests
    {
        private readonly FuritenEvaluatorTestDriver driver = FuritenEvaluatorTestDriver.Create();

        [Test]
        public void EvaluateAll_TwoPlayers_ReturnsTwoResults()
        {
            object gameState = driver.CreateGameState("East", "South");

            object resultSet = driver.EvaluateAll(gameState);

            Assert.That(driver.ResultCount(resultSet), Is.EqualTo(2));
            Assert.That(driver.TryGetSeatResult(resultSet, "East", out _), Is.True);
            Assert.That(driver.TryGetSeatResult(resultSet, "South", out _), Is.True);
        }

        [Test]
        public void EvaluateAll_ThreePlayers_ReturnsThreeResults()
        {
            object gameState = driver.CreateGameState("East", "South", "West");

            object resultSet = driver.EvaluateAll(gameState);

            Assert.That(driver.ResultCount(resultSet), Is.EqualTo(3));
            Assert.That(driver.TryGetSeatResult(resultSet, "East", out _), Is.True);
            Assert.That(driver.TryGetSeatResult(resultSet, "South", out _), Is.True);
            Assert.That(driver.TryGetSeatResult(resultSet, "West", out _), Is.True);
        }

        [Test]
        public void EvaluateAll_FourPlayers_ReturnsFourResults()
        {
            object gameState = driver.CreateGameState("East", "South", "West", "North");

            object resultSet = driver.EvaluateAll(gameState);

            Assert.That(driver.ResultCount(resultSet), Is.EqualTo(4));
            Assert.That(driver.TryGetSeatResult(resultSet, "East", out _), Is.True);
            Assert.That(driver.TryGetSeatResult(resultSet, "South", out _), Is.True);
            Assert.That(driver.TryGetSeatResult(resultSet, "West", out _), Is.True);
            Assert.That(driver.TryGetSeatResult(resultSet, "North", out _), Is.True);
        }

        [Test]
        public void EvaluateAll_ExcludesEmptySeats()
        {
            object gameState = driver.CreateGameState("East", "North");

            object resultSet = driver.EvaluateAll(gameState);

            Assert.That(driver.ResultCount(resultSet), Is.EqualTo(2));
            Assert.That(driver.TryGetSeatResult(resultSet, "East", out _), Is.True);
            Assert.That(driver.TryGetSeatResult(resultSet, "North", out _), Is.True);
            Assert.That(driver.TryGetSeatResult(resultSet, "South", out _), Is.False);
            Assert.That(driver.TryGetSeatResult(resultSet, "West", out _), Is.False);
        }

        [Test]
        public void EvaluateAll_EvaluatesParticipantTypesWithSameRules()
        {
            object gameState = driver.CreateGameState("East", "South", "West");
            driver.SetParticipantType(gameState, "West", "RemoteHuman");
            driver.AssignHand(gameState, "East", FuritenTestHands.EvaluatorSingleWait());
            driver.AssignHand(gameState, "South", FuritenTestHands.EvaluatorSingleWait());
            driver.AssignHand(gameState, "West", FuritenTestHands.EvaluatorSingleWait());
            driver.AddDiscard(gameState, "East", "C", 1);
            driver.AddDiscard(gameState, "South", "C", 2);
            driver.AddDiscard(gameState, "West", "C", 3);

            object resultSet = driver.EvaluateAll(gameState);
            object east = driver.GetSeatResult(resultSet, "East");
            object south = driver.GetSeatResult(resultSet, "South");
            object west = driver.GetSeatResult(resultSet, "West");

            Assert.That(driver.IsEvaluated(east), Is.True);
            Assert.That(driver.IsTenpai(east), Is.True);
            Assert.That(driver.IsDiscardFuriten(east), Is.True);
            Assert.That(driver.IsFuriten(east), Is.True);
            Assert.That(driver.IsEvaluated(south), Is.True);
            Assert.That(driver.IsTenpai(south), Is.True);
            Assert.That(driver.IsDiscardFuriten(south), Is.True);
            Assert.That(driver.IsFuriten(south), Is.True);
            Assert.That(driver.IsEvaluated(west), Is.True);
            Assert.That(driver.IsTenpai(west), Is.True);
            Assert.That(driver.IsDiscardFuriten(west), Is.True);
            Assert.That(driver.IsFuriten(west), Is.True);
        }

        [Test]
        public void EvaluateAll_TenpaiAndOwnDiscardContainsWait_IsDiscardFuriten()
        {
            object gameState = driver.CreateGameState("East");
            driver.AssignHand(gameState, "East", FuritenTestHands.EvaluatorSingleWait());
            driver.AddDiscard(gameState, "East", "C", 1);

            object result = driver.EvaluateSeat(gameState, "East");

            Assert.That(driver.IsEvaluated(result), Is.True);
            Assert.That(driver.IsTenpai(result), Is.True);
            Assert.That(driver.IsDiscardFuriten(result), Is.True);
            Assert.That(driver.IsFuriten(result), Is.True);
        }

        [Test]
        public void EvaluateAll_TenpaiButOwnDiscardDoesNotContainWait_IsNotFuriten()
        {
            object gameState = driver.CreateGameState("East");
            driver.AssignHand(gameState, "East", FuritenTestHands.EvaluatorSingleWait());
            driver.AddDiscard(gameState, "East", "9m", 1);

            object result = driver.EvaluateSeat(gameState, "East");

            Assert.That(driver.IsEvaluated(result), Is.True);
            Assert.That(driver.IsTenpai(result), Is.True);
            Assert.That(driver.IsDiscardFuriten(result), Is.False);
            Assert.That(driver.IsFuriten(result), Is.False);
        }

        [Test]
        public void EvaluateAll_OtherDiscardOnlyDoesNotCauseFuriten()
        {
            object gameState = driver.CreateGameState("East", "South");
            driver.AssignHand(gameState, "East", FuritenTestHands.EvaluatorSingleWait());
            driver.AddDiscard(gameState, "South", "C", 1);

            object result = driver.EvaluateSeat(gameState, "East");

            Assert.That(driver.IsEvaluated(result), Is.True);
            Assert.That(driver.IsTenpai(result), Is.True);
            Assert.That(driver.IsDiscardFuriten(result), Is.False);
            Assert.That(driver.IsFuriten(result), Is.False);
        }

        [Test]
        public void EvaluateAll_SameStateCanDifferBySeat()
        {
            object gameState = driver.CreateGameState("East", "South");
            driver.AssignHand(gameState, "East", FuritenTestHands.EvaluatorSingleWait());
            driver.AssignHand(gameState, "South", FuritenTestHands.EvaluatorSingleWait());
            driver.AddDiscard(gameState, "East", "C", 1);
            driver.AddDiscard(gameState, "South", "9m", 2);

            object resultSet = driver.EvaluateAll(gameState);
            object east = driver.GetSeatResult(resultSet, "East");
            object south = driver.GetSeatResult(resultSet, "South");

            Assert.That(driver.IsEvaluated(east), Is.True);
            Assert.That(driver.IsTenpai(east), Is.True);
            Assert.That(driver.IsDiscardFuriten(east), Is.True);
            Assert.That(driver.IsFuriten(east), Is.True);
            Assert.That(driver.IsEvaluated(south), Is.True);
            Assert.That(driver.IsTenpai(south), Is.True);
            Assert.That(driver.IsDiscardFuriten(south), Is.False);
            Assert.That(driver.IsFuriten(south), Is.False);
        }

        [Test]
        public void EvaluateAll_MultiWaitWithOneOwnDiscardedWait_IsDiscardFuriten()
        {
            Assert.That(driver.CanWinWithTile(FuritenTestHands.EvaluatorMultiWait(), "3m"), Is.True);
            Assert.That(driver.CanWinWithTile(FuritenTestHands.EvaluatorMultiWait(), "6m"), Is.True);
            object gameState = driver.CreateGameState("East");
            driver.AssignHand(gameState, "East", FuritenTestHands.EvaluatorMultiWait());
            driver.AddDiscard(gameState, "East", "3m", 1);

            object result = driver.EvaluateSeat(gameState, "East");

            Assert.That(driver.IsEvaluated(result), Is.True);
            Assert.That(driver.IsTenpai(result), Is.True);
            Assert.That(driver.IsDiscardFuriten(result), Is.True);
            Assert.That(driver.IsFuriten(result), Is.True);
        }

        [Test]
        public void EvaluateAll_MultiWaitWithoutOwnDiscardedWait_IsNotFuriten()
        {
            object gameState = driver.CreateGameState("East");
            driver.AssignHand(gameState, "East", FuritenTestHands.EvaluatorMultiWait());
            driver.AddDiscard(gameState, "East", "C", 1);

            object result = driver.EvaluateSeat(gameState, "East");

            Assert.That(driver.IsEvaluated(result), Is.True);
            Assert.That(driver.IsTenpai(result), Is.True);
            Assert.That(driver.IsDiscardFuriten(result), Is.False);
            Assert.That(driver.IsFuriten(result), Is.False);
        }

        [Test]
        public void EvaluateAll_NotTenpai_IsNotFuriten()
        {
            object gameState = driver.CreateGameState("East");
            driver.AssignHand(gameState, "East", FuritenTestHands.NonTenpai());
            driver.AddDiscard(gameState, "East", "E", 1);

            object result = driver.EvaluateSeat(gameState, "East");

            Assert.That(driver.IsEvaluated(result), Is.True);
            Assert.That(driver.IsTenpai(result), Is.False);
            Assert.That(driver.IsDiscardFuriten(result), Is.False);
            Assert.That(driver.IsFuriten(result), Is.False);
        }

        [Test]
        public void EvaluateAll_SevenPairsWaitCanBeDiscardFuriten()
        {
            object gameState = driver.CreateGameState("East");
            driver.AssignHand(gameState, "East", FuritenTestHands.SevenPairsWait());
            driver.AddDiscard(gameState, "East", "C", 1);

            object result = driver.EvaluateSeat(gameState, "East");

            Assert.That(driver.IsEvaluated(result), Is.True);
            Assert.That(driver.IsTenpai(result), Is.True);
            Assert.That(driver.IsDiscardFuriten(result), Is.True);
            Assert.That(driver.IsFuriten(result), Is.True);
        }

        [Test]
        public void EvaluateAll_ThirteenOrphansWaitCanBeDiscardFuriten()
        {
            object gameState = driver.CreateGameState("East");
            driver.AssignHand(gameState, "East", FuritenTestHands.ThirteenOrphansWait());
            driver.AddDiscard(gameState, "East", "E", 1);

            object result = driver.EvaluateSeat(gameState, "East");

            Assert.That(driver.IsEvaluated(result), Is.True);
            Assert.That(driver.IsTenpai(result), Is.True);
            Assert.That(driver.IsDiscardFuriten(result), Is.True);
            Assert.That(driver.IsFuriten(result), Is.True);
        }

        [Test]
        public void EvaluateAll_TwelveTileHand_IsNotEvaluated()
        {
            object gameState = driver.CreateGameState("East");
            driver.AssignHandText(gameState, "East", "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E");

            object result = driver.EvaluateSeat(gameState, "East");

            Assert.That(driver.IsEvaluated(result), Is.False);
            Assert.That(driver.IsTenpai(result), Is.False);
            Assert.That(driver.IsDiscardFuriten(result), Is.False);
            Assert.That(driver.IsFuriten(result), Is.False);
        }

        [Test]
        public void EvaluateAll_FourteenTileHand_IsNotEvaluated()
        {
            object gameState = driver.CreateGameState("East");
            driver.AssignHandText(
                gameState,
                "East",
                "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C 1m");

            object result = driver.EvaluateSeat(gameState, "East");

            Assert.That(driver.IsEvaluated(result), Is.False);
            Assert.That(driver.IsTenpai(result), Is.False);
            Assert.That(driver.IsDiscardFuriten(result), Is.False);
            Assert.That(driver.IsFuriten(result), Is.False);
        }

        [Test]
        public void EvaluateAll_SeatWithDrawnTile_IsNotEvaluated()
        {
            object gameState = driver.CreateGameState("East");
            driver.AssignHand(gameState, "East", FuritenTestHands.EvaluatorSingleWait());
            driver.SetDrawnTile(gameState, "East", "1m");

            object result = driver.EvaluateSeat(gameState, "East");

            Assert.That(driver.IsEvaluated(result), Is.False);
            Assert.That(driver.IsTenpai(result), Is.False);
            Assert.That(driver.IsDiscardFuriten(result), Is.False);
            Assert.That(driver.IsFuriten(result), Is.False);
        }

        [Test]
        public void EvaluateAll_InvalidTileInHand_IsNotEvaluated()
        {
            object gameState = driver.CreateGameState("East");
            driver.AssignHandText(gameState, "East", "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E");
            driver.AddHandTile(gameState, "East", driver.CreateInvalidTile());

            object result = driver.EvaluateSeat(gameState, "East");

            Assert.That(driver.IsEvaluated(result), Is.False);
            Assert.That(driver.IsTenpai(result), Is.False);
            Assert.That(driver.IsDiscardFuriten(result), Is.False);
            Assert.That(driver.IsFuriten(result), Is.False);
        }

        [Test]
        public void EvaluateAll_FiveCopiesInHand_IsNotEvaluated()
        {
            object gameState = driver.CreateGameState("East");
            driver.AssignHandText(
                gameState,
                "East",
                "1m 1m 1m 1m 1m 2p 3p 4p 5s 6s 7s E E");

            object result = driver.EvaluateSeat(gameState, "East");

            Assert.That(driver.IsEvaluated(result), Is.False);
            Assert.That(driver.IsTenpai(result), Is.False);
            Assert.That(driver.IsDiscardFuriten(result), Is.False);
            Assert.That(driver.IsFuriten(result), Is.False);
        }

        [Test]
        public void EvaluateAll_NullGameState_ReturnsEmptyResultSet()
        {
            object resultSet = driver.EvaluateAll(null);

            Assert.That(driver.ResultCount(resultSet), Is.EqualTo(0));
        }

        [Test]
        public void EvaluateAll_DoesNotChangeHandContentsOrOrder()
        {
            object gameState = driver.CreateGameState("East");
            driver.AssignHand(gameState, "East", FuritenTestHands.EvaluatorMultiWait());
            driver.AddDiscard(gameState, "East", "3m", 1);
            string before = driver.HandDisplayString(gameState, "East");

            driver.EvaluateAll(gameState);

            Assert.That(driver.HandDisplayString(gameState, "East"), Is.EqualTo(before));
        }

        [Test]
        public void EvaluateAll_DoesNotChangeDiscardHistory()
        {
            object gameState = driver.CreateGameState("East", "South");
            driver.AssignHand(gameState, "East", FuritenTestHands.EvaluatorSingleWait());
            driver.AddDiscard(gameState, "South", "C", 1);
            driver.AddDiscard(gameState, "East", "9m", 2);
            string before = driver.DiscardSnapshot(gameState);

            driver.EvaluateAll(gameState);

            Assert.That(driver.DiscardSnapshot(gameState), Is.EqualTo(before));
        }

        [Test]
        public void EvaluateAll_DoesNotChangeTurnSeatsOrReachState()
        {
            object gameState = driver.CreateGameState("East", "South");
            driver.AssignHand(gameState, "East", FuritenTestHands.EvaluatorSingleWait());
            driver.AssignHand(gameState, "South", FuritenTestHands.EvaluatorSingleWait());
            driver.SetCurrentTurn(gameState, "South");
            driver.DeclareReach(gameState, "South", 17);
            string before = driver.GameStateSnapshot(gameState);

            driver.EvaluateAll(gameState);

            Assert.That(driver.GameStateSnapshot(gameState), Is.EqualTo(before));
        }
    }
}
