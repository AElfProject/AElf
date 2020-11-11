using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Election
{
    public partial class ElectionContractTests
    {
        [Fact]
        public async Task ElectionContract_ReplaceCandidatePubkey_Test()
        {
            var announceElectionKeyPair = ValidationDataCenterKeyPairs.First();
            var candidateAdmin = ValidationDataCenterKeyPairs.Last();
            var candidateAdminAddress = Address.FromPublicKey(candidateAdmin.PublicKey);
            await AnnounceElectionAsync(announceElectionKeyPair, candidateAdminAddress);

            // Check candidate admin
            {
                var admin = await ElectionContractStub.GetCandidateAdmin.CallAsync(new StringValue
                    {Value = announceElectionKeyPair.PublicKey.ToHex()});
                admin.ShouldBe(candidateAdminAddress);
            }

            // Check candidates.
            {
                var candidates = await ElectionContractStub.GetCandidates.CallAsync(new Empty());
                candidates.Value.ShouldContain(ByteString.CopyFrom(announceElectionKeyPair.PublicKey));
            }

            var candidateAdminStub =
                GetTester<ElectionContractImplContainer.ElectionContractImplStub>(ElectionContractAddress,
                    candidateAdmin);
            var newKeyPair = ValidationDataCenterKeyPairs.Skip(1).First();
            await candidateAdminStub.ReplaceCandidatePubkey.SendAsync(new ReplaceCandidatePubkeyInput
            {
                OldPubkey = announceElectionKeyPair.PublicKey.ToHex(),
                NewPubkey = newKeyPair.PublicKey.ToHex()
            });

            // Check candidates again.
            {
                var candidates = await ElectionContractStub.GetCandidates.CallAsync(new Empty());
                candidates.Value.ShouldContain(ByteString.CopyFrom(newKeyPair.PublicKey));
                candidates.Value.ShouldNotContain(ByteString.CopyFrom(announceElectionKeyPair.PublicKey));
            }

            // Check candidate information
            {
                var oldCandidateInformation =
                    await ElectionContractStub.GetCandidateInformation.CallAsync(new StringValue
                        {Value = announceElectionKeyPair.PublicKey.ToHex()});
                oldCandidateInformation.IsCurrentCandidate.ShouldBeFalse();
                var newPubkeyInformation =
                    await ElectionContractStub.GetCandidateInformation.CallAsync(new StringValue
                        {Value = newKeyPair.PublicKey.ToHex()});
                newPubkeyInformation.IsCurrentCandidate.ShouldBeTrue();
            }

            // Two pubkeys cannot quit election.
            {
                var stub = GetTester<ElectionContractImplContainer.ElectionContractImplStub>(ElectionContractAddress,
                    announceElectionKeyPair);
                var result = await stub.QuitElection.SendAsync(new StringValue {Value = newKeyPair.PublicKey.ToHex()});
                result.TransactionResult.Error.ShouldContain("Only admin can quit election.");
            }
            {
                var stub = GetTester<ElectionContractImplContainer.ElectionContractImplStub>(ElectionContractAddress,
                    newKeyPair);
                var result = await stub.QuitElection.SendAsync(new StringValue {Value = newKeyPair.PublicKey.ToHex()});
                result.TransactionResult.Error.ShouldContain("Only admin can quit election.");
            }

            // Only admin can quit election.
            {
                await candidateAdminStub.QuitElection.SendAsync(new StringValue
                {
                    Value = newKeyPair.PublicKey.ToHex()
                });
                var candidates = await ElectionContractStub.GetCandidates.CallAsync(new Empty());
                candidates.Value.ShouldNotContain(ByteString.CopyFrom(newKeyPair.PublicKey));
            }
        }

        [Fact]
        public async Task ElectionContract_ReplaceCandidatePubkey_NotCandidateAdmin_Test()
        {
            var announceElectionKeyPair = ValidationDataCenterKeyPairs.First();
            var candidateAdmin = ValidationDataCenterKeyPairs.Last();
            var candidateAdminAddress = Address.FromPublicKey(candidateAdmin.PublicKey);
            await AnnounceElectionAsync(announceElectionKeyPair, candidateAdminAddress);

            var newKeyPair = ValidationDataCenterKeyPairs.Skip(1).First();

            {
                var result = await ElectionContractStub.ReplaceCandidatePubkey.SendAsync(new ReplaceCandidatePubkeyInput
                {
                    OldPubkey = announceElectionKeyPair.PublicKey.ToHex(),
                    NewPubkey = newKeyPair.PublicKey.ToHex()
                });
                result.TransactionResult.Error.ShouldContain("No permission.");
            }

            // The one announced election neither can replace pubkey.
            {
                var stub = GetTester<ElectionContractImplContainer.ElectionContractImplStub>(ElectionContractAddress,
                    announceElectionKeyPair);
                var result = await stub.ReplaceCandidatePubkey.SendAsync(new ReplaceCandidatePubkeyInput
                {
                    OldPubkey = announceElectionKeyPair.PublicKey.ToHex(),
                    NewPubkey = newKeyPair.PublicKey.ToHex()
                });
                result.TransactionResult.Error.ShouldContain("No permission.");
            }
        }

        [Fact]
        public async Task ElectionContract_SetCandidateAdmin_NotCandidateAdmin_Test()
        {
            var announceElectionKeyPair = ValidationDataCenterKeyPairs.First();
            var candidateAdmin = ValidationDataCenterKeyPairs.Last();
            var candidateAdminAddress = Address.FromPublicKey(candidateAdmin.PublicKey);
            await AnnounceElectionAsync(announceElectionKeyPair, candidateAdminAddress);

            var newCandidateAdmin = ValidationDataCenterKeyPairs.Skip(2).First();
            var newCandidateAdminAddress = Address.FromPublicKey(newCandidateAdmin.PublicKey);

            var candidateAdminStub =
                GetTester<ElectionContractImplContainer.ElectionContractImplStub>(ElectionContractAddress,
                    candidateAdmin);
            await candidateAdminStub.SetCandidateAdmin.SendAsync(new SetCandidateAdminInput
            {
                Admin = newCandidateAdminAddress,
                Pubkey = announceElectionKeyPair.PublicKey.ToHex()
            });

            // Check new admin.
            {
                var admin = await ElectionContractStub.GetCandidateAdmin.CallAsync(new StringValue
                    {Value = announceElectionKeyPair.PublicKey.ToHex()});
                admin.ShouldBe(newCandidateAdminAddress);
            }

            // New admin can quit election.
            var newCandidateAdminStub =
                GetTester<ElectionContractImplContainer.ElectionContractImplStub>(ElectionContractAddress,
                    newCandidateAdmin);
            await newCandidateAdminStub.QuitElection.SendAsync(new StringValue
            {
                Value = announceElectionKeyPair.PublicKey.ToHex()
            });
            var candidates = await ElectionContractStub.GetCandidates.CallAsync(new Empty());
            candidates.Value.ShouldNotContain(ByteString.CopyFrom(announceElectionKeyPair.PublicKey));
        }
    }
}