using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken.Messages;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Election
{
    public partial class ElectionContractTests : ElectionContractTestBase
    {
        #region ReleaseTreasury

        [Fact]
        public async Task ElectionContract_ReleaseTreasuryProfits_CandidatesNotEnough()
        {
            await ElectionContract_GetVictories_CandidatesNotEnough();

            await NextTerm(BootMinerKeyPair);

            var round = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());

            foreach (var initialMinersKeyPair in InitialCoreDataCenterKeyPairs)
            {
                round.RealTimeMinersInformation.Keys.ShouldContain(initialMinersKeyPair.PublicKey.ToHex());
            }
        }

        [Fact]
        public async Task ElectionContract_ReleaseTreasuryProfits_NoValidCandidate()
        {
            await ElectionContract_GetVictories_NoValidCandidate();

            await NextTerm(BootMinerKeyPair);

            var round = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());

            foreach (var initialMinersKeyPair in InitialCoreDataCenterKeyPairs)
            {
                round.RealTimeMinersInformation.Keys.ShouldContain(initialMinersKeyPair.PublicKey.ToHex());
            }
        }

        [Fact]
        public async Task ElectionContract_ReleaseTreasuryProfits_ValidCandidatesNotEnough()
        {
            var firstRound = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());

            var victories = await ElectionContract_GetVictories_ValidCandidatesNotEnough();

            await NextTerm(BootMinerKeyPair);

            var round = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());

            foreach (var validCandidateKeyPair in victories)
            {
                round.RealTimeMinersInformation.Keys.ShouldContain(validCandidateKeyPair);
            }
        }

        [Fact]
        public async Task ElectionContract_ReleaseTreasuryProfits_NotAllCandidatesGetVotes()
        {
            var validCandidates = await ElectionContract_GetVictories_NotAllCandidatesGetVotes();

            await NextTerm(BootMinerKeyPair);

            var round = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());

            foreach (var validCandidateKeyPair in validCandidates)
            {
                round.RealTimeMinersInformation.Keys.ShouldContain(validCandidateKeyPair.PublicKey.ToHex());
            }
        }

        #endregion
    }
}