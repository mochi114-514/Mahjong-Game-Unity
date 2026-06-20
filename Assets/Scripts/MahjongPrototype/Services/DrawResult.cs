using MahjongPrototype.Domain;
using MahjongPrototype.Skills;

namespace MahjongPrototype.Services
{
    public readonly struct DrawResult
    {
        public DrawResult(
            bool success,
            SeatId seat,
            Tile tile,
            DrawPurpose purpose,
            DrawSource source,
            int wallCountAfterDraw,
            ActiveSkillEffect resolvedSkillEffect,
            bool skillWasPresent,
            bool skillApplied,
            string message)
        {
            Success = success;
            Seat = seat;
            Tile = tile;
            Purpose = purpose;
            Source = source;
            WallCountAfterDraw = wallCountAfterDraw;
            ResolvedSkillEffect = resolvedSkillEffect;
            SkillWasPresent = skillWasPresent;
            SkillApplied = skillApplied;
            Message = message ?? string.Empty;
        }

        public bool Success { get; }
        public SeatId Seat { get; }
        public Tile Tile { get; }
        public DrawPurpose Purpose { get; }
        public DrawSource Source { get; }
        public int WallCountAfterDraw { get; }
        public ActiveSkillEffect ResolvedSkillEffect { get; }
        public bool SkillWasPresent { get; }
        public bool SkillApplied { get; }
        public string Message { get; }
    }
}
