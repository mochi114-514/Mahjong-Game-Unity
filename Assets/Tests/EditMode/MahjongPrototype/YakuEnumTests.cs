using MahjongPrototype.Tests.TestSupport.Features.Yaku;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class YakuEnumTests
    {
        [Test]
        public void HanValue_NumericValuesMatchHanCounts()
        {
            YakuEnumTestDriver driver = YakuEnumTestDriver.Create();

            Assert.That(driver.HanValue("None"), Is.EqualTo(0));
            Assert.That(driver.HanValue("One"), Is.EqualTo(1));
            Assert.That(driver.HanValue("Two"), Is.EqualTo(2));
            Assert.That(driver.HanValue("Three"), Is.EqualTo(3));
            Assert.That(driver.HanValue("Four"), Is.EqualTo(4));
            Assert.That(driver.HanValue("Five"), Is.EqualTo(5));
            Assert.That(driver.HanValue("Six"), Is.EqualTo(6));
        }

        [Test]
        public void YakuKind_ContainsMajorStandardYaku()
        {
            YakuEnumTestDriver driver = YakuEnumTestDriver.Create();

            Assert.That(driver.IsYakuDefined("Tanyao"), Is.True);
            Assert.That(driver.IsYakuDefined("SevenPairs"), Is.True);
            Assert.That(driver.IsYakuDefined("KokushiMusou"), Is.True);
            Assert.That(driver.IsYakuDefined("Toitoi"), Is.True);
            Assert.That(driver.IsYakuDefined("Sanankou"), Is.True);
            Assert.That(driver.IsYakuDefined("Suuankou"), Is.True);
            Assert.That(driver.IsYakuDefined("Honitsu"), Is.True);
            Assert.That(driver.IsYakuDefined("Chinitsu"), Is.True);
            Assert.That(driver.IsYakuDefined("YakuhaiWhiteDragon"), Is.True);
            Assert.That(driver.IsYakuDefined("YakuhaiGreenDragon"), Is.True);
            Assert.That(driver.IsYakuDefined("YakuhaiRedDragon"), Is.True);
            Assert.That(driver.IsYakuDefined("YakuhaiSeatWind"), Is.True);
            Assert.That(driver.IsYakuDefined("YakuhaiRoundWind"), Is.True);
        }

        [Test]
        public void YakuKind_DoesNotContainDoraKinds()
        {
            YakuEnumTestDriver driver = YakuEnumTestDriver.Create();

            Assert.That(driver.IsYakuDefined("Dora"), Is.False);
            Assert.That(driver.IsYakuDefined("AkaDora"), Is.False);
            Assert.That(driver.IsYakuDefined("UraDora"), Is.False);
        }
    }
}
