using MahjongPrototype.Tests.TestSupport.Features.Turn;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class TurnLifecycleGameFlowTests
    {
        [Test]
        public void AutoDraw_StartNewRoundPlacesDrawnTile()
        {
            using (TurnLifecycleGameFlowTestDriver driver =
                TurnLifecycleGameFlowTestDriver.Create(enableAutoDraw: true))
            {
                driver.StartNewRound();

                Assert.That(driver.CurrentPlayerHasDrawnTile, Is.True);
                Assert.That(driver.TurnPhaseName, Is.EqualTo("WaitingForDiscard"));
            }
        }

        [Test]
        public void AutoDraw_SkipsWhenDrawnTileAlreadyExists()
        {
            using (TurnLifecycleGameFlowTestDriver driver =
                TurnLifecycleGameFlowTestDriver.Create(enableAutoDraw: true))
            {
                driver.StartNewRound();
                int wallCount = driver.WallCount;
                string drawnTile = driver.CurrentPlayerDrawnTileCodeOrNull;

                driver.StartCurrentTurnAgain();

                Assert.That(driver.CurrentPlayerDrawnTileCodeOrNull, Is.EqualTo(drawnTile));
                Assert.That(driver.WallCount, Is.EqualTo(wallCount));
            }
        }

        [Test]
        public void AutoDraw_SkipsWhenRoundEnded()
        {
            using (TurnLifecycleGameFlowTestDriver driver =
                TurnLifecycleGameFlowTestDriver.Create(enableAutoDraw: true))
            {
                driver.StartNewRound();
                driver.ClearCurrentPlayerDrawnTile();
                driver.SetRoundEnded(true);
                int wallCount = driver.WallCount;

                driver.StartCurrentTurnAgain();

                Assert.That(driver.CurrentPlayerHasDrawnTile, Is.False);
                Assert.That(driver.WallCount, Is.EqualTo(wallCount));
            }
        }

        [Test]
        public void AutoDraw_SkipsDuringWinDecision()
        {
            using (TurnLifecycleGameFlowTestDriver driver =
                TurnLifecycleGameFlowTestDriver.Create(enableAutoDraw: true))
            {
                driver.StartNewRound();
                driver.ClearCurrentPlayerDrawnTile();
                driver.BeginWinDecisionForCurrentTurn();
                int wallCount = driver.WallCount;

                driver.StartCurrentTurnAgain();

                Assert.That(driver.CurrentPlayerHasDrawnTile, Is.False);
                Assert.That(driver.WallCount, Is.EqualTo(wallCount));
            }
        }

        [Test]
        public void AutoDraw_DrawsAgainAfterDiscard()
        {
            using (TurnLifecycleGameFlowTestDriver driver =
                TurnLifecycleGameFlowTestDriver.Create(enableAutoDraw: true))
            {
                driver.StartNewRound();

                driver.RequestDiscardDrawnTile();

                Assert.That(driver.TurnIndex, Is.EqualTo(2));
                Assert.That(driver.CurrentPlayerHasDrawnTile, Is.True);
                Assert.That(driver.TurnPhaseName, Is.EqualTo("WaitingForDiscard"));
            }
        }

        [Test]
        public void AutoDraw_RetryStartsWithDrawnTile()
        {
            using (TurnLifecycleGameFlowTestDriver driver =
                TurnLifecycleGameFlowTestDriver.Create(enableAutoDraw: true))
            {
                driver.StartNewRound();

                driver.RetryPrototype();

                Assert.That(driver.TurnIndex, Is.EqualTo(1));
                Assert.That(driver.CurrentPlayerHasDrawnTile, Is.True);
            }
        }

        [Test]
        public void RetryPrototype_RebuildsOccupiedSeatsWithoutOldSelfSeat()
        {
            using (TurnLifecycleGameFlowTestDriver driver =
                TurnLifecycleGameFlowTestDriver.Create(fixedSelfSeatName: "South"))
            {
                driver.StartNewRound();
                Assert.That(driver.OccupiedSeatNames, Is.EqualTo(new[] { "South" }));
                Assert.That(driver.ActiveTurnSeatNames, Is.EqualTo(new[] { "South" }));
                Assert.That(driver.CurrentTurnName, Is.EqualTo("South"));

                driver.SetFixedSelfSeat("West");
                driver.RetryPrototype();

                Assert.That(driver.OccupiedSeatNames, Is.EqualTo(new[] { "West" }));
                Assert.That(driver.ActiveTurnSeatNames, Is.EqualTo(new[] { "West" }));
                Assert.That(driver.CurrentTurnName, Is.EqualTo("West"));

                AssertSeatSlot(driver, 0, "East", null);
                AssertSeatSlot(driver, 1, "South", null);
                AssertSeatSlot(driver, 2, "West", "Player1");
                AssertSeatSlot(driver, 3, "North", null);
            }
        }

        [TestCase("East")]
        [TestCase("South")]
        [TestCase("West")]
        [TestCase("North")]
        public void GameFlow_FixedSelfSeatSetsSingleActiveTurnPlayer(string selfSeatName)
        {
            using (TurnLifecycleGameFlowTestDriver driver =
                TurnLifecycleGameFlowTestDriver.Create(fixedSelfSeatName: selfSeatName))
            {
                driver.StartNewRound();

                Assert.That(driver.SelfSeatName, Is.EqualTo(selfSeatName));
                Assert.That(driver.SelfWindName, Is.EqualTo(selfSeatName));
                Assert.That(driver.SelfPlayerIdName, Is.EqualTo("Player1"));
                Assert.That(driver.CurrentTurnName, Is.EqualTo(selfSeatName));
                Assert.That(driver.CurrentTurnPlayerIdName, Is.EqualTo("Player1"));
                Assert.That(driver.IsSelfTurn, Is.True);
                Assert.That(driver.ActiveTurnSeatNames.Length, Is.EqualTo(1));
                Assert.That(driver.ActiveTurnSeatNames[0], Is.EqualTo(selfSeatName));
                Assert.That(driver.ActiveSeatNames, Is.EqualTo(new[] { selfSeatName }));

                Assert.That(driver.SeatSlotCount, Is.EqualTo(4));
                Assert.That(
                    driver.SeatSlotPlayerIdNameOrNullAt(0),
                    Is.EqualTo(selfSeatName == "East" ? "Player1" : null));
                Assert.That(
                    driver.SeatSlotPlayerIdNameOrNullAt(1),
                    Is.EqualTo(selfSeatName == "South" ? "Player1" : null));
                Assert.That(
                    driver.SeatSlotPlayerIdNameOrNullAt(2),
                    Is.EqualTo(selfSeatName == "West" ? "Player1" : null));
                Assert.That(
                    driver.SeatSlotPlayerIdNameOrNullAt(3),
                    Is.EqualTo(selfSeatName == "North" ? "Player1" : null));
                AssertSeatSlot(
                    driver,
                    0,
                    "East",
                    selfSeatName == "East" ? "Player1" : null);
                AssertSeatSlot(
                    driver,
                    1,
                    "South",
                    selfSeatName == "South" ? "Player1" : null);
                AssertSeatSlot(
                    driver,
                    2,
                    "West",
                    selfSeatName == "West" ? "Player1" : null);
                AssertSeatSlot(
                    driver,
                    3,
                    "North",
                    selfSeatName == "North" ? "Player1" : null);

                Assert.That(driver.SelfSeatSlotWindName, Is.EqualTo(selfSeatName));
                Assert.That(driver.CurrentTurnSlotWindName, Is.EqualTo(selfSeatName));
                Assert.That(driver.SeatByPlayerId("Player1"), Is.EqualTo(selfSeatName));
                Assert.That(driver.IsSelfSeat(selfSeatName), Is.True);
            }
        }

        [TestCase("East", "West")]
        [TestCase("South", "North")]
        [TestCase("West", "East")]
        [TestCase("North", "South")]
        public void GameFlow_TwoParticipantsPlacesPlayer2AcrossFromSelf(
            string selfSeatName,
            string player2SeatName)
        {
            using (TurnLifecycleGameFlowTestDriver driver =
                TurnLifecycleGameFlowTestDriver.Create(
                    participantCount: 2,
                    fixedSelfSeatName: selfSeatName))
            {
                driver.StartNewRound();

                Assert.That(driver.SeatByPlayerId("Player1"), Is.EqualTo(selfSeatName));
                Assert.That(driver.SeatByPlayerId("Player2"), Is.EqualTo(player2SeatName));
                Assert.That(driver.CurrentTurnName, Is.EqualTo(selfSeatName));
                Assert.That(driver.CurrentTurnPlayerIdName, Is.EqualTo("Player1"));
                Assert.That(driver.OccupiedSeatNames.Length, Is.EqualTo(2));
                Assert.That(driver.ActiveTurnSeatNames.Length, Is.EqualTo(2));
            }
        }

        [Test]
        public void GameFlow_ThreeParticipantsAssignsPlayer1ThroughPlayer3()
        {
            using (TurnLifecycleGameFlowTestDriver driver =
                TurnLifecycleGameFlowTestDriver.Create(participantCount: 3))
            {
                driver.StartNewRound();

                Assert.That(driver.OccupiedSeatNames, Is.EqualTo(new[] { "East", "South", "West" }));
                Assert.That(driver.ActiveTurnSeatNames, Is.EqualTo(new[] { "East", "South", "West" }));
                Assert.That(driver.CurrentTurnName, Is.EqualTo("East"));
                Assert.That(driver.CurrentTurnPlayerIdName, Is.EqualTo("Player1"));
                Assert.That(driver.SeatByPlayerId("Player1"), Is.EqualTo("East"));
                Assert.That(driver.SeatByPlayerId("Player2"), Is.EqualTo("South"));
                Assert.That(driver.SeatByPlayerId("Player3"), Is.EqualTo("West"));
            }
        }

        [Test]
        public void GameFlow_FourParticipantsAssignsAllPlayers()
        {
            using (TurnLifecycleGameFlowTestDriver driver =
                TurnLifecycleGameFlowTestDriver.Create(participantCount: 4))
            {
                driver.StartNewRound();

                Assert.That(
                    driver.OccupiedSeatNames,
                    Is.EqualTo(new[] { "East", "South", "West", "North" }));
                Assert.That(
                    driver.ActiveTurnSeatNames,
                    Is.EqualTo(new[] { "East", "South", "West", "North" }));
                Assert.That(driver.CurrentTurnName, Is.EqualTo("East"));
                Assert.That(driver.CurrentTurnPlayerIdName, Is.EqualTo("Player1"));
                Assert.That(driver.SeatByPlayerId("Player1"), Is.EqualTo("East"));
                Assert.That(driver.SeatByPlayerId("Player2"), Is.EqualTo("South"));
                Assert.That(driver.SeatByPlayerId("Player3"), Is.EqualTo("West"));
                Assert.That(driver.SeatByPlayerId("Player4"), Is.EqualTo("North"));
            }
        }

        [Test]
        public void ParticipantTypes_AssignSelfAsLocalHumanAndOtherSeatsAsCpu()
        {
            using (TurnLifecycleGameFlowTestDriver driver =
                TurnLifecycleGameFlowTestDriver.Create(participantCount: 2))
            {
                driver.StartNewRound();

                Assert.That(driver.SelfParticipantTypeNameOrNull, Is.EqualTo("LocalHuman"));
                Assert.That(driver.Player2ParticipantTypeNameOrNull, Is.EqualTo("Cpu"));
                Assert.That(driver.SouthParticipantTypeNameOrNull, Is.Null);
            }
        }

        private static void AssertSeatSlot(
            TurnLifecycleGameFlowTestDriver driver,
            int index,
            string wind,
            string playerId)
        {
            Assert.That(driver.SeatSlotWindAt(index), Is.EqualTo(wind));
            Assert.That(driver.SeatSlotPlayerIdNameOrNullAt(index), Is.EqualTo(playerId));
            Assert.That(driver.SeatSlotHasPlayerAt(index), Is.EqualTo(playerId != null));
            Assert.That(driver.SeatSlotIsEmptyAt(index), Is.EqualTo(playerId == null));
            Assert.That(driver.SeatSlotStateLabelAt(index), Is.EqualTo(playerId ?? "Empty"));
        }
    }
}
