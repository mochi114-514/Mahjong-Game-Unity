using System;
using System.Collections;
using System.Collections.Generic;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;
using NUnit.Framework;

namespace MahjongPrototype.Tests.TestSupport.Features.Win
{
    internal sealed class WinningTileCandidateEvaluatorTestDriver
    {
        private const string EvaluatorTypeName =
            "MahjongPrototype.Services.WinningTileCandidateEvaluator, Assembly-CSharp";
        private const string CounterTypeName =
            "MahjongPrototype.Services.VisibleRemainingTileCounter, Assembly-CSharp";
        private const string ReachCheckerTypeName =
            "MahjongPrototype.Services.ReachChecker, Assembly-CSharp";
        private const string ReachCandidateTypeName =
            "MahjongPrototype.Services.ReachDiscardCandidate, Assembly-CSharp";
        private const string PlayerMeldTypeName =
            "MahjongPrototype.Domain.PlayerMeld, Assembly-CSharp";
        private const string DiscardSourceTypeName =
            "MahjongPrototype.Domain.DiscardSource, Assembly-CSharp";

        private readonly ReflectionTestAccess reflection;
        private readonly CollectionTestAccess collections;
        private readonly MahjongTestDataFactory dataFactory;
        private readonly object evaluator;
        private readonly object counter;

        private WinningTileCandidateEvaluatorTestDriver(
            ReflectionTestAccess reflection,
            CollectionTestAccess collections,
            MahjongTestDataFactory dataFactory,
            object evaluator,
            object counter)
        {
            this.reflection = reflection;
            this.collections = collections;
            this.dataFactory = dataFactory;
            this.evaluator = evaluator;
            this.counter = counter;
        }

        public static WinningTileCandidateEvaluatorTestDriver Create()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            CollectionTestAccess collections = new CollectionTestAccess(reflection);
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            object evaluator = reflection.CreateInstance(
                reflection.RequireType(EvaluatorTypeName));
            object counter = reflection.CreateInstance(
                reflection.RequireType(CounterTypeName));
            return new WinningTileCandidateEvaluatorTestDriver(
                reflection,
                collections,
                dataFactory,
                evaluator,
                counter);
        }

        public object CreateGameState(int seed = 12345)
        {
            Type stateType = reflection.RequireType(
                "MahjongPrototype.Domain.MahjongGameState, Assembly-CSharp");
            object state = reflection.CreateInstance(stateType, dataFactory.CreateWall(seed));
            dataFactory.AssignPlayersToSeats(
                state,
                new[] { "East", "South", "West", "North" });
            return state;
        }

        public void AddHand(object gameState, string seatName, string handText)
        {
            dataFactory.AddHandTilesFromText(
                dataFactory.GetPlayerSeat(gameState, seatName),
                handText);
        }

        public void SetDrawnTile(object gameState, string seatName, string tileCode)
        {
            dataFactory.SetDrawnTile(gameState, seatName, tileCode);
        }

        public object EvaluateCurrent(object gameState)
        {
            return reflection.Invoke(evaluator, "EvaluateCurrentHand", gameState);
        }

        public object EvaluateAfterDiscard(object gameState, object discardCandidate)
        {
            return reflection.Invoke(
                evaluator,
                "EvaluateAfterDiscard",
                gameState,
                discardCandidate);
        }

        public object EvaluateReachCandidates(object gameState, object discardCandidates)
        {
            return reflection.Invoke(
                evaluator,
                "EvaluateReachCandidates",
                gameState,
                discardCandidates);
        }

        public object GroupReachCandidates(object gameState, object discardCandidates)
        {
            return reflection.Invoke(
                evaluator,
                "GroupReachCandidates",
                gameState,
                discardCandidates);
        }

        public object CheckReach(object gameState, string handText, string drawnTileCode)
        {
            object reachChecker = reflection.CreateInstance(
                reflection.RequireType(ReachCheckerTypeName));
            return reflection.Invoke(
                reachChecker,
                "CheckReach",
                dataFactory.CreateTileArrayFromText(handText),
                dataFactory.CreateTile(drawnTileCode));
        }

        public object ReachCandidates(object reachResult)
        {
            return reflection.GetProperty(reachResult, "Candidates");
        }

        public object CreateReachCandidate(
            string sourceName,
            int handIndex,
            string tileCode)
        {
            Type sourceType = reflection.RequireType(DiscardSourceTypeName);
            return reflection.CreateInstance(
                reflection.RequireType(ReachCandidateTypeName),
                Enum.Parse(sourceType, sourceName),
                handIndex,
                dataFactory.CreateTile(tileCode));
        }

        public object CreateReachCandidateList(params object[] candidates)
        {
            Type candidateType = reflection.RequireType(ReachCandidateTypeName);
            Type listType = typeof(List<>).MakeGenericType(candidateType);
            IList list = (IList)reflection.CreateInstance(listType);
            for (int i = 0; i < candidates.Length; i++)
                list.Add(candidates[i]);

            return list;
        }

        public object AddDiscard(
            object gameState,
            string seatName,
            string tileCode,
            int turnIndex)
        {
            object record = dataFactory.CreateDiscardRecord(seatName, tileCode, turnIndex);
            return reflection.Invoke(gameState, "AddDiscard", record);
        }

        public object AddPonMeld(
            object gameState,
            string ownerSeatName,
            string sourceSeatName,
            string tileCode,
            object sourceDiscard,
            bool markClaimed = true)
        {
            return AddOpenMeld(
                gameState,
                "CreatePon",
                ownerSeatName,
                sourceSeatName,
                string.Join(" ", tileCode, tileCode, tileCode),
                tileCode,
                sourceDiscard,
                markClaimed);
        }

        public object AddChiMeld(
            object gameState,
            string ownerSeatName,
            string sourceSeatName,
            string meldTiles,
            string acquiredTileCode,
            object sourceDiscard,
            bool markClaimed = true)
        {
            return AddOpenMeld(
                gameState,
                "CreateChi",
                ownerSeatName,
                sourceSeatName,
                meldTiles,
                acquiredTileCode,
                sourceDiscard,
                markClaimed);
        }

        public int CountVisibleRemaining(
            object gameState,
            string localSeatName,
            string tileCode)
        {
            return (int)reflection.Invoke(
                counter,
                "CountVisibleRemaining",
                gameState,
                dataFactory.ParseSeat(localSeatName),
                dataFactory.CreateTile(tileCode));
        }

        public string[] TileCodes(object winningCandidates)
        {
            int count = collections.Count(winningCandidates);
            string[] codes = new string[count];
            for (int i = 0; i < count; i++)
            {
                object candidate = collections.Item(winningCandidates, i);
                codes[i] = reflection.GetProperty(candidate, "Tile").ToString();
            }

            return codes;
        }

        public int RemainingCount(object winningCandidates, string tileCode)
        {
            int count = collections.Count(winningCandidates);
            for (int i = 0; i < count; i++)
            {
                object candidate = collections.Item(winningCandidates, i);
                if (reflection.GetProperty(candidate, "Tile").ToString() == tileCode)
                {
                    return (int)reflection.GetProperty(
                        candidate,
                        "VisibleRemainingCount");
                }
            }

            Assert.Fail($"Winning candidate not found: {tileCode}");
            return -1;
        }

        public object FindReachEvaluation(
            object evaluations,
            string sourceName,
            string discardTileCode)
        {
            int count = collections.Count(evaluations);
            for (int i = 0; i < count; i++)
            {
                object evaluation = collections.Item(evaluations, i);
                object candidate = reflection.GetProperty(
                    evaluation,
                    "DiscardCandidate");
                if (reflection.GetProperty(candidate, "Source").ToString() == sourceName &&
                    reflection.GetProperty(candidate, "Tile").ToString() == discardTileCode)
                {
                    return evaluation;
                }
            }

            return null;
        }

        public object WinningTiles(object evaluationOrGroup)
        {
            return reflection.GetProperty(evaluationOrGroup, "WinningTiles");
        }

        public int Count(object collection)
        {
            return collections.Count(collection);
        }

        public int GroupDiscardCandidateCount(object group)
        {
            return collections.Count(reflection.GetProperty(group, "DiscardCandidates"));
        }

        public object Item(object collection, int index)
        {
            return collections.Item(collection, index);
        }

        private object AddOpenMeld(
            object gameState,
            string factoryMethod,
            string ownerSeatName,
            string sourceSeatName,
            string meldTiles,
            string acquiredTileCode,
            object sourceDiscard,
            bool markClaimed)
        {
            int discardId = (int)reflection.GetProperty(sourceDiscard, "Id");
            Type playerMeldType = reflection.RequireType(PlayerMeldTypeName);
            object meld = reflection.InvokeStatic(
                playerMeldType,
                factoryMethod,
                dataFactory.CreateTileArrayFromText(meldTiles),
                dataFactory.ParseSeat(ownerSeatName),
                dataFactory.ParseSeat(sourceSeatName),
                dataFactory.CreateTile(acquiredTileCode),
                discardId);

            if (markClaimed)
                Assert.That(reflection.Invoke(gameState, "TryClaimDiscard", meld), Is.True);

            reflection.Invoke(
                dataFactory.GetPlayerSeat(gameState, ownerSeatName),
                "AddMeld",
                meld);
            return meld;
        }
    }
}
