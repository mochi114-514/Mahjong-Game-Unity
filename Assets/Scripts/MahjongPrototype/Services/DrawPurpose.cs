namespace MahjongPrototype.Services
{
    public enum DrawPurpose
    {
        InitialDeal = 0,
        TurnDraw = 1
    }

    public enum DrawSource
    {
        None = 0,
        Normal = 1,
        InitialDeal = 2,
        SkillModified = 3,
        SkillFallbackNormal = 4
    }
}
