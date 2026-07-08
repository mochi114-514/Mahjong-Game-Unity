using MahjongPrototype.Tests.TestSupport.Features.Win;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class WinDecisionGameFlowTests
    {
        [Test]
        public void CheckWinPrototype_StoresPendingDecisionInGameState()
        {
            using (WinDecisionGameFlowTestDriver driver =
                WinDecisionGameFlowTestDriver.Create(initialHandTileCount: 0))
            {
                driver.StartNewRound();
                driver.SetSelfWinningTsumoHand();

                driver.CheckWinPrototype();

                Assert.That(driver.IsWinDecisionPending, Is.True);
                Assert.That(driver.WinDecisionSeatName, Is.EqualTo(driver.CurrentTurnName));
                Assert.That(driver.WinDecisionTurnIndex, Is.EqualTo(driver.TurnIndex));
                Assert.That(driver.WinDecisionTypeName, Is.EqualTo("Tsumo"));
                Assert.That(driver.WinningTileCodeOrNull, Is.EqualTo("C"));
                Assert.That(driver.WinSourceSeatNameOrNull, Is.Null);
                Assert.That(driver.PendingWinDeclarationEvaluationIsNull, Is.False);
                Assert.That(driver.FlowIsWinDecisionPending, Is.True);
            }
        }

        [Test]
        public void RequestDeclineWin_ClearsGameStateWinDecision()
        {
            using (WinDecisionGameFlowTestDriver driver = WinDecisionGameFlowTestDriver.Create())
            {
                driver.StartNewRound();
                driver.SetWinDecisionPendingForCurrentTurn();

                driver.RequestDeclineWin();

                Assert.That(driver.IsWinDecisionPending, Is.False);
                Assert.That(driver.IsRoundEnded, Is.False);
                Assert.That(driver.IsInteractionLocked, Is.False);
            }
        }

        [Test]
        public void RequestDeclareWin_ClearsDecisionAndWaitsForRoundResultAdvance()
        {
            using (WinDecisionGameFlowTestDriver driver = WinDecisionGameFlowTestDriver.Create())
            {
                driver.StartNewRound();
                driver.SetWinDecisionPendingForCurrentTurn();

                driver.RequestDeclareWin();

                Assert.That(driver.PreviousStateIsWinDecisionPending, Is.False);
                Assert.That(driver.WindProgressRoundWindName, Is.EqualTo("East"));
                Assert.That(driver.WindProgressHandNumber, Is.EqualTo(1));
                Assert.That(driver.IsRoundEnded, Is.True);
                Assert.That(driver.IsRoundResultPending, Is.True);
                Assert.That(driver.TurnPhaseName, Is.EqualTo("RoundResult"));
                Assert.That(driver.RoundResultTypeName, Is.EqualTo("Win"));
                Assert.That(driver.IsInteractionLocked, Is.True);

                driver.RequestAdvanceFromRoundResult();

                Assert.That(driver.WindProgressRoundWindName, Is.EqualTo("East"));
                Assert.That(driver.WindProgressHandNumber, Is.EqualTo(2));
                Assert.That(driver.IsRoundEnded, Is.False);
                Assert.That(driver.IsRoundResultPending, Is.False);
                Assert.That(driver.IsInteractionLocked, Is.False);
            }
        }

        [Test]
        public void RetryPrototype_StartsWithNoWinDecisionPending()
        {
            using (WinDecisionGameFlowTestDriver driver = WinDecisionGameFlowTestDriver.Create())
            {
                driver.StartNewRound();
                driver.SetWinDecisionPendingForCurrentTurn();

                driver.RetryPrototype();

                Assert.That(driver.IsWinDecisionPending, Is.False);
                Assert.That(driver.WinDecisionTurnIndex, Is.EqualTo(0));
                Assert.That(driver.IsInteractionLocked, Is.False);
            }
        }

        [Test]
        public void CpuDiscard_WhenSelfCanRon_CreatesRonDecisionWithoutAdvancingTurn()
        {
            using (WinDecisionGameFlowTestDriver driver =
                WinDecisionGameFlowTestDriver.CreateTwoPlayerWithoutInitialHand())
            {
                driver.StartNewRound();
                driver.SetupSelfCanRonFromCpuDrawnTile();
                int turnIndexBeforeDiscard = driver.TurnIndex;

                bool discarded = driver.TryDiscardCpuDrawnTile();

                Assert.That(discarded, Is.True);
                Assert.That(driver.IsWinDecisionPending, Is.True);
                Assert.That(driver.WinDecisionSeatName, Is.EqualTo(driver.SelfSeatName));
                Assert.That(driver.WinDecisionTypeName, Is.EqualTo("Ron"));
                Assert.That(driver.WinningTileCodeOrNull, Is.EqualTo("C"));
                Assert.That(driver.WinSourceSeatNameOrNull, Is.EqualTo(driver.CpuSeatName));
                Assert.That(driver.WinDecisionTurnIndex, Is.EqualTo(turnIndexBeforeDiscard));
                Assert.That(driver.CurrentTurnName, Is.EqualTo(driver.CpuSeatName));
                Assert.That(driver.TurnIndex, Is.EqualTo(turnIndexBeforeDiscard));
            }
        }

        [Test]
        public void CpuDiscard_WhenSelfCannotRon_AdvancesTurnNormally()
        {
            using (WinDecisionGameFlowTestDriver driver =
                WinDecisionGameFlowTestDriver.CreateTwoPlayerWithoutInitialHand())
            {
                driver.StartNewRound();
                driver.SetupSelfCannotRonFromCpuDrawnTile();

                bool discarded = driver.TryDiscardCpuDrawnTile();

                Assert.That(discarded, Is.True);
                Assert.That(driver.IsWinDecisionPending, Is.False);
                Assert.That(driver.CurrentTurnName, Is.EqualTo(driver.SelfSeatName));
                Assert.That(driver.TurnIndex, Is.EqualTo(2));
            }
        }

        [Test]
        public void RequestDeclineWin_AfterRon_ClearsDecisionAndAdvancesTurn()
        {
            using (WinDecisionGameFlowTestDriver driver =
                WinDecisionGameFlowTestDriver.CreateTwoPlayerWithoutInitialHand())
            {
                driver.StartNewRound();
                driver.SetupSelfCanRonFromCpuDrawnTile();
                driver.TryDiscardCpuDrawnTile();

                driver.RequestDeclineWin();

                Assert.That(driver.IsWinDecisionPending, Is.False);
                Assert.That(driver.WinDecisionTypeName, Is.Null);
                Assert.That(driver.CurrentTurnName, Is.EqualTo(driver.SelfSeatName));
                Assert.That(driver.TurnIndex, Is.EqualTo(2));
                Assert.That(driver.IsRoundEnded, Is.False);
            }
        }
    }
}
