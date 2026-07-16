using System;
using System.Collections.Generic;
using MahjongPrototype.Domain;

namespace MahjongPrototype
{
    /// <summary>
    /// Local UI decision endpoint. UI decision screens will provide responses
    /// in a later phase; this endpoint only owns the provider identity today.
    /// </summary>
    public sealed class LocalUiDecisionProvider : IDecisionProvider
    {
        private readonly Dictionary<long, Func<DecisionResponse, DecisionResponseResult>>
            respondersByRequestId =
                new Dictionary<long, Func<DecisionResponse, DecisionResponseResult>>();

        public DecisionProviderRoute Route => DecisionProviderRoute.LocalUi;
        public bool IsAvailable => true;

        public event Action<
            DecisionRequest,
            Func<DecisionResponse, DecisionResponseResult>> DecisionRequested;

        public void RequestDecision(
            DecisionRequest request,
            Func<DecisionResponse, DecisionResponseResult> respond)
        {
            if (request == null || respond == null)
                return;

            respondersByRequestId[request.RequestId] = respond;
            DecisionRequested?.Invoke(request, respond);
        }

        /// <summary>
        /// Accepts a UI-produced response for a pending request. The callback
        /// is owned by DecisionCoordinator and only queues the authority work.
        /// </summary>
        public bool TrySubmitResponse(DecisionResponse response)
        {
            if (response == null ||
                !respondersByRequestId.TryGetValue(
                    response.RequestId,
                    out Func<DecisionResponse, DecisionResponseResult> respond))
            {
                return false;
            }

            DecisionResponseResult result = respond(response);
            if (result.Accepted)
                respondersByRequestId.Remove(response.RequestId);

            return result.Accepted;
        }

        public void CancelDecision(long requestId)
        {
            respondersByRequestId.Remove(requestId);
        }
    }

    /// <summary>
    /// CPU decision endpoint. CPU call/ron decisions are intentionally not
    /// registered in this phase; normal active turn commands use the authority
    /// command port directly.
    /// </summary>
    public sealed class CpuAgentDecisionProvider : IDecisionProvider
    {
        public DecisionProviderRoute Route => DecisionProviderRoute.CpuAgent;
        public bool IsAvailable => true;

        public void RequestDecision(
            DecisionRequest request,
            Func<DecisionResponse, DecisionResponseResult> respond)
        {
        }

        public void CancelDecision(long requestId)
        {
        }
    }
}
