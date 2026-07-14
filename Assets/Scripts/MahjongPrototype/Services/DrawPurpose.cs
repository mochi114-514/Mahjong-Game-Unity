namespace MahjongPrototype.Services
{
    public enum DrawPurpose
    {
        InitialDeal = 0,
        TurnDraw = 1,
        RinshanDraw = 2
    }

    public enum DrawSource
    {
        None = 0,
        Normal = 1,
        InitialDeal = 2,
        SkillModified = 3,
        SkillFallbackNormal = 4,
        Rinshan = 5
    }
}
