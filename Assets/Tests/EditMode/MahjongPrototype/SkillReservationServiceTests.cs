using MahjongPrototype.Tests.TestSupport.Features.Skills;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class SkillReservationServiceTests
    {
        [Test]
        public void Reserve_StoresReservationWithOwnerSeat()
        {
            SkillServicesTestDriver driver = SkillServicesTestDriver.Create();
            object reservation = driver.CreateReservation("East", "ForceDrawTile", "1m", "South", 3);

            Assert.That(driver.Reserve(reservation), Is.True);
            Assert.That(driver.HasReservation("East"), Is.True);

            Assert.That(driver.TryConsumeForTurn("East", out object consumed), Is.True);
            Assert.That(driver.ReservationOwnerSeat(consumed), Is.EqualTo("East"));
            Assert.That(driver.ReservationEffectKind(consumed), Is.EqualTo("ForceDrawTile"));
            Assert.That(driver.ReservationTargetTile(consumed), Is.EqualTo("1m"));
            Assert.That(driver.ReservationReservedOnTurnSeat(consumed), Is.EqualTo("South"));
            Assert.That(driver.ReservationTurnIndex(consumed), Is.EqualTo(3));
        }

        [Test]
        public void TryConsumeForTurn_ConsumesOnlyMatchingSeat()
        {
            SkillServicesTestDriver driver = SkillServicesTestDriver.Create();
            object reservation = driver.CreateReservation("East", "ForceDrawTile", "2m", "South", 4);
            driver.Reserve(reservation);

            object southReservation;
            Assert.That(driver.TryConsumeForTurn("South", out southReservation), Is.False);
            Assert.That(driver.HasReservation("East"), Is.True);

            object eastReservation;
            Assert.That(driver.TryConsumeForTurn("East", out eastReservation), Is.True);
            Assert.That(driver.HasReservation("East"), Is.False);
        }
    }
}
