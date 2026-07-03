using System;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;

namespace MahjongPrototype.Tests.TestSupport.Features.Yaku
{
    internal sealed class YakuEnumTestDriver
    {
        private readonly MahjongTestTypes types;

        private YakuEnumTestDriver(MahjongTestTypes types)
        {
            this.types = types;
        }

        public static YakuEnumTestDriver Create()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            return new YakuEnumTestDriver(types);
        }

        public int HanValue(string name)
        {
            return (int)Enum.Parse(types.HanValue, name);
        }

        public bool IsYakuDefined(string name)
        {
            return Enum.IsDefined(types.YakuKind, name);
        }
    }
}

