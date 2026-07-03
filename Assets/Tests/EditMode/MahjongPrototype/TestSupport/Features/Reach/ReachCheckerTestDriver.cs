using System;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;

namespace MahjongPrototype.Tests.TestSupport.Features.Reach
{
    internal sealed class ReachCheckerTestDriver
    {
        private const string ReachCheckerTypeName =
            "MahjongPrototype.Services.ReachChecker, Assembly-CSharp";

        private readonly ReflectionTestAccess reflection;
        private readonly CollectionTestAccess collections;
        private readonly MahjongTestDataFactory dataFactory;
        private readonly object reachChecker;

        private ReachCheckerTestDriver(
            ReflectionTestAccess reflection,
            CollectionTestAccess collections,
            MahjongTestDataFactory dataFactory,
            object reachChecker)
        {
            this.reflection = reflection;
            this.collections = collections;
            this.dataFactory = dataFactory;
            this.reachChecker = reachChecker;
        }

        public static ReachCheckerTestDriver Create()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            CollectionTestAccess collections = new CollectionTestAccess(reflection);
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            object reachChecker = reflection.CreateInstance(
                reflection.RequireType(ReachCheckerTypeName));
            return new ReachCheckerTestDriver(
                reflection,
                collections,
                dataFactory,
                reachChecker);
        }

        public object CheckReach(string handText, string drawnTileCode)
        {
            return reflection.Invoke(
                reachChecker,
                "CheckReach",
                CreateTiles(handText),
                dataFactory.CreateTile(drawnTileCode));
        }

        public object CheckReachWithInvalidDrawnTile(string handText)
        {
            return reflection.Invoke(
                reachChecker,
                "CheckReach",
                CreateTiles(handText),
                dataFactory.CreateInvalidTile());
        }

        public bool CanReach(object result)
        {
            return (bool)reflection.GetProperty(result, "CanReach");
        }

        public int CandidateCount(object result)
        {
            return collections.Count(Candidates(result));
        }

        public object FindCandidate(object result, string sourceName, string tileCode)
        {
            object candidates = Candidates(result);
            int count = collections.Count(candidates);

            for (int i = 0; i < count; i++)
            {
                object candidate = collections.Item(candidates, i);
                if (reflection.GetProperty(candidate, "Source").ToString() == sourceName &&
                    reflection.GetProperty(candidate, "Tile").ToString() == tileCode)
                {
                    return candidate;
                }
            }

            return null;
        }

        public int CandidateHandIndex(object candidate)
        {
            return (int)reflection.GetProperty(candidate, "HandIndex");
        }

        private object CreateTiles(string handText)
        {
            return dataFactory.CreateTileArray(SplitCodes(handText));
        }

        private object Candidates(object result)
        {
            return reflection.GetProperty(result, "Candidates");
        }

        private static string[] SplitCodes(string handText)
        {
            return handText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
