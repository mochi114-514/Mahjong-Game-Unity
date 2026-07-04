namespace MahjongPrototype.Tests.TestSupport.Mahjong
{
    internal static class MahjongTestCatalogFactory
    {
        public static object CreateStandardGameFlowYakuCatalog(
            MahjongTestDataFactory dataFactory)
        {
            return dataFactory.CreateYakuCatalog(
                dataFactory.CreateYakuDefinition("MenzenTsumo", "One", "None"),
                dataFactory.CreateYakuDefinition("Reach", "One", "None"),
                dataFactory.CreateYakuDefinition("Tanyao", "One", "One"));
        }
    }
}
