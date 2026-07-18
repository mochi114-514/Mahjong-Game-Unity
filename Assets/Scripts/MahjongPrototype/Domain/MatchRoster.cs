using System;
using System.Collections.Generic;

namespace MahjongPrototype.Domain
{
    /// <summary>
    /// Shared match participant attribute. Input and transport are deliberately
    /// kept outside of this value.
    /// </summary>
    public enum ParticipantKind
    {
        Human = 0,
        Cpu = 1
    }

    public readonly struct MatchParticipant
    {
        public MatchParticipant(PlayerId playerId, ParticipantKind kind)
        {
            PlayerId = playerId;
            Kind = kind;
        }

        public PlayerId PlayerId { get; }
        public ParticipantKind Kind { get; }
    }

    /// <summary>
    /// Match-lifetime participant configuration. Seats are assigned separately
    /// for each round, so lookups are intentionally keyed by PlayerId.
    /// </summary>
    public sealed class MatchRoster
    {
        private readonly List<MatchParticipant> participants;

        public MatchRoster(IEnumerable<MatchParticipant> participants)
        {
            if (participants == null)
                throw new ArgumentNullException(nameof(participants));

            this.participants = new List<MatchParticipant>(participants);
        }

        public IReadOnlyList<MatchParticipant> Participants => participants;

        public bool TryGetParticipant(PlayerId playerId, out MatchParticipant participant)
        {
            participant = default;
            bool found = false;
            for (int i = 0; i < participants.Count; i++)
            {
                MatchParticipant current = participants[i];
                if (current.PlayerId != playerId)
                    continue;

                if (found)
                    return false;

                participant = current;
                found = true;
            }

            return found;
        }
    }
}
