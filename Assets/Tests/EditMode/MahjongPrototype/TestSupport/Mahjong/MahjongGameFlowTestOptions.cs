namespace MahjongPrototype.Tests.TestSupport.Mahjong
{
    internal sealed class MahjongGameFlowTestOptions
    {
        public string RootName { get; set; }
        public bool AddEventNotifier { get; set; }
        public bool AddGameLogRecorder { get; set; }
        public bool? LogWarnings { get; set; }
        public int? ParticipantCount { get; set; }
        public int? InitialHandTileCount { get; set; }
        public bool? AutoStart { get; set; }
        public bool? UseFixedRandomSeed { get; set; }
        public int? FixedRandomSeed { get; set; }
        public bool? EnableAutoDraw { get; set; }
        public float? AutoDiscardDrawnTileDelaySeconds { get; set; }
        public bool? RandomizeSelfSeat { get; set; }
        public string FixedSelfSeatName { get; set; }
        public object YakuDefinitionCatalog { get; set; }
    }
}
