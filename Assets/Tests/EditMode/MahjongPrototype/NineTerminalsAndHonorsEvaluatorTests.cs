using System;
using System.Collections;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class NineTerminalsAndHonorsEvaluatorTests
    {
        private const string EvaluatorTypeName =
            "MahjongPrototype.Services.NineTerminalsAndHonorsEvaluator, Assembly-CSharp";

        private readonly ReflectionTestAccess reflection = new ReflectionTestAccess();
        private readonly MahjongTestTypes types;
        private readonly MahjongTestDataFactory data;
        private readonly object evaluator;

        public NineTerminalsAndHonorsEvaluatorTests()
        {
            types = new MahjongTestTypes(reflection);
            data = new MahjongTestDataFactory(reflection, types);
            evaluator = reflection.CreateInstance(reflection.RequireType(EvaluatorTypeName));
        }

        [Test]
        public void CanDeclare_EastFirstDrawWithNineDistinctTypes_ReturnsTrue()
        {
            object gameState = CreateFirstDrawState(
                "East",
                "1m", "9m", "1p", "9p", "1s", "9s", "E", "S",
                "2m", "3m", "4m", "5m", "6m");
            data.SetDrawnTile(gameState, "East", "W");

            Assert.That(CanDeclare(gameState, "East"), Is.True);
        }

        [Test]
        public void CanDeclare_ChildFirstDrawAllowsOtherSeatsDiscards_ReturnsTrue()
        {
            object gameState = CreateFirstDrawState(
                "South",
                "1m", "9m", "1p", "9p", "1s", "9s", "E", "S", "W",
                "2m", "3m", "4m", "5m");
            data.AddDiscard(gameState, "East", "6m", 1);
            data.SetDrawnTile(gameState, "South", "2p");

            Assert.That(CanDeclare(gameState, "South"), Is.True);
        }

        [Test]
        public void CanDeclare_DuplicateTerminalOrHonorCountsOnce_ReturnsFalse()
        {
            object gameState = CreateFirstDrawState(
                "North",
                "1m", "9m", "1p", "9p", "1s", "9s", "E", "S",
                "2m", "3m", "4m", "5m", "6m");
            data.SetDrawnTile(gameState, "North", "E");

            Assert.That(CanDeclare(gameState, "North"), Is.False);
        }

        [Test]
        public void CanDeclare_EightDistinctTypes_ReturnsFalse()
        {
            object gameState = CreateFirstDrawState(
                "East",
                "1m", "9m", "1p", "9p", "1s", "9s", "E",
                "2m", "3m", "4m", "5m", "6m", "7m");
            data.SetDrawnTile(gameState, "East", "S");

            Assert.That(CanDeclare(gameState, "East"), Is.False);
        }

        [Test]
        public void CanDeclare_AfterOwnDiscard_ReturnsFalse()
        {
            object gameState = CreateFirstDrawState(
                "South",
                "1m", "9m", "1p", "9p", "1s", "9s", "E", "S", "W",
                "2m", "3m", "4m", "5m");
            data.AddDiscard(gameState, "South", "6m", 4);
            data.SetDrawnTile(gameState, "South", "P");

            Assert.That(CanDeclare(gameState, "South"), Is.False);
        }

        [Test]
        public void CanDeclare_AfterCallOrKan_ReturnsFalse()
        {
            object gameState = CreateFirstDrawState(
                "East",
                "1m", "9m", "1p", "9p", "1s", "9s", "E", "S", "W",
                "2m", "3m", "4m", "5m");
            data.SetDrawnTile(gameState, "East", "P");
            reflection.Invoke(gameState, "MarkCallOccurred");

            Assert.That(CanDeclare(gameState, "East"), Is.False);
        }

        [Test]
        public void CanDeclare_WithoutDrawnTile_ReturnsFalse()
        {
            object gameState = CreateFirstDrawState(
                "East",
                "1m", "9m", "1p", "9p", "1s", "9s", "E", "S", "W",
                "2m", "3m", "4m", "5m");

            Assert.That(CanDeclare(gameState, "East"), Is.False);
        }

        [Test]
        public void CanDeclare_DoesNotMutateInputState()
        {
            object gameState = CreateFirstDrawState(
                "South",
                "1m", "9m", "1p", "9p", "1s", "9s", "E", "S",
                "2m", "3m", "4m", "5m", "6m");
            data.AddDiscard(gameState, "East", "7m", 1);
            data.SetDrawnTile(gameState, "South", "W");
            object playerSeat = data.GetPlayerSeat(gameState, "South");
            string handBefore = data.HandDisplayString(gameState, "South");
            string drawnTileBefore = reflection.GetProperty(
                playerSeat,
                "DrawnTile").ToString();
            int discardCountBefore = ((ICollection)reflection.GetProperty(
                gameState,
                "Discards")).Count;

            Assert.That(CanDeclare(gameState, "South"), Is.True);

            Assert.That(data.HandDisplayString(gameState, "South"), Is.EqualTo(handBefore));
            Assert.That(
                reflection.GetProperty(playerSeat, "DrawnTile").ToString(),
                Is.EqualTo(drawnTileBefore));
            Assert.That(
                ((ICollection)reflection.GetProperty(gameState, "Discards")).Count,
                Is.EqualTo(discardCountBefore));
            Assert.That(
                (bool)reflection.GetProperty(gameState, "HasCallOccurred"),
                Is.False);
        }

        private object CreateFirstDrawState(string currentSeat, params string[] handTiles)
        {
            object gameState = data.CreateGameState("East", "South", "West", "North");
            data.SetCurrentTurn(gameState, currentSeat);
            data.AddHandTiles(data.GetPlayerSeat(gameState, currentSeat), handTiles);
            return gameState;
        }

        private bool CanDeclare(object gameState, string seatName)
        {
            object playerSeat = data.GetPlayerSeat(gameState, seatName);
            return (bool)reflection.Invoke(
                evaluator,
                "CanDeclare",
                playerSeat,
                reflection.GetProperty(gameState, "Discards"),
                reflection.GetProperty(gameState, "HasCallOccurred"));
        }
    }

    public sealed class NineTerminalsAndHonorsGameFlowTests
    {
        private const string DecisionKindTypeName =
            "MahjongPrototype.Domain.DecisionKind, Assembly-CSharp";
        private const string DecisionResponseTypeName =
            "MahjongPrototype.Domain.DecisionResponse, Assembly-CSharp";

        [Test]
        public void NormalDraw_WhenEligible_CreatesTypedAbortiveDrawDecision()
        {
            using (MahjongGameFlowTestSession session = CreateSession())
            {
                PrepareEligibleDraw(session);

                object request = GetPendingDecision(session, "AbortiveDraw");

                Assert.That(session.Query.TurnPhaseName, Is.EqualTo("AbortiveDrawDecision"));
                Assert.That(
                    session.Reflection.GetProperty(
                        session.Reflection.GetProperty(request, "AbortiveDraw"),
                        "Kind").ToString(),
                    Is.EqualTo("NineTerminalsAndHonors"));
            }
        }

        [Test]
        public void TsumoAndNineTerminals_WhenBothEligible_OffersTsumoFirstThenAbortiveDraw()
        {
            using (MahjongGameFlowTestSession session = CreateSession(
                       includeKokushiMusou: true))
            {
                session.Commands.StartNewRound();
                session.DataFactory.AddHandTiles(
                    session.Query.GetPlayerSeat("East"),
                    "1m", "9m", "1p", "9p", "1s", "9s",
                    "E", "S", "W", "N", "P", "F", "C");
                DrawForcedTile(session, "1m");

                Assert.That(session.Query.TurnPhaseName, Is.EqualTo("WinDecision"));
                Assert.That(HasPendingDecision(session, "WinDeclaration"), Is.True);
                Assert.That(HasPendingDecision(session, "AbortiveDraw"), Is.False);

                session.Commands.RequestDeclineWin();

                Assert.That(session.Query.TurnPhaseName, Is.EqualTo("AbortiveDrawDecision"));
                Assert.That(HasPendingDecision(session, "WinDeclaration"), Is.False);
                Assert.That(HasPendingDecision(session, "AbortiveDraw"), Is.True);
            }
        }

        [Test]
        public void AbortiveDrawDecision_Accepted_EndsRoundAsNineTerminalsAndHonors()
        {
            using (MahjongGameFlowTestSession session = CreateSession())
            {
                PrepareEligibleDraw(session);
                object request = GetPendingDecision(session, "AbortiveDraw");

                Assert.That(SubmitDecisionResponse(session, request, true), Is.True);
                PumpDecisionCoordinator(session);

                Assert.That(session.Query.IsRoundResultPending, Is.True);
                Assert.That(session.Query.RoundResultTypeName, Is.EqualTo("AbortiveDraw"));
                Assert.That(
                    session.Query.RoundResultAbortiveDrawKindNameOrNull,
                    Is.EqualTo("NineTerminalsAndHonors"));
            }
        }

        [Test]
        public void AbortiveDrawDecision_Declined_ContinuesToSelfKanDecision()
        {
            using (MahjongGameFlowTestSession session = CreateSession())
            {
                session.Commands.StartNewRound();
                session.DataFactory.AddHandTiles(
                    session.Query.GetPlayerSeat("East"),
                    "P", "P", "P",
                    "1m", "9m", "1p", "9p", "1s", "9s", "E", "S",
                    "2m", "3m");
                DrawForcedTile(session, "P");
                object request = GetPendingDecision(session, "AbortiveDraw");

                Assert.That(SubmitDecisionResponse(session, request, false), Is.True);
                PumpDecisionCoordinator(session);

                Assert.That(session.Query.TurnPhaseName, Is.EqualTo("SelfKanDecision"));
                Assert.That(HasPendingDecision(session, "AbortiveDraw"), Is.False);
                Assert.That(HasPendingDecision(session, "SelfKan"), Is.True);
            }
        }

        [Test]
        public void AbortiveDrawDecision_DeclinedWithoutLaterChoices_ReturnsToDiscard()
        {
            using (MahjongGameFlowTestSession session = CreateSession())
            {
                PrepareEligibleDraw(session);
                object request = GetPendingDecision(session, "AbortiveDraw");

                Assert.That(SubmitDecisionResponse(session, request, false), Is.True);
                PumpDecisionCoordinator(session);

                Assert.That(session.Query.TurnPhaseName, Is.EqualTo("WaitingForDiscard"));
                Assert.That(session.Query.CurrentPlayerHasDrawnTile, Is.True);
                Assert.That(HasPendingDecision(session, "AbortiveDraw"), Is.False);
            }
        }

        [Test]
        public void AbortiveDrawDecision_WhenProviderCannotReceive_DoesNotStopTurn()
        {
            using (MahjongGameFlowTestSession session = CreateSession())
            {
                session.Commands.StartNewRound();
                object registry = session.Reflection.GetProperty(
                    session.GameFlow,
                    "DecisionProviderRegistry");
                ((IList)session.Reflection.GetPrivateField(
                    registry,
                    "registrations")).Clear();
                session.DataFactory.AddHandTiles(
                    session.Query.GetPlayerSeat("East"),
                    "1m", "9m", "1p", "9p", "1s", "9s", "E", "S", "W",
                    "2m", "3m", "4m", "5m");

                DrawForcedTile(session, "P");

                Assert.That(session.Query.TurnPhaseName, Is.EqualTo("WaitingForDiscard"));
                Assert.That(session.Query.CurrentPlayerHasDrawnTile, Is.True);
                Assert.That(HasPendingDecision(session, "AbortiveDraw"), Is.False);
            }
        }

        [Test]
        public void NormalDraw_WhenNotEligible_DoesNotCreateAbortiveDrawDecision()
        {
            using (MahjongGameFlowTestSession session = CreateSession())
            {
                session.Commands.StartNewRound();
                session.DataFactory.AddHandTiles(
                    session.Query.GetPlayerSeat("East"),
                    "1m", "9m", "1p", "9p", "1s", "9s", "E",
                    "2m", "3m", "4m", "5m", "6m", "7m");
                DrawForcedTile(session, "S");

                Assert.That(HasPendingDecision(session, "AbortiveDraw"), Is.False);
                Assert.That(
                    session.Query.TurnPhaseName,
                    Is.Not.EqualTo("AbortiveDrawDecision"));
            }
        }

        [Test]
        public void AbortiveDrawDecision_RejectsWrongTurnAndDuplicateResponses()
        {
            using (MahjongGameFlowTestSession session = CreateSession())
            {
                PrepareEligibleDraw(session);
                object request = GetPendingDecision(session, "AbortiveDraw");
                object staleResponse = CreateDecisionResponse(
                    session,
                    request,
                    true,
                    (int)session.Reflection.GetProperty(request, "TurnIndex") + 1);

                Assert.That(
                    SubmitResponseToLocalProvider(session, staleResponse),
                    Is.False);
                Assert.That(session.Query.TurnPhaseName, Is.EqualTo("AbortiveDrawDecision"));

                object validResponse = CreateDecisionResponse(
                    session,
                    request,
                    true,
                    (int)session.Reflection.GetProperty(request, "TurnIndex"));
                Assert.That(
                    SubmitResponseToLocalProvider(session, validResponse),
                    Is.True);
                Assert.That(
                    SubmitResponseToLocalProvider(session, validResponse),
                    Is.False);

                PumpDecisionCoordinator(session);

                Assert.That(session.Query.IsRoundResultPending, Is.True);
                Assert.That(
                    session.Query.RoundResultAbortiveDrawKindNameOrNull,
                    Is.EqualTo("NineTerminalsAndHonors"));
            }
        }

        [Test]
        public void TileClick_DuringAbortiveDrawDecision_DeclinesAndDiscards()
        {
            using (MahjongGameFlowTestSession session = CreateSession())
            {
                PrepareEligibleDraw(session);
                object request = GetPendingDecision(session, "AbortiveDraw");

                Assert.That(
                    session.Commands.TryRequestDiscardHandFromTileClickForSeat(
                        "East",
                        0),
                    Is.True);

                Assert.That(session.Query.DiscardCount, Is.EqualTo(1));
                Assert.That(HasPendingDecision(session, "AbortiveDraw"), Is.False);
                Assert.That(
                    SubmitDecisionResponse(session, request, true),
                    Is.False);
            }
        }

        private static MahjongGameFlowTestSession CreateSession(
            bool includeKokushiMusou = false)
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            CollectionTestAccess collections = new CollectionTestAccess(reflection);
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory =
                new MahjongTestDataFactory(reflection, types);
            object catalog = includeKokushiMusou
                ? dataFactory.CreateYakuCatalog(
                    dataFactory.CreateYakuDefinition(
                        "KokushiMusou",
                        "None",
                        "None",
                        isYakuman: true))
                : MahjongTestCatalogFactory.CreateStandardGameFlowYakuCatalog(
                    dataFactory);
            MahjongGameFlowTestSession session = MahjongGameFlowTestSession.Create(
                new MahjongGameFlowTestOptions
                {
                    RootName = "NineTerminalsAndHonorsGameFlowTest",
                    AddEventNotifier = true,
                    LogWarnings = false,
                    ParticipantCount = 1,
                    InitialHandTileCount = 0,
                    AutoStart = false,
                    UseFixedRandomSeed = true,
                    FixedRandomSeed = 12345,
                    EnableAutoDraw = false,
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

        private static void PrepareEligibleDraw(MahjongGameFlowTestSession session)
        {
            session.Commands.StartNewRound();
            session.DataFactory.AddHandTiles(
                session.Query.GetPlayerSeat("East"),
                "1m", "9m", "1p", "9p", "1s", "9s", "E", "S", "W",
                "2m", "3m", "4m", "5m");
            DrawForcedTile(session, "P");
        }

        private static void DrawForcedTile(
            MahjongGameFlowTestSession session,
            string tileCode)
        {
            session.Commands.RequestForceDrawSkill(tileCode);
            session.Commands.RequestDraw();
        }

        private static object GetPendingDecision(
            MahjongGameFlowTestSession session,
            string kindName)
        {
            object[] arguments =
            {
                session.DataFactory.ParsePlayerId("Player1"),
                Enum.Parse(
                    session.Reflection.RequireType(DecisionKindTypeName),
                    kindName),
                null
            };
            Assert.That(
                (bool)session.Reflection.Invoke(
                    session.GameFlow,
                    "TryGetPendingDecisionRequest",
                    arguments),
                Is.True);
            return arguments[2];
        }

        private static bool HasPendingDecision(
            MahjongGameFlowTestSession session,
            string kindName)
        {
            object[] arguments =
            {
                session.DataFactory.ParsePlayerId("Player1"),
                Enum.Parse(
                    session.Reflection.RequireType(DecisionKindTypeName),
                    kindName),
                null
            };
            return (bool)session.Reflection.Invoke(
                session.GameFlow,
                "TryGetPendingDecisionRequest",
                arguments);
        }

        private static bool SubmitDecisionResponse(
            MahjongGameFlowTestSession session,
            object request,
            bool accepted)
        {
            object response = CreateDecisionResponse(
                session,
                request,
                accepted,
                (int)session.Reflection.GetProperty(request, "TurnIndex"));
            return SubmitResponseToLocalProvider(session, response);
        }

        private static object CreateDecisionResponse(
            MahjongGameFlowTestSession session,
            object request,
            bool accepted,
            int turnIndex)
        {
            return session.Reflection.CreateInstance(
                session.Reflection.RequireType(DecisionResponseTypeName),
                session.Reflection.GetProperty(request, "RequestId"),
                session.Reflection.GetProperty(request, "Kind"),
                session.Reflection.GetProperty(request, "PlayerId"),
                session.Reflection.GetProperty(request, "ActorSeat"),
                turnIndex,
                accepted);
        }

        private static bool SubmitResponseToLocalProvider(
            MahjongGameFlowTestSession session,
            object response)
        {
            object[] providerArguments =
            {
                session.DataFactory.ParsePlayerId("Player1"),
                null
            };
            Assert.That(
                (bool)session.Reflection.Invoke(
                    session.GameFlow,
                    "TryGetLocalUiDecisionProvider",
                    providerArguments),
                Is.True);
            return (bool)session.Reflection.Invoke(
                providerArguments[1],
                "TrySubmitResponse",
                response);
        }

        private static void PumpDecisionCoordinator(
            MahjongGameFlowTestSession session)
        {
            session.Reflection.Invoke(
                session.Reflection.GetProperty(
                    session.GameFlow,
                    "DecisionCoordinator"),
                "Pump");
        }
    }
}
