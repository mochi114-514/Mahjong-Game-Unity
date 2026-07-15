using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class KanGameFlowTests
    {
        [Test]
        public void DaiminkanCandidate_AppearsBesidePonAndAutoDrawsRinshanAfterReactionWindowCloses()
        {
            using (MahjongGameFlowTestSession session = CreateDaiminkanSession())
            {
                object reactionWindow = session.Query.CurrentReactionWindow;
                int windowId = session.Query.ReactionWindowId;
                int sourceDiscardId = session.Query.LastDiscardId;
                int turnIndex = session.Query.TurnIndex;
                int liveWallBefore = session.Query.WallCount;
                int rinshanBefore = RemainingRinshanTileCount(session);
                object sourcePlayerSeat = session.Query.GetPlayerSeat("West");
                EventSequenceRecorder events = new EventSequenceRecorder(
                    session.EventNotifier,
                    "ReactionWindowClosed",
                    "TileDrawn");
                session.Reflection.Invoke(sourcePlayerSeat, "DeclareReach", turnIndex);

                Assert.That(GetCandidateKinds(session), Does.Contain("Pon"));
                Assert.That(GetCandidateKinds(session), Does.Contain("Daiminkan"));
                Assert.That(
                    (bool)session.Reflection.GetProperty(
                        sourcePlayerSeat,
                        "IsIppatsuEligible"),
                    Is.True);
                try
                {
                    Assert.That(
                        session.Commands.TryRequestDeclareDaiminkanForSeat("East", windowId),
                        Is.True);
                }
                finally
                {
                    events.Dispose();
                }

                Assert.That(session.Query.CurrentTurnName, Is.EqualTo("East"));
                Assert.That(session.Query.TurnIndex, Is.EqualTo(turnIndex + 1));
                Assert.That(session.Query.IsReactionWindowPending, Is.False);
                Assert.That(session.Query.TurnPhaseName, Is.EqualTo("WaitingForDiscard"));
                Assert.That(session.Query.HandCount("East"), Is.EqualTo(10));
                Assert.That(session.Query.HasDrawnTile("East"), Is.True);
                Assert.That(session.Query.MeldCount("East"), Is.EqualTo(1));
                Assert.That(session.Query.IsClosed("East"), Is.False);
                Assert.That(session.Query.TryGetDiscardClaim(sourceDiscardId, out _), Is.True);
                Assert.That(session.Query.WallCount, Is.EqualTo(liveWallBefore - 1));
                Assert.That(RemainingRinshanTileCount(session), Is.EqualTo(rinshanBefore - 1));
                Assert.That(HasCallOccurred(session), Is.True);
                Assert.That(
                    (bool)session.Reflection.GetProperty(
                        sourcePlayerSeat,
                        "IsIppatsuEligible"),
                    Is.False);

                object meld = session.Query.MeldAt("East", 0);
                Assert.That(PropertyText(session, meld, "Type"), Is.EqualTo("Daiminkan"));
                Assert.That(
                    session.Collections.Count(session.Reflection.GetProperty(meld, "PhysicalTiles")),
                    Is.EqualTo(4));

                Assert.That(
                    session.Commands.TryRequestDeclareDaiminkanForSeat("East", windowId),
                    Is.False);
                Assert.That(session.Query.HandCount("East"), Is.EqualTo(10));
                Assert.That(session.Query.MeldCount("East"), Is.EqualTo(1));
                Assert.That(
                    GetCandidateResponseStates(session, reactionWindow),
                    Does.Contain("Declared"));
                Assert.That(events.Names, Is.EqualTo(new[] { "ReactionWindowClosed", "TileDrawn" }));
                Assert.That(
                    session.Reflection.GetProperty(session.CurrentState, "LastTurnDraw"),
                    Is.Null,
                    "A rinshan draw must not be recorded as a normal last-live-wall draw.");
                Assert.That(session.Commands.TryRequestDrawForSeat("East"), Is.False);
                Assert.That(session.Query.WallCount, Is.EqualTo(liveWallBefore - 1));
                Assert.That(RemainingRinshanTileCount(session), Is.EqualTo(rinshanBefore - 1));
                Assert.That(session.Commands.TryRequestDiscardDrawnTileForSeat("East"), Is.True);
            }
        }

        [Test]
        public void Daiminkan_AutoRinshanAllowsHandDiscardWithoutExternalDrawRequest()
        {
            using (MahjongGameFlowTestSession session = CreateDaiminkanSession())
            {
                Assert.That(
                    session.Commands.TryRequestDeclareDaiminkanForSeat(
                        "East",
                        session.Query.ReactionWindowId),
                    Is.True);
                int discardCountBefore = session.Query.DiscardCount;

                session.Commands.RequestDiscard(0);

                Assert.That(session.Query.DiscardCount, Is.EqualTo(discardCountBefore + 1));
            }
        }

        [Test]
        public void PendingRon_BlocksDaiminkanUntilRonIsResolved()
        {
            using (MahjongGameFlowTestSession session = CreateSession(2))
            {
                session.Commands.StartNewRound();
                session.DataFactory.AddHandTiles(
                    session.Query.GetPlayerSeat("East"),
                    "5m", "5m", "5m", "3m", "4m", "6m", "7m",
                    "3p", "4p", "5p",
                    "3s", "4s", "5s");
                DiscardFromWest(session, "5m");

                Assert.That(GetCandidateKinds(session), Does.Contain("Ron"));
                Assert.That(GetCandidateKinds(session), Does.Contain("Daiminkan"));
                Assert.That(
                    session.Commands.TryRequestDeclareDaiminkanForSeat(
                        "East",
                        session.Query.ReactionWindowId),
                    Is.False);
                Assert.That(session.Query.HandCount("East"), Is.EqualTo(13));
                Assert.That(session.Query.MeldCount("East"), Is.EqualTo(0));
                Assert.That(session.Query.IsReactionWindowPending, Is.True);
            }
        }

        [Test]
        public void Daiminkan_RemainsAheadOfChiAfterSameTierPonIsDeclined()
        {
            using (MahjongGameFlowTestSession session = CreateSession(2))
            {
                session.Commands.StartNewRound();
                session.DataFactory.AddHandTiles(
                    session.Query.GetPlayerSeat("East"),
                    "5m", "5m", "5m", "3m", "4m",
                    "1p", "4p", "7p", "9p",
                    "1s", "4s", "7s", "9s");
                DiscardFromWest(session, "5m");
                int windowId = session.Query.ReactionWindowId;

                Assert.That(GetCandidateKinds(session), Does.Contain("Pon"));
                Assert.That(GetCandidateKinds(session), Does.Contain("Daiminkan"));
                Assert.That(GetCandidateKinds(session), Does.Contain("Chi"));
                Assert.That(
                    session.Commands.TryRequestDeclinePonForSeat("East", windowId),
                    Is.True);

                object pendingCandidate = session.Reflection.GetProperty(
                    session.Query.CurrentReactionWindow,
                    "PendingCandidate");
                Assert.That(
                    PropertyText(session, pendingCandidate, "Kind"),
                    Is.EqualTo("Daiminkan"));
                Assert.That(
                    session.Commands.TryRequestDeclareDaiminkanForSeat("East", windowId),
                    Is.True);
            }
        }

        [Test]
        public void DaiminkanCommit_WhenRinshanBecomesUnavailableLeavesCallStateUntouched()
        {
            using (MahjongGameFlowTestSession session = CreateDaiminkanSession())
            {
                int windowId = session.Query.ReactionWindowId;
                int sourceDiscardId = session.Query.LastDiscardId;
                object wall = session.Reflection.GetProperty(session.CurrentState, "Wall");
                ClearLiveWall(session, wall);

                Assert.That(
                    session.Commands.TryRequestDeclareDaiminkanForSeat("East", windowId),
                    Is.False);
                Assert.That(session.Query.WallCount, Is.EqualTo(0));
                Assert.That(session.Query.HandCount("East"), Is.EqualTo(13));
                Assert.That(session.Query.MeldCount("East"), Is.EqualTo(0));
                Assert.That(session.Query.TryGetDiscardClaim(sourceDiscardId, out _), Is.False);
                Assert.That(session.Query.IsReactionWindowPending, Is.True);
                Assert.That(HasCallOccurred(session), Is.False);
            }
        }

        [Test]
        public void DaiminkanCandidate_DoesNotAppearAfterAllFourRinshanTilesAreUsed()
        {
            using (MahjongGameFlowTestSession session = CreateSession(2))
            {
                session.Commands.StartNewRound();
                session.DataFactory.AddHandTiles(
                    session.Query.GetPlayerSeat("East"),
                    "P", "P", "P",
                    "1m", "2m", "4m", "7m",
                    "1p", "4p", "7p",
                    "1s", "4s", "7s");
                object wall = session.Reflection.GetProperty(session.CurrentState, "Wall");
                for (int i = 0; i < 4; i++)
                {
                    object[] args = { null };
                    Assert.That(
                        (bool)session.Reflection.Invoke(wall, "TryTakeRinshan", args),
                        Is.True);
                }

                DiscardFromWest(session, "P");

                Assert.That(GetCandidateKinds(session), Does.Contain("Pon"));
                Assert.That(GetCandidateKinds(session), Does.Not.Contain("Daiminkan"));
            }
        }

        [Test]
        public void Ankan_UsesThreeHandTilesAndDrawnFourthThenAutoDrawsRinshan()
        {
            using (MahjongGameFlowTestSession session = CreateSession(2))
            {
                session.Commands.StartNewRound();
                object otherPlayerSeat = session.Query.GetPlayerSeat("West");
                session.Reflection.Invoke(
                    otherPlayerSeat,
                    "DeclareReach",
                    session.Query.TurnIndex);
                session.DataFactory.AddHandTiles(
                    session.Query.GetPlayerSeat("East"),
                    "P", "P", "P",
                    "1m", "3m", "7m", "9m",
                    "1p", "4p", "8p",
                    "2s", "6s", "9s");
                session.DataFactory.SetDrawnTile(session.CurrentState, "East", "P");
                int liveWallBefore = session.Query.WallCount;
                int rinshanBefore = RemainingRinshanTileCount(session);

                Assert.That(AnkanCandidateCodes(session), Is.EqualTo(new[] { "P" }));
                Assert.That(session.Commands.TryRequestDeclareAnkanForSeat("East", "P"), Is.True);

                Assert.That(session.Query.HandCount("East"), Is.EqualTo(10));
                Assert.That(session.Query.HasDrawnTile("East"), Is.True);
                Assert.That(session.Query.MeldCount("East"), Is.EqualTo(1));
                Assert.That(session.Query.IsClosed("East"), Is.True);
                Assert.That(
                    session.Collections.Count(session.Reflection.GetProperty(
                        session.CurrentState,
                        "DiscardClaims")),
                    Is.EqualTo(0));
                Assert.That(session.Query.CurrentTurnName, Is.EqualTo("East"));
                Assert.That(session.Query.TurnPhaseName, Is.EqualTo("WaitingForDiscard"));
                Assert.That(session.Query.WallCount, Is.EqualTo(liveWallBefore - 1));
                Assert.That(RemainingRinshanTileCount(session), Is.EqualTo(rinshanBefore - 1));
                Assert.That(HasCallOccurred(session), Is.True);
                Assert.That(
                    (bool)session.Reflection.GetProperty(
                        otherPlayerSeat,
                        "IsIppatsuEligible"),
                    Is.False);
                Assert.That(
                    session.Commands.TryRequestDeclareAnkanForSeat("East", "P"),
                    Is.False);
                Assert.That(session.Query.HandCount("East"), Is.EqualTo(10));
                Assert.That(session.Query.MeldCount("East"), Is.EqualTo(1));

                Assert.That(session.Commands.TryRequestDrawForSeat("East"), Is.False);
                Assert.That(session.Query.WallCount, Is.EqualTo(liveWallBefore - 1));
                Assert.That(session.Commands.TryRequestDiscardDrawnTileForSeat("East"), Is.True);
            }
        }

        [Test]
        public void Ankan_WithFourHandTilesMovesUnrelatedDrawnTileIntoHand()
        {
            using (MahjongGameFlowTestSession session = CreateSession(1))
            {
                session.Commands.StartNewRound();
                session.DataFactory.AddHandTiles(
                    session.Query.GetPlayerSeat("East"),
                    "P", "P", "P", "P",
                    "3m", "7m", "9m",
                    "1p", "4p", "8p",
                    "2s", "6s", "9s");
                session.DataFactory.SetDrawnTile(session.CurrentState, "East", "1m");

                Assert.That(session.Commands.TryRequestDeclareAnkanForSeat("East", "P"), Is.True);

                Assert.That(session.Query.HandCount("East"), Is.EqualTo(10));
                Assert.That(CountHandTile(session, "East", "P"), Is.EqualTo(0));
                Assert.That(CountHandTile(session, "East", "1m"), Is.EqualTo(1));
                Assert.That(session.Query.HasDrawnTile("East"), Is.True);
                Assert.That(session.Query.TurnPhaseName, Is.EqualTo("WaitingForDiscard"));
                int discardCountBefore = session.Query.DiscardCount;

                session.Commands.RequestDiscard(0);

                Assert.That(session.Query.DiscardCount, Is.EqualTo(discardCountBefore + 1));
            }
        }

        [Test]
        public void Ankan_MultipleCandidatesAreSelectableAndReachAllowsWaitPreservingKan()
        {
            using (MahjongGameFlowTestSession session = CreateSession(1))
            {
                session.Commands.StartNewRound();
                session.DataFactory.AddHandTiles(
                    session.Query.GetPlayerSeat("East"),
                    "P", "P", "P", "P",
                    "C", "C", "C", "C",
                    "1m", "3m", "7p", "2s", "9s");
                session.DataFactory.SetDrawnTile(session.CurrentState, "East", "4m");

                Assert.That(AnkanCandidateCodes(session), Is.EquivalentTo(new[] { "P", "C" }));
                Assert.That(session.Commands.TryRequestDeclareAnkanForSeat("East", "C"), Is.True);
                Assert.That(CountHandTile(session, "East", "P"), Is.EqualTo(4));
                Assert.That(CountHandTile(session, "East", "C"), Is.EqualTo(0));
            }

            using (MahjongGameFlowTestSession session = CreateSession(1))
            {
                session.Commands.StartNewRound();
                object playerSeat = session.Query.GetPlayerSeat("East");
                session.DataFactory.AddHandTiles(
                    playerSeat,
                    "P", "P", "P",
                    "1m", "3m", "7m", "9m",
                    "1p", "4p", "8p",
                    "2s", "6s", "9s");
                session.DataFactory.SetDrawnTile(session.CurrentState, "East", "P");
                session.Reflection.Invoke(playerSeat, "DeclareReach", session.Query.TurnIndex);
                int liveWallBefore = session.Query.WallCount;

                Assert.That(AnkanCandidateCodes(session), Is.EqualTo(new[] { "P" }));
                Assert.That(session.Commands.TryRequestDeclareAnkanForSeat("East", "P"), Is.True);
                Assert.That(session.Query.HandCount("East"), Is.EqualTo(10));
                Assert.That(session.Query.HasDrawnTile("East"), Is.True);
                Assert.That(session.Query.MeldCount("East"), Is.EqualTo(1));
                Assert.That(
                    (bool)session.Reflection.GetProperty(playerSeat, "IsReachDeclared"),
                    Is.True);
                Assert.That(
                    (bool)session.Reflection.GetProperty(playerSeat, "IsIppatsuEligible"),
                    Is.False);
                Assert.That(session.Query.WallCount, Is.EqualTo(liveWallBefore - 1));
            }
        }

        [Test]
        public void Ankan_WhenRinshanUnavailableLeavesHandDrawnTileMeldAndWallUntouched()
        {
            using (MahjongGameFlowTestSession session = CreateSession(1))
            {
                session.Commands.StartNewRound();
                session.DataFactory.AddHandTiles(
                    session.Query.GetPlayerSeat("East"),
                    "P", "P", "P",
                    "1m", "3m", "7m", "9m",
                    "1p", "4p", "8p",
                    "2s", "6s", "9s");
                session.DataFactory.SetDrawnTile(session.CurrentState, "East", "P");
                ClearLiveWall(
                    session,
                    session.Reflection.GetProperty(session.CurrentState, "Wall"));

                Assert.That(AnkanCandidateCodes(session), Is.Empty);
                Assert.That(session.Commands.TryRequestDeclareAnkanForSeat("East", "P"), Is.False);
                Assert.That(session.Query.HandCount("East"), Is.EqualTo(13));
                Assert.That(session.Query.HasDrawnTile("East"), Is.True);
                Assert.That(session.Query.MeldCount("East"), Is.EqualTo(0));
                Assert.That(session.Query.WallCount, Is.EqualTo(0));
                Assert.That(HasCallOccurred(session), Is.False);
            }
        }

        [Test]
        public void ReachAnkanDecision_BlocksDiscardUntilDeclinedThenTsumogirisOnce()
        {
            using (MahjongGameFlowTestSession session = CreateSession(1))
            {
                session.Commands.StartNewRound();
                object east = session.Query.GetPlayerSeat("East");
                session.DataFactory.AddHandTiles(
                    east,
                    "P", "P", "P",
                    "1m", "2m", "3m", "1p", "2p", "3p",
                    "1s", "2s", "3s", "C");
                session.DataFactory.SetDrawnTile(session.CurrentState, "East", "P");
                session.Reflection.Invoke(east, "DeclareReach", session.Query.TurnIndex);
                int discardCountBefore = session.Query.DiscardCount;

                session.Commands.ResolveAfterDraw("East");

                Assert.That(session.Query.TurnPhaseName, Is.EqualTo("SelfKanDecision"));
                Assert.That(session.Query.HasDrawnTile("East"), Is.True);
                Assert.That(session.Commands.TryRequestDiscardDrawnTileForSeat("East"), Is.False);
                Assert.That(
                    session.Commands.TryRequestDeclineSelfKanForSeat("East"),
                    Is.True);
                Assert.That(session.Query.DiscardCount, Is.EqualTo(discardCountBefore + 1));
                Assert.That(session.Query.HasDrawnTile("East"), Is.False);
                Assert.That(
                    session.Commands.TryRequestDeclineSelfKanForSeat("East"),
                    Is.False);
            }
        }

        [Test]
        public void Ankan_AutoRinshanRunsWinEvaluationBeforeDiscard()
        {
            using (MahjongGameFlowTestSession session = CreateSession(
                1,
                useRinshanOnlyCatalog: true))
            {
                session.Commands.StartNewRound();
                object wall = session.Reflection.GetProperty(session.CurrentState, "Wall");
                object rinshanTiles = session.Reflection.Invoke(wall, "GetRinshanSnapshot");
                string winningTileCode = session.Reflection.GetProperty(
                    session.Collections.Item(rinshanTiles, 0),
                    "Code").ToString();
                string ankanTileCode = winningTileCode == "P" ? "C" : "P";
                List<string> handTiles = new List<string>
                {
                    ankanTileCode,
                    ankanTileCode,
                    ankanTileCode,
                    "1m", "2m", "3m",
                    "1p", "2p", "3p",
                    "1s", "2s", "3s",
                    winningTileCode
                };
                session.DataFactory.AddHandTiles(
                    session.Query.GetPlayerSeat("East"),
                    handTiles.ToArray());
                session.DataFactory.SetDrawnTile(
                    session.CurrentState,
                    "East",
                    ankanTileCode);
                ReduceLiveWallTo(session, wall, 1);

                Assert.That(
                    session.Commands.TryRequestDeclareAnkanForSeat("East", ankanTileCode),
                    Is.True);

                Assert.That(session.Query.TurnPhaseName, Is.EqualTo("WinDecision"));
                Assert.That(session.Query.WallCount, Is.EqualTo(0));
                Assert.That(session.Query.HasDrawnTile("East"), Is.True);
                Assert.That(
                    session.Reflection.GetProperty(session.CurrentState, "LastTurnDraw"),
                    Is.Null);
                Assert.That(
                    session.Reflection.GetProperty(session.CurrentState, "WinningTile").ToString(),
                    Is.EqualTo(winningTileCode));
                Assert.That(
                    EvaluationContainsYaku(
                        session,
                        session.Query.PendingWinDeclarationEvaluation,
                        "RinshanKaihou"),
                    Is.True);
                Assert.That(session.Commands.TryRequestDeclareWinForSeat("East"), Is.True);
                Assert.That(
                    RoundResultContainsYaku(session, "RinshanKaihou"),
                    Is.True);
            }
        }

        [Test]
        public void Kakan_UpgradesItsSourcePonAndAutoDrawsRinshanWithoutAddingADiscard()
        {
            using (MahjongGameFlowTestSession session = CreateSession(2))
            {
                session.Commands.StartNewRound();
                session.DataFactory.AddHandTiles(
                    session.Query.GetPlayerSeat("East"),
                    "P", "P", "1m", "2m", "4m", "7m",
                    "1p", "4p", "7p", "1s", "4s", "7s", "9s");
                DiscardFromWest(session, "P");
                int sourceDiscardId = session.Query.LastDiscardId;
                Assert.That(
                    session.Commands.TryRequestDeclarePonForSeat(
                        "East",
                        session.Query.ReactionWindowId),
                    Is.True);

                session.DataFactory.AddHandTiles(session.Query.GetPlayerSeat("East"), "P");
                session.DataFactory.SetDrawnTile(session.CurrentState, "East", "1m");
                session.Reflection.Invoke(session.CurrentState, "EnterWaitingForDiscard");
                int discardCountBefore = session.Query.DiscardCount;
                int liveWallBefore = session.Query.WallCount;
                int rinshanBefore = RemainingRinshanTileCount(session);

                Assert.That(
                    session.Commands.TryRequestDeclareKakanForSeat("East", "P", 0),
                    Is.True);

                object meld = session.Query.MeldAt("East", 0);
                Assert.That(session.Query.MeldCount("East"), Is.EqualTo(1));
                Assert.That(PropertyText(session, meld, "Type"), Is.EqualTo("Kakan"));
                Assert.That(
                    session.Collections.Count(session.Reflection.GetProperty(meld, "PhysicalTiles")),
                    Is.EqualTo(4));
                Assert.That(session.Query.DiscardCount, Is.EqualTo(discardCountBefore));
                Assert.That(session.Query.TryGetDiscardClaim(sourceDiscardId, out object claim), Is.True);
                Assert.That(
                    PropertyText(session, session.Reflection.GetProperty(claim, "Meld"), "Type"),
                    Is.EqualTo("Kakan"));
                Assert.That(session.Query.CurrentTurnName, Is.EqualTo("East"));
                Assert.That(session.Query.TurnPhaseName, Is.EqualTo("WaitingForDiscard"));
                Assert.That(session.Query.HasDrawnTile("East"), Is.True);
                Assert.That(CountHandTile(session, "East", "1m"), Is.EqualTo(2));
                Assert.That(session.Query.WallCount, Is.EqualTo(liveWallBefore - 1));
                Assert.That(RemainingRinshanTileCount(session), Is.EqualTo(rinshanBefore - 1));
                Assert.That(
                    session.Commands.TryRequestDeclareKakanForSeat("East", "P", 0),
                    Is.False);
            }
        }

        [Test]
        public void ChankanRon_LeavesPendingKakanAsPonAndCarriesChankanIntoRoundResult()
        {
            using (MahjongGameFlowTestSession session = CreateSession(
                3,
                useChankanCatalog: true))
            {
                session.Commands.StartNewRound();
                session.DataFactory.SetParticipantType(
                    session.CurrentState,
                    "South",
                    "LocalHuman");
                session.DataFactory.AddHandTiles(
                    session.Query.GetPlayerSeat("East"),
                    "P", "P", "1m", "2m", "4m", "7m",
                    "1p", "4p", "7p", "1s", "4s", "7s", "9s");
                DiscardFromWest(session, "P");
                Assert.That(
                    session.Commands.TryRequestDeclarePonForSeat(
                        "East",
                        session.Query.ReactionWindowId),
                    Is.True);

                object south = session.Query.GetPlayerSeat("South");
                session.DataFactory.AddHandTiles(
                    south,
                    "1m", "2m", "3m", "1p", "2p", "3p",
                    "1s", "2s", "3s", "E", "E", "E", "P");
                session.Reflection.Invoke(south, "DeclareReach", session.Query.TurnIndex);
                session.DataFactory.SetDrawnTile(session.CurrentState, "East", "P");
                session.Reflection.Invoke(session.CurrentState, "EnterWaitingForDiscard");
                int wallBefore = session.Query.WallCount;
                int discardCountBefore = session.Query.DiscardCount;

                Assert.That(
                    session.Commands.TryRequestDeclareKakanForSeat("East", "P", 0),
                    Is.True);
                Assert.That(session.Query.IsReactionWindowPending, Is.True);
                Assert.That(GetCandidateKinds(session), Is.EqualTo(new[] { "Ron" }));
                Assert.That(
                    session.Commands.TryRequestDeclareRonForSeat(
                        "South",
                        session.Query.ReactionWindowId),
                    Is.True);

                Assert.That(PropertyText(session, session.Query.MeldAt("East", 0), "Type"),
                    Is.EqualTo("Pon"));
                Assert.That(session.Query.DiscardCount, Is.EqualTo(discardCountBefore));
                Assert.That(session.Query.WallCount, Is.EqualTo(wallBefore));
                Assert.That(
                    session.Reflection.GetProperty(session.Query.CurrentRoundResult, "WinningTile").ToString(),
                    Is.EqualTo("P"));
                Assert.That(
                    session.Reflection.GetProperty(session.Query.CurrentRoundResult, "SourceSeat").ToString(),
                    Is.EqualTo("East"));
                Assert.That(RoundResultContainsYaku(session, "Chankan"), Is.True);
                Assert.That(RoundResultContainsYaku(session, "Reach"), Is.True);
                Assert.That(RoundResultContainsYaku(session, "Ippatsu"), Is.True);
            }
        }

        [Test]
        public void Daiminkan_AutoRinshanCarriesRinshanKaihouIntoWinDecisionAndRoundResult()
        {
            using (MahjongGameFlowTestSession session = CreateSession(
                2,
                useRinshanOnlyCatalog: true))
            {
                session.Commands.StartNewRound();
                object wall = session.Reflection.GetProperty(session.CurrentState, "Wall");
                object rinshanTiles = session.Reflection.Invoke(wall, "GetRinshanSnapshot");
                string winningTileCode = session.Reflection.GetProperty(
                    session.Collections.Item(rinshanTiles, 0),
                    "Code").ToString();
                string kanTileCode = winningTileCode == "P" ? "C" : "P";
                session.DataFactory.AddHandTiles(
                    session.Query.GetPlayerSeat("East"),
                    kanTileCode,
                    kanTileCode,
                    kanTileCode,
                    "1m", "2m", "3m",
                    "1p", "2p", "3p",
                    "1s", "2s", "3s",
                    winningTileCode);
                DiscardFromWest(session, kanTileCode);
                EventSequenceRecorder events = new EventSequenceRecorder(
                    session.EventNotifier,
                    "ReactionWindowClosed",
                    "TileDrawn");

                try
                {
                    Assert.That(
                        session.Commands.TryRequestDeclareDaiminkanForSeat(
                            "East",
                            session.Query.ReactionWindowId),
                        Is.True);
                }
                finally
                {
                    events.Dispose();
                }

                Assert.That(session.Query.IsReactionWindowPending, Is.False);
                Assert.That(session.Query.TurnPhaseName, Is.EqualTo("WinDecision"));
                Assert.That(
                    EvaluationContainsYaku(
                        session,
                        session.Query.PendingWinDeclarationEvaluation,
                        "RinshanKaihou"),
                    Is.True);
                Assert.That(
                    session.Reflection.GetProperty(session.CurrentState, "LastTurnDraw"),
                    Is.Null);
                Assert.That(
                    events.Names,
                    Is.EqualTo(new[] { "ReactionWindowClosed", "TileDrawn" }));
                Assert.That(session.Commands.TryRequestDeclareWinForSeat("East"), Is.True);
                Assert.That(
                    RoundResultContainsYaku(session, "RinshanKaihou"),
                    Is.True);
            }
        }

        private static MahjongGameFlowTestSession CreateDaiminkanSession()
        {
            MahjongGameFlowTestSession session = CreateSession(2);
            session.Commands.StartNewRound();
            session.DataFactory.AddHandTiles(
                session.Query.GetPlayerSeat("East"),
                "P", "P", "P",
                "1m", "2m", "4m", "7m",
                "1p", "4p", "7p",
                "1s", "4s", "7s");
            DiscardFromWest(session, "P");
            Assert.That(session.Query.IsReactionWindowPending, Is.True);
            return session;
        }

        private static void DiscardFromWest(MahjongGameFlowTestSession session, string tileCode)
        {
            session.DataFactory.SetCurrentTurn(session.CurrentState, "West");
            session.DataFactory.SetDrawnTile(session.CurrentState, "West", tileCode);
            Assert.That(session.Commands.TryRequestDiscardDrawnTileForSeat("West"), Is.True);
        }

        private static MahjongGameFlowTestSession CreateSession(
            int participantCount,
            bool useRinshanOnlyCatalog = false,
            bool useChankanCatalog = false)
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            CollectionTestAccess collections = new CollectionTestAccess(reflection);
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            object catalog = useRinshanOnlyCatalog
                ? dataFactory.CreateYakuCatalog(
                    dataFactory.CreateYakuDefinition(
                        "RinshanKaihou",
                        "One",
                        "One"))
                : useChankanCatalog
                    ? dataFactory.CreateYakuCatalog(
                        dataFactory.CreateYakuDefinition("Chankan", "One", "One"),
                        dataFactory.CreateYakuDefinition("Reach", "One", "None"),
                        dataFactory.CreateYakuDefinition("Ippatsu", "One", "None"))
                : MahjongTestCatalogFactory.CreateStandardGameFlowYakuCatalog(dataFactory);
            MahjongGameFlowTestSession session = MahjongGameFlowTestSession.Create(
                new MahjongGameFlowTestOptions
                {
                    RootName = "KanGameFlowTest",
                    AddEventNotifier = true,
                    LogWarnings = false,
                    ParticipantCount = participantCount,
                    InitialHandTileCount = 0,
                    AutoStart = false,
                    UseFixedRandomSeed = true,
                    FixedRandomSeed = 12345,
                    EnableAutoDraw = false,
                    AutoDiscardDrawnTileDelaySeconds = 0f,
                    RandomizeSelfSeat = false,
                    FixedSelfSeatName = "East",
                    YakuDefinitionCatalog = catalog
                },
                reflection,
                collections,
                types,
                dataFactory);
            session.RegisterOwnedScriptableObject(catalog);
            return session;
        }

        private static string[] GetCandidateKinds(MahjongGameFlowTestSession session)
        {
            string[] kinds = new string[session.Query.ReactionWindowCandidateCount];
            for (int i = 0; i < kinds.Length; i++)
                kinds[i] = session.Query.ReactionWindowCandidateKindAt(i);

            return kinds;
        }

        private static string[] GetCandidateResponseStates(
            MahjongGameFlowTestSession session,
            object reactionWindow)
        {
            object candidates = session.Reflection.GetProperty(reactionWindow, "Candidates");
            string[] states = new string[session.Collections.Count(candidates)];
            for (int i = 0; i < states.Length; i++)
            {
                states[i] = session.Reflection.GetProperty(
                    session.Collections.Item(candidates, i),
                    "ResponseState").ToString();
            }

            return states;
        }

        private static string[] AnkanCandidateCodes(MahjongGameFlowTestSession session)
        {
            object candidates = session.Commands.GetAnkanCandidatesForSeat("East");
            string[] codes = new string[session.Collections.Count(candidates)];
            for (int i = 0; i < codes.Length; i++)
            {
                codes[i] = session.Reflection.GetProperty(
                    session.Collections.Item(candidates, i),
                    "Code").ToString();
            }

            return codes;
        }

        private static int CountHandTile(
            MahjongGameFlowTestSession session,
            string seatName,
            string tileCode)
        {
            object playerSeat = session.Query.GetPlayerSeat(seatName);
            object hand = session.Reflection.GetProperty(playerSeat, "Hand");
            object tiles = session.Reflection.Invoke(hand, "GetTiles");
            int count = 0;
            for (int i = 0; i < session.Collections.Count(tiles); i++)
            {
                string code = session.Reflection.GetProperty(
                    session.Collections.Item(tiles, i),
                    "Code").ToString();
                if (code == tileCode)
                    count++;
            }

            return count;
        }

        private static int RemainingRinshanTileCount(MahjongGameFlowTestSession session)
        {
            return (int)session.Reflection.GetProperty(
                session.Reflection.GetProperty(session.CurrentState, "Wall"),
                "RemainingRinshanTileCount");
        }

        private static void ClearLiveWall(MahjongGameFlowTestSession session, object wall)
        {
            ReduceLiveWallTo(session, wall, 0);
        }

        private static void ReduceLiveWallTo(
            MahjongGameFlowTestSession session,
            object wall,
            int targetCount)
        {
            while ((int)session.Reflection.GetProperty(wall, "Count") > targetCount)
            {
                object[] args = { null };
                Assert.That(
                    (bool)session.Reflection.Invoke(wall, "TryTakeNext", args),
                    Is.True);
            }
        }

        private static bool HasCallOccurred(MahjongGameFlowTestSession session)
        {
            return (bool)session.Reflection.GetProperty(
                session.CurrentState,
                "HasCallOccurred");
        }

        private static bool EvaluationContainsYaku(
            MahjongGameFlowTestSession session,
            object evaluation,
            string yakuKindName)
        {
            object handEvaluation = session.Reflection.GetProperty(
                evaluation,
                "HandEvaluationResult");
            object candidateResults = session.Reflection.GetProperty(
                handEvaluation,
                "CandidateResults");
            for (int i = 0; i < session.Collections.Count(candidateResults); i++)
            {
                object candidate = session.Collections.Item(candidateResults, i);
                object yakus = session.Reflection.GetProperty(candidate, "Yakus");
                if (YakuCollectionContains(session, yakus, yakuKindName))
                    return true;
            }

            return false;
        }

        private static bool RoundResultContainsYaku(
            MahjongGameFlowTestSession session,
            string yakuKindName)
        {
            return YakuCollectionContains(
                session,
                session.Reflection.GetProperty(
                    session.Query.CurrentRoundResult,
                    "Yakus"),
                yakuKindName);
        }

        private static bool YakuCollectionContains(
            MahjongGameFlowTestSession session,
            object yakus,
            string yakuKindName)
        {
            for (int i = 0; i < session.Collections.Count(yakus); i++)
            {
                object yaku = session.Collections.Item(yakus, i);
                if (session.Reflection.GetProperty(yaku, "Kind").ToString() == yakuKindName)
                    return true;
            }

            return false;
        }

        private static string PropertyText(
            MahjongGameFlowTestSession session,
            object target,
            string propertyName)
        {
            return session.Reflection.GetProperty(target, propertyName)?.ToString();
        }

        private sealed class EventSequenceRecorder : IDisposable
        {
            private readonly object eventSource;
            private readonly List<string> names = new List<string>();
            private readonly List<EventSubscription> subscriptions =
                new List<EventSubscription>();

            public EventSequenceRecorder(object eventSource, params string[] eventNames)
            {
                this.eventSource = eventSource;
                for (int i = 0; i < eventNames.Length; i++)
                    Subscribe(eventNames[i]);
            }

            public IReadOnlyList<string> Names => names;

            public void Dispose()
            {
                for (int i = subscriptions.Count - 1; i >= 0; i--)
                    subscriptions[i].EventInfo.RemoveEventHandler(
                        eventSource,
                        subscriptions[i].Handler);

                subscriptions.Clear();
            }

            private void Subscribe(string eventName)
            {
                EventInfo eventInfo = eventSource.GetType().GetEvent(
                    eventName,
                    BindingFlags.Public | BindingFlags.Instance);
                Assert.That(eventInfo, Is.Not.Null, $"Event not found: {eventName}");

                ParameterInfo[] parameters = eventInfo.EventHandlerType
                    .GetMethod("Invoke")
                    .GetParameters();
                ParameterExpression[] expressions = new ParameterExpression[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    expressions[i] = Expression.Parameter(
                        parameters[i].ParameterType,
                        parameters[i].Name);
                }

                MethodInfo record = GetType().GetMethod(
                    nameof(Record),
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Delegate handler = Expression.Lambda(
                    eventInfo.EventHandlerType,
                    Expression.Call(
                        Expression.Constant(this),
                        record,
                        Expression.Constant(eventName)),
                    expressions).Compile();
                eventInfo.AddEventHandler(eventSource, handler);
                subscriptions.Add(new EventSubscription(eventInfo, handler));
            }

            private void Record(string eventName)
            {
                names.Add(eventName);
            }

            private readonly struct EventSubscription
            {
                public EventSubscription(EventInfo eventInfo, Delegate handler)
                {
                    EventInfo = eventInfo;
                    Handler = handler;
                }

                public EventInfo EventInfo { get; }
                public Delegate Handler { get; }
            }
        }
    }
}
