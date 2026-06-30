using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MahjongPrototype.Tests
{
    public sealed class ReachGameFlowTests
    {
        private const string SeatIdTypeName = "MahjongPrototype.Domain.SeatId, Assembly-CSharp";
        private const string TileTypeName = "MahjongPrototype.Domain.Tile, Assembly-CSharp";
        private const string PlayerIdTypeName = "MahjongPrototype.Domain.PlayerId, Assembly-CSharp";
        private const string ParticipantTypeTypeName = "MahjongPrototype.Domain.ParticipantType, Assembly-CSharp";
        private const string MahjongGameFlowTypeName = "MahjongPrototype.MahjongGameFlow, Assembly-CSharp";
        private const string MahjongEventNotifierTypeName =
            "MahjongPrototype.Notifications.MahjongEventNotifier, Assembly-CSharp";

        [Test]
        public void DrawReachableHand_BeginsReachDecision()
        {
            GameObject gameObject = new GameObject("ReachDecisionAfterDrawTest");
            try
            {
                object gameFlow = CreateConfiguredGameFlow(gameObject);
                object gameState = DrawReachableHand(gameFlow);

                Assert.That(GetProperty(gameState, "IsWinDecisionPending"), Is.False);
                Assert.That(GetProperty(gameState, "IsReachDecisionPending"), Is.True);
                Assert.That(GetProperty(gameState, "TurnPhase").ToString(), Is.EqualTo("ReachDecision"));
                Assert.That(GetListCount(GetProperty(gameState, "ReachDiscardCandidates")), Is.GreaterThan(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void DrawWinningHand_PrioritizesWinDecisionOverReachDecision()
        {
            GameObject gameObject = new GameObject("ReachWinPriorityTest");
            try
            {
                object gameFlow = CreateConfiguredGameFlow(gameObject);
                Invoke(gameFlow, "StartNewRound");
                object gameState = GetProperty(gameFlow, "CurrentState");
                object playerSeat = GetPlayerSeat(gameState, "East");
                AddHandTiles(
                    playerSeat,
                    "1m", "2m", "3m",
                    "1p", "2p", "3p",
                    "1s", "2s", "3s",
                    "E", "E", "E",
                    "C");

                Invoke(gameFlow, "RequestForceDrawSkill", "C");
                Invoke(gameFlow, "RequestDraw");

                Assert.That(GetProperty(gameState, "IsWinDecisionPending"), Is.True);
                Assert.That(GetProperty(gameState, "IsReachDecisionPending"), Is.False);
                Assert.That(GetProperty(gameState, "TurnPhase").ToString(), Is.EqualTo("WinDecision"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void RequestDeclareReach_MovesToReachDiscardSelection()
        {
            GameObject gameObject = new GameObject("ReachDeclareRequestTest");
            try
            {
                object gameFlow = CreateConfiguredGameFlow(gameObject);
                object gameState = DrawReachableHand(gameFlow);

                Invoke(gameFlow, "RequestDeclareReach");

                Assert.That(GetProperty(gameState, "IsReachDecisionPending"), Is.False);
                Assert.That(GetProperty(gameState, "IsReachDiscardSelectionPending"), Is.True);
                Assert.That(GetProperty(gameState, "TurnPhase").ToString(), Is.EqualTo("ReachDiscardSelection"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void RequestCancelReachDiscardSelection_ReturnsToReachDecisionAndKeepsCandidates()
        {
            GameObject gameObject = new GameObject("ReachCancelDiscardSelectionTest");
            try
            {
                object gameFlow = CreateConfiguredGameFlow(gameObject);
                object gameState = DrawReachableHand(gameFlow);
                Invoke(gameFlow, "RequestDeclareReach");
                int candidateCountBefore = GetListCount(GetProperty(gameState, "ReachDiscardCandidates"));
                int discardCountBefore = GetListCount(GetProperty(gameState, "Discards"));

                Invoke(gameFlow, "RequestCancelReachDiscardSelection");

                object playerSeat = GetPlayerSeat(gameState, "East");
                Assert.That(GetProperty(gameState, "IsReachDecisionPending"), Is.True);
                Assert.That(GetProperty(gameState, "IsReachDiscardSelectionPending"), Is.False);
                Assert.That(GetProperty(gameState, "TurnPhase").ToString(), Is.EqualTo("ReachDecision"));
                Assert.That(
                    GetListCount(GetProperty(gameState, "ReachDiscardCandidates")),
                    Is.EqualTo(candidateCountBefore));
                Assert.That(GetProperty(playerSeat, "IsReachDeclared"), Is.False);
                Assert.That(GetListCount(GetProperty(gameState, "Discards")), Is.EqualTo(discardCountBefore));

                Invoke(gameFlow, "RequestDeclareReach");

                Assert.That(GetProperty(gameState, "IsReachDecisionPending"), Is.False);
                Assert.That(GetProperty(gameState, "IsReachDiscardSelectionPending"), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void RequestDeclineReach_ClearsReachDecision()
        {
            GameObject gameObject = new GameObject("ReachDeclineRequestTest");
            try
            {
                object gameFlow = CreateConfiguredGameFlow(gameObject);
                object gameState = DrawReachableHand(gameFlow);

                Invoke(gameFlow, "RequestDeclineReach");

                Assert.That(GetProperty(gameState, "IsReachDecisionPending"), Is.False);
                Assert.That(GetProperty(gameState, "IsReachDiscardSelectionPending"), Is.False);
                Assert.That(GetProperty(gameState, "TurnPhase").ToString(), Is.EqualTo("WaitingForDiscard"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ReachDiscardSelection_RejectsNonCandidateHandDiscard()
        {
            GameObject gameObject = new GameObject("ReachRejectNonCandidateTest");
            try
            {
                object gameFlow = CreateConfiguredGameFlow(gameObject);
                object gameState = DrawReachableHand(gameFlow);
                Invoke(gameFlow, "RequestDeclareReach");
                int discardCountBefore = GetListCount(GetProperty(gameState, "Discards"));

                Invoke(gameFlow, "RequestDiscard", 0);

                object playerSeat = GetPlayerSeat(gameState, "East");
                Assert.That(GetListCount(GetProperty(gameState, "Discards")), Is.EqualTo(discardCountBefore));
                Assert.That(GetProperty(playerSeat, "IsReachDeclared"), Is.False);
                Assert.That(GetProperty(gameState, "IsReachDiscardSelectionPending"), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ReachDiscardSelection_DeclaresReachAfterCandidateHandDiscard()
        {
            GameObject gameObject = new GameObject("ReachCandidateHandDiscardTest");
            try
            {
                object gameFlow = CreateConfiguredGameFlow(gameObject);
                object gameState = DrawReachableHand(gameFlow);
                Invoke(gameFlow, "RequestDeclareReach");

                Invoke(gameFlow, "RequestDiscard", 12);

                object playerSeat = GetPlayerSeat(gameState, "East");
                Assert.That(GetProperty(playerSeat, "IsReachDeclared"), Is.True);
                Assert.That(GetProperty(playerSeat, "ReachDeclaredTurnIndex"), Is.EqualTo(1));
                Assert.That(GetProperty(gameState, "IsReachDecisionPending"), Is.False);
                Assert.That(GetProperty(gameState, "IsReachDiscardSelectionPending"), Is.False);
                Assert.That(GetListCount(GetProperty(gameState, "Discards")), Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ReachDiscardSelection_DeclaresReachAfterDrawnTileDiscardCandidate()
        {
            GameObject gameObject = new GameObject("ReachCandidateDrawnDiscardTest");
            try
            {
                object gameFlow = CreateConfiguredGameFlow(gameObject);
                object gameState = DrawReachableHand(gameFlow);
                Invoke(gameFlow, "RequestDeclareReach");

                Invoke(gameFlow, "RequestDiscardDrawnTile");

                object playerSeat = GetPlayerSeat(gameState, "East");
                object firstDiscard = GetListItem(GetProperty(gameState, "Discards"), 0);
                Assert.That(GetProperty(playerSeat, "IsReachDeclared"), Is.True);
                Assert.That(GetProperty(firstDiscard, "Source").ToString(), Is.EqualTo("DrawnTile"));
                Assert.That(GetProperty(firstDiscard, "Tile").ToString(), Is.EqualTo("6m"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ReachDeclared_RejectsNormalHandDiscard()
        {
            GameObject gameObject = new GameObject("ReachHandLockTest");
            try
            {
                object gameFlow = CreateConfiguredGameFlow(gameObject);
                object gameState = DrawReachableHand(gameFlow);
                Invoke(gameFlow, "RequestDeclareReach");
                Invoke(gameFlow, "RequestDiscard", 12);
                object playerSeat = GetPlayerSeat(gameState, "East");
                Invoke(playerSeat, "SetDrawnTile", CreateTile("7m"));
                int discardCountBefore = GetListCount(GetProperty(gameState, "Discards"));

                Invoke(gameFlow, "RequestDiscard", 0);

                Assert.That(GetProperty(playerSeat, "IsReachDeclared"), Is.True);
                Assert.That(GetListCount(GetProperty(gameState, "Discards")), Is.EqualTo(discardCountBefore));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ReachDeclared_StartTurn_UsesCommonAutoDrawPolicy()
        {
            GameObject gameObject = new GameObject("ReachCommonAutoDrawPolicyTest");
            try
            {
                object gameFlow = CreateConfiguredGameFlow(gameObject, 2);
                object gameState = DrawReachableHand(gameFlow);
                SetSeatParticipantType(gameState, "West", "LocalHuman");
                Invoke(gameFlow, "RequestDeclareReach");
                Invoke(gameFlow, "RequestDiscard", 12);

                object policy = Invoke(gameFlow, "BuildTurnAutomationPolicy", ParseSeat("East"));
                Assert.That(GetProperty(policy, "IsCpu"), Is.False);
                Assert.That(GetProperty(policy, "AutoDrawAtTurnStart"), Is.True);
                Assert.That(GetProperty(policy, "AutoDiscardDrawnTileAfterDraw"), Is.True);
                Assert.That(GetProperty(policy, "UseCpuController"), Is.False);

                Invoke(gameFlow, "RequestForceDrawSkillForSeat", ParseSeat("East"), "9m");
                DrawAndDiscardDrawnTileForSeat(gameFlow, "West", "C");

                object lastDiscard = GetLastListItem(GetProperty(gameState, "Discards"));
                Assert.That(GetProperty(lastDiscard, "ActorSeat").ToString(), Is.EqualTo("East"));
                Assert.That(GetProperty(lastDiscard, "Source").ToString(), Is.EqualTo("DrawnTile"));
                Assert.That(GetProperty(lastDiscard, "Tile").ToString(), Is.EqualTo("9m"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void NormalLocalHuman_EnableAutoDrawFalse_DoesNotAutoDraw()
        {
            GameObject gameObject = new GameObject("NormalLocalHumanManualDrawWaitTest");
            try
            {
                object gameFlow = CreateConfiguredGameFlow(gameObject, 1, false);
                Invoke(gameFlow, "StartNewRound");
                object gameState = GetProperty(gameFlow, "CurrentState");
                object playerSeat = GetPlayerSeat(gameState, "East");

                Assert.That(GetProperty(gameState, "CurrentTurn").ToString(), Is.EqualTo("East"));
                Assert.That(GetProperty(playerSeat, "IsReachDeclared"), Is.False);
                Assert.That(GetProperty(playerSeat, "HasDrawnTile"), Is.False);
                Assert.That(GetProperty(gameState, "TurnPhase").ToString(), Is.EqualTo("WaitingForDraw"));
                Assert.That(GetListCount(GetProperty(gameState, "Discards")), Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void NormalLocalHuman_EnableAutoDrawTrue_AutoDrawsButDoesNotAutoDiscard()
        {
            GameObject gameObject = new GameObject("NormalLocalHumanAutoDrawOnlyTest");
            try
            {
                object gameFlow = CreateConfiguredGameFlow(gameObject, 1, true);
                Invoke(gameFlow, "StartNewRound");
                object gameState = GetProperty(gameFlow, "CurrentState");
                object playerSeat = GetPlayerSeat(gameState, "East");

                Assert.That(GetProperty(gameState, "CurrentTurn").ToString(), Is.EqualTo("East"));
                Assert.That(GetProperty(playerSeat, "IsReachDeclared"), Is.False);
                Assert.That(GetProperty(playerSeat, "HasDrawnTile"), Is.True);
                Assert.That(GetProperty(gameState, "TurnPhase").ToString(), Is.EqualTo("WaitingForDiscard"));
                Assert.That(GetListCount(GetProperty(gameState, "Discards")), Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void CpuSeat_TurnAutomationPolicy_UsesCpuController()
        {
            GameObject gameObject = new GameObject("CpuAutomationPolicyTest");
            try
            {
                object gameFlow = CreateConfiguredGameFlow(gameObject, 2, false);
                Invoke(gameFlow, "StartNewRound");

                object policy = Invoke(gameFlow, "BuildTurnAutomationPolicy", ParseSeat("West"));

                Assert.That(GetProperty(policy, "IsCpu"), Is.True);
                Assert.That(GetProperty(policy, "AutoDrawAtTurnStart"), Is.False);
                Assert.That(GetProperty(policy, "AutoDiscardDrawnTileAfterDraw"), Is.False);
                Assert.That(GetProperty(policy, "UseCpuController"), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ReachDeclared_AutoDrawsAndAutoDiscards()
        {
            GameObject gameObject = new GameObject("ReachTurnStartAutoDiscardTest");
            try
            {
                object gameFlow = CreateConfiguredGameFlow(gameObject, 2);
                object gameState = DrawReachableHand(gameFlow);
                SetSeatParticipantType(gameState, "West", "LocalHuman");
                Invoke(gameFlow, "RequestDeclareReach");
                Invoke(gameFlow, "RequestDiscard", 12);
                int discardCountBeforeWestTurnEnds = GetListCount(GetProperty(gameState, "Discards"));
                int westTurnIndex = (int)GetProperty(gameState, "TurnIndex");

                Assert.That(GetProperty(gameState, "CurrentTurn").ToString(), Is.EqualTo("West"));

                Invoke(gameFlow, "RequestForceDrawSkillForSeat", ParseSeat("East"), "9m");
                DrawAndDiscardDrawnTileForSeat(gameFlow, "West", "C");

                object eastPlayerSeat = GetPlayerSeat(gameState, "East");
                object lastDiscard = GetLastListItem(GetProperty(gameState, "Discards"));
                Assert.That(GetProperty(eastPlayerSeat, "HasDrawnTile"), Is.False);
                Assert.That(GetListCount(GetProperty(gameState, "Discards")), Is.EqualTo(discardCountBeforeWestTurnEnds + 2));
                Assert.That(GetProperty(lastDiscard, "ActorSeat").ToString(), Is.EqualTo("East"));
                Assert.That(GetProperty(lastDiscard, "Source").ToString(), Is.EqualTo("DrawnTile"));
                Assert.That(GetProperty(lastDiscard, "Tile").ToString(), Is.EqualTo("9m"));
                Assert.That(GetProperty(gameState, "CurrentTurn").ToString(), Is.EqualTo("West"));
                Assert.That((int)GetProperty(gameState, "TurnIndex"), Is.GreaterThan(westTurnIndex));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [UnityTest]
        public IEnumerator ReachDeclared_AutoDiscardDelay_HoldsDrawnTileBeforeDiscard()
        {
            GameObject gameObject = new GameObject("ReachAutoDiscardDelayTest");
            try
            {
                object gameFlow = CreateConfiguredGameFlow(gameObject, 2);
                SetPrivateField(gameFlow, "autoDiscardDrawnTileDelaySeconds", 0.05f);
                object gameState = DrawReachableHand(gameFlow);
                SetSeatParticipantType(gameState, "West", "LocalHuman");
                Invoke(gameFlow, "RequestDeclareReach");
                Invoke(gameFlow, "RequestDiscard", 12);
                int discardCountBeforeWestTurnEnds = GetListCount(GetProperty(gameState, "Discards"));

                Invoke(gameFlow, "RequestForceDrawSkillForSeat", ParseSeat("East"), "9m");
                DrawAndDiscardDrawnTileForSeat(gameFlow, "West", "C");

                object eastPlayerSeat = GetPlayerSeat(gameState, "East");
                Assert.That(GetProperty(gameState, "CurrentTurn").ToString(), Is.EqualTo("East"));
                Assert.That(GetProperty(eastPlayerSeat, "HasDrawnTile"), Is.True);
                Assert.That(GetProperty(eastPlayerSeat, "DrawnTile").ToString(), Is.EqualTo("9m"));
                Assert.That(GetListCount(GetProperty(gameState, "Discards")), Is.EqualTo(discardCountBeforeWestTurnEnds + 1));

                yield return new WaitForSeconds(0.08f);
                yield return null;

                object lastDiscard = GetLastListItem(GetProperty(gameState, "Discards"));
                Assert.That(GetProperty(eastPlayerSeat, "HasDrawnTile"), Is.False);
                Assert.That(GetListCount(GetProperty(gameState, "Discards")), Is.EqualTo(discardCountBeforeWestTurnEnds + 2));
                Assert.That(GetProperty(lastDiscard, "ActorSeat").ToString(), Is.EqualTo("East"));
                Assert.That(GetProperty(lastDiscard, "Source").ToString(), Is.EqualTo("DrawnTile"));
                Assert.That(GetProperty(lastDiscard, "Tile").ToString(), Is.EqualTo("9m"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ReachDeclared_DrawWinningTile_StopsAtTsumoDecision()
        {
            GameObject gameObject = new GameObject("ReachTurnStartTsumoDecisionTest");
            try
            {
                object gameFlow = CreateConfiguredGameFlow(gameObject, 2);
                object gameState = DrawReachableHand(gameFlow);
                SetSeatParticipantType(gameState, "West", "LocalHuman");
                Invoke(gameFlow, "RequestDeclareReach");
                Invoke(gameFlow, "RequestDiscard", 12);
                int discardCountBeforeWestTurnEnds = GetListCount(GetProperty(gameState, "Discards"));

                Invoke(gameFlow, "RequestForceDrawSkillForSeat", ParseSeat("East"), "6m");
                DrawAndDiscardDrawnTileForSeat(gameFlow, "West", "C");

                object eastPlayerSeat = GetPlayerSeat(gameState, "East");
                Assert.That(GetProperty(gameState, "IsWinDecisionPending"), Is.True);
                Assert.That(GetProperty(gameState, "WinDecisionType").ToString(), Is.EqualTo("Tsumo"));
                Assert.That(GetProperty(eastPlayerSeat, "HasDrawnTile"), Is.True);
                Assert.That(GetProperty(eastPlayerSeat, "DrawnTile").ToString(), Is.EqualTo("6m"));
                Assert.That(GetListCount(GetProperty(gameState, "Discards")), Is.EqualTo(discardCountBeforeWestTurnEnds + 1));
                Assert.That(GetProperty(gameState, "CurrentTurn").ToString(), Is.EqualTo("East"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ReachDeclared_TurnStartAutoDiscardAllowsRonDecision()
        {
            GameObject gameObject = new GameObject("ReachTurnStartAutoDiscardRonTest");
            try
            {
                object gameFlow = CreateConfiguredGameFlow(gameObject, 2);
                object gameState = DrawReachableHand(gameFlow);
                SetSeatParticipantType(gameState, "West", "LocalHuman");
                AddHandTiles(
                    GetPlayerSeat(gameState, "West"),
                    "1m", "2m", "3m",
                    "1p", "2p", "3p",
                    "1s", "2s", "3s",
                    "E", "E", "E",
                    "9m");
                Invoke(gameFlow, "RequestDeclareReach");
                Invoke(gameFlow, "RequestDiscard", 12);
                int eastTurnIndexBeforeAutoDiscard = (int)GetProperty(gameState, "TurnIndex") + 1;

                Invoke(gameFlow, "RequestForceDrawSkillForSeat", ParseSeat("East"), "9m");
                DrawAndDiscardDrawnTileForSeat(gameFlow, "West", "C");

                object lastDiscard = GetLastListItem(GetProperty(gameState, "Discards"));
                Assert.That(GetProperty(lastDiscard, "ActorSeat").ToString(), Is.EqualTo("East"));
                Assert.That(GetProperty(lastDiscard, "Source").ToString(), Is.EqualTo("DrawnTile"));
                Assert.That(GetProperty(lastDiscard, "Tile").ToString(), Is.EqualTo("9m"));
                Assert.That(GetProperty(gameState, "IsWinDecisionPending"), Is.True);
                Assert.That(GetProperty(gameState, "WinDecisionSeat").ToString(), Is.EqualTo("West"));
                Assert.That(GetProperty(gameState, "WinDecisionType").ToString(), Is.EqualTo("Ron"));
                Assert.That(GetProperty(gameState, "WinSourceSeat").ToString(), Is.EqualTo("East"));
                Assert.That(GetProperty(gameState, "CurrentTurn").ToString(), Is.EqualTo("East"));
                Assert.That(GetProperty(gameState, "TurnIndex"), Is.EqualTo(eastTurnIndexBeforeAutoDiscard));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ReachDeclared_DeclineTsumoWin_UsesAutoDiscardPolicy()
        {
            GameObject gameObject = new GameObject("ReachDeclineTsumoAutoDiscardTest");
            try
            {
                object gameFlow = CreateConfiguredGameFlow(gameObject, 2);
                object gameState = DrawReachableHand(gameFlow);
                SetSeatParticipantType(gameState, "West", "LocalHuman");
                Invoke(gameFlow, "RequestDeclareReach");
                Invoke(gameFlow, "RequestDiscard", 12);
                Invoke(gameFlow, "RequestForceDrawSkillForSeat", ParseSeat("East"), "6m");
                DrawAndDiscardDrawnTileForSeat(gameFlow, "West", "C");
                int discardCountBeforeDecline = GetListCount(GetProperty(gameState, "Discards"));

                Invoke(gameFlow, "RequestDeclineWin");

                object eastPlayerSeat = GetPlayerSeat(gameState, "East");
                object lastDiscard = GetLastListItem(GetProperty(gameState, "Discards"));
                Assert.That(GetProperty(gameState, "IsWinDecisionPending"), Is.False);
                Assert.That(GetProperty(eastPlayerSeat, "HasDrawnTile"), Is.False);
                Assert.That(GetListCount(GetProperty(gameState, "Discards")), Is.EqualTo(discardCountBeforeDecline + 1));
                Assert.That(GetProperty(lastDiscard, "ActorSeat").ToString(), Is.EqualTo("East"));
                Assert.That(GetProperty(lastDiscard, "Source").ToString(), Is.EqualTo("DrawnTile"));
                Assert.That(GetProperty(lastDiscard, "Tile").ToString(), Is.EqualTo("6m"));
                Assert.That(GetProperty(gameState, "CurrentTurn").ToString(), Is.EqualTo("West"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ReachDeclared_DrawNonWinningTile_AutoDiscardsDrawnTile()
        {
            GameObject gameObject = new GameObject("ReachAutoDiscardTest");
            try
            {
                object gameFlow = CreateConfiguredGameFlow(gameObject);
                object gameState = DrawReachableHandAndDeclareReach(gameFlow);
                object playerSeat = GetPlayerSeat(gameState, "East");
                int discardCountBefore = GetListCount(GetProperty(gameState, "Discards"));
                int turnIndexBefore = (int)GetProperty(gameState, "TurnIndex");

                Invoke(gameFlow, "RequestForceDrawSkill", "9m");
                Invoke(gameFlow, "RequestDraw");

                object lastDiscard = GetListItem(GetProperty(gameState, "Discards"), discardCountBefore);
                Assert.That(GetProperty(playerSeat, "IsReachDeclared"), Is.True);
                Assert.That(GetProperty(playerSeat, "HasDrawnTile"), Is.False);
                Assert.That(GetListCount(GetProperty(gameState, "Discards")), Is.EqualTo(discardCountBefore + 1));
                Assert.That(GetProperty(lastDiscard, "Source").ToString(), Is.EqualTo("DrawnTile"));
                Assert.That(GetProperty(lastDiscard, "Tile").ToString(), Is.EqualTo("9m"));
                Assert.That(GetProperty(gameState, "IsWinDecisionPending"), Is.False);
                Assert.That(GetProperty(gameState, "CurrentTurn").ToString(), Is.EqualTo("East"));
                Assert.That((int)GetProperty(gameState, "TurnIndex"), Is.GreaterThan(turnIndexBefore));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ReachDeclared_DrawWinningTile_DoesNotAutoDiscardAndShowsTsumoDecision()
        {
            GameObject gameObject = new GameObject("ReachTsumoPriorityTest");
            try
            {
                object gameFlow = CreateConfiguredGameFlow(gameObject);
                object gameState = DrawReachableHandAndDeclareReach(gameFlow);
                object playerSeat = GetPlayerSeat(gameState, "East");
                int discardCountBefore = GetListCount(GetProperty(gameState, "Discards"));

                Invoke(gameFlow, "RequestForceDrawSkill", "6m");
                Invoke(gameFlow, "RequestDraw");

                Assert.That(GetProperty(playerSeat, "IsReachDeclared"), Is.True);
                Assert.That(GetProperty(playerSeat, "HasDrawnTile"), Is.True);
                Assert.That(GetProperty(playerSeat, "DrawnTile").ToString(), Is.EqualTo("6m"));
                Assert.That(GetListCount(GetProperty(gameState, "Discards")), Is.EqualTo(discardCountBefore));
                Assert.That(GetProperty(gameState, "IsWinDecisionPending"), Is.True);
                Assert.That(GetProperty(gameState, "WinDecisionType").ToString(), Is.EqualTo("Tsumo"));
                Assert.That(GetProperty(gameState, "TurnPhase").ToString(), Is.EqualTo("WinDecision"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ReachDeclared_AutoDiscardAllowsRonDecision()
        {
            GameObject gameObject = new GameObject("ReachAutoDiscardRonTest");
            try
            {
                object gameFlow = CreateConfiguredGameFlow(gameObject);
                object gameState = DrawReachableHand(gameFlow);
                AddLocalHumanSeat(gameState, "Player2", "West");
                AddHandTiles(
                    GetPlayerSeat(gameState, "West"),
                    "1m", "2m", "3m",
                    "1p", "2p", "3p",
                    "1s", "2s", "3s",
                    "E", "E", "E",
                    "9m");

                Invoke(gameFlow, "RequestDeclareReach");
                Invoke(gameFlow, "RequestDiscard", 12);
                int discardCountBefore = GetListCount(GetProperty(gameState, "Discards"));
                int turnIndexBefore = (int)GetProperty(gameState, "TurnIndex");

                Invoke(gameFlow, "RequestForceDrawSkill", "9m");
                Invoke(gameFlow, "RequestDraw");

                object lastDiscard = GetListItem(GetProperty(gameState, "Discards"), discardCountBefore);
                Assert.That(GetProperty(lastDiscard, "Source").ToString(), Is.EqualTo("DrawnTile"));
                Assert.That(GetProperty(lastDiscard, "Tile").ToString(), Is.EqualTo("9m"));
                Assert.That(GetProperty(gameState, "IsWinDecisionPending"), Is.True);
                Assert.That(GetProperty(gameState, "WinDecisionSeat").ToString(), Is.EqualTo("West"));
                Assert.That(GetProperty(gameState, "WinDecisionType").ToString(), Is.EqualTo("Ron"));
                Assert.That(GetProperty(gameState, "WinSourceSeat").ToString(), Is.EqualTo("East"));
                Assert.That(GetProperty(gameState, "CurrentTurn").ToString(), Is.EqualTo("East"));
                Assert.That(GetProperty(gameState, "TurnIndex"), Is.EqualTo(turnIndexBefore));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ReachDiscardSelection_DoesNotAutoDiscardBeforeReachConfirmed()
        {
            GameObject gameObject = new GameObject("ReachSelectionNoAutoDiscardTest");
            try
            {
                object gameFlow = CreateConfiguredGameFlow(gameObject);
                object gameState = DrawReachableHand(gameFlow);
                object playerSeat = GetPlayerSeat(gameState, "East");
                Invoke(gameFlow, "RequestDeclareReach");
                int discardCountBefore = GetListCount(GetProperty(gameState, "Discards"));

                object shouldAutoDiscard = Invoke(gameFlow, "ShouldAutoDiscardDrawnTileAfterDraw", ParseSeat("East"));
                Invoke(gameFlow, "TryAutoDiscardDrawnTileAfterDraw", ParseSeat("East"));

                Assert.That(shouldAutoDiscard, Is.False);
                Assert.That(GetProperty(gameState, "IsReachDiscardSelectionPending"), Is.True);
                Assert.That(GetProperty(playerSeat, "IsReachDeclared"), Is.False);
                Assert.That(GetProperty(playerSeat, "HasDrawnTile"), Is.True);
                Assert.That(GetListCount(GetProperty(gameState, "Discards")), Is.EqualTo(discardCountBefore));

                Invoke(gameFlow, "RequestCancelReachDiscardSelection");

                Assert.That(GetProperty(gameState, "IsReachDecisionPending"), Is.True);
                Assert.That(GetProperty(gameState, "IsReachDiscardSelectionPending"), Is.False);
                Assert.That(GetProperty(playerSeat, "IsReachDeclared"), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private static object DrawReachableHandAndDeclareReach(object gameFlow)
        {
            object gameState = DrawReachableHand(gameFlow);
            Invoke(gameFlow, "RequestDeclareReach");
            Invoke(gameFlow, "RequestDiscard", 12);
            return gameState;
        }

        private static object DrawReachableHand(object gameFlow)
        {
            Invoke(gameFlow, "StartNewRound");
            object gameState = GetProperty(gameFlow, "CurrentState");
            object playerSeat = GetPlayerSeat(gameState, "East");
            AddHandTiles(
                playerSeat,
                "1m", "2m", "3m",
                "2p", "3p", "4p",
                "7s", "8s", "9s",
                "E", "E", "E",
                "5m");

            Invoke(gameFlow, "RequestForceDrawSkill", "6m");
            Invoke(gameFlow, "RequestDraw");
            return gameState;
        }

        private static object CreateConfiguredGameFlow(GameObject gameObject)
        {
            return CreateConfiguredGameFlow(gameObject, 1, false);
        }

        private static object CreateConfiguredGameFlow(GameObject gameObject, int participantCount)
        {
            return CreateConfiguredGameFlow(gameObject, participantCount, false);
        }

        private static object CreateConfiguredGameFlow(GameObject gameObject, int participantCount, bool enableAutoDraw)
        {
            gameObject.AddComponent(Type.GetType(MahjongEventNotifierTypeName, true));
            object gameFlow = gameObject.AddComponent(Type.GetType(MahjongGameFlowTypeName, true));
            SetPrivateField(gameFlow, "logWarnings", false);
            SetPrivateField(gameFlow, "participantCount", participantCount);
            SetPrivateField(gameFlow, "initialHandTileCount", 0);
            SetPrivateField(gameFlow, "autoStart", false);
            SetPrivateField(gameFlow, "useFixedRandomSeed", true);
            SetPrivateField(gameFlow, "fixedRandomSeed", 12345);
            SetPrivateField(gameFlow, "enableAutoDraw", enableAutoDraw);
            SetPrivateField(gameFlow, "autoDiscardDrawnTileDelaySeconds", 0f);
            SetPrivateField(gameFlow, "randomizeSelfSeat", false);
            SetPrivateField(gameFlow, "fixedSelfSeat", ParseSeat("East"));
            return gameFlow;
        }

        private static void AddHandTiles(object playerSeat, params string[] tileCodes)
        {
            object hand = GetProperty(playerSeat, "Hand");
            for (int i = 0; i < tileCodes.Length; i++)
                Invoke(hand, "Add", CreateTile(tileCodes[i]));
        }

        private static object GetPlayerSeat(object gameState, string seatName)
        {
            return Invoke(gameState, "GetPlayerSeat", ParseSeat(seatName));
        }

        private static object CreateTile(string code)
        {
            Type tileType = Type.GetType(TileTypeName, true);
            ConstructorInfo constructor = tileType.GetConstructor(new[] { typeof(string) });
            Assert.That(constructor, Is.Not.Null);
            return constructor.Invoke(new object[] { code });
        }

        private static object ParseSeat(string seatName)
        {
            return Enum.Parse(Type.GetType(SeatIdTypeName, true), seatName);
        }

        private static object ParsePlayerId(string playerName)
        {
            return Enum.Parse(Type.GetType(PlayerIdTypeName, true), playerName);
        }

        private static object ParseParticipantType(string participantTypeName)
        {
            return Enum.Parse(Type.GetType(ParticipantTypeTypeName, true), participantTypeName);
        }

        private static void AddLocalHumanSeat(object gameState, string playerName, string seatName)
        {
            object seat = ParseSeat(seatName);
            Invoke(gameState, "AssignPlayerToSeat", ParsePlayerId(playerName), seat);
            Invoke(gameState, "SetParticipantType", seat, ParseParticipantType("LocalHuman"));
        }

        private static void SetSeatParticipantType(object gameState, string seatName, string participantTypeName)
        {
            Invoke(
                gameState,
                "SetParticipantType",
                ParseSeat(seatName),
                ParseParticipantType(participantTypeName));
        }

        private static void DrawAndDiscardDrawnTileForSeat(object gameFlow, string seatName, string tileCode)
        {
            object seat = ParseSeat(seatName);
            Invoke(gameFlow, "RequestForceDrawSkillForSeat", seat, tileCode);
            Assert.That(Invoke(gameFlow, "TryRequestDrawForSeat", seat), Is.True);
            Assert.That(Invoke(gameFlow, "TryRequestDiscardDrawnTileForSeat", seat), Is.True);
        }

        private static int GetListCount(object list)
        {
            return (int)GetProperty(list, "Count");
        }

        private static object GetListItem(object list, int index)
        {
            PropertyInfo itemProperty = list.GetType().GetProperty("Item");
            Assert.That(itemProperty, Is.Not.Null);
            return itemProperty.GetValue(list, new object[] { index });
        }

        private static object GetLastListItem(object list)
        {
            return GetListItem(list, GetListCount(list) - 1);
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

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }
    }
}
