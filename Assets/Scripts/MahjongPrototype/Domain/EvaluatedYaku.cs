namespace MahjongPrototype.Domain
{
    public readonly struct EvaluatedYaku
    {
        public EvaluatedYaku(YakuKind kind, string displayName, HanValue han, bool isYakuman)
        {
            Kind = kind;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? kind.ToString() : displayName;
            Han = han;
            IsYakuman = isYakuman;
        }

        public YakuKind Kind { get; }
        public string DisplayName { get; }
        public HanValue Han { get; }
        public bool IsYakuman { get; }
    }
}
