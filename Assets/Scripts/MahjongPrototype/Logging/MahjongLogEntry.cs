namespace MahjongPrototype.Logging
{
    public sealed class MahjongLogEntry
    {
        public string Time { get; set; }
        public int Frame { get; set; }
        public string Scene { get; set; }
        public string Category { get; set; }
        public string EventName { get; set; }
        public string Message { get; set; }
        public string Seat { get; set; }
        public string Tile { get; set; }
        public string Hand { get; set; }
        public int? WallCount { get; set; }
        public int? TurnIndex { get; set; }
        public string ActiveSkill { get; set; }
        public string StackTrace { get; set; }
    }
}
