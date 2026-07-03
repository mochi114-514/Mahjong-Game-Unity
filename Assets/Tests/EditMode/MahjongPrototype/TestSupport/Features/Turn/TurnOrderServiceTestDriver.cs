using System;
using System.Collections;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;

namespace MahjongPrototype.Tests.TestSupport.Features.Turn
{
    internal sealed class TurnOrderServiceTestDriver
    {
        private const string TurnOrderServiceTypeName =
            "MahjongPrototype.Services.TurnOrderService, Assembly-CSharp";

        private readonly ReflectionTestAccess reflection;
        private readonly MahjongTestTypes types;
        private readonly MahjongTestDataFactory dataFactory;
        private readonly object service;

        private TurnOrderServiceTestDriver(
            ReflectionTestAccess reflection,
            MahjongTestTypes types,
            MahjongTestDataFactory dataFactory,
            object service)
        {
            this.reflection = reflection;
            this.types = types;
            this.dataFactory = dataFactory;
            this.service = service;
        }

        public static TurnOrderServiceTestDriver Create()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            object service = reflection.CreateInstance(reflection.RequireType(TurnOrderServiceTypeName));
            return new TurnOrderServiceTestDriver(reflection, types, dataFactory, service);
        }

        public string GetNextSeat(string currentTurnName, params string[] activeSeatNames)
        {
            object result = reflection.Invoke(
                service,
                "GetNextSeat",
                CreateSeatList(activeSeatNames),
                dataFactory.ParseSeat(currentTurnName));
            return result.ToString();
        }

        public string GetNextSeatWithNullActiveSeats(string currentTurnName)
        {
            object result = reflection.Invoke(
                service,
                "GetNextSeat",
                null,
                dataFactory.ParseSeat(currentTurnName));
            return result.ToString();
        }

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

