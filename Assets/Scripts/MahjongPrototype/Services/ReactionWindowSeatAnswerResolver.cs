using System;
using System.Collections.Generic;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    /// <summary>
    /// Resolves fully collected seat answers without changing the reaction
    /// window, its candidates, or any game state.
    /// </summary>
    public sealed class ReactionWindowSeatAnswerResolver
    {
        private readonly TurnOrderService turnOrderService;

        public ReactionWindowSeatAnswerResolver()
            : this(new TurnOrderService())
        {
        }

        public ReactionWindowSeatAnswerResolver(TurnOrderService turnOrderService)
        {
            this.turnOrderService = turnOrderService ??
                throw new ArgumentNullException(nameof(turnOrderService));
        }

        public ReactionWindowSeatAnswerResolution Resolve(
            ReactionWindowSeatAnswerCollection answers,
            IReadOnlyList<SeatId> activeSeats)
        {
            if (answers == null)
                throw new ArgumentNullException(nameof(answers));

            ReactionWindow reactionWindow = answers.ReactionWindow;
            if (answers.HasUnansweredSeats)
            {
                return ReactionWindowSeatAnswerResolution.PendingAnswers(
                    reactionWindow.WindowId,
                    reactionWindow.Source);
            }

            if (!HasDeclaredAnswer(answers))
            {
                return ReactionWindowSeatAnswerResolution.NoReaction(
                    reactionWindow.WindowId,
                    reactionWindow.Source);
            }

            ValidateActiveSeats(activeSeats, reactionWindow.Source.ActorSeat);

            ReactionWindowSeatAnswer selectedAnswer = null;
            ReactionWindowCandidate selectedCandidate = null;
            int selectedPriority = int.MaxValue;
            int selectedDistance = int.MaxValue;

            for (int i = 0; i < answers.TargetSeats.Count; i++)
            {
                SeatId seat = answers.TargetSeats[i];
                if (!answers.TryGetRegisteredAnswer(seat, out ReactionWindowSeatAnswer answer) ||
                    answer.Kind == ReactionWindowSeatAnswerKind.Pass)
                {
                    continue;
                }

                if (!answers.TryGetDeclaredCandidate(answer, out ReactionWindowCandidate candidate))
                {
                    throw new InvalidOperationException(
                        "A registered declaration did not resolve to a reaction candidate.");
                }

                int priority = GetPriority(candidate.Kind);
                int distance = GetSeatDistance(
                    activeSeats,
                    reactionWindow.Source.ActorSeat,
                    candidate.Seat);
                if (priority < selectedPriority ||
                    priority == selectedPriority && distance < selectedDistance)
                {
                    selectedAnswer = answer;
                    selectedCandidate = candidate;
                    selectedPriority = priority;
                    selectedDistance = distance;
                }
            }

            if (selectedAnswer == null || selectedCandidate == null)
            {
                throw new InvalidOperationException(
                    "A collected declaration could not be selected.");
            }

            // PROTOTYPE: Until multiple-ron settlement is implemented, the
            // nearest ron seat wins this priority tie and other ron answers remain recorded.
            return ReactionWindowSeatAnswerResolution.DeclarationSelected(
                reactionWindow.WindowId,
                reactionWindow.Source,
                selectedAnswer,
                selectedCandidate);
        }

        private static bool HasDeclaredAnswer(ReactionWindowSeatAnswerCollection answers)
        {
            IReadOnlyList<ReactionWindowSeatAnswer> registeredAnswers =
                answers.RegisteredAnswers;
            for (int i = 0; i < registeredAnswers.Count; i++)
            {
                if (registeredAnswers[i].Kind != ReactionWindowSeatAnswerKind.Pass)
                    return true;
            }

            return false;
        }

        private static int GetPriority(ReactionKind kind)
        {
            switch (kind)
            {
                case ReactionKind.Ron:
                    return 0;
                case ReactionKind.Pon:
                case ReactionKind.Daiminkan:
                    return 1;
                case ReactionKind.Chi:
                    return 2;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        private void ValidateActiveSeats(
            IReadOnlyList<SeatId> activeSeats,
            SeatId sourceSeat)
        {
            if (activeSeats == null)
                throw new ArgumentNullException(nameof(activeSeats));
            if (activeSeats.Count <= 0)
                throw new ArgumentException("At least one active seat is required.", nameof(activeSeats));

            bool containsSourceSeat = false;
            HashSet<SeatId> knownSeats = new HashSet<SeatId>();
            for (int i = 0; i < activeSeats.Count; i++)
            {
                SeatId seat = activeSeats[i];
                if (!knownSeats.Add(seat))
                {
                    throw new ArgumentException(
                        "Active seats must not contain duplicates.",
                        nameof(activeSeats));
                }

                if (seat == sourceSeat)
                    containsSourceSeat = true;
            }

            if (!containsSourceSeat)
            {
                throw new ArgumentException(
                    "The reaction source seat must be active.",
                    nameof(activeSeats));
            }
        }

        private int GetSeatDistance(
            IReadOnlyList<SeatId> activeSeats,
            SeatId sourceSeat,
            SeatId targetSeat)
        {
            if (sourceSeat == targetSeat)
                return 0;

            SeatId currentSeat = sourceSeat;
            for (int distance = 1; distance < activeSeats.Count; distance++)
            {
                currentSeat = turnOrderService.GetNextSeat(activeSeats, currentSeat);
                if (currentSeat == targetSeat)
                    return distance;
            }

            throw new ArgumentException(
                "Every declaring seat must be present in the active seat order.",
                nameof(activeSeats));
        }
    }
}
