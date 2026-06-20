using MahjongPrototype.Domain;
using MahjongPrototype.Skills;

namespace MahjongPrototype.Services
{
    public sealed class DrawService
    {
        public DrawResult DrawTile(SeatId seat, MahjongGameState gameState, DrawPurpose purpose)
        {
            if (gameState == null)
            {
                return new DrawResult(
                    false,
                    seat,
                    default,
                    purpose,
                    DrawSource.None,
                    0,
                    null,
                    false,
                    false,
                    "GameState is not available.");
            }

            ActiveSkillEffect effect = purpose == DrawPurpose.TurnDraw
                ? gameState.FindNextDrawEffect(seat)
                : null;

            if (effect != null)
                return DrawWithSkillEffect(seat, gameState, purpose, effect);

            return DrawNormal(seat, gameState, purpose, null, false, false);
        }

        private DrawResult DrawWithSkillEffect(
            SeatId seat,
            MahjongGameState gameState,
            DrawPurpose purpose,
            ActiveSkillEffect effect)
        {
            if (gameState.Wall.TryTakeSpecific(effect.TargetTile, out Tile skillTile))
            {
                gameState.GetPlayerSeat(seat).Hand.Add(skillTile);
                gameState.RemoveActiveSkillEffect(effect);

                return new DrawResult(
                    true,
                    seat,
                    skillTile,
                    purpose,
                    DrawSource.SkillModified,
                    gameState.Wall.Count,
                    effect,
                    true,
                    true,
                    "Target tile was found in wall.");
            }

            DrawResult fallback = DrawNormal(seat, gameState, purpose, effect, true, false);
            gameState.RemoveActiveSkillEffect(effect);
            return fallback;
        }

        private DrawResult DrawNormal(
            SeatId seat,
            MahjongGameState gameState,
            DrawPurpose purpose,
            ActiveSkillEffect resolvedSkillEffect,
            bool skillWasPresent,
            bool skillApplied)
        {
            if (!gameState.Wall.TryTakeNext(out Tile tile))
            {
                return new DrawResult(
                    false,
                    seat,
                    default,
                    purpose,
                    DrawSource.None,
                    gameState.Wall.Count,
                    resolvedSkillEffect,
                    skillWasPresent,
                    skillApplied,
                    "Wall is empty.");
            }

            gameState.GetPlayerSeat(seat).Hand.Add(tile);

            DrawSource source = purpose == DrawPurpose.InitialDeal
                ? DrawSource.InitialDeal
                : skillWasPresent
                    ? DrawSource.SkillFallbackNormal
                    : DrawSource.Normal;

            string message = skillWasPresent
                ? "Target tile was not found. Fell back to normal draw."
                : "Normal draw.";

            return new DrawResult(
                true,
                seat,
                tile,
                purpose,
                source,
                gameState.Wall.Count,
                resolvedSkillEffect,
                skillWasPresent,
                skillApplied,
                message);
        }
    }
}
