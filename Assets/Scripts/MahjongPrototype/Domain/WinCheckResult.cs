namespace MahjongPrototype.Domain
{
    public readonly struct WinCheckResult
    {
        public WinCheckResult(bool canWin, WinningHandShape shape)
        {
            CanWin = canWin && shape != WinningHandShape.None;
            Shape = CanWin ? shape : WinningHandShape.None;
        }

        public bool CanWin { get; }
        public WinningHandShape Shape { get; }

        public static WinCheckResult NotWin => new WinCheckResult(false, WinningHandShape.None);

        public static WinCheckResult Win(WinningHandShape shape)
        {
            if (shape == WinningHandShape.None)
                return NotWin;

            return new WinCheckResult(true, shape);
        }
    }
}
