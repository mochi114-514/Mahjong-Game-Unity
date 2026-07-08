using System;
using MahjongPrototype.Tests.TestSupport.Core;

namespace MahjongPrototype.Tests.TestSupport.Mahjong
{
    internal sealed class MahjongTestTypes
    {
        private const string SeatIdTypeName = "MahjongPrototype.Domain.SeatId, Assembly-CSharp";
        private const string TileTypeName = "MahjongPrototype.Domain.Tile, Assembly-CSharp";
        private const string HandTypeName = "MahjongPrototype.Domain.Hand, Assembly-CSharp";
        private const string PlayerSeatTypeName = "MahjongPrototype.Domain.PlayerSeat, Assembly-CSharp";
        private const string ParticipantTypeName = "MahjongPrototype.Domain.ParticipantType, Assembly-CSharp";
        private const string DiscardRecordTypeName = "MahjongPrototype.Domain.DiscardRecord, Assembly-CSharp";
        private const string PlayerIdTypeName = "MahjongPrototype.Domain.PlayerId, Assembly-CSharp";
        private const string RoundWindTypeName = "MahjongPrototype.Domain.RoundWind, Assembly-CSharp";
        private const string WindProgressTypeName = "MahjongPrototype.Domain.WindProgress, Assembly-CSharp";
        private const string RoundResultTypeName = "MahjongPrototype.Domain.RoundResult, Assembly-CSharp";
        private const string WinTypeTypeName = "MahjongPrototype.Domain.WinType, Assembly-CSharp";
        private const string WallTypeName = "MahjongPrototype.Domain.Wall, Assembly-CSharp";
        private const string MahjongGameStateTypeName =
            "MahjongPrototype.Domain.MahjongGameState, Assembly-CSharp";
        private const string MahjongGameFlowTypeName = "MahjongPrototype.MahjongGameFlow, Assembly-CSharp";
        private const string MahjongEventNotifierTypeName =
            "MahjongPrototype.Notifications.MahjongEventNotifier, Assembly-CSharp";
        private const string MahjongGameLogRecorderTypeName =
            "MahjongPrototype.Logging.MahjongGameLogRecorder, Assembly-CSharp";
        private const string HanValueTypeName = "MahjongPrototype.Domain.HanValue, Assembly-CSharp";
        private const string YakuKindTypeName = "MahjongPrototype.Domain.YakuKind, Assembly-CSharp";
        private const string YakuDefinitionTypeName =
            "MahjongPrototype.Definitions.YakuDefinition, Assembly-CSharp";
        private const string YakuDefinitionCatalogTypeName =
            "MahjongPrototype.Definitions.YakuDefinitionCatalog, Assembly-CSharp";

        private readonly ReflectionTestAccess reflection;

        public MahjongTestTypes(ReflectionTestAccess reflection)
        {
            this.reflection = reflection;
        }

        public Type SeatId => reflection.RequireType(SeatIdTypeName);
        public Type Tile => reflection.RequireType(TileTypeName);
        public Type Hand => reflection.RequireType(HandTypeName);
        public Type PlayerSeat => reflection.RequireType(PlayerSeatTypeName);
        public Type ParticipantType => reflection.RequireType(ParticipantTypeName);
        public Type DiscardRecord => reflection.RequireType(DiscardRecordTypeName);
        public Type PlayerId => reflection.RequireType(PlayerIdTypeName);
        public Type RoundWind => reflection.RequireType(RoundWindTypeName);
        public Type WindProgress => reflection.RequireType(WindProgressTypeName);
        public Type RoundResult => reflection.RequireType(RoundResultTypeName);
        public Type WinType => reflection.RequireType(WinTypeTypeName);
        public Type Wall => reflection.RequireType(WallTypeName);
        public Type MahjongGameState => reflection.RequireType(MahjongGameStateTypeName);
        public Type MahjongGameFlow => reflection.RequireType(MahjongGameFlowTypeName);
        public Type MahjongEventNotifier => reflection.RequireType(MahjongEventNotifierTypeName);
        public Type MahjongGameLogRecorder => reflection.RequireType(MahjongGameLogRecorderTypeName);
        public Type HanValue => reflection.RequireType(HanValueTypeName);
        public Type YakuKind => reflection.RequireType(YakuKindTypeName);
        public Type YakuDefinition => reflection.RequireType(YakuDefinitionTypeName);
        public Type YakuDefinitionCatalog => reflection.RequireType(YakuDefinitionCatalogTypeName);
    }
}
