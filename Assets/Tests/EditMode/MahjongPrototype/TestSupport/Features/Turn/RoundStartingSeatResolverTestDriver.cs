using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;

namespace MahjongPrototype.Tests.TestSupport.Features.Turn
{
    internal sealed class RoundStartingSeatResolverTestDriver
    {
        private const string RoundStartingSeatResolverTypeName =
            "MahjongPrototype.Services.RoundStartingSeatResolver, Assembly-CSharp";

        private readonly ReflectionTestAccess reflection;
        private readonly MahjongTestTypes types;
        private readonly MahjongTestDataFactory dataFactory;
        private readonly object resolver;

        private RoundStartingSeatResolverTestDriver(
            ReflectionTestAccess reflection,
            MahjongTestTypes types,
            MahjongTestDataFactory dataFactory,
            object resolver)
        {
            this.reflection = reflection;
            this.types = types;
            this.dataFactory = dataFactory;
            this.resolver = resolver;
        }

        public static RoundStartingSeatResolverTestDriver Create()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            object resolver = reflection.CreateInstance(
                reflection.RequireType(RoundStartingSeatResolverTypeName));
            return new RoundStartingSeatResolverTestDriver(reflection, types, dataFactory, resolver);
        }

        public string Resolve(params string[] activeSeatNames)
        {
            object result = reflection.Invoke(resolver, "Resolve", CreateSeatList(activeSeatNames));
            return result.ToString();
        }

        public Exception ResolveEmptyException()
        {
            return CaptureException(CreateSeatList());
        }

        public Exception ResolveNullException()
        {
            return CaptureException(null);
        }

        private Exception CaptureException(object activeTurnSeats)
        {
            try
            {
                reflection.Invoke(resolver, "Resolve", activeTurnSeats);
                return null;
            }
            catch (TargetInvocationException exception)
            {
                return exception.InnerException;
            }
        }

        private IList CreateSeatList(params string[] seatNames)
        {
            Type listType = typeof(List<>).MakeGenericType(types.SeatId);
            IList list = (IList)reflection.CreateInstance(listType);

            for (int i = 0; i < seatNames.Length; i++)
                list.Add(dataFactory.ParseSeat(seatNames[i]));

            return list;
        }
    }
}
