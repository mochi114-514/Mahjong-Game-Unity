using MahjongPrototype.Domain;

namespace MahjongPrototype.Skills
{
    public sealed class SkillSystem
    {
        public SkillActivationResult ActivateForceDrawTile(
            MahjongGameState gameState,
            SeatId actorSeat,
            Tile targetTile)
        {
            if (gameState == null)
                return SkillActivationResult.Failed("GameState is not available.");

            if (gameState.IsRoundEnded)
                return SkillActivationResult.Failed("Round already ended.");

            if (gameState.CurrentTurn != actorSeat)
                return SkillActivationResult.Failed("Skill can only be activated during the actor's turn.");

            if (gameState.HasActiveSkillEffect(actorSeat, SkillEffectKind.ForceDrawTile))
                return SkillActivationResult.Failed("Force draw skill is already active.");

            ActiveSkillEffect effect = new ActiveSkillEffect(
                SkillEffectKind.ForceDrawTile,
                actorSeat,
                targetTile,
                SkillEffectDuration.NextDraw);

            gameState.AddActiveSkillEffect(effect);
            return SkillActivationResult.Activated(effect);
        }
    }
}
