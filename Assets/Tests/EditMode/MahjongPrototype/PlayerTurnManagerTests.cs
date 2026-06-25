using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MahjongPrototype.Tests
{
    public sealed class PlayerTurnManagerTests
    {
        private const string SeatIdTypeName = "MahjongPrototype.Domain.SeatId, Assembly-CSharp";
        private const string WallTypeName = "MahjongPrototype.Domain.Wall, Assembly-CSharp";
        private const string MahjongGameStateTypeName = "MahjongPrototype.Domain.MahjongGameState, Assembly-CSharp";
        private const string TurnOrderServiceTypeName = "MahjongPrototype.Services.TurnOrderService, Assembly-CSharp";
        private const string PlayerTurnManagerTypeName = "MahjongPrototype.Services.PlayerTurnManager, Assembly-CSharp";
        private const string MahjongGameFlowTypeName = "MahjongPrototype.MahjongGameFlow, Assembly-CSharp";
        private const string PlayerIdTypeName = "MahjongPrototype.Domain.PlayerId, Assembly-CSharp";
        private const string ParticipantTypeName = "MahjongPrototype.Domain.ParticipantType, Assembly-CSharp";

        [Test]
        public void WinDecisionState_BeginsAndClearsInGameState()
        {
            object gameState = CreateGameState("East");

            Invoke(gameState, "BeginWinDecision", ParseSeat("East"), 7);

            Assert.That(GetProperty(gameState, "IsWinDecisionPending"), Is.True);
            Assert.That(GetProperty(gameState, "WinDecisionSeat").ToString(), Is.EqualTo("East"));
            Assert.That(GetProperty(gameState, "WinDecisionType").ToString(), Is.EqualTo("Tsumo"));
            Assert.That(GetProperty(gameState, "WinSourceSeat"), Is.Null);
            Assert.That(GetProperty(gameState, "WinDecisionTurnIndex"), Is.EqualTo(7));
            Assert.That(GetProperty(gameState, "TurnPhase").ToString(), Is.EqualTo("WinDecision"));
            Assert.That(GetProperty(gameState, "IsInteractionLocked"), Is.True);

            Invoke(GetCurrentPlayerSeat(gameState), "SetDrawnTile", CreateTile("E"));
            Assert.That(GetProperty(gameState, "TurnPhase").ToString(), Is.EqualTo("WinDecision"));

            Invoke(gameState, "ClearWinDecision");

            Assert.That(GetProperty(gameState, "IsWinDecisionPending"), Is.False);
            Assert.That(GetProperty(gameState, "WinDecisionType"), Is.Null);
            Assert.That(GetProperty(gameState, "WinningTile"), Is.Null);
            Assert.That(GetProperty(gameState, "WinSourceSeat"), Is.Null);
            Assert.That(GetProperty(gameState, "WinDecisionTurnIndex"), Is.EqualTo(0));
            Assert.That(GetProperty(gameState, "TurnPhase").ToString(), Is.EqualTo("WaitingForDiscard"));
            Assert.That(GetProperty(gameState, "IsInteractionLocked"), Is.False);
        }

        [Test]
        public void TurnPhase_RoundEndedTakesPriorityOverWinDecision()
        {
            object gameState = CreateGameState("East");
            Invoke(
                gameState,
                "BeginWinDecision",
                GetProperty(gameState, "CurrentTurn"),
                GetProperty(gameState, "TurnIndex"));

            SetProperty(gameState, "IsRoundEnded", true);

            Assert.That(GetProperty(gameState, "TurnPhase").ToString(), Is.EqualTo("RoundEnded"));
            Assert.That(GetProperty(gameState, "IsInteractionLocked"), Is.True);
        }

        [Test]
        public void CheckWinPrototype_StoresPendingDecisionInGameState()
        {
            GameObject gameObject = new GameObject("MahjongGameFlowWinDecisionStateTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject, false);
                SetPrivateField(gameFlow, "initialHandTileCount", 0);
                Invoke(gameFlow, "StartNewRound");
                object gameState = GetProperty(gameFlow, "CurrentState");
                object playerSeat = GetCurrentPlayerSeat(gameState);
                object hand = GetProperty(playerSeat, "Hand");

                string[] handTiles =
                {
                    "1m", "2m", "3m",
                    "1p", "2p", "3p",
                    "1s", "2s", "3s",
                    "E", "E", "E",
                    "C"
                };
                for (int i = 0; i < handTiles.Length; i++)
                    Invoke(hand, "Add", CreateTile(handTiles[i]));

                Invoke(playerSeat, "SetDrawnTile", CreateTile("C"));
                Invoke(gameFlow, "CheckWinPrototype");

                Assert.That(GetProperty(gameState, "IsWinDecisionPending"), Is.True);
                Assert.That(
                    GetProperty(gameState, "WinDecisionSeat"),
                    Is.EqualTo(GetProperty(gameState, "CurrentTurn")));
                Assert.That(
                    GetProperty(gameState, "WinDecisionTurnIndex"),
                    Is.EqualTo(GetProperty(gameState, "TurnIndex")));
                Assert.That(GetProperty(gameState, "WinDecisionType").ToString(), Is.EqualTo("Tsumo"));
                Assert.That(GetProperty(gameState, "WinningTile"), Is.EqualTo(CreateTile("C")));
                Assert.That(GetProperty(gameState, "WinSourceSeat"), Is.Null);
                Assert.That(GetProperty(gameFlow, "IsWinDecisionPending"), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void RequestDeclineWin_ClearsGameStateWinDecision()
        {
            GameObject gameObject = new GameObject("MahjongGameFlowDeclineWinDecisionTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject, false);
                Invoke(gameFlow, "StartNewRound");
                object gameState = GetProperty(gameFlow, "CurrentState");
                Invoke(
                    gameFlow,
                    "SetWinDecisionPending",
                    true,
                    GetProperty(gameState, "CurrentTurn"),
                    GetProperty(gameState, "TurnIndex"));

                Invoke(gameFlow, "RequestDeclineWin");

                Assert.That(GetProperty(gameState, "IsWinDecisionPending"), Is.False);
                Assert.That(GetProperty(gameState, "IsRoundEnded"), Is.False);
                Assert.That(GetProperty(gameState, "IsInteractionLocked"), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void RequestDeclareWin_ClearsDecisionAndEndsRound()
        {
            GameObject gameObject = new GameObject("MahjongGameFlowDeclareWinDecisionTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject, false);
                Invoke(gameFlow, "StartNewRound");
                object gameState = GetProperty(gameFlow, "CurrentState");
                Invoke(
                    gameFlow,
                    "SetWinDecisionPending",
                    true,
                    GetProperty(gameState, "CurrentTurn"),
                    GetProperty(gameState, "TurnIndex"));

                Invoke(gameFlow, "RequestDeclareWin");

                Assert.That(GetProperty(gameState, "IsWinDecisionPending"), Is.False);
                Assert.That(GetProperty(gameState, "IsRoundEnded"), Is.True);
                Assert.That(GetProperty(gameState, "IsInteractionLocked"), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void RetryPrototype_StartsWithNoWinDecisionPending()
        {
            GameObject gameObject = new GameObject("MahjongGameFlowRetryWinDecisionTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject, false);
                Invoke(gameFlow, "StartNewRound");
                object firstState = GetProperty(gameFlow, "CurrentState");
                Invoke(
                    gameFlow,
                    "SetWinDecisionPending",
                    true,
                    GetProperty(firstState, "CurrentTurn"),
                    GetProperty(firstState, "TurnIndex"));

                Invoke(gameFlow, "RetryPrototype");

                object retryState = GetProperty(gameFlow, "CurrentState");
                Assert.That(GetProperty(retryState, "IsWinDecisionPending"), Is.False);
                Assert.That(GetProperty(retryState, "WinDecisionTurnIndex"), Is.EqualTo(0));
                Assert.That(GetProperty(retryState, "IsInteractionLocked"), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void InitializeRound_SetsCurrentTurnAndTurnIndex()
        {
            object gameState = CreateGameState("East", "South");
            object manager = CreatePlayerTurnManager();

            Invoke(manager, "InitializeRound", gameState, ParseSeat("South"));

            Assert.That(GetProperty(gameState, "CurrentTurn").ToString(), Is.EqualTo("South"));
            Assert.That(GetProperty(gameState, "TurnIndex"), Is.EqualTo(1));
            Assert.That(GetProperty(gameState, "TurnPhase").ToString(), Is.EqualTo("WaitingForDraw"));
        }

        [Test]
        public void EndTurnAndSelectNext_AdvancesSeatAndTurnIndex()
        {
            object activeSeats = CreateSeatList("East", "South", "West", "North");
            object gameState = CreateGameState("East", "South", "West", "North");
            object manager = CreatePlayerTurnManager();
            Invoke(manager, "InitializeRound", gameState, ParseSeat("East"));

            object nextSeat = Invoke(manager, "EndTurnAndSelectNext", gameState, activeSeats);

            Assert.That(nextSeat.ToString(), Is.EqualTo("South"));
            Assert.That(GetProperty(gameState, "CurrentTurn").ToString(), Is.EqualTo("South"));
            Assert.That(GetProperty(gameState, "TurnIndex"), Is.EqualTo(2));
        }

        [Test]
        public void EndTurnAndSelectNext_ReturnsSameSeat_WhenOnlyEastIsActive()
        {
            object activeSeats = CreateSeatList("East");
            object gameState = CreateGameState("East");
            object manager = CreatePlayerTurnManager();
            Invoke(manager, "InitializeRound", gameState, ParseSeat("East"));

            object nextSeat = Invoke(manager, "EndTurnAndSelectNext", gameState, activeSeats);

            Assert.That(nextSeat.ToString(), Is.EqualTo("East"));
            Assert.That(GetProperty(gameState, "CurrentTurn").ToString(), Is.EqualTo("East"));
            Assert.That(GetProperty(gameState, "TurnIndex"), Is.EqualTo(2));
        }

        [Test]
        public void TurnPhase_UsesDrawnTileAsPhaseSource()
        {
            object gameState = CreateGameState("East");
            object playerSeat = GetCurrentPlayerSeat(gameState);

            Assert.That(GetProperty(gameState, "TurnPhase").ToString(), Is.EqualTo("WaitingForDraw"));

            Invoke(playerSeat, "SetDrawnTile", CreateTile("E"));

            Assert.That(GetProperty(gameState, "TurnPhase").ToString(), Is.EqualTo("WaitingForDiscard"));
        }

        [Test]
        public void GameFlow_UsesDrawnTileAsDiscardGuard()
        {
            GameObject gameObject = new GameObject("MahjongGameFlowTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject, false);

                Invoke(gameFlow, "StartNewRound");
                object gameState = GetProperty(gameFlow, "CurrentState");
                object playerSeat = GetCurrentPlayerSeat(gameState);

                Assert.That(GetProperty(playerSeat, "HasDrawnTile"), Is.False);
                Assert.That(GetProperty(gameState, "TurnPhase").ToString(), Is.EqualTo("WaitingForDraw"));

                Invoke(gameFlow, "RequestDiscard", 0);
                Assert.That(GetProperty(gameState, "TurnIndex"), Is.EqualTo(1));
                Assert.That(GetProperty(playerSeat, "HasDrawnTile"), Is.False);
                Assert.That(GetProperty(gameState, "TurnPhase").ToString(), Is.EqualTo("WaitingForDraw"));

                Invoke(gameFlow, "RequestDraw");
                Assert.That(GetProperty(playerSeat, "HasDrawnTile"), Is.True);
                Assert.That(GetProperty(gameState, "TurnPhase").ToString(), Is.EqualTo("WaitingForDiscard"));

                Invoke(gameFlow, "RequestDiscard", 0);
                playerSeat = GetCurrentPlayerSeat(gameState);

                Assert.That(GetProperty(playerSeat, "HasDrawnTile"), Is.False);
                Assert.That(GetProperty(gameState, "TurnIndex"), Is.EqualTo(2));
                Assert.That(GetProperty(gameState, "TurnPhase").ToString(), Is.EqualTo("WaitingForDraw"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void AutoDraw_StartNewRoundPlacesDrawnTile()
        {
            GameObject gameObject = new GameObject("MahjongGameFlowAutoDrawStartTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject, true);

                Invoke(gameFlow, "StartNewRound");

                object gameState = GetProperty(gameFlow, "CurrentState");
                object playerSeat = GetCurrentPlayerSeat(gameState);

                Assert.That(GetProperty(playerSeat, "HasDrawnTile"), Is.True);
                Assert.That(GetProperty(gameState, "TurnPhase").ToString(), Is.EqualTo("WaitingForDiscard"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void AutoDraw_SkipsWhenDrawnTileAlreadyExists()
        {
            GameObject gameObject = new GameObject("MahjongGameFlowAutoDrawExistingTileTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject, true);
                Invoke(gameFlow, "StartNewRound");
                object gameState = GetProperty(gameFlow, "CurrentState");
                object wall = GetProperty(gameState, "Wall");
                int wallCount = (int)GetProperty(wall, "Count");
                object drawnTile = GetProperty(GetCurrentPlayerSeat(gameState), "DrawnTile");

                Invoke(gameFlow, "StartTurn", GetProperty(gameState, "CurrentTurn"), GetProperty(gameState, "TurnIndex"));

                object playerSeat = GetCurrentPlayerSeat(gameState);
                Assert.That(GetProperty(playerSeat, "DrawnTile"), Is.EqualTo(drawnTile));
                Assert.That(GetProperty(wall, "Count"), Is.EqualTo(wallCount));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void AutoDraw_SkipsWhenRoundEnded()
        {
            GameObject gameObject = new GameObject("MahjongGameFlowAutoDrawRoundEndedTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject, true);
                Invoke(gameFlow, "StartNewRound");
                object gameState = GetProperty(gameFlow, "CurrentState");
                object playerSeat = GetCurrentPlayerSeat(gameState);
                Invoke(playerSeat, "ClearDrawnTile");
                SetProperty(gameState, "IsRoundEnded", true);
                object wall = GetProperty(gameState, "Wall");
                int wallCount = (int)GetProperty(wall, "Count");

                Invoke(gameFlow, "StartTurn", GetProperty(gameState, "CurrentTurn"), GetProperty(gameState, "TurnIndex"));

                Assert.That(GetProperty(playerSeat, "HasDrawnTile"), Is.False);
                Assert.That(GetProperty(wall, "Count"), Is.EqualTo(wallCount));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void AutoDraw_SkipsDuringWinDecision()
        {
            GameObject gameObject = new GameObject("MahjongGameFlowAutoDrawWinDecisionTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject, true);
                Invoke(gameFlow, "StartNewRound");
                object gameState = GetProperty(gameFlow, "CurrentState");
                object playerSeat = GetCurrentPlayerSeat(gameState);
                Invoke(playerSeat, "ClearDrawnTile");
                Invoke(
                    gameState,
                    "BeginWinDecision",
                    GetProperty(gameState, "CurrentTurn"),
                    GetProperty(gameState, "TurnIndex"));
                object wall = GetProperty(gameState, "Wall");
                int wallCount = (int)GetProperty(wall, "Count");

                Invoke(gameFlow, "StartTurn", GetProperty(gameState, "CurrentTurn"), GetProperty(gameState, "TurnIndex"));

                Assert.That(GetProperty(playerSeat, "HasDrawnTile"), Is.False);
                Assert.That(GetProperty(wall, "Count"), Is.EqualTo(wallCount));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void AutoDraw_DrawsAgainAfterDiscard()
        {
            GameObject gameObject = new GameObject("MahjongGameFlowAutoDrawAfterDiscardTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject, true);
                Invoke(gameFlow, "StartNewRound");
                object gameState = GetProperty(gameFlow, "CurrentState");

                Invoke(gameFlow, "RequestDiscardDrawnTile");

                object playerSeat = GetCurrentPlayerSeat(gameState);
                Assert.That(GetProperty(gameState, "TurnIndex"), Is.EqualTo(2));
                Assert.That(GetProperty(playerSeat, "HasDrawnTile"), Is.True);
                Assert.That(GetProperty(gameState, "TurnPhase").ToString(), Is.EqualTo("WaitingForDiscard"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void AutoDraw_RetryStartsWithDrawnTile()
        {
            GameObject gameObject = new GameObject("MahjongGameFlowAutoDrawRetryTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject, true);
                Invoke(gameFlow, "StartNewRound");

                Invoke(gameFlow, "RetryPrototype");

                object gameState = GetProperty(gameFlow, "CurrentState");
                object playerSeat = GetCurrentPlayerSeat(gameState);
                Assert.That(GetProperty(gameState, "TurnIndex"), Is.EqualTo(1));
                Assert.That(GetProperty(playerSeat, "HasDrawnTile"), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void RetryPrototype_RebuildsOccupiedSeatsWithoutOldSelfSeat()
        {
            GameObject gameObject = new GameObject("MahjongGameFlowRetryOccupiedSeatsTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject, false);
                SetPrivateField(gameFlow, "fixedSelfSeat", ParseSeat("South"));

                Invoke(gameFlow, "StartNewRound");
                object firstState = GetProperty(gameFlow, "CurrentState");
                AssertOccupiedSeats(firstState, "South");
                AssertSeatList(GetProperty(firstState, "ActiveTurnSeats"), "South");
                Assert.That(GetProperty(firstState, "CurrentTurn").ToString(), Is.EqualTo("South"));

                SetPrivateField(gameFlow, "fixedSelfSeat", ParseSeat("West"));
                Invoke(gameFlow, "RetryPrototype");

                object retryState = GetProperty(gameFlow, "CurrentState");
                AssertOccupiedSeats(retryState, "West");
                AssertSeatList(GetProperty(retryState, "ActiveTurnSeats"), "West");
                Assert.That(GetProperty(retryState, "CurrentTurn").ToString(), Is.EqualTo("West"));

                object seatSlots = GetProperty(retryState, "SeatSlots");
                AssertSeatSlot(seatSlots, 0, "East", null);
                AssertSeatSlot(seatSlots, 1, "South", null);
                AssertSeatSlot(seatSlots, 2, "West", "Player1");
                AssertSeatSlot(seatSlots, 3, "North", null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void RebuildActiveTurnSeatsFromSeatSlots_SkipsEmptySeatsAndRepairsCurrentTurn()
        {
            object gameState = CreateGameState("East");
            Invoke(gameState, "SetSelfSeat", ParseSeat("South"));
            Invoke(gameState, "AssignPlayerToSeat", ParsePlayerId("Player2"), ParseSeat("North"));
            SetProperty(gameState, "CurrentTurn", ParseSeat("East"));

            Invoke(gameState, "RebuildActiveTurnSeatsFromSeatSlots");

            AssertSeatList(GetProperty(gameState, "ActiveTurnSeats"), "South", "North");
            AssertSeatList(GetProperty(gameState, "ActiveSeats"), "South", "North");
            Assert.That(GetProperty(gameState, "CurrentTurn").ToString(), Is.EqualTo("South"));
            Assert.That(GetProperty(gameState, "CurrentTurnPlayerId").ToString(), Is.EqualTo("Player1"));
        }

        [TestCase("East")]
        [TestCase("South")]
        [TestCase("West")]
        [TestCase("North")]
        public void GameFlow_FixedSelfSeatSetsSingleActiveTurnPlayer(string selfSeatName)
        {
            GameObject gameObject = new GameObject("MahjongGameFlowFixedSelfSeatTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject, false);
                SetPrivateField(gameFlow, "randomizeSelfSeat", false);
                SetPrivateField(gameFlow, "fixedSelfSeat", ParseSeat(selfSeatName));

                Invoke(gameFlow, "StartNewRound");

                object gameState = GetProperty(gameFlow, "CurrentState");
                object activeTurnSeats = GetProperty(gameState, "ActiveTurnSeats");
                PropertyInfo itemProperty = activeTurnSeats.GetType().GetProperty("Item");
                Assert.That(itemProperty, Is.Not.Null);

                Assert.That(GetProperty(gameState, "SelfSeat").ToString(), Is.EqualTo(selfSeatName));
                Assert.That(GetProperty(gameState, "SelfWind").ToString(), Is.EqualTo(selfSeatName));
                Assert.That(GetProperty(gameState, "SelfPlayerId").ToString(), Is.EqualTo("Player1"));
                Assert.That(GetProperty(gameState, "CurrentTurn").ToString(), Is.EqualTo(selfSeatName));
                Assert.That(GetProperty(gameState, "CurrentTurnPlayerId").ToString(), Is.EqualTo("Player1"));
                Assert.That(GetProperty(gameState, "IsSelfTurn"), Is.True);
                Assert.That(GetProperty(activeTurnSeats, "Count"), Is.EqualTo(1));
                Assert.That(itemProperty.GetValue(activeTurnSeats, new object[] { 0 }).ToString(), Is.EqualTo(selfSeatName));
                AssertSeatList(GetProperty(gameState, "ActiveSeats"), selfSeatName);

                object seatSlots = GetProperty(gameState, "SeatSlots");
                Assert.That(GetProperty(seatSlots, "Count"), Is.EqualTo(4));
                AssertSeatSlot(seatSlots, 0, "East", selfSeatName == "East" ? "Player1" : null);
                AssertSeatSlot(seatSlots, 1, "South", selfSeatName == "South" ? "Player1" : null);
                AssertSeatSlot(seatSlots, 2, "West", selfSeatName == "West" ? "Player1" : null);
                AssertSeatSlot(seatSlots, 3, "North", selfSeatName == "North" ? "Player1" : null);

                object selfSeatSlot = Invoke(gameState, "GetSelfSeatSlot");
                Assert.That(GetProperty(selfSeatSlot, "Wind").ToString(), Is.EqualTo(selfSeatName));
                object currentTurnSlot = GetProperty(gameState, "CurrentTurnSlot");
                Assert.That(GetProperty(currentTurnSlot, "Wind").ToString(), Is.EqualTo(selfSeatName));
                Assert.That(Invoke(gameState, "GetSeatByPlayerId", ParsePlayerId("Player1")).ToString(), Is.EqualTo(selfSeatName));
                Assert.That((bool)Invoke(gameState, "IsSelfSeat", ParseSeat(selfSeatName)), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
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
            GameObject gameObject = new GameObject("MahjongGameFlowTwoParticipantSeatTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject, false);
                SetPrivateField(gameFlow, "participantCount", 2);
                SetPrivateField(gameFlow, "fixedSelfSeat", ParseSeat(selfSeatName));

                Invoke(gameFlow, "StartNewRound");

                object gameState = GetProperty(gameFlow, "CurrentState");
                Assert.That(
                    Invoke(gameState, "GetSeatByPlayerId", ParsePlayerId("Player1")).ToString(),
                    Is.EqualTo(selfSeatName));
                Assert.That(
                    Invoke(gameState, "GetSeatByPlayerId", ParsePlayerId("Player2")).ToString(),
                    Is.EqualTo(player2SeatName));
                Assert.That(GetProperty(gameState, "CurrentTurn").ToString(), Is.EqualTo(selfSeatName));
                Assert.That(GetProperty(gameState, "CurrentTurnPlayerId").ToString(), Is.EqualTo("Player1"));
                Assert.That(GetProperty(GetProperty(gameState, "OccupiedSeats"), "Count"), Is.EqualTo(2));
                Assert.That(GetProperty(GetProperty(gameState, "ActiveTurnSeats"), "Count"), Is.EqualTo(2));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void GameFlow_ThreeParticipantsAssignsPlayer1ThroughPlayer3()
        {
            GameObject gameObject = new GameObject("MahjongGameFlowThreeParticipantSeatTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject, false);
                SetPrivateField(gameFlow, "participantCount", 3);
                SetPrivateField(gameFlow, "fixedSelfSeat", ParseSeat("East"));

                Invoke(gameFlow, "StartNewRound");

                object gameState = GetProperty(gameFlow, "CurrentState");
                AssertSeatList(GetProperty(gameState, "OccupiedSeats"), "East", "South", "West");
                AssertSeatList(GetProperty(gameState, "ActiveTurnSeats"), "East", "South", "West");
                Assert.That(GetProperty(gameState, "CurrentTurn").ToString(), Is.EqualTo("East"));
                Assert.That(GetProperty(gameState, "CurrentTurnPlayerId").ToString(), Is.EqualTo("Player1"));
                Assert.That(
                    Invoke(gameState, "GetSeatByPlayerId", ParsePlayerId("Player1")).ToString(),
                    Is.EqualTo("East"));
                Assert.That(
                    Invoke(gameState, "GetSeatByPlayerId", ParsePlayerId("Player2")).ToString(),
                    Is.EqualTo("South"));
                Assert.That(
                    Invoke(gameState, "GetSeatByPlayerId", ParsePlayerId("Player3")).ToString(),
                    Is.EqualTo("West"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void GameFlow_FourParticipantsAssignsAllPlayers()
        {
            GameObject gameObject = new GameObject("MahjongGameFlowFourParticipantSeatTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject, false);
                SetPrivateField(gameFlow, "participantCount", 4);
                SetPrivateField(gameFlow, "fixedSelfSeat", ParseSeat("East"));

                Invoke(gameFlow, "StartNewRound");

                object gameState = GetProperty(gameFlow, "CurrentState");
                AssertSeatList(GetProperty(gameState, "OccupiedSeats"), "East", "South", "West", "North");
                AssertSeatList(GetProperty(gameState, "ActiveTurnSeats"), "East", "South", "West", "North");
                Assert.That(GetProperty(gameState, "CurrentTurn").ToString(), Is.EqualTo("East"));
                Assert.That(GetProperty(gameState, "CurrentTurnPlayerId").ToString(), Is.EqualTo("Player1"));
                Assert.That(
                    Invoke(gameState, "GetSeatByPlayerId", ParsePlayerId("Player1")).ToString(),
                    Is.EqualTo("East"));
                Assert.That(
                    Invoke(gameState, "GetSeatByPlayerId", ParsePlayerId("Player2")).ToString(),
                    Is.EqualTo("South"));
                Assert.That(
                    Invoke(gameState, "GetSeatByPlayerId", ParsePlayerId("Player3")).ToString(),
                    Is.EqualTo("West"));
                Assert.That(
                    Invoke(gameState, "GetSeatByPlayerId", ParsePlayerId("Player4")).ToString(),
                    Is.EqualTo("North"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void AutoSortToggle_DuringOpponentTurn_SortsOnlySelfHand()
        {
            GameObject gameObject = new GameObject("AutoSortToggleSelfHandTest");
            try
            {
                object gameFlow = AddTwoPlayerFlowWithoutInitialHand(gameObject);
                Invoke(gameFlow, "StartNewRound");
                object gameState = GetProperty(gameFlow, "CurrentState");
                object selfSeat = GetProperty(gameState, "SelfSeat");
                object opponentSeat = Invoke(gameState, "GetSeatByPlayerId", ParsePlayerId("Player2"));
                object selfPlayerSeat = Invoke(gameState, "GetPlayerSeat", selfSeat);
                object opponentPlayerSeat = Invoke(gameState, "GetPlayerSeat", opponentSeat);
                AddHandTiles(selfPlayerSeat, "9m", "1m");
                AddHandTiles(opponentPlayerSeat, "9p", "1p");
                SetProperty(gameState, "CurrentTurn", opponentSeat);

                Invoke(gameFlow, "RequestSetAutoSortEnabled", true);

                Assert.That(GetHandDisplayString(selfPlayerSeat), Is.EqualTo("1m 9m"));
                Assert.That(GetHandDisplayString(opponentPlayerSeat), Is.EqualTo("9p 1p"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void InitialDealAutoSort_SortsOnlySelfHand()
        {
            GameObject gameObject = new GameObject("InitialDealAutoSortSelfHandTest");
            try
            {
                object gameFlow = AddTwoPlayerFlowWithoutInitialHand(gameObject);
                Invoke(gameFlow, "StartNewRound");
                object gameState = GetProperty(gameFlow, "CurrentState");
                object selfSeat = GetProperty(gameState, "SelfSeat");
                object opponentSeat = Invoke(gameState, "GetSeatByPlayerId", ParsePlayerId("Player2"));
                object selfPlayerSeat = Invoke(gameState, "GetPlayerSeat", selfSeat);
                object opponentPlayerSeat = Invoke(gameState, "GetPlayerSeat", opponentSeat);
                AddHandTiles(selfPlayerSeat, "9m", "1m");
                AddHandTiles(opponentPlayerSeat, "9p", "1p");
                SetPrivateField(gameFlow, "autoSortEnabled", true);

                Invoke(gameFlow, "DealInitialHands");

                Assert.That(GetHandDisplayString(selfPlayerSeat), Is.EqualTo("1m 9m"));
                Assert.That(GetHandDisplayString(opponentPlayerSeat), Is.EqualTo("9p 1p"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void AutoSortAfterHandChange_IgnoresOpponentSeat()
        {
            GameObject gameObject = new GameObject("AutoSortChangedHandSelfGuardTest");
            try
            {
                object gameFlow = AddTwoPlayerFlowWithoutInitialHand(gameObject);
                Invoke(gameFlow, "StartNewRound");
                object gameState = GetProperty(gameFlow, "CurrentState");
                object selfSeat = GetProperty(gameState, "SelfSeat");
                object opponentSeat = Invoke(gameState, "GetSeatByPlayerId", ParsePlayerId("Player2"));
                object selfPlayerSeat = Invoke(gameState, "GetPlayerSeat", selfSeat);
                object opponentPlayerSeat = Invoke(gameState, "GetPlayerSeat", opponentSeat);
                AddHandTiles(selfPlayerSeat, "9m", "1m");
                AddHandTiles(opponentPlayerSeat, "9p", "1p");
                SetPrivateField(gameFlow, "autoSortEnabled", true);

                Invoke(gameFlow, "ApplyAutoSortIfEnabled", opponentSeat, "Test", false);
                Invoke(gameFlow, "ApplyAutoSortIfEnabled", selfSeat, "Test", false);

                Assert.That(GetHandDisplayString(selfPlayerSeat), Is.EqualTo("1m 9m"));
                Assert.That(GetHandDisplayString(opponentPlayerSeat), Is.EqualTo("9p 1p"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void RequestDraw_DuringOpponentTurn_DoesNotDraw()
        {
            GameObject gameObject = new GameObject("RequestDrawSelfTurnGuardTest");
            try
            {
                object gameFlow = AddTwoPlayerFlowWithoutInitialHand(gameObject);
                Invoke(gameFlow, "StartNewRound");
                object gameState = GetProperty(gameFlow, "CurrentState");
                object opponentSeat = Invoke(gameState, "GetSeatByPlayerId", ParsePlayerId("Player2"));
                object opponentPlayerSeat = Invoke(gameState, "GetPlayerSeat", opponentSeat);
                SetProperty(gameState, "CurrentTurn", opponentSeat);
                int wallCount = (int)GetProperty(GetProperty(gameState, "Wall"), "Count");

                Invoke(gameFlow, "RequestDraw");

                Assert.That(GetProperty(GetProperty(gameState, "Wall"), "Count"), Is.EqualTo(wallCount));
                Assert.That(GetProperty(opponentPlayerSeat, "HasDrawnTile"), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void RequestDiscard_DuringOpponentTurn_DoesNotDiscard()
        {
            GameObject gameObject = new GameObject("RequestDiscardSelfTurnGuardTest");
            try
            {
                object gameFlow = AddTwoPlayerFlowWithoutInitialHand(gameObject);
                Invoke(gameFlow, "StartNewRound");
                object gameState = GetProperty(gameFlow, "CurrentState");
                object opponentSeat = Invoke(gameState, "GetSeatByPlayerId", ParsePlayerId("Player2"));
                object opponentPlayerSeat = Invoke(gameState, "GetPlayerSeat", opponentSeat);
                AddHandTiles(opponentPlayerSeat, "9p");
                Invoke(opponentPlayerSeat, "SetDrawnTile", CreateTile("1p"));
                SetProperty(gameState, "CurrentTurn", opponentSeat);

                Invoke(gameFlow, "RequestDiscard", 0);

                Assert.That(GetProperty(GetProperty(opponentPlayerSeat, "Hand"), "Count"), Is.EqualTo(1));
                Assert.That(GetProperty(opponentPlayerSeat, "HasDrawnTile"), Is.True);
                Assert.That(GetProperty(GetProperty(gameState, "Discards"), "Count"), Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void RequestDiscardDrawnTile_DuringOpponentTurn_DoesNotDiscard()
        {
            GameObject gameObject = new GameObject("RequestDiscardDrawnTileSelfTurnGuardTest");
            try
            {
                object gameFlow = AddTwoPlayerFlowWithoutInitialHand(gameObject);
                Invoke(gameFlow, "StartNewRound");
                object gameState = GetProperty(gameFlow, "CurrentState");
                object opponentSeat = Invoke(gameState, "GetSeatByPlayerId", ParsePlayerId("Player2"));
                object opponentPlayerSeat = Invoke(gameState, "GetPlayerSeat", opponentSeat);
                Invoke(opponentPlayerSeat, "SetDrawnTile", CreateTile("1p"));
                SetProperty(gameState, "CurrentTurn", opponentSeat);

                Invoke(gameFlow, "RequestDiscardDrawnTile");

                Assert.That(GetProperty(opponentPlayerSeat, "HasDrawnTile"), Is.True);
                Assert.That(GetProperty(GetProperty(gameState, "Discards"), "Count"), Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void RequestForceDrawSkill_DuringOpponentTurn_DoesNotActivateSkill()
        {
            GameObject gameObject = new GameObject("RequestSkillSelfTurnGuardTest");
            try
            {
                object gameFlow = AddTwoPlayerFlowWithoutInitialHand(gameObject);
                Invoke(gameFlow, "StartNewRound");
                object gameState = GetProperty(gameFlow, "CurrentState");
                object opponentSeat = Invoke(gameState, "GetSeatByPlayerId", ParsePlayerId("Player2"));
                SetProperty(gameState, "CurrentTurn", opponentSeat);

                Invoke(gameFlow, "RequestForceDrawSkill", "5m");

                Assert.That(GetProperty(GetProperty(gameState, "ActiveSkillEffects"), "Count"), Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void RequestForceDrawSkill_DuringSelfTurn_UsesSelfSeatAsOwner()
        {
            GameObject gameObject = new GameObject("RequestSkillSelfOwnerTest");
            try
            {
                object gameFlow = AddTwoPlayerFlowWithoutInitialHand(gameObject);
                Invoke(gameFlow, "StartNewRound");
                object gameState = GetProperty(gameFlow, "CurrentState");

                Invoke(gameFlow, "RequestForceDrawSkill", "5m");

                object effects = GetProperty(gameState, "ActiveSkillEffects");
                Assert.That(GetProperty(effects, "Count"), Is.EqualTo(1));
                PropertyInfo itemProperty = effects.GetType().GetProperty("Item");
                Assert.That(itemProperty, Is.Not.Null);
                object effect = itemProperty.GetValue(effects, new object[] { 0 });
                Assert.That(
                    GetProperty(effect, "OwnerSeat"),
                    Is.EqualTo(GetProperty(gameState, "SelfSeat")));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void SeatSpecificDrawAndDiscardApis_UseActorSeatAndAdvanceTurn()
        {
            GameObject gameObject = new GameObject("SeatSpecificCpuApiTest");
            try
            {
                object gameFlow = AddTwoPlayerFlowWithoutInitialHand(gameObject);
                Invoke(gameFlow, "StartNewRound");
                object gameState = GetProperty(gameFlow, "CurrentState");
                object selfSeat = GetProperty(gameState, "SelfSeat");
                object cpuSeat = Invoke(gameState, "GetSeatByPlayerId", ParsePlayerId("Player2"));
                SetProperty(gameState, "CurrentTurn", cpuSeat);

                bool drew = (bool)Invoke(gameFlow, "TryRequestDrawForSeat", cpuSeat);
                object drawnTile = GetProperty(GetPlayerSeat(gameState, cpuSeat), "DrawnTile");
                bool discarded = (bool)Invoke(
                    gameFlow,
                    "TryRequestDiscardDrawnTileForSeat",
                    cpuSeat);

                Assert.That(drew, Is.True);
                Assert.That(discarded, Is.True);
                object discards = GetProperty(gameState, "Discards");
                object record = GetListItem(discards, 0);
                Assert.That(GetProperty(record, "ActorSeat"), Is.EqualTo(cpuSeat));
                Assert.That(GetProperty(record, "Tile"), Is.EqualTo(drawnTile));
                Assert.That(GetProperty(record, "Source").ToString(), Is.EqualTo("DrawnTile"));
                Assert.That(GetProperty(gameState, "CurrentTurn"), Is.EqualTo(selfSeat));
                Assert.That(GetProperty(gameState, "TurnIndex"), Is.EqualTo(2));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void CpuDiscard_WhenSelfCanRon_CreatesRonDecisionWithoutAdvancingTurn()
        {
            GameObject gameObject = new GameObject("CpuDiscardCreatesRonDecisionTest");
            try
            {
                object gameFlow = AddTwoPlayerFlowWithoutInitialHand(gameObject);
                Invoke(gameFlow, "StartNewRound");
                object gameState = GetProperty(gameFlow, "CurrentState");
                object selfSeat = GetProperty(gameState, "SelfSeat");
                object cpuSeat = Invoke(gameState, "GetSeatByPlayerId", ParsePlayerId("Player2"));
                object selfPlayerSeat = GetPlayerSeat(gameState, selfSeat);
                object cpuPlayerSeat = GetPlayerSeat(gameState, cpuSeat);
                AddHandTiles(
                    selfPlayerSeat,
                    "1m", "2m", "3m",
                    "1p", "2p", "3p",
                    "1s", "2s", "3s",
                    "E", "E", "E",
                    "C");
                Invoke(cpuPlayerSeat, "SetDrawnTile", CreateTile("C"));
                SetProperty(gameState, "CurrentTurn", cpuSeat);
                int turnIndexBeforeDiscard = (int)GetProperty(gameState, "TurnIndex");

                bool discarded = (bool)Invoke(
                    gameFlow,
                    "TryRequestDiscardDrawnTileForSeat",
                    cpuSeat);

                Assert.That(discarded, Is.True);
                Assert.That(GetProperty(gameState, "IsWinDecisionPending"), Is.True);
                Assert.That(GetProperty(gameState, "WinDecisionSeat"), Is.EqualTo(selfSeat));
                Assert.That(GetProperty(gameState, "WinDecisionType").ToString(), Is.EqualTo("Ron"));
                Assert.That(GetProperty(gameState, "WinningTile"), Is.EqualTo(CreateTile("C")));
                Assert.That(GetProperty(gameState, "WinSourceSeat"), Is.EqualTo(cpuSeat));
                Assert.That(GetProperty(gameState, "WinDecisionTurnIndex"), Is.EqualTo(turnIndexBeforeDiscard));
                Assert.That(GetProperty(gameState, "CurrentTurn"), Is.EqualTo(cpuSeat));
                Assert.That(GetProperty(gameState, "TurnIndex"), Is.EqualTo(turnIndexBeforeDiscard));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void CpuDiscard_WhenSelfCannotRon_AdvancesTurnNormally()
        {
            GameObject gameObject = new GameObject("CpuDiscardWithoutRonAdvancesTest");
            try
            {
                object gameFlow = AddTwoPlayerFlowWithoutInitialHand(gameObject);
                Invoke(gameFlow, "StartNewRound");
                object gameState = GetProperty(gameFlow, "CurrentState");
                object selfSeat = GetProperty(gameState, "SelfSeat");
                object cpuSeat = Invoke(gameState, "GetSeatByPlayerId", ParsePlayerId("Player2"));
                object selfPlayerSeat = GetPlayerSeat(gameState, selfSeat);
                object cpuPlayerSeat = GetPlayerSeat(gameState, cpuSeat);
                AddHandTiles(
                    selfPlayerSeat,
                    "1m", "2m", "3m",
                    "1p", "2p", "3p",
                    "1s", "2s", "3s",
                    "E", "E", "E",
                    "P");
                Invoke(cpuPlayerSeat, "SetDrawnTile", CreateTile("C"));
                SetProperty(gameState, "CurrentTurn", cpuSeat);

                bool discarded = (bool)Invoke(
                    gameFlow,
                    "TryRequestDiscardDrawnTileForSeat",
                    cpuSeat);

                Assert.That(discarded, Is.True);
                Assert.That(GetProperty(gameState, "IsWinDecisionPending"), Is.False);
                Assert.That(GetProperty(gameState, "CurrentTurn"), Is.EqualTo(selfSeat));
                Assert.That(GetProperty(gameState, "TurnIndex"), Is.EqualTo(2));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void RequestDeclineWin_AfterRon_ClearsDecisionAndAdvancesTurn()
        {
            GameObject gameObject = new GameObject("DeclineRonAdvancesTurnTest");
            try
            {
                object gameFlow = AddTwoPlayerFlowWithoutInitialHand(gameObject);
                Invoke(gameFlow, "StartNewRound");
                object gameState = GetProperty(gameFlow, "CurrentState");
                object selfSeat = GetProperty(gameState, "SelfSeat");
                object cpuSeat = Invoke(gameState, "GetSeatByPlayerId", ParsePlayerId("Player2"));
                object selfPlayerSeat = GetPlayerSeat(gameState, selfSeat);
                object cpuPlayerSeat = GetPlayerSeat(gameState, cpuSeat);
                AddHandTiles(
                    selfPlayerSeat,
                    "1m", "2m", "3m",
                    "1p", "2p", "3p",
                    "1s", "2s", "3s",
                    "E", "E", "E",
                    "C");
                Invoke(cpuPlayerSeat, "SetDrawnTile", CreateTile("C"));
                SetProperty(gameState, "CurrentTurn", cpuSeat);
                Invoke(gameFlow, "TryRequestDiscardDrawnTileForSeat", cpuSeat);

                Invoke(gameFlow, "RequestDeclineWin");

                Assert.That(GetProperty(gameState, "IsWinDecisionPending"), Is.False);
                Assert.That(GetProperty(gameState, "WinDecisionType"), Is.Null);
                Assert.That(GetProperty(gameState, "CurrentTurn"), Is.EqualTo(selfSeat));
                Assert.That(GetProperty(gameState, "TurnIndex"), Is.EqualTo(2));
                Assert.That(GetProperty(gameState, "IsRoundEnded"), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ParticipantTypes_AssignSelfAsLocalHumanAndOtherSeatsAsCpu()
        {
            GameObject gameObject = new GameObject("ParticipantTypeAssignmentTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject, false);
                SetPrivateField(gameFlow, "participantCount", 2);
                Invoke(gameFlow, "StartNewRound");
                object gameState = GetProperty(gameFlow, "CurrentState");
                object selfSeat = GetProperty(gameState, "SelfSeat");
                object cpuSeat = Invoke(gameState, "GetSeatByPlayerId", ParsePlayerId("Player2"));

                object selfSlot = Invoke(gameState, "GetSeatSlot", selfSeat);
                object cpuSlot = Invoke(gameState, "GetSeatSlot", cpuSeat);
                object emptySlot = Invoke(gameState, "GetSeatSlot", ParseSeat("South"));

                Assert.That(GetProperty(selfSlot, "ParticipantType").ToString(), Is.EqualTo("LocalHuman"));
                Assert.That(GetProperty(cpuSlot, "ParticipantType").ToString(), Is.EqualTo("Cpu"));
                Assert.That(GetProperty(emptySlot, "ParticipantType"), Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [UnityTest]
        public IEnumerator CpuTurn_AutoDrawsAndDiscardsDrawnTileThroughGameFlow()
        {
            GameObject gameObject = new GameObject("CpuAutoDiscardIntegrationTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject, true);
                SetPrivateField(gameFlow, "participantCount", 2);
                SetPrivateField(gameFlow, "initialHandTileCount", 1);
                object notifier = gameObject.AddComponent(
                    Type.GetType("MahjongPrototype.Notifications.MahjongEventNotifier, Assembly-CSharp", true));
                SetPrivateField(gameFlow, "eventNotifier", notifier);
                SetCpuDiscardDelay(gameFlow, 0f);

                Invoke(gameFlow, "StartNewRound");
                object gameState = GetProperty(gameFlow, "CurrentState");
                object selfSeat = GetProperty(gameState, "SelfSeat");
                object cpuSeat = Invoke(gameState, "GetSeatByPlayerId", ParsePlayerId("Player2"));
                List<string> eventOrder = new List<string>();
                object cpuDiscardRecord = null;

                AddSingleArgumentEventHandler(
                    notifier,
                    "TileDrawn",
                    drawResult =>
                    {
                        if (GetProperty(drawResult, "Seat").Equals(cpuSeat) &&
                            GetProperty(drawResult, "Purpose").ToString() == "TurnDraw")
                        {
                            eventOrder.Add("CpuTileDrawn");
                        }
                    });
                AddSingleArgumentEventHandler(
                    notifier,
                    "TileDiscarded",
                    discardRecord =>
                    {
                        if (GetProperty(discardRecord, "ActorSeat").Equals(cpuSeat))
                        {
                            cpuDiscardRecord = discardRecord;
                            eventOrder.Add("CpuTileDiscarded");
                        }
                    });
                AddTwoArgumentEventHandler(
                    notifier,
                    "TurnStarted",
                    (seat, _) =>
                    {
                        if (seat.Equals(selfSeat) && eventOrder.Contains("CpuTileDiscarded"))
                            eventOrder.Add("NextTurnStarted");
                    });

                Invoke(gameFlow, "RequestDiscardDrawnTile");

                Assert.That(GetProperty(GetPlayerSeat(gameState, cpuSeat), "HasDrawnTile"), Is.True);

                yield return null;
                yield return null;

                Assert.That(cpuDiscardRecord, Is.Not.Null);
                Assert.That(GetProperty(cpuDiscardRecord, "ActorSeat"), Is.EqualTo(cpuSeat));
                Assert.That(GetProperty(cpuDiscardRecord, "Source").ToString(), Is.EqualTo("DrawnTile"));
                Assert.That(eventOrder.IndexOf("CpuTileDrawn"), Is.GreaterThanOrEqualTo(0));
                Assert.That(
                    eventOrder.IndexOf("CpuTileDiscarded"),
                    Is.GreaterThan(eventOrder.IndexOf("CpuTileDrawn")));
                Assert.That(
                    eventOrder.IndexOf("NextTurnStarted"),
                    Is.GreaterThan(eventOrder.IndexOf("CpuTileDiscarded")));
                Assert.That(GetProperty(gameState, "CurrentTurn"), Is.EqualTo(selfSeat));
                Assert.That(GetProperty(gameState, "TurnIndex"), Is.EqualTo(3));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [UnityTest]
        public IEnumerator RemoteHumanTurn_DoesNotStartCpuAutoDiscard()
        {
            GameObject gameObject = new GameObject("RemoteHumanDoesNotAutoDiscardTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject, true);
                SetPrivateField(gameFlow, "participantCount", 2);
                SetPrivateField(gameFlow, "initialHandTileCount", 1);
                SetCpuDiscardDelay(gameFlow, 0f);
                Invoke(gameFlow, "StartNewRound");
                object gameState = GetProperty(gameFlow, "CurrentState");
                object remoteSeat = Invoke(gameState, "GetSeatByPlayerId", ParsePlayerId("Player2"));
                Invoke(
                    gameState,
                    "SetParticipantType",
                    remoteSeat,
                    ParseParticipantType("RemoteHuman"));

                Invoke(gameFlow, "RequestDiscardDrawnTile");

                yield return null;
                yield return null;

                Assert.That(GetProperty(gameState, "CurrentTurn"), Is.EqualTo(remoteSeat));
                Assert.That(GetProperty(gameState, "TurnIndex"), Is.EqualTo(2));
                Assert.That(GetProperty(GetPlayerSeat(gameState, remoteSeat), "HasDrawnTile"), Is.True);
                Assert.That(GetProperty(GetProperty(gameState, "Discards"), "Count"), Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [UnityTest]
        public IEnumerator CpuTurn_DoesNotDiscardWhileWinDecisionIsPending()
        {
            GameObject gameObject = new GameObject("CpuWinDecisionStopsDiscardTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject, true);
                SetPrivateField(gameFlow, "participantCount", 2);
                SetPrivateField(gameFlow, "initialHandTileCount", 1);
                SetCpuDiscardDelay(gameFlow, 0f);
                Invoke(gameFlow, "StartNewRound");
                object gameState = GetProperty(gameFlow, "CurrentState");
                object cpuSeat = Invoke(gameState, "GetSeatByPlayerId", ParsePlayerId("Player2"));

                Invoke(gameFlow, "RequestDiscardDrawnTile");
                Invoke(
                    gameState,
                    "BeginWinDecision",
                    cpuSeat,
                    GetProperty(gameState, "TurnIndex"));

                yield return null;
                yield return null;

                Assert.That(GetProperty(gameState, "CurrentTurn"), Is.EqualTo(cpuSeat));
                Assert.That(GetProperty(GetPlayerSeat(gameState, cpuSeat), "HasDrawnTile"), Is.True);
                Assert.That(GetProperty(GetProperty(gameState, "Discards"), "Count"), Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [UnityTest]
        public IEnumerator CpuTurn_DoesNotDiscardAfterRoundEnds()
        {
            GameObject gameObject = new GameObject("CpuRoundEndStopsDiscardTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject, true);
                SetPrivateField(gameFlow, "participantCount", 2);
                SetPrivateField(gameFlow, "initialHandTileCount", 1);
                SetCpuDiscardDelay(gameFlow, 0f);
                Invoke(gameFlow, "StartNewRound");
                object gameState = GetProperty(gameFlow, "CurrentState");
                object cpuSeat = Invoke(gameState, "GetSeatByPlayerId", ParsePlayerId("Player2"));

                Invoke(gameFlow, "RequestDiscardDrawnTile");
                SetProperty(gameState, "IsRoundEnded", true);

                yield return null;
                yield return null;

                Assert.That(GetProperty(gameState, "CurrentTurn"), Is.EqualTo(cpuSeat));
                Assert.That(GetProperty(GetPlayerSeat(gameState, cpuSeat), "HasDrawnTile"), Is.True);
                Assert.That(GetProperty(GetProperty(gameState, "Discards"), "Count"), Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [UnityTest]
        public IEnumerator RetryPrototype_CancelsCpuActionFromPreviousGameState()
        {
            GameObject gameObject = new GameObject("RetryCancelsOldCpuTurnTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject, true);
                SetPrivateField(gameFlow, "participantCount", 2);
                SetPrivateField(gameFlow, "initialHandTileCount", 1);
                SetCpuDiscardDelay(gameFlow, 0f);
                Invoke(gameFlow, "StartNewRound");
                object previousState = GetProperty(gameFlow, "CurrentState");

                Invoke(gameFlow, "RequestDiscardDrawnTile");
                Invoke(gameFlow, "RetryPrototype");
                object retryState = GetProperty(gameFlow, "CurrentState");

                yield return null;
                yield return null;

                Assert.That(retryState, Is.Not.SameAs(previousState));
                Assert.That(GetProperty(GetProperty(retryState, "Discards"), "Count"), Is.EqualTo(0));
                Assert.That(
                    GetProperty(retryState, "CurrentTurn"),
                    Is.EqualTo(GetProperty(retryState, "SelfSeat")));
                Assert.That(GetProperty(retryState, "TurnIndex"), Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private static object AddTwoPlayerFlowWithoutInitialHand(GameObject gameObject)
        {
            object gameFlow = AddConfiguredGameFlow(gameObject, false);
            SetPrivateField(gameFlow, "participantCount", 2);
            SetPrivateField(gameFlow, "initialHandTileCount", 0);
            return gameFlow;
        }

        private static void AddHandTiles(object playerSeat, params string[] tileCodes)
        {
            object hand = GetProperty(playerSeat, "Hand");
            for (int i = 0; i < tileCodes.Length; i++)
                Invoke(hand, "Add", CreateTile(tileCodes[i]));
        }

        private static string GetHandDisplayString(object playerSeat)
        {
            return (string)Invoke(GetProperty(playerSeat, "Hand"), "ToDisplayString");
        }

        private static object AddConfiguredGameFlow(GameObject gameObject, bool enableAutoDraw)
        {
            object gameFlow = gameObject.AddComponent(GetMahjongGameFlowType());
            SetPrivateField(gameFlow, "enableDevLog", false);
            SetPrivateField(gameFlow, "logWarnings", false);
            SetPrivateField(gameFlow, "initialHandTileCount", 1);
            SetPrivateField(gameFlow, "autoStart", false);
            SetPrivateField(gameFlow, "useFixedRandomSeed", true);
            SetPrivateField(gameFlow, "fixedRandomSeed", 12345);
            SetPrivateField(gameFlow, "enableAutoDraw", enableAutoDraw);
            SetPrivateField(gameFlow, "randomizeSelfSeat", false);
            SetPrivateField(gameFlow, "fixedSelfSeat", ParseSeat("East"));
            return gameFlow;
        }

        private static object CreatePlayerTurnManager()
        {
            Type managerType = Type.GetType(PlayerTurnManagerTypeName, true);
            Type turnOrderServiceType = Type.GetType(TurnOrderServiceTypeName, true);
            object turnOrderService = Activator.CreateInstance(turnOrderServiceType);
            return Activator.CreateInstance(managerType, turnOrderService);
        }

        private static object CreateGameState(params string[] seatNames)
        {
            Type gameStateType = Type.GetType(MahjongGameStateTypeName, true);
            Type wallType = Type.GetType(WallTypeName, true);
            MethodInfo createWall = wallType.GetMethod("CreateStandardShuffled");
            Assert.That(createWall, Is.Not.Null);

            object wall = createWall.Invoke(null, new object[] { null });
            object gameState = Activator.CreateInstance(gameStateType, wall);
            AssignPlayersToSeats(gameState, seatNames);
            return gameState;
        }

        private static void AssignPlayersToSeats(object gameState, string[] seatNames)
        {
            for (int i = 0; i < seatNames.Length; i++)
            {
                Invoke(
                    gameState,
                    "AssignPlayerToSeat",
                    ParsePlayerId($"Player{i + 1}"),
                    ParseSeat(seatNames[i]));
            }

            Invoke(gameState, "RebuildActiveTurnSeatsFromSeatSlots");
        }

        private static object CreateTile(string code)
        {
            Type tileType = Type.GetType("MahjongPrototype.Domain.Tile, Assembly-CSharp", true);
            ConstructorInfo constructor = tileType.GetConstructor(new[] { typeof(string) });
            Assert.That(constructor, Is.Not.Null);
            return constructor.Invoke(new object[] { code });
        }

        private static object GetCurrentPlayerSeat(object gameState)
        {
            MethodInfo method = gameState.GetType().GetMethod("GetPlayerSeat");
            Assert.That(method, Is.Not.Null);
            return method.Invoke(gameState, new[] { GetProperty(gameState, "CurrentTurn") });
        }

        private static object GetListItem(object list, int index)
        {
            PropertyInfo itemProperty = list.GetType().GetProperty("Item");
            Assert.That(itemProperty, Is.Not.Null);
            return itemProperty.GetValue(list, new object[] { index });
        }

        private static object Invoke(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null);
            return method.Invoke(target, args);
        }

        private static object GetProperty(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null);
            return property.GetValue(target);
        }

        private static void SetProperty(object target, string propertyName, object value)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null);
            property.SetValue(target, value);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }

        private static void AssertOccupiedSeats(object gameState, params string[] expectedSeatNames)
        {
            object occupiedSeats = GetProperty(gameState, "OccupiedSeats");
            AssertSeatList(occupiedSeats, expectedSeatNames);
        }

        private static void AssertSeatList(object seats, params string[] expectedSeatNames)
        {
            PropertyInfo countProperty = seats.GetType().GetProperty("Count");
            PropertyInfo itemProperty = seats.GetType().GetProperty("Item");
            Assert.That(countProperty, Is.Not.Null);
            Assert.That(itemProperty, Is.Not.Null);

            Assert.That(countProperty.GetValue(seats), Is.EqualTo(expectedSeatNames.Length));
            for (int i = 0; i < expectedSeatNames.Length; i++)
            {
                object seat = itemProperty.GetValue(seats, new object[] { i });
                Assert.That(seat.ToString(), Is.EqualTo(expectedSeatNames[i]));
            }
        }

        private static void AssertSeatSlot(object seatSlots, int index, string wind, string playerId)
        {
            PropertyInfo itemProperty = seatSlots.GetType().GetProperty("Item");
            Assert.That(itemProperty, Is.Not.Null);

            object slot = itemProperty.GetValue(seatSlots, new object[] { index });
            Assert.That(GetProperty(slot, "Wind").ToString(), Is.EqualTo(wind));
            object actualPlayerId = GetProperty(slot, "PlayerId");
            Assert.That(actualPlayerId == null ? null : actualPlayerId.ToString(), Is.EqualTo(playerId));
            Assert.That(GetProperty(slot, "HasPlayer"), Is.EqualTo(playerId != null));
            Assert.That(GetProperty(slot, "IsEmpty"), Is.EqualTo(playerId == null));
            Assert.That(GetProperty(slot, "StateLabel"), Is.EqualTo(playerId ?? "Empty"));
        }

        private static IList CreateSeatList(params string[] seatNames)
        {
            Type seatIdType = GetSeatIdType();
            Type listType = typeof(System.Collections.Generic.List<>).MakeGenericType(seatIdType);
            IList list = (IList)Activator.CreateInstance(listType);

            for (int i = 0; i < seatNames.Length; i++)
                list.Add(ParseSeat(seatNames[i]));

            return list;
        }

        private static object ParseSeat(string seatName)
        {
            return Enum.Parse(GetSeatIdType(), seatName);
        }

        private static object ParsePlayerId(string playerId)
        {
            return Enum.Parse(Type.GetType(PlayerIdTypeName, true), playerId);
        }

        private static object ParseParticipantType(string participantType)
        {
            return Enum.Parse(Type.GetType(ParticipantTypeName, true), participantType);
        }

        private static Type GetSeatIdType()
        {
            return Type.GetType(SeatIdTypeName, true);
        }

        private static Type GetMahjongGameFlowType()
        {
            return Type.GetType(MahjongGameFlowTypeName, true);
        }

        private static object GetPlayerSeat(object gameState, object seat)
        {
            return Invoke(gameState, "GetPlayerSeat", seat);
        }

        private static void SetCpuDiscardDelay(object gameFlow, float delaySeconds)
        {
            object cpuTurnController = GetPrivateField(gameFlow, "cpuTurnController");
            Assert.That(cpuTurnController, Is.Not.Null);
            SetPrivateField(cpuTurnController, "cpuDiscardDelaySeconds", delaySeconds);
        }

        private static object GetPrivateField(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null);
            return field.GetValue(target);
        }

        private static void AddSingleArgumentEventHandler(
            object source,
            string eventName,
            Action<object> callback)
        {
            EventInfo eventInfo = source.GetType().GetEvent(eventName);
            Assert.That(eventInfo, Is.Not.Null);
            Type delegateType = eventInfo.EventHandlerType;
            MethodInfo invokeMethod = delegateType.GetMethod("Invoke");
            ParameterInfo[] parameters = invokeMethod.GetParameters();
            Assert.That(parameters.Length, Is.EqualTo(1));

            ParameterExpression valueParameter =
                Expression.Parameter(parameters[0].ParameterType, "value");
            InvocationExpression callbackInvocation = Expression.Invoke(
                Expression.Constant(callback),
                Expression.Convert(valueParameter, typeof(object)));
            Delegate handler = Expression.Lambda(
                delegateType,
                callbackInvocation,
                valueParameter).Compile();
            eventInfo.AddEventHandler(source, handler);
        }

        private static void AddTwoArgumentEventHandler(
            object source,
            string eventName,
            Action<object, int> callback)
        {
            EventInfo eventInfo = source.GetType().GetEvent(eventName);
            Assert.That(eventInfo, Is.Not.Null);
            Type delegateType = eventInfo.EventHandlerType;
            MethodInfo invokeMethod = delegateType.GetMethod("Invoke");
            ParameterInfo[] parameters = invokeMethod.GetParameters();
            Assert.That(parameters.Length, Is.EqualTo(2));

            ParameterExpression firstParameter =
                Expression.Parameter(parameters[0].ParameterType, "first");
            ParameterExpression secondParameter =
                Expression.Parameter(parameters[1].ParameterType, "second");
            InvocationExpression callbackInvocation = Expression.Invoke(
                Expression.Constant(callback),
                Expression.Convert(firstParameter, typeof(object)),
                secondParameter);
            Delegate handler = Expression.Lambda(
                delegateType,
                callbackInvocation,
                firstParameter,
                secondParameter).Compile();
            eventInfo.AddEventHandler(source, handler);
        }
    }
}
