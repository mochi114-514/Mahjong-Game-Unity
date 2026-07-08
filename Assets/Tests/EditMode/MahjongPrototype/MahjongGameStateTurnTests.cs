using MahjongPrototype.Tests.TestSupport.Features.Turn;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class MahjongGameStateTurnTests
    {
        [Test]
        public void WinDecisionState_BeginsAndClearsInGameState()
        {
            MahjongGameStateTurnTestDriver driver = MahjongGameStateTurnTestDriver.Create("East");

            driver.BeginWinDecision("East", 7);

            Assert.That(driver.IsWinDecisionPending, Is.True);
            Assert.That(driver.WinDecisionSeatName, Is.EqualTo("East"));
            Assert.That(driver.WinDecisionTypeName, Is.EqualTo("Tsumo"));
            Assert.That(driver.WinSourceSeatNameOrNull, Is.Null);
            Assert.That(driver.WinDecisionTurnIndex, Is.EqualTo(7));
            Assert.That(driver.TurnPhaseName, Is.EqualTo("WinDecision"));
            Assert.That(driver.IsInteractionLocked, Is.True);

            driver.SetDrawnTile("East", "E");
            Assert.That(driver.TurnPhaseName, Is.EqualTo("WinDecision"));

            driver.ClearWinDecision();

            Assert.That(driver.IsWinDecisionPending, Is.False);
            Assert.That(driver.WinDecisionTypeName, Is.Null);
            Assert.That(driver.WinningTileCodeOrNull, Is.Null);
            Assert.That(driver.WinSourceSeatNameOrNull, Is.Null);
            Assert.That(driver.WinDecisionTurnIndex, Is.EqualTo(0));
            Assert.That(driver.TurnPhaseName, Is.EqualTo("WaitingForDiscard"));
            Assert.That(driver.IsInteractionLocked, Is.False);
        }

        [Test]
        public void TurnPhase_RoundEndedTakesPriorityOverWinDecision()
        {
            MahjongGameStateTurnTestDriver driver = MahjongGameStateTurnTestDriver.Create("East");
            driver.BeginWinDecision(driver.CurrentTurnName, driver.TurnIndex);

            driver.SetRoundEnded(true);

            Assert.That(driver.TurnPhaseName, Is.EqualTo("RoundEnded"));
            Assert.That(driver.IsInteractionLocked, Is.True);
        }

        [Test]
        public void TurnPhase_RoundResultTakesPriorityOverRoundEndedAndWinDecision()
        {
            MahjongGameStateTurnTestDriver driver = MahjongGameStateTurnTestDriver.Create("East");
            driver.BeginWinDecision(driver.CurrentTurnName, driver.TurnIndex);

            driver.BeginExhaustiveDrawRoundResult();

            Assert.That(driver.IsRoundResultPending, Is.True);
            Assert.That(driver.TurnPhaseName, Is.EqualTo("RoundResult"));
            Assert.That(driver.IsInteractionLocked, Is.True);
        }

        [Test]
        public void TurnPhase_GameEndedTakesPriorityAfterRoundResultCompletion()
        {
            MahjongGameStateTurnTestDriver driver = MahjongGameStateTurnTestDriver.Create("East");
            driver.BeginExhaustiveDrawRoundResult(isFinalRound: true);

            driver.CompleteRoundResult(gameEnded: true);

            Assert.That(driver.IsGameEnded, Is.True);
            Assert.That(driver.IsRoundResultPending, Is.False);
            Assert.That(driver.CurrentRoundResultIsNull, Is.False);
            Assert.That(driver.TurnPhaseName, Is.EqualTo("GameEnded"));
            Assert.That(driver.IsInteractionLocked, Is.True);
        }

        [Test]
        public void TurnPhase_UsesDrawnTileAsPhaseSource()
        {
            MahjongGameStateTurnTestDriver driver = MahjongGameStateTurnTestDriver.Create("East");

            Assert.That(driver.TurnPhaseName, Is.EqualTo("WaitingForDraw"));

            driver.SetDrawnTile("East", "E");

            Assert.That(driver.TurnPhaseName, Is.EqualTo("WaitingForDiscard"));
        }

        [Test]
        public void RebuildActiveTurnSeatsFromSeatSlots_SkipsEmptySeatsAndRepairsCurrentTurn()
        {
            MahjongGameStateTurnTestDriver driver = MahjongGameStateTurnTestDriver.Create("East");
            driver.SetSelfSeat("South");
            driver.AssignPlayerToSeat("Player2", "North");
            driver.SetCurrentTurn("East");

            driver.RebuildActiveTurnSeats();

            Assert.That(driver.ActiveTurnSeatNames, Is.EqualTo(new[] { "South", "North" }));
            Assert.That(driver.ActiveSeatNames, Is.EqualTo(new[] { "South", "North" }));
            Assert.That(driver.CurrentTurnName, Is.EqualTo("South"));
            Assert.That(driver.CurrentTurnPlayerIdName, Is.EqualTo("Player1"));
        }
    }
}
