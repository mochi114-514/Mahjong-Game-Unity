namespace MahjongPrototype.Domain
{
    public enum RoundResultType
    {
        Win = 1,
        ExhaustiveDraw = 2,
        AbortiveDraw = 3,
        NagashiMangan = 4
    }

    public enum AbortiveDrawKind
    {
        NineTerminalsAndHonors = 1,
        FourWinds = 2,
        FourReaches = 3,
        FourKans = 4
    }
}
