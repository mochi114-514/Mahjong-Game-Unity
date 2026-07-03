using MahjongPrototype.Tests.TestSupport.Features.Retry;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class MahjongGameFlowRetryTests
    {
        [Test]
        public void RetryPrototype_ClearsDiscards()
        {
            using (GameFlowRetryTestDriver driver =
                GameFlowRetryTestDriver.CreateDiscardResetScenario())
            {
                driver.StartRound();

                driver.RequestDraw();
                driver.RequestDiscard(0);
                Assert.That(driver.DiscardCount, Is.EqualTo(1));

                driver.Retry();

                Assert.That(driver.DiscardCount, Is.EqualTo(0));
            }
        }
    }
}

