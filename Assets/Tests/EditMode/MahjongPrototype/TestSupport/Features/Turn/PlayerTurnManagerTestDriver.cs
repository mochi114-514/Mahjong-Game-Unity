using System;
using System.Collections;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;

namespace MahjongPrototype.Tests.TestSupport.Features.Turn
{
    internal sealed class PlayerTurnManagerTestDriver
    {
        private const string TurnOrderServiceTypeName =
            "MahjongPrototype.Services.TurnOrderService, Assembly-CSharp";
        private const string PlayerTurnManagerTypeName =
            "MahjongPrototype.Services.PlayerTurnManager, Assembly-CSharp";

        private readonly ReflectionTestAccess reflection;
        private readonly MahjongTestTypes types;
        private readonly MahjongTestDataFactory dataFactory;
        private readonly object gameState;
        private readonly object manager;

        private PlayerTurnManagerTestDriver(
            ReflectionTestAccess reflection,
            MahjongTestTypes types,
            MahjongTestDataFactory dataFactory,
            object gameState,
            object manager)
        {
            this.reflection = reflection;
            this.types = types;
            this.dataFactory = dataFactory;
            this.gameState = gameState;
            this.manager = manager;
        }

        public static PlayerTurnManagerTestDriver Create(params string[] occupiedSeatNames)
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            object manager = reflection.CreateInstance(
                reflection.RequireType(PlayerTurnManagerTypeName),
                reflection.CreateInstance(reflection.RequireType(TurnOrderServiceTypeName)));

            return new PlayerTurnManagerTestDriver(
                reflection,
                types,
                dataFactory,
                dataFactory.CreateGameState(occupiedSeatNames),
                manager);
        }

        public void InitializeRound(string firstSeatName)
        {
            reflection.Invoke(manager, "InitializeRound", gameState, dataFactory.ParseSeat(firstSeatName));
        }

        public string EndTurnAndSelectNext(params string[] activeSeatNames)
        {
            object nextSeat = reflection.Invoke(
                manager,
                "EndTurnAndSelectNext",
                gameState,
                CreateSeatList(activeSeatNames));
            return nextSeat.ToString();
        }

        public string CurrentTurnName =>
            reflection.GetProperty(gameState, "CurrentTurn").ToString();

        public int TurnIndex =>
            (int)reflection.GetProperty(gameState, "TurnIndex");

        public string TurnPhaseName =>
            reflection.GetProperty(gameState, "TurnPhase").ToString();

        private IList CreateSeatList(params string[] seatNames)
        {
            Type listType = typeof(System.Collections.Generic.List<>).MakeGenericType(types.SeatId);
            IList list = (IList)reflection.CreateInstance(listType);

            for (int i = 0; i < seatNames.Length; i++)
                list.Add(dataFactory.ParseSeat(seatNames[i]));

            return list;
        }
    }
}
