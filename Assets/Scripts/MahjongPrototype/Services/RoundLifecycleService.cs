using System;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public sealed class RoundLifecycleService
    {
        public const string RoundEndReasonWin = "Win";
        public const string RoundEndReasonWallEmpty = "WallEmpty";

        private readonly WinningCandidateSelector winningCandidateSelector;

        public RoundLifecycleService(WinningCandidateSelector winningCandidateSelector)
        {
            this.winningCandidateSelector = winningCandidateSelector ??
                throw new ArgumentNullException(nameof(winningCandidateSelector));
        }

        public WindProgress GetInitialWindProgress()
        {
            return WindProgress.East1;
        }

        public RoundLifecycleEndResult EndRound(MahjongGameState gameState, string reason)
        {
            return EndRound(gameState, reason, ReactionWindowResolution.None);
        }

        public RoundLifecycleEndResult EndRound(
            MahjongGameState gameState,
            string reason,
            ReactionWindowResolution reactionResolution)
        {
            if (gameState == null)
                throw new ArgumentNullException(nameof(gameState));

            RoundResult roundResult = CreateRoundResultForRoundEnd(
                gameState,
                reason,
                reactionResolution);
            gameState.ClearWinDecision();
            gameState.ClearReachDecision();
            if (roundResult != null)
                gameState.BeginRoundResult(roundResult);
            else
                gameState.EndRoundWithoutResult();

            return new RoundLifecycleEndResult(roundResult);
        }

        public RoundResult GetPendingRoundResult(MahjongGameState gameState)
        {
            if (gameState == null || !gameState.IsRoundResultPending)
                return null;

            return gameState.CurrentRoundResult;
        }

        public RoundLifecycleTransition AdvanceFromRoundResult(MahjongGameState gameState)
        {
            RoundResult result = GetPendingRoundResult(gameState);
            if (result == null)
                return RoundLifecycleTransition.NoTransition;

            if (IsFinalRound(gameState.WindProgress))
            {
                gameState.CompleteRoundResult(true);
                return RoundLifecycleTransition.GameEnded(result);
            }

            if (!gameState.WindProgress.TryGetNext(out WindProgress nextWindProgress))
                return RoundLifecycleTransition.NoTransition;

            SeatId nextSelfSeat = RotateSelfSeatForNextRound(gameState.SelfSeat);
            gameState.CompleteRoundResult(false);
            return RoundLifecycleTransition.StartNextRound(nextWindProgress, nextSelfSeat);
        }

        public static SeatId RotateSelfSeatForNextRound(SeatId currentSeat)
        {
            return (SeatId)(((int)currentSeat + 3) % 4);
        }

        private RoundResult CreateRoundResultForRoundEnd(
            MahjongGameState gameState,
            string reason,
            ReactionWindowResolution reactionResolution)
        {
            switch (reason)
            {
                case RoundEndReasonWin:
                    return CreateWinRoundResult(gameState, reactionResolution);
                case RoundEndReasonWallEmpty:
                    return RoundResult.CreateExhaustiveDraw(
                        gameState.WindProgress,
                        gameState.TurnIndex,
                        IsFinalRound(gameState.WindProgress));
                default:
                    return null;
            }
        }

        private RoundResult CreateWinRoundResult(
            MahjongGameState gameState,
            ReactionWindowResolution reactionResolution)
        {
            if (reactionResolution.Type == ReactionWindowResolutionType.RonDeclared &&
                reactionResolution.Candidate != null)
            {
                ReactionWindowCandidate candidate = reactionResolution.Candidate;
                HandEvaluationCandidateResult selectedRonCandidate =
                    winningCandidateSelector.Select(
                        candidate.WinDeclarationEvaluation?.HandEvaluationResult);
                return RoundResult.CreateWin(
                    gameState.WindProgress,
                    reactionResolution.Source.TurnIndex,
                    candidate.Seat,
                    WinType.Ron,
                    reactionResolution.Source.ActorSeat,
                    reactionResolution.Source.Tile,
                    selectedRonCandidate,
                    IsFinalRound(gameState.WindProgress));
            }

            WinType winType = gameState.WinDecisionType ?? WinType.Tsumo;
            WinDeclarationEvaluationResult evaluationResult =
                gameState.PendingWinDeclarationEvaluation;
            HandEvaluationCandidateResult selectedCandidate =
                winningCandidateSelector.Select(evaluationResult?.HandEvaluationResult);

            return RoundResult.CreateWin(
                gameState.WindProgress,
                gameState.WinDecisionTurnIndex,
                gameState.WinDecisionSeat,
                winType,
                gameState.WinSourceSeat,
                gameState.WinningTile,
                selectedCandidate,
                IsFinalRound(gameState.WindProgress));
        }

        private static bool IsFinalRound(WindProgress windProgress)
        {
            return !windProgress.TryGetNext(out _);
        }
    }

    public readonly struct RoundLifecycleEndResult
    {
        public RoundLifecycleEndResult(RoundResult roundResult)
        {
            RoundResult = roundResult;
        }

        public RoundResult RoundResult { get; }
    }

    public enum RoundLifecycleTransitionType
    {
        None,
        StartNextRound,
        GameEnded
    }

    public readonly struct RoundLifecycleTransition
    {
        private RoundLifecycleTransition(
            RoundLifecycleTransitionType type,
            WindProgress? nextWindProgress,
            SeatId? nextSelfSeat,
            RoundResult roundResult)
        {
            Type = type;
            NextWindProgress = nextWindProgress;
            NextSelfSeat = nextSelfSeat;
            RoundResult = roundResult;
        }

        public static RoundLifecycleTransition NoTransition =>
            new RoundLifecycleTransition(RoundLifecycleTransitionType.None, null, null, null);

        public RoundLifecycleTransitionType Type { get; }
        public WindProgress? NextWindProgress { get; }
        public SeatId? NextSelfSeat { get; }
        public RoundResult RoundResult { get; }

        public static RoundLifecycleTransition StartNextRound(
            WindProgress nextWindProgress,
            SeatId nextSelfSeat)
        {
            return new RoundLifecycleTransition(
                RoundLifecycleTransitionType.StartNextRound,
                nextWindProgress,
                nextSelfSeat,
                null);
        }

        public static RoundLifecycleTransition GameEnded(RoundResult roundResult)
        {
            return new RoundLifecycleTransition(
                RoundLifecycleTransitionType.GameEnded,
                null,
                null,
                roundResult ?? throw new ArgumentNullException(nameof(roundResult)));
        }
    }
}
