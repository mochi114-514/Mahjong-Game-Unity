namespace MahjongPrototype.Skills
{
    public readonly struct SkillActivationResult
    {
        private SkillActivationResult(bool success, ActiveSkillEffect effect, string reason)
        {
            Success = success;
            Effect = effect;
            Reason = reason ?? string.Empty;
        }

        public bool Success { get; }
        public ActiveSkillEffect Effect { get; }
        public string Reason { get; }

        public static SkillActivationResult Activated(ActiveSkillEffect effect)
        {
            return new SkillActivationResult(true, effect, string.Empty);
        }

        public static SkillActivationResult Failed(string reason)
        {
            return new SkillActivationResult(false, null, reason);
        }
    }
}
