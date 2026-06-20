using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public readonly struct DiscardResult
    {
        private DiscardResult(bool success, DiscardRecord record, string reason)
        {
            Success = success;
            Record = record;
            Reason = reason ?? string.Empty;
        }

        public bool Success { get; }
        public DiscardRecord Record { get; }
        public string Reason { get; }

        public static DiscardResult Discarded(DiscardRecord record)
        {
            return new DiscardResult(true, record, string.Empty);
        }

        public static DiscardResult Failed(string reason)
        {
            return new DiscardResult(false, default, reason);
        }
    }
}
