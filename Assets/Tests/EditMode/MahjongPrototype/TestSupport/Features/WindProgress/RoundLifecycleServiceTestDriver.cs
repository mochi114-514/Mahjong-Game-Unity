using System;
using System.Collections;
using System.Collections.Generic;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;

namespace MahjongPrototype.Tests.TestSupport.Features.WindProgress
{
    internal sealed class RoundLifecycleServiceTestDriver
    {
        private const string RoundLifecycleServiceTypeName =
            "MahjongPrototype.Services.RoundLifecycleService, Assembly-CSharp";
        private const string WinningCandidateSelectorTypeName =
            "MahjongPrototype.Services.WinningCandidateSelector, Assembly-CSharp";
        private const string SevenPairsAnalysisTypeName =
            "MahjongPrototype.Domain.SevenPairsAnalysis, Assembly-CSharp";
        private const string HandEvaluationCandidateTypeName =
            "MahjongPrototype.Domain.HandEvaluationCandidate, Assembly-CSharp";
        private const string HandEvaluationCandidateResultTypeName =
            "MahjongPrototype.Domain.HandEvaluationCandidateResult, Assembly-CSharp";
        private const string HandEvaluationResultTypeName =
            "MahjongPrototype.Domain.HandEvaluationResult, Assembly-CSharp";
        private const string EvaluatedYakuTypeName =
            "MahjongPrototype.Domain.EvaluatedYaku, Assembly-CSharp";
        private const string WinCheckResultTypeName =
            "MahjongPrototype.Domain.WinCheckResult, Assembly-CSharp";
        private const string WinningHandShapeTypeName =
            "MahjongPrototype.Domain.WinningHandShape, Assembly-CSharp";
        private const string WinDeclarationEvaluationResultTypeName =
            "MahjongPrototype.Domain.WinDeclarationEvaluationResult, Assembly-CSharp";

        private readonly ReflectionTestAccess reflection;
        private readonly CollectionTestAccess collections;
        private readonly MahjongTestDataFactory dataFactory;
        private readonly MahjongTestTypes types;
        private readonly object service;
        private object gameState;
        private object lastSelectedCandidate;

        private RoundLifecycleServiceTestDriver(
            ReflectionTestAccess reflection,
            CollectionTestAccess collections,
            MahjongTestDataFactory dataFactory,
            MahjongTestTypes types,
            object service)
        {
            this.reflection = reflection;
            this.collections = collections;
            this.dataFactory = dataFactory;
            this.types = types;
            this.service = service;
        }

        public static RoundLifecycleServiceTestDriver Create()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            CollectionTestAccess collections = new CollectionTestAccess(reflection);
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            object selector = reflection.CreateInstance(
                reflection.RequireType(WinningCandidateSelectorTypeName));
            object service = reflection.CreateInstance(
                reflection.RequireType(RoundLifecycleServiceTypeName),
                selector);
            return new RoundLifecycleServiceTestDriver(
                reflection,
                collections,
                dataFactory,
                types,
                service);
        }

        public string InitialRoundWindName => WindProgressProperty(
            reflection.Invoke(service, "GetInitialWindProgress"),
            "RoundWind");

        public int InitialHandNumber => (int)reflection.GetProperty(
            reflection.Invoke(service, "GetInitialWindProgress"),
            "HandNumber");

        public object StateToken => gameState;

        public bool IsRoundEnded => Query.IsRoundEnded;

        public bool IsRoundResultPending => Query.IsRoundResultPending;

        public bool IsGameEnded => Query.IsGameEnded;

        public object CurrentRoundResult => Query.CurrentRoundResult;

        public object LastSelectedCandidate => lastSelectedCandidate;

        public bool CurrentRoundResultIsNull => Query.CurrentRoundResultIsNull;

        public void StartRound(string roundWindName, int handNumber, string selfSeatName)
        {
            object windProgress = dataFactory.CreateWindProgress(roundWindName, handNumber);
            gameState = dataFactory.CreateGameStateWithWindProgress(windProgress);
            reflection.Invoke(gameState, "SetSelfSeat", dataFactory.ParseSeat(selfSeatName));
            reflection.Invoke(gameState, "RebuildActiveTurnSeatsFromSeatSlots");
        }

        public object EndRound(string reason)
        {
            object endResult = reflection.Invoke(service, "EndRound", gameState, reason);
            return reflection.GetProperty(endResult, "RoundResult");
        }

        public object EndAbortiveDraw(string kindName)
        {
            object kind = Enum.Parse(
                reflection.RequireType(
                    "MahjongPrototype.Domain.AbortiveDrawKind, Assembly-CSharp"),
                kindName);
            object endResult = reflection.Invoke(
                service,
                "EndAbortiveDraw",
                gameState,
                kind);
            return reflection.GetProperty(endResult, "RoundResult");
        }

        public object EndWinWithSelectedCandidate()
        {
            lastSelectedCandidate = CreateSelectedCandidate();
            object evaluationResult = CreateWinEvaluation(lastSelectedCandidate);
            reflection.Invoke(
                gameState,
                "BeginWinDecisionDetailed",
                dataFactory.ParseSeat("East"),
                dataFactory.ParseWinType("Tsumo"),
                dataFactory.CreateTile("7s"),
                null,
                5,
                evaluationResult);

            return EndRound("Win");
        }

        public object AdvanceFromRoundResult()
        {
            return reflection.Invoke(service, "AdvanceFromRoundResult", gameState);
        }

        public string TransitionType(object transition)
        {
            return reflection.GetProperty(transition, "Type").ToString();
        }

        public string TransitionNextRoundWindName(object transition)
        {
            object windProgress = reflection.GetProperty(transition, "NextWindProgress");
            return WindProgressProperty(windProgress, "RoundWind");
        }

        public int TransitionNextHandNumber(object transition)
        {
            object windProgress = reflection.GetProperty(transition, "NextWindProgress");
            return (int)reflection.GetProperty(windProgress, "HandNumber");
        }

        public string TransitionNextSelfSeatName(object transition)
        {
            return reflection.GetProperty(transition, "NextSelfSeat").ToString();
        }

        public object TransitionRoundResult(object transition)
        {
            return reflection.GetProperty(transition, "RoundResult");
        }

        public string RoundResultTypeName(object result)
        {
            return reflection.GetProperty(result, "Type").ToString();
        }

        public bool RoundResultIsFinalRound(object result)
        {
            return (bool)reflection.GetProperty(result, "IsFinalRound");
        }

        public object RoundResultSelectedCandidate(object result)
        {
            return reflection.GetProperty(result, "SelectedCandidate");
        }

        public string RoundResultAbortiveDrawKindName(object result)
        {
            object kind = reflection.GetProperty(result, "AbortiveDrawKind");
            return kind?.ToString();
        }

        private object CreateSelectedCandidate()
        {
            object sevenPairsAnalysis = reflection.InvokeStatic(
                reflection.RequireType(SevenPairsAnalysisTypeName),
                "Win",
                CreateTileList("1m", "2m", "3m", "4m", "5m", "6m", "7m"));
            object candidate = reflection.InvokeStatic(
                reflection.RequireType(HandEvaluationCandidateTypeName),
                "SevenPairs",
                sevenPairsAnalysis);
            object evaluatedYaku = reflection.CreateInstance(
                reflection.RequireType(EvaluatedYakuTypeName),
                Enum.Parse(types.YakuKind, "Tanyao"),
                "Tanyao",
                Enum.Parse(types.HanValue, "One"),
                false);
            Type evaluatedYakuListType = typeof(List<>).MakeGenericType(
                reflection.RequireType(EvaluatedYakuTypeName));
            IList evaluatedYakus = (IList)reflection.CreateInstance(evaluatedYakuListType);
            evaluatedYakus.Add(evaluatedYaku);
            return reflection.CreateInstance(
                reflection.RequireType(HandEvaluationCandidateResultTypeName),
                candidate,
                evaluatedYakus);
        }

        private object CreateWinEvaluation(object selectedCandidate)
        {
            Type candidateResultType = reflection.RequireType(HandEvaluationCandidateResultTypeName);
            Type candidateResultListType = typeof(List<>).MakeGenericType(candidateResultType);
            IList candidateResults = (IList)reflection.CreateInstance(candidateResultListType);
            candidateResults.Add(selectedCandidate);

            Type evaluatedYakuType = reflection.RequireType(EvaluatedYakuTypeName);
            Type evaluatedYakuListType = typeof(List<>).MakeGenericType(evaluatedYakuType);
            IList emptyYakus = (IList)reflection.CreateInstance(evaluatedYakuListType);
            object handEvaluationResult = reflection.CreateInstance(
                reflection.RequireType(HandEvaluationResultTypeName),
                emptyYakus,
                candidateResults);
            object winCheckResult = reflection.InvokeStatic(
                reflection.RequireType(WinCheckResultTypeName),
                "Win",
                Enum.Parse(
                    reflection.RequireType(WinningHandShapeTypeName),
                    "SevenPairs"));
            return reflection.CreateInstance(
                reflection.RequireType(WinDeclarationEvaluationResultTypeName),
                winCheckResult,
                handEvaluationResult);
        }

        private IList CreateTileList(params string[] tileCodes)
        {
            Type listType = typeof(List<>).MakeGenericType(types.Tile);
            IList tiles = (IList)reflection.CreateInstance(listType);
            for (int i = 0; i < tileCodes.Length; i++)
                tiles.Add(dataFactory.CreateTile(tileCodes[i]));

            return tiles;
        }

        private string WindProgressProperty(object windProgress, string propertyName)
        {
            if (windProgress == null)
                return null;

            Type nullableType = windProgress.GetType();
            if (Nullable.GetUnderlyingType(nullableType) != null)
            {
                bool hasValue = (bool)reflection.GetProperty(windProgress, "HasValue");
                if (!hasValue)
                    return null;

                windProgress = reflection.GetProperty(windProgress, "Value");
            }

            return reflection.GetProperty(windProgress, propertyName).ToString();
        }

        private MahjongGameStateTestQuery Query => MahjongGameStateTestQuery.ForState(
            gameState,
            reflection,
            collections,
            dataFactory);
    }
}
