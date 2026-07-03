using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;

namespace MahjongPrototype.Tests.TestSupport.Features.ViewSlot
{
    internal sealed class SeatToViewSlotResolverTestDriver
    {
        private const string ResolverTypeName =
            "MahjongPrototype.UI.SeatToViewSlotResolver, Assembly-CSharp";

        private readonly ReflectionTestAccess reflection;
        private readonly MahjongTestDataFactory dataFactory;
        private readonly System.Type resolverType;

        private SeatToViewSlotResolverTestDriver(
            ReflectionTestAccess reflection,
            MahjongTestDataFactory dataFactory,
            System.Type resolverType)
        {
            this.reflection = reflection;
            this.dataFactory = dataFactory;
            this.resolverType = resolverType;
        }

        public static SeatToViewSlotResolverTestDriver Create()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            return new SeatToViewSlotResolverTestDriver(
                reflection,
                dataFactory,
                reflection.RequireType(ResolverTypeName));
        }

        public string Resolve(string selfSeatName, string targetSeatName)
        {
            object resolved = reflection.InvokeStatic(
                resolverType,
                "Resolve",
                dataFactory.ParseSeat(selfSeatName),
                dataFactory.ParseSeat(targetSeatName));
            return resolved.ToString();
        }
    }
}

