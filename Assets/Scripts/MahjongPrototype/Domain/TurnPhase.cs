namespace MahjongPrototype.Domain
{
    public enum TurnPhase
    {
        NotStarted = 0,
        WaitingForDraw = 1,
        WaitingForDiscard = 2,
        ReachDiscardSelection = 3,
        ReachDecision = 4,
        WinDecision = 5,
        RoundEnded = 6,
        RoundResult = 7,
        GameEnded = 8,
        ReactionWindow = 9,
        WaitingForDiscardAfterCall = 10
    }
}
