using System.Collections.Generic;
using MahjongPrototype.Domain;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class PlayerActionDecisionContractTests
    {
        [Test]
        public void SelfKanDecisionRequest_CopiesOptionsAndRejectsInvalidPayloads()
        {
            List<SelfKanCandidate> candidates = new List<SelfKanCandidate>
            {
                new SelfKanCandidate(
                    SelfKanKind.Ankan,
                    SeatId.East,
                    new Tile("5m"),
                    SelfKanTileLocation.Hand,
                    12),
                new SelfKanCandidate(
                    SelfKanKind.Kakan,
                    SeatId.East,
                    new Tile("P"),
                    SelfKanTileLocation.DrawnTile,
                    12,
                    3)
            };
            SelfKanDecisionRequest request = new SelfKanDecisionRequest(candidates);
            candidates.Clear();

            Assert.That(request.Options, Has.Count.EqualTo(2));
            Assert.That(request.Options[0].Kind, Is.EqualTo(SelfKanKind.Ankan));
            Assert.That(request.Options[1].SourcePonMeldIndex, Is.EqualTo(3));

            DecisionResponse declineWithOption = new DecisionResponse(
                401,
                DecisionKind.SelfKan,
                PlayerId.Player1,
                SeatId.East,
                12,
                false,
                new SelfKanDecisionResponse(0));
            Assert.That(
                request.TryValidateResponse(declineWithOption, out string declineReason),
                Is.False);
            Assert.That(declineReason, Is.EqualTo("SelfKanOptionNotAllowed"));

            DecisionResponse staleOption = new DecisionResponse(
                401,
                DecisionKind.SelfKan,
                PlayerId.Player1,
                SeatId.East,
                12,
                true,
                new SelfKanDecisionResponse(7));
            Assert.That(
                request.TryValidateResponse(staleOption, out string staleReason),
                Is.False);
            Assert.That(staleReason, Is.EqualTo("SelfKanOptionMissing"));

            DecisionResponse valid = new DecisionResponse(
                401,
                DecisionKind.SelfKan,
                PlayerId.Player1,
                SeatId.East,
                12,
                true,
                new SelfKanDecisionResponse(1));
            Assert.That(request.TryValidateResponse(valid, out _), Is.True);
        }

        [Test]
        public void SelfKanDecisionCoordinator_QueuesOnlyOneValidRequestBoundResponse()
        {
            LocalUiDecisionProvider provider = new LocalUiDecisionProvider();
            DecisionProviderRegistry registry = new DecisionProviderRegistry(
                new[]
                {
                    new DecisionProviderRegistration(
                        PlayerId.Player1,
                        DecisionProviderRoute.LocalUi,
                        provider)
                });
            RecordingAuthority authority = new RecordingAuthority();
            DecisionCoordinator coordinator = new DecisionCoordinator(registry, authority);
            DecisionRequest request = new DecisionRequest(
                402,
                DecisionKind.SelfKan,
                PlayerId.Player1,
                SeatId.East,
                13,
                new SelfKanDecisionRequest(new[]
                {
                    new SelfKanCandidate(
                        SelfKanKind.Ankan,
                        SeatId.East,
                        new Tile("6m"),
                        SelfKanTileLocation.DrawnTile,
                        13)
                }));
            DecisionResponse invalid = new DecisionResponse(
                request.RequestId,
                request.Kind,
                request.PlayerId,
                request.ActorSeat,
                request.TurnIndex,
                true,
                new SelfKanDecisionResponse(3));
            DecisionResponse valid = new DecisionResponse(
                request.RequestId,
                request.Kind,
                request.PlayerId,
                request.ActorSeat,
                request.TurnIndex,
                true,
                new SelfKanDecisionResponse(0));

            Assert.That(coordinator.Request(request).Accepted, Is.True);
            Assert.That(provider.TrySubmitResponse(invalid), Is.False);
            Assert.That(provider.TrySubmitResponse(valid), Is.True);
            Assert.That(provider.TrySubmitResponse(valid), Is.False);

            coordinator.Pump();

            Assert.That(authority.Responses, Has.Count.EqualTo(1));
            Assert.That(authority.Responses[0].RequestId, Is.EqualTo(request.RequestId));
            Assert.That(authority.Responses[0].SelfKan.OptionId, Is.EqualTo(0));
        }

        [Test]
        public void CpuDecisionProvider_DeclinesSelfKanInsteadOfLeavingItPending()
        {
            CpuAgentDecisionProvider provider = new CpuAgentDecisionProvider();
            DecisionResponse captured = null;
            DecisionRequest request = new DecisionRequest(
                403,
                DecisionKind.SelfKan,
                PlayerId.Player2,
                SeatId.South,
                14,
                new SelfKanDecisionRequest(new[]
                {
                    new SelfKanCandidate(
                        SelfKanKind.Ankan,
                        SeatId.South,
                        new Tile("7m"),
                        SelfKanTileLocation.Hand,
                        14)
                }));

            provider.RequestDecision(request, response =>
            {
                captured = response;
                return DecisionResponseResult.Succeeded();
            });

            Assert.That(captured, Is.Not.Null);
            Assert.That(captured.RequestId, Is.EqualTo(request.RequestId));
            Assert.That(captured.Accepted, Is.False);
        }

        private sealed class RecordingAuthority : IMahjongAuthorityDecisionPort
        {
            public List<DecisionResponse> Responses { get; } =
                new List<DecisionResponse>();

            public DecisionResponseResult TryExecuteDecisionResponse(DecisionResponse response)
            {
                Responses.Add(response);
                return DecisionResponseResult.Succeeded();
            }
        }
    }
}
