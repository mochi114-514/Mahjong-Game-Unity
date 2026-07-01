using System;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class YakuEnumTests
    {
        private const string HanValueTypeName = "MahjongPrototype.Domain.HanValue, Assembly-CSharp";
        private const string YakuKindTypeName = "MahjongPrototype.Domain.YakuKind, Assembly-CSharp";

        [Test]
        public void HanValue_NumericValuesMatchHanCounts()
        {
            Type hanValueType = GetHanValueType();

            Assert.That(GetEnumValue(hanValueType, "None"), Is.EqualTo(0));
            Assert.That(GetEnumValue(hanValueType, "One"), Is.EqualTo(1));
            Assert.That(GetEnumValue(hanValueType, "Two"), Is.EqualTo(2));
            Assert.That(GetEnumValue(hanValueType, "Three"), Is.EqualTo(3));
            Assert.That(GetEnumValue(hanValueType, "Four"), Is.EqualTo(4));
            Assert.That(GetEnumValue(hanValueType, "Five"), Is.EqualTo(5));
            Assert.That(GetEnumValue(hanValueType, "Six"), Is.EqualTo(6));
        }

        [Test]
        public void YakuKind_ContainsMajorStandardYaku()
        {
            Type yakuKindType = GetYakuKindType();

            Assert.That(Enum.IsDefined(yakuKindType, "Tanyao"), Is.True);
            Assert.That(Enum.IsDefined(yakuKindType, "SevenPairs"), Is.True);
            Assert.That(Enum.IsDefined(yakuKindType, "KokushiMusou"), Is.True);
            Assert.That(Enum.IsDefined(yakuKindType, "Toitoi"), Is.True);
            Assert.That(Enum.IsDefined(yakuKindType, "Sanankou"), Is.True);
            Assert.That(Enum.IsDefined(yakuKindType, "Suuankou"), Is.True);
            Assert.That(Enum.IsDefined(yakuKindType, "Honitsu"), Is.True);
            Assert.That(Enum.IsDefined(yakuKindType, "Chinitsu"), Is.True);
            Assert.That(Enum.IsDefined(yakuKindType, "YakuhaiWhiteDragon"), Is.True);
            Assert.That(Enum.IsDefined(yakuKindType, "YakuhaiGreenDragon"), Is.True);
            Assert.That(Enum.IsDefined(yakuKindType, "YakuhaiRedDragon"), Is.True);
            Assert.That(Enum.IsDefined(yakuKindType, "YakuhaiSeatWind"), Is.True);
            Assert.That(Enum.IsDefined(yakuKindType, "YakuhaiRoundWind"), Is.True);
        }

        [Test]
        public void YakuKind_DoesNotContainDoraKinds()
        {
            Type yakuKindType = GetYakuKindType();

            Assert.That(Enum.IsDefined(yakuKindType, "Dora"), Is.False);
            Assert.That(Enum.IsDefined(yakuKindType, "AkaDora"), Is.False);
            Assert.That(Enum.IsDefined(yakuKindType, "UraDora"), Is.False);
        }

        private static int GetEnumValue(Type enumType, string name)
        {
            return (int)Enum.Parse(enumType, name);
        }

        private static Type GetHanValueType()
        {
            return Type.GetType(HanValueTypeName, true);
        }

        private static Type GetYakuKindType()
        {
            return Type.GetType(YakuKindTypeName, true);
        }
    }
}
