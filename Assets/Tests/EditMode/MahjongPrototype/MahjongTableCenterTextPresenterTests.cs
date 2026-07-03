using MahjongPrototype.Tests.TestSupport.Features.TableCenterText;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class MahjongTableCenterTextPresenterTests
    {
        [Test]
        public void TableCenterTextPresenter_RefreshShowsWindProgress()
        {
            using (MahjongTableCenterTextPresenterTestDriver driver =
                MahjongTableCenterTextPresenterTestDriver.Create())
            {
                driver.Refresh("South", 3);

                Assert.That(driver.WindProgressText, Is.EqualTo("南三局"));
            }
        }

        [Test]
        public void TableCenterTextPresenter_ClearShowsDashForWindProgress()
        {
            using (MahjongTableCenterTextPresenterTestDriver driver =
                MahjongTableCenterTextPresenterTestDriver.Create())
            {
                driver.RefreshNull();

                Assert.That(driver.WindProgressText, Is.EqualTo("-"));
            }
        }
    }
}
