namespace MahjongPrototype.Domain
{
    public enum TurnPhase
    {
        NotStarted = 0,
        WaitingForDraw = 1,
        WaitingForDiscard = 2,
        WinDecision = 3,
        RoundEnded = 4
    }
}
