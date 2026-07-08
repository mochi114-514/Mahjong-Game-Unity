using System;
using System.Collections;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class LastTileYakuGameFlowTests
    {
        private const string StandardHand =
            "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C";

        [Test]
        public void TurnDrawNotLast_RecordsNonLastDraw()
        {
            using (Driver driver = Driver.Create(participantCount: 1))
            {
                driver.StartNewRound();
                driver.SetTurnDrawOrder("C", "9m");

                driver.DrawForSeat("East");

                Assert.That(driver.HasLastTurnDraw, Is.True);
                Assert.That(driver.LastTurnDrawActorSeatName, Is.EqualTo("East"));
                Assert.That(driver.LastTurnDrawTileCode, Is.EqualTo("C"));
                Assert.That(driver.LastTurnDrawIsLastLiveWallDraw, Is.False);
                Assert.That(driver.WallCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void TurnDrawWithWallCountAfterDrawZero_RecordsLastDraw()
        {
            using (Driver driver = Driver.Create(participantCount: 1))
            {
                driver.StartNewRound();
                driver.SetTurnDrawOrder("C");

                driver.DrawForSeat("East");

                Assert.That(driver.HasLastTurnDraw, Is.True);
                Assert.That(driver.LastTurnDrawActorSeatName, Is.EqualTo("East"));
                Assert.That(driver.LastTurnDrawTileCode, Is.EqualTo("C"));
                Assert.That(driver.LastTurnDrawIsLastLiveWallDraw, Is.True);
                Assert.That(driver.WallCount, Is.EqualTo(0));
            }
        }

        [Test]
        public void InitialDeal_DoesNotRecordLastTurnDraw()
        {
            using (Driver driver = Driver.Create(participantCount: 1))
            {
                driver.StartNewRound();
                driver.SetInitialHandTileCount(1);
                driver.SetTurnDrawOrder("C");

                driver.DealInitialHands();

                Assert.That(driver.HasLastTurnDraw, Is.False);
                Assert.That(driver.WallCount, Is.EqualTo(0));
            }
        }

        [Test]
        public void LastDrawTsumoCandidate_IncludesHaitei()
        {
            using (Driver driver = Driver.Create(participantCount: 1))
            {
                driver.StartNewRound();
                driver.AddHandTiles("East", StandardHand);
                driver.SetTurnDrawOrder("C");

                driver.DrawForSeat("East");

                Assert.That(driver.IsWinDecisionPending, Is.True);
                Assert.That(driver.PendingCandidateContainsYaku("HaiteiRaoyue"), Is.True);
                Assert.That(driver.PendingCandidateContainsYaku("HouteiRaoyui"), Is.False);
            }
        }

        [Test]
        public void NonLastDrawTsumoCandidate_DoesNotIncludeLastTileYaku()
        {
            using (Driver driver = Driver.Create(participantCount: 1))
            {
                driver.StartNewRound();
                driver.AddHandTiles("East", StandardHand);
                driver.SetTurnDrawOrder("C", "9m");

                driver.DrawForSeat("East");

                Assert.That(driver.IsWinDecisionPending, Is.True);
                Assert.That(driver.WallCount, Is.EqualTo(1));
                Assert.That(driver.LastTurnDrawIsLastLiveWallDraw, Is.False);
                Assert.That(driver.PendingCandidateContainsYaku("MenzenTsumo"), Is.True);
                Assert.That(driver.PendingCandidateContainsYaku("HaiteiRaoyue"), Is.False);
                Assert.That(driver.PendingCandidateContainsYaku("HouteiRaoyui"), Is.False);
            }
        }

        [Test]
        public void LastDrawThenTsumogiri_RonCandidateIncludesHoutei()
        {
            using (Driver driver = Driver.Create(participantCount: 2))
            {
                driver.StartNewRound();
                driver.SetParticipantType("West", "LocalHuman");
                driver.AddHandTiles("West", StandardHand);
                driver.SetTurnDrawOrder("C");

                driver.DrawForSeat("East");
                driver.DiscardDrawnTile();

                Assert.That(driver.LastDiscardIsLastLiveWallDiscard, Is.True);
                Assert.That(driver.IsWinDecisionPending, Is.True);
                Assert.That(driver.PendingCandidateContainsYaku("HouteiRaoyui"), Is.True);
                Assert.That(driver.PendingCandidateContainsYaku("HaiteiRaoyue"), Is.False);
            }
        }

        [Test]
        public void LastDrawThenHandDiscard_RonCandidateIncludesHoutei()
        {
            using (Driver driver = Driver.Create(participantCount: 2))
            {
                driver.StartNewRound();
                driver.SetParticipantType("West", "LocalHuman");
                driver.AddHandTiles("East", "C");
                driver.AddHandTiles("West", StandardHand);
                driver.SetTurnDrawOrder("9m");

                driver.DrawForSeat("East");
                driver.DiscardHandTile(0);

                Assert.That(driver.LastDiscardSourceName, Is.EqualTo("Hand"));
                Assert.That(driver.LastDiscardIsLastLiveWallDiscard, Is.True);
                Assert.That(driver.IsWinDecisionPending, Is.True);
                Assert.That(driver.PendingCandidateContainsYaku("HouteiRaoyui"), Is.True);
            }
        }

        [Test]
        public void NormalDrawThenDiscard_RonCandidateDoesNotIncludeHoutei()
        {
            using (Driver driver = Driver.Create(participantCount: 2))
            {
                driver.StartNewRound();
                driver.SetParticipantType("West", "LocalHuman");
                driver.AddHandTiles("East", "C");
                driver.AddHandTiles("West", StandardHand);
                driver.SetTurnDrawOrder("9m", "8m");

                driver.DrawForSeat("East");
                driver.DiscardHandTile(0);

                Assert.That(driver.LastDiscardIsLastLiveWallDiscard, Is.False);
                Assert.That(driver.IsWinDecisionPending, Is.True);
                Assert.That(driver.PendingCandidateContainsYaku("HouteiRaoyui"), Is.False);
                Assert.That(driver.PendingCandidateContainsYaku("YakuhaiRoundWind"), Is.True);
            }
        }

        [Test]
        public void LastDiscardWithoutRonCandidate_EndsRoundByWallEmpty()
        {
            using (Driver driver = Driver.Create(participantCount: 2))
            {
                driver.StartNewRound();
                driver.SetTurnDrawOrder("C");

                driver.DrawForSeat("East");
                driver.DiscardDrawnTile();

                Assert.That(driver.IsRoundResultPending, Is.True);
                Assert.That(driver.RoundResultTypeName, Is.EqualTo("ExhaustiveDraw"));
                Assert.That(driver.WindProgressHandNumber, Is.EqualTo(1));
                Assert.That(driver.DiscardCount, Is.EqualTo(1));
                Assert.That(driver.HasLastTurnDraw, Is.True);

                driver.AdvanceFromRoundResult();

                Assert.That(driver.WindProgressHandNumber, Is.EqualTo(2));
                Assert.That(driver.DiscardCount, Is.EqualTo(0));
                Assert.That(driver.HasLastTurnDraw, Is.False);
            }
        }

        [Test]
        public void DeclinedRonOnLastDiscard_EndsRoundByWallEmpty()
        {
            using (Driver driver = Driver.Create(participantCount: 2))
            {
                driver.StartNewRound();
                driver.SetParticipantType("West", "LocalHuman");
                driver.AddHandTiles("West", StandardHand);
                driver.SetTurnDrawOrder("C");

                driver.DrawForSeat("East");
                driver.DiscardDrawnTile();

                Assert.That(driver.IsWinDecisionPending, Is.True);

                driver.DeclineWin();

                Assert.That(driver.IsRoundResultPending, Is.True);
                Assert.That(driver.RoundResultTypeName, Is.EqualTo("ExhaustiveDraw"));
                Assert.That(driver.WindProgressHandNumber, Is.EqualTo(1));
                Assert.That(driver.DiscardCount, Is.EqualTo(1));
                Assert.That(driver.HasLastTurnDraw, Is.True);

                driver.AdvanceFromRoundResult();

                Assert.That(driver.WindProgressHandNumber, Is.EqualTo(2));
                Assert.That(driver.DiscardCount, Is.EqualTo(0));
                Assert.That(driver.HasLastTurnDraw, Is.False);
            }
        }

        [Test]
        public void StartNewRound_ClearsLastTurnDraw()
        {
            using (Driver driver = Driver.Create(participantCount: 1))
            {
                driver.StartNewRound();
                driver.SetTurnDrawOrder("C");
                driver.DrawForSeat("East");

                Assert.That(driver.HasLastTurnDraw, Is.True);

                driver.StartNewRound();

                Assert.That(driver.HasLastTurnDraw, Is.False);
            }
        }

        private sealed class Driver : IDisposable
        {
            private readonly MahjongGameFlowTestSession session;
            private bool disposed;

            private Driver(MahjongGameFlowTestSession session)
            {
                this.session = session;
            }

            public static Driver Create(int participantCount)
            {
                ReflectionTestAccess reflection = new ReflectionTestAccess();
                CollectionTestAccess collections = new CollectionTestAccess(reflection);
                MahjongTestTypes types = new MahjongTestTypes(reflection);
                MahjongTestDataFactory dataFactory =
                    new MahjongTestDataFactory(reflection, types);
                object catalog = dataFactory.CreateYakuCatalog(
                    dataFactory.CreateYakuDefinition("HaiteiRaoyue", "One", "One"),
                    dataFactory.CreateYakuDefinition("HouteiRaoyui", "One", "One"),
                    dataFactory.CreateYakuDefinition("MenzenTsumo", "One", "None"),
                    dataFactory.CreateYakuDefinition("YakuhaiRoundWind", "One", "One"));
                MahjongGameFlowTestOptions options = new MahjongGameFlowTestOptions
                {
                    RootName = "LastTileYakuGameFlowTest",
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
                };

                MahjongGameFlowTestSession session = MahjongGameFlowTestSession.Create(
                    options,
                    reflection,
                    collections,
                    types,
                    dataFactory);
                session.RegisterOwnedScriptableObject(catalog);
                return new Driver(session);
            }

            public bool HasLastTurnDraw => session.Query.HasLastTurnDraw;
            public string LastTurnDrawActorSeatName => session.Query.LastTurnDrawActorSeatName;
            public string LastTurnDrawTileCode => session.Query.LastTurnDrawTileCode;
            public bool LastTurnDrawIsLastLiveWallDraw =>
                session.Query.LastTurnDrawIsLastLiveWallDraw;
            public int WallCount => session.Query.WallCount;
            public bool IsWinDecisionPending => session.Query.IsWinDecisionPending;
            public bool IsRoundResultPending => session.Query.IsRoundResultPending;
            public string RoundResultTypeName => session.Query.RoundResultTypeName;
            public bool LastDiscardIsLastLiveWallDiscard =>
                session.Query.LastDiscardIsLastLiveWallDiscard;
            public string LastDiscardSourceName => session.Query.LastDiscardSourceName;
            public int WindProgressHandNumber => session.Query.WindProgressHandNumber;
            public int DiscardCount => session.Query.DiscardCount;

            public void StartNewRound()
            {
                session.Commands.StartNewRound();
            }

            public void DealInitialHands()
            {
                session.Commands.DealInitialHands();
            }

            public void SetInitialHandTileCount(int count)
            {
                session.Reflection.SetPrivateField(
                    session.GameFlow,
                    "initialHandTileCount",
                    count);
            }

            public void SetParticipantType(
                string seatName,
                string participantTypeName)
            {
                session.DataFactory.SetParticipantType(
                    session.CurrentState,
                    seatName,
                    participantTypeName);
            }

            public void AddHandTiles(string seatName, string tileText)
            {
                session.DataFactory.AddHandTilesFromText(
                    session.Query.GetPlayerSeat(seatName),
                    tileText);
            }

            public void SetTurnDrawOrder(params string[] tileCodes)
            {
                object wall = session.Reflection.GetProperty(session.CurrentState, "Wall");
                IList tiles = (IList)session.Reflection.GetPrivateField(wall, "tiles");
                tiles.Clear();

                for (int i = tileCodes.Length - 1; i >= 0; i--)
                    tiles.Add(session.DataFactory.CreateTile(tileCodes[i]));
            }

            public void DrawForSeat(string seatName)
            {
                Assert.That(session.Commands.TryRequestDrawForSeat(seatName), Is.True);
            }

            public void DiscardDrawnTile()
            {
                session.Commands.RequestDiscardDrawnTile();
            }

            public void DiscardHandTile(int handIndex)
            {
                session.Commands.RequestDiscard(handIndex);
            }

            public void DeclineWin()
            {
                session.Commands.RequestDeclineWin();
            }

            public void AdvanceFromRoundResult()
            {
                session.Commands.RequestAdvanceFromRoundResult();
            }

            public bool PendingCandidateContainsYaku(string yakuKindName)
            {
                object evaluation = session.Query.PendingWinDeclarationEvaluation;
                if (evaluation == null)
                    return false;

                object handEvaluation = session.Reflection.GetProperty(
                    evaluation,
                    "HandEvaluationResult");
                object candidateResults = session.Reflection.GetProperty(
                    handEvaluation,
                    "CandidateResults");
                int candidateCount = session.Collections.Count(candidateResults);

                for (int i = 0; i < candidateCount; i++)
                {
                    object candidate = session.Collections.Item(candidateResults, i);
                    if (CandidateContainsYaku(candidate, yakuKindName))
                        return true;
                }

                return false;
            }

            public void Dispose()
            {
                if (disposed)
                    return;

                disposed = true;
                session.Dispose();
            }

            private bool CandidateContainsYaku(object candidate, string yakuKindName)
            {
                object yakus = session.Reflection.GetProperty(candidate, "Yakus");
                int yakuCount = session.Collections.Count(yakus);

                for (int i = 0; i < yakuCount; i++)
                {
                    object yaku = session.Collections.Item(yakus, i);
                    if (session.Reflection.GetProperty(yaku, "Kind").ToString() == yakuKindName)
                        return true;
                }

                return false;
            }
        }
    }
}
