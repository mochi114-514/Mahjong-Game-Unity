using MahjongPrototype.Tests.TestSupport.Features.Furiten;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class FuritenRonGameFlowTests
    {
        [Test]
        public void RonDecision_NonFuritenCandidate_CanRon()
        {
            using (FuritenRonTestDriver driver = FuritenRonTestDriver.Create(2))
            {
                driver.StartRound();
                driver.SetHand("East", FuritenTestHands.SimpleFiveManWait());
                driver.SetDrawnTile("West", "5m");
                driver.SetCurrentTurn("West");

                bool discarded = driver.DiscardDrawnTile("West");

                Assert.That(discarded, Is.True);
                Assert.That(driver.IsWinDecisionPending, Is.True);
                Assert.That(driver.WinDecisionType, Is.EqualTo("Ron"));
                Assert.That(driver.WinDecisionSeat, Is.EqualTo("East"));
                Assert.That(driver.WinSourceSeat, Is.EqualTo("West"));
            }
        }

        [Test]
        public void RonDecision_OwnDiscardedWait_DoesNotStartRonDecisionAndAdvancesTurn()
        {
            using (FuritenRonTestDriver driver = FuritenRonTestDriver.Create(2))
            {
                driver.StartRound();
                driver.SetHand("East", FuritenTestHands.SimpleFiveManWait());
                driver.AddDiscard("East", "5m", 0);
                driver.SetDrawnTile("West", "5m");
                driver.SetCurrentTurn("West");

                bool discarded = driver.DiscardDrawnTile("West");

                Assert.That(discarded, Is.True);
                Assert.That(driver.IsWinDecisionPending, Is.False);
                Assert.That(driver.CurrentTurn, Is.EqualTo("East"));
                Assert.That(driver.TurnIndex, Is.EqualTo(2));
            }
        }

        [Test]
        public void RonDecision_OtherSeatDiscardedWait_DoesNotCauseFuriten()
        {
            using (FuritenRonTestDriver driver = FuritenRonTestDriver.Create(2))
            {
                driver.StartRound();
                driver.SetHand("East", FuritenTestHands.SimpleFiveManWait());
                driver.AddDiscard("West", "5m", 0);
                driver.SetDrawnTile("West", "5m");
                driver.SetCurrentTurn("West");

                bool discarded = driver.DiscardDrawnTile("West");

                Assert.That(discarded, Is.True);
                Assert.That(driver.IsWinDecisionPending, Is.True);
                Assert.That(driver.WinDecisionSeat, Is.EqualTo("East"));
                Assert.That(driver.WinDecisionType, Is.EqualTo("Ron"));
            }
        }

        [Test]
        public void RonDecision_MultiWaitWithOneOwnDiscardedWait_BlocksDifferentWaitRon()
        {
            using (FuritenRonTestDriver driver = FuritenRonTestDriver.Create(2))
            {
                driver.StartRound();
                driver.SetHand("East", FuritenTestHands.RonMultiWait());
                driver.AddDiscard("East", "3m", 0);
                Assert.That(driver.IsSeatDiscardFuriten("East"), Is.True);
                Assert.That(driver.IsSeatFuriten("East"), Is.True);
                driver.SetDrawnTile("West", "6m");
                driver.SetCurrentTurn("West");

                bool discarded = driver.DiscardDrawnTile("West");

                Assert.That(discarded, Is.True);
                Assert.That(driver.IsWinDecisionPending, Is.False);
                Assert.That(driver.CurrentTurn, Is.EqualTo("East"));
                Assert.That(driver.TurnIndex, Is.EqualTo(2));
            }
        }

        [Test]
        public void RonDecision_FirstCandidateFuriten_ContinuesToLaterCandidate()
        {
            using (FuritenRonTestDriver driver = FuritenRonTestDriver.Create(3))
            {
                driver.StartRound();
                driver.SetSeatParticipantType("South", "LocalHuman");
                driver.SetSeatParticipantType("West", "LocalHuman");
                driver.SetHand("South", FuritenTestHands.SimpleFiveManWait());
                driver.SetHand("West", FuritenTestHands.SimpleFiveManWait());
                driver.AddDiscard("South", "5m", 0);
                driver.SetDrawnTile("East", "5m");
                driver.SetCurrentTurn("East");

                bool discarded = driver.DiscardDrawnTile("East");

                Assert.That(discarded, Is.True);
                Assert.That(driver.IsWinDecisionPending, Is.True);
                Assert.That(driver.WinDecisionSeat, Is.EqualTo("West"));
                Assert.That(driver.WinDecisionType, Is.EqualTo("Ron"));
            }
        }

        [Test]
        public void RonDecision_AllCandidatesFuriten_AdvancesTurnOnce()
        {
            using (FuritenRonTestDriver driver = FuritenRonTestDriver.Create(3))
            {
                driver.StartRound();
                driver.SetSeatParticipantType("South", "LocalHuman");
                driver.SetSeatParticipantType("West", "LocalHuman");
                driver.SetHand("South", FuritenTestHands.SimpleFiveManWait());
                driver.SetHand("West", FuritenTestHands.SimpleFiveManWait());
                driver.AddDiscard("South", "5m", 0);
                driver.AddDiscard("West", "5m", 0);
                driver.SetDrawnTile("East", "5m");
                driver.SetCurrentTurn("East");

                bool discarded = driver.DiscardDrawnTile("East");

                Assert.That(discarded, Is.True);
                Assert.That(driver.IsWinDecisionPending, Is.False);
                Assert.That(driver.CurrentTurn, Is.EqualTo("South"));
                Assert.That(driver.TurnIndex, Is.EqualTo(2));
            }
        }

        [Test]
        public void RonDecision_ReachPlayerDeclinesRon_MarksReachPassFuriten()
        {
            using (FuritenRonTestDriver driver = FuritenRonTestDriver.Create(2))
            {
                StartRonDecisionFromWest(driver, true);

                driver.RequestDeclineWin();

                Assert.That(driver.IsSeatReachPassFuriten("East"), Is.True);
                Assert.That(driver.IsSeatTemporaryFuriten("East"), Is.False);
            }
        }

        [Test]
        public void RonDecision_ReachPassFuriten_BlocksLaterRonDecision()
        {
            using (FuritenRonTestDriver driver = FuritenRonTestDriver.Create(2))
            {
                StartRonDecisionFromWest(driver, true);
                driver.SetSeatParticipantType("West", "LocalHuman");
                driver.RequestDeclineWin();
                DeclinePendingWinIfAny(driver);
                driver.SetCurrentTurn("West");
                driver.SetDrawnTile("West", "5m");

                bool discarded = driver.DiscardDrawnTile("West");

                Assert.That(discarded, Is.True);
                Assert.That(driver.IsWinDecisionPending, Is.False);
            }
        }

        [Test]
        public void Draw_ReachPassFuriten_RemainsAfterOwnSuccessfulDraw()
        {
            using (FuritenRonTestDriver driver = FuritenRonTestDriver.Create(2))
            {
                StartRonDecisionFromWest(driver, true);
                driver.RequestDeclineWin();
                DeclinePendingWinIfAny(driver);
                driver.SetCurrentTurn("East");
                driver.ClearDrawnTile("East");

                bool drew = driver.DrawTileForSeat("East", "9m");

                Assert.That(drew, Is.True);
                Assert.That(driver.IsSeatReachPassFuriten("East"), Is.True);
            }
        }

        [Test]
        public void TsumoDecision_ReachPassFuriten_DoesNotBlockTsumo()
        {
            using (FuritenRonTestDriver driver = FuritenRonTestDriver.Create(2))
            {
                StartRonDecisionFromWest(driver, true);
                driver.RequestDeclineWin();
                DeclinePendingWinIfAny(driver);
                driver.SetCurrentTurn("East");
                driver.ClearDrawnTile("East");

                bool drew = driver.DrawTileForSeat("East", "5m");

                Assert.That(drew, Is.True);
                Assert.That(driver.IsSeatReachPassFuriten("East"), Is.True);
                Assert.That(driver.IsWinDecisionPending, Is.True);
                Assert.That(driver.WinDecisionType, Is.EqualTo("Tsumo"));
                Assert.That(driver.WinDecisionSeat, Is.EqualTo("East"));
            }
        }

        [Test]
        public void StartRound_ReachPassFuriten_DoesNotCarryToNewRound()
        {
            using (FuritenRonTestDriver driver = FuritenRonTestDriver.Create(2))
            {
                StartRonDecisionFromWest(driver, true);
                driver.RequestDeclineWin();
                DeclinePendingWinIfAny(driver);
                Assert.That(driver.IsSeatReachPassFuriten("East"), Is.True);

                driver.StartRound();

                Assert.That(driver.IsSeatReachPassFuriten("East"), Is.False);
                Assert.That(driver.IsSeatTemporaryFuriten("East"), Is.False);
            }
        }

        [Test]
        public void RonDecision_NonReachPlayerDeclinesRon_MarksTemporaryFuriten()
        {
            using (FuritenRonTestDriver driver = FuritenRonTestDriver.Create(2))
            {
                StartRonDecisionFromWest(driver, false);

                driver.RequestDeclineWin();

                Assert.That(driver.IsSeatTemporaryFuriten("East"), Is.True);
                Assert.That(driver.IsSeatReachPassFuriten("East"), Is.False);
                Assert.That(driver.IsSeatFuriten("East"), Is.True);
            }
        }

        [Test]
        public void RonDecision_TemporaryFuriten_BlocksLaterRonDecision()
        {
            using (FuritenRonTestDriver driver = FuritenRonTestDriver.Create(2))
            {
                StartRonDecisionFromWest(driver, false);
                driver.RequestDeclineWin();
                driver.SetCurrentTurn("West");
                driver.SetDrawnTile("West", "5m");

                bool discarded = driver.DiscardDrawnTile("West");

                Assert.That(discarded, Is.True);
                Assert.That(driver.IsWinDecisionPending, Is.False);
            }
        }

        [Test]
        public void Draw_TemporaryFuriten_ClearsOnOwnSuccessfulDraw()
        {
            using (FuritenRonTestDriver driver = FuritenRonTestDriver.Create(2))
            {
                StartRonDecisionFromWest(driver, false);
                driver.RequestDeclineWin();
                driver.SetCurrentTurn("East");

                bool drew = driver.DrawTileForSeat("East", "9m");

                Assert.That(drew, Is.True);
                Assert.That(driver.IsSeatTemporaryFuriten("East"), Is.False);
                Assert.That(driver.IsSeatReachPassFuriten("East"), Is.False);
            }
        }

        [Test]
        public void RonDecision_TemporaryFuritenClearedByDraw_CanRonAgain()
        {
            using (FuritenRonTestDriver driver = FuritenRonTestDriver.Create(2))
            {
                StartRonDecisionFromWest(driver, false);
                driver.SetSeatParticipantType("West", "LocalHuman");
                driver.RequestDeclineWin();
                driver.SetCurrentTurn("East");
                Assert.That(driver.DrawTileForSeat("East", "9m"), Is.True);
                driver.RequestDeclineReach();
                Assert.That(driver.DiscardDrawnTile("East"), Is.True);
                driver.SetCurrentTurn("West");
                driver.SetDrawnTile("West", "5m");

                bool discarded = driver.DiscardDrawnTile("West");

                Assert.That(discarded, Is.True);
                Assert.That(driver.IsWinDecisionPending, Is.True);
                Assert.That(driver.WinDecisionType, Is.EqualTo("Ron"));
                Assert.That(driver.WinDecisionSeat, Is.EqualTo("East"));
            }
        }

        [Test]
        public void Draw_OtherSeatDoesNotClearTemporaryFuriten()
        {
            using (FuritenRonTestDriver driver = FuritenRonTestDriver.Create(2))
            {
                StartRonDecisionFromWest(driver, false);
                driver.RequestDeclineWin();
                driver.SetCurrentTurn("West");

                bool drew = driver.DrawTileForSeat("West", "9m");

                Assert.That(drew, Is.True);
                Assert.That(driver.IsSeatTemporaryFuriten("East"), Is.True);
            }
        }

        [Test]
        public void TsumoDecision_DeclineTsumo_DoesNotMarkMissedRonFuriten()
        {
            using (FuritenRonTestDriver driver = FuritenRonTestDriver.Create(1))
            {
                driver.StartRound();
                driver.SetHand("East", FuritenTestHands.SimpleFiveManWait());
                driver.DrawSelfTile("5m");
                Assert.That(driver.IsWinDecisionPending, Is.True);
                Assert.That(driver.WinDecisionType, Is.EqualTo("Tsumo"));

                driver.RequestDeclineWin();

                Assert.That(driver.IsSeatTemporaryFuriten("East"), Is.False);
                Assert.That(driver.IsSeatReachPassFuriten("East"), Is.False);
            }
        }

        [Test]
        public void RonDecision_NoYakuWinningShape_MarksTemporaryFuritenWithoutDecision()
        {
            using (FuritenRonTestDriver driver = FuritenRonTestDriver.Create(2))
            {
                StartNoYakuWinningShapeDiscardFromWest(driver);

                Assert.That(driver.IsWinDecisionPending, Is.False);
                Assert.That(driver.IsSeatTemporaryFuriten("East"), Is.True);
                Assert.That(driver.IsSeatReachPassFuriten("East"), Is.False);
                Assert.That(driver.CurrentTurn, Is.EqualTo("East"));
                Assert.That(driver.TurnIndex, Is.EqualTo(2));
            }
        }

        [Test]
        public void RonDecision_NoYakuTemporaryFuriten_BlocksLaterYakuRonDecision()
        {
            using (FuritenRonTestDriver driver = FuritenRonTestDriver.Create(2))
            {
                StartNoYakuWinningShapeDiscardFromWest(driver, true);
                driver.DeclareReach("East", driver.TurnIndex);
                driver.SetCurrentTurn("West");
                driver.SetDrawnTile("West", "P");

                bool discarded = driver.DiscardDrawnTile("West");

                Assert.That(discarded, Is.True);
                Assert.That(driver.IsWinDecisionPending, Is.False);
                Assert.That(driver.IsSeatTemporaryFuriten("East"), Is.True);
            }
        }

        [Test]
        public void TurnStart_NoYakuTemporaryFuriten_DoesNotClearBeforeDraw()
        {
            using (FuritenRonTestDriver driver = FuritenRonTestDriver.Create(2))
            {
                StartNoYakuWinningShapeDiscardFromWest(driver);

                Assert.That(driver.CurrentTurn, Is.EqualTo("East"));
                Assert.That(driver.IsSeatTemporaryFuriten("East"), Is.True);
            }
        }

        [Test]
        public void Draw_NoYakuTemporaryFuriten_ClearsOnOwnSuccessfulDraw()
        {
            using (FuritenRonTestDriver driver = FuritenRonTestDriver.Create(2))
            {
                StartNoYakuWinningShapeDiscardFromWest(driver);
                driver.SetCurrentTurn("East");

                bool drew = driver.DrawTileForSeat("East", "9m");

                Assert.That(drew, Is.True);
                Assert.That(driver.IsSeatTemporaryFuriten("East"), Is.False);
            }
        }

        [Test]
        public void RonDecision_NoYakuTemporaryFuritenClearedByDraw_CanRonAgain()
        {
            using (FuritenRonTestDriver driver = FuritenRonTestDriver.Create(2))
            {
                StartNoYakuWinningShapeDiscardFromWest(driver, true);
                driver.SetCurrentTurn("East");
                Assert.That(driver.DrawTileForSeat("East", "9m"), Is.True);
                driver.RequestDeclineReach();
                Assert.That(driver.DiscardDrawnTile("East"), Is.True);
                driver.DeclareReach("East", driver.TurnIndex);
                driver.SetCurrentTurn("West");
                driver.SetDrawnTile("West", "P");

                bool discarded = driver.DiscardDrawnTile("West");

                Assert.That(discarded, Is.True);
                Assert.That(driver.IsWinDecisionPending, Is.True);
                Assert.That(driver.WinDecisionType, Is.EqualTo("Ron"));
                Assert.That(driver.WinDecisionSeat, Is.EqualTo("East"));
            }
        }

        [Test]
        public void Draw_OtherSeatDoesNotClearNoYakuTemporaryFuriten()
        {
            using (FuritenRonTestDriver driver = FuritenRonTestDriver.Create(2))
            {
                StartNoYakuWinningShapeDiscardFromWest(driver, true);
                driver.SetCurrentTurn("West");

                bool drew = driver.DrawTileForSeat("West", "9m");

                Assert.That(drew, Is.True);
                Assert.That(driver.IsSeatTemporaryFuriten("East"), Is.True);
            }
        }

        [Test]
        public void RonDecision_NonWinningShape_DoesNotMarkTemporaryFuriten()
        {
            using (FuritenRonTestDriver driver = FuritenRonTestDriver.Create(2))
            {
                driver.StartRound();
                driver.SetHand("East", FuritenTestHands.NoYakuSingleWait());
                driver.SetDrawnTile("West", "9m");
                driver.SetCurrentTurn("West");

                bool discarded = driver.DiscardDrawnTile("West");

                Assert.That(discarded, Is.True);
                Assert.That(driver.IsWinDecisionPending, Is.False);
                Assert.That(driver.IsSeatTemporaryFuriten("East"), Is.False);
            }
        }

        [Test]
        public void RonDecision_YakuWinningDiscard_DoesNotMarkTemporaryBeforeDecline()
        {
            using (FuritenRonTestDriver driver = FuritenRonTestDriver.Create(2))
            {
                driver.StartRound();
                driver.SetHand("East", FuritenTestHands.SimpleFiveManWait());
                driver.SetDrawnTile("West", "5m");
                driver.SetCurrentTurn("West");

                bool discarded = driver.DiscardDrawnTile("West");

                Assert.That(discarded, Is.True);
                Assert.That(driver.IsWinDecisionPending, Is.True);
                Assert.That(driver.IsSeatTemporaryFuriten("East"), Is.False);
            }
        }

        [Test]
        public void TsumoDecision_NoYakuTemporaryFuriten_DoesNotBlockTsumo()
        {
            using (FuritenRonTestDriver driver = FuritenRonTestDriver.Create(2))
            {
                StartNoYakuWinningShapeDiscardFromWest(driver);
                driver.SetCurrentTurn("East");

                bool drew = driver.DrawTileForSeat("East", "P");

                Assert.That(drew, Is.True);
                Assert.That(driver.IsSeatTemporaryFuriten("East"), Is.False);
                Assert.That(driver.IsWinDecisionPending, Is.True);
                Assert.That(driver.WinDecisionType, Is.EqualTo("Tsumo"));
                Assert.That(driver.WinDecisionSeat, Is.EqualTo("East"));
            }
        }

        [Test]
        public void StartRound_NoYakuTemporaryFuriten_DoesNotCarryToNewRound()
        {
            using (FuritenRonTestDriver driver = FuritenRonTestDriver.Create(2))
            {
                StartNoYakuWinningShapeDiscardFromWest(driver);
                Assert.That(driver.IsSeatTemporaryFuriten("East"), Is.True);

                driver.StartRound();

                Assert.That(driver.IsSeatTemporaryFuriten("East"), Is.False);
            }
        }

        [Test]
        public void TsumoDecision_OwnDiscardFuriten_DoesNotBlockTsumo()
        {
            using (FuritenRonTestDriver driver = FuritenRonTestDriver.Create(1))
            {
                driver.StartRound();
                driver.SetHand("East", FuritenTestHands.SimpleFiveManWait());
                driver.AddDiscard("East", "5m", 0);

                driver.DrawSelfTile("5m");

                Assert.That(driver.IsWinDecisionPending, Is.True);
                Assert.That(driver.WinDecisionType, Is.EqualTo("Tsumo"));
                Assert.That(driver.WinDecisionSeat, Is.EqualTo("East"));
            }
        }

        [Test]
        public void EvaluateAllFuriten_ReturnsOccupiedSeatsWithoutChangingGameState()
        {
            using (FuritenRonTestDriver driver = FuritenRonTestDriver.Create(2))
            {
                driver.StartRound();
                string before = driver.GameStateSnapshot();

                object resultSet = driver.EvaluateAllFuriten();

                Assert.That(driver.ResultCount(resultSet), Is.EqualTo(2));
                Assert.That(driver.TryGetSeatResult(resultSet, "East", out _), Is.True);
                Assert.That(driver.TryGetSeatResult(resultSet, "West", out _), Is.True);
                Assert.That(driver.TryGetSeatResult(resultSet, "South", out _), Is.False);
                Assert.That(driver.GameStateSnapshot(), Is.EqualTo(before));
            }
        }

        private static void StartNoYakuWinningShapeDiscardFromWest(
            FuritenRonTestDriver driver,
            bool makeWestLocalHuman = false)
        {
            driver.StartRound();
            if (makeWestLocalHuman)
                driver.SetSeatParticipantType("West", "LocalHuman");

            driver.SetHand("East", FuritenTestHands.NoYakuSingleWait());
            driver.SetDrawnTile("West", "P");
            driver.SetCurrentTurn("West");

            Assert.That(driver.DiscardDrawnTile("West"), Is.True);
        }

        private static void StartRonDecisionFromWest(
            FuritenRonTestDriver driver,
            bool declareReach)
        {
            driver.StartRound();
            driver.SetHand("East", FuritenTestHands.SimpleFiveManWait());
            if (declareReach)
                driver.DeclareReach("East", 1);

            driver.SetDrawnTile("West", "5m");
            driver.SetCurrentTurn("West");

            Assert.That(driver.DiscardDrawnTile("West"), Is.True);
            Assert.That(driver.IsWinDecisionPending, Is.True);
            Assert.That(driver.WinDecisionType, Is.EqualTo("Ron"));
            Assert.That(driver.WinDecisionSeat, Is.EqualTo("East"));
        }

        private static void DeclinePendingWinIfAny(FuritenRonTestDriver driver)
        {
            if (driver.IsWinDecisionPending)
                driver.RequestDeclineWin();
        }
    }
}
