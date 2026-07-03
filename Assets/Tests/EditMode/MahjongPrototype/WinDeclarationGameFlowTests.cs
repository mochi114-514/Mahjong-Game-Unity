using MahjongPrototype.Tests.TestSupport.Features.Win;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class WinDeclarationGameFlowTests
    {
        [Test]
        public void WinningShapeWithoutYakuCatalog_DoesNotBeginWinDecision()
        {
            using (WinDeclarationGameFlowTestDriver driver =
                WinDeclarationGameFlowTestDriver.CreateWithoutYakuCatalog())
            {
                driver.DrawStandardClosedTsumoShape();

                Assert.That(driver.IsWinDecisionPending, Is.False);
                Assert.That(driver.PendingWinDeclarationEvaluation, Is.Null);
            }
        }

        [Test]
        public void WinningShapeWithRegisteredYaku_BeginsWinDecisionAndStoresEvaluation()
        {
            using (WinDeclarationGameFlowTestDriver driver =
                WinDeclarationGameFlowTestDriver.CreateWithRegisteredYaku("MenzenTsumo", "One", "None"))
            {
                driver.DrawStandardClosedTsumoShape();
                object evaluation = driver.PendingWinDeclarationEvaluation;

                Assert.That(driver.IsWinDecisionPending, Is.True);
                Assert.That(evaluation, Is.Not.Null);
                Assert.That(driver.CanDeclareWin(evaluation), Is.True);
                Assert.That(driver.TotalHan(evaluation), Is.EqualTo(1));
            }
        }
    }
}
