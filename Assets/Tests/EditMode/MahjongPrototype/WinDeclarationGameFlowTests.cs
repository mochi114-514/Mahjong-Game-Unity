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
                object candidate = FindCandidateContainingYaku(driver, evaluation, "MenzenTsumo");
                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(1));
                Assert.That(driver.TotalHan(evaluation), Is.EqualTo(0));
            }
        }

        [Test]
        public void PinfuOnlyWinningShape_BeginsWinDecisionAndDoesNotShowNoYakuTenpai()
        {
            using (WinDeclarationGameFlowTestDriver driver =
                WinDeclarationGameFlowTestDriver.CreateWithRegisteredYaku("Pinfu", "One", "None"))
            {
                object tenpaiEvaluation = driver.EvaluateBasicPinfuTenpai();

                Assert.That(driver.NoYakuTenpaiIsEvaluated(tenpaiEvaluation), Is.True);
                Assert.That(driver.NoYakuTenpaiIsTenpai(tenpaiEvaluation), Is.True);
                Assert.That(driver.NoYakuTenpaiHasAnyYakuWait(tenpaiEvaluation), Is.True);
                Assert.That(driver.NoYakuTenpaiShouldShowZeroHanTenpai(tenpaiEvaluation), Is.False);

                driver.DrawBasicPinfuTsumoShape();
                object evaluation = driver.PendingWinDeclarationEvaluation;
                object candidate = FindCandidateContainingYaku(driver, evaluation, "Pinfu");

                Assert.That(driver.IsWinDecisionPending, Is.True);
                Assert.That(evaluation, Is.Not.Null);
                Assert.That(driver.CanDeclareWin(evaluation), Is.True);
                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(1));
            }
        }

        private static object FindCandidateContainingYaku(
            WinDeclarationGameFlowTestDriver driver,
            object evaluation,
            string yakuKindName)
        {
            for (int i = 0; i < driver.CandidateResultCount(evaluation); i++)
            {
                object candidate = driver.CandidateResultAt(evaluation, i);
                if (driver.CandidateContainsYaku(candidate, yakuKindName))
                    return candidate;
            }

            return null;
        }
    }
}
