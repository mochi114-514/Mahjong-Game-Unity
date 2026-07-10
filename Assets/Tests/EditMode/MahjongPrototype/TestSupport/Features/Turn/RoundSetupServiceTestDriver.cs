using System.Collections;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;

namespace MahjongPrototype.Tests.TestSupport.Features.Turn
{
    internal sealed class RoundSetupServiceTestDriver
    {
        private const string RoundSetupServiceTypeName =
            "MahjongPrototype.Services.RoundSetupService, Assembly-CSharp";

        private readonly ReflectionTestAccess reflection;
        private readonly CollectionTestAccess collections;
        private readonly MahjongTestDataFactory dataFactory;
        private readonly object service;
        private object gameState;

        private RoundSetupServiceTestDriver(
            ReflectionTestAccess reflection,
            CollectionTestAccess collections,
            MahjongTestDataFactory dataFactory,
            object service)
        {
            this.reflection = reflection;
            this.collections = collections;
            this.dataFactory = dataFactory;
            this.service = service;
        }

        public static RoundSetupServiceTestDriver Create()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            CollectionTestAccess collections = new CollectionTestAccess(reflection);
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            object service = reflection.CreateInstance(
                reflection.RequireType(RoundSetupServiceTypeName),
                reflection.CreateInstance(reflection.RequireType(
                    "MahjongPrototype.Services.RoundStartingSeatResolver, Assembly-CSharp")),
                reflection.CreateInstance(
                    reflection.RequireType("MahjongPrototype.Services.PlayerTurnManager, Assembly-CSharp"),
                    reflection.CreateInstance(reflection.RequireType(
                        "MahjongPrototype.Services.TurnOrderService, Assembly-CSharp"))),
                reflection.CreateInstance(reflection.RequireType(
                    "MahjongPrototype.Services.DrawService, Assembly-CSharp")));
            return new RoundSetupServiceTestDriver(
                reflection,
                collections,
                dataFactory,
                service);
        }

        public string SetupRound(int participantCount, string selfSeatName)
        {
            object result = reflection.Invoke(
                service,
                "SetupRound",
                dataFactory.CreateWindProgress("East", 1),
                null,
                dataFactory.ParseSeat(selfSeatName),
                participantCount);
            gameState = reflection.GetProperty(result, "GameState");
            return reflection.GetProperty(result, "StartingSeat").ToString();
        }

        public bool DealInitialHands(int initialHandTileCount)
        {
            object result = reflection.Invoke(
                service,
                "DealInitialHands",
                gameState,
                initialHandTileCount);
            return (bool)reflection.GetProperty(result, "Success");
        }

        public void ClearWall()
        {
            object wall = reflection.GetProperty(gameState, "Wall");
            ((IList)reflection.GetPrivateField(wall, "tiles")).Clear();
        }

        public string SeatByPlayerId(string playerIdName) =>
            Query.SeatByPlayerIdName(playerIdName);

        public string[] ActiveTurnSeatNames => Query.ActiveTurnSeatNames;

        public string CurrentTurnName => Query.CurrentTurnName;

        public int TurnIndex => Query.TurnIndex;

        public int WallCount => Query.WallCount;

        public int HandCount(string seatName)
        {
            object hand = reflection.GetProperty(Query.GetPlayerSeat(seatName), "Hand");
            return collections.Count(hand);
        }

        private MahjongGameStateTestQuery Query => MahjongGameStateTestQuery.ForState(
            gameState,
            reflection,
            collections,
            dataFactory);
    }
}
