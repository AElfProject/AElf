using System.Threading.Tasks;
using AElf.Kernel;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Election
{
    public partial class ElectionContractTests : ElectionContractTestBase
    {
        [Fact]
        public async Task ElectionContract_GetCandidateHistory()
        {
            const int roundCount = 5;

            var minerKeyPair = FullNodesKeyPairs[0];

            await ElectionContract_GetVictories_ValidCandidatesEnough();

            await NextTerm(BootMinerKeyPair);

            await ProduceBlocks(minerKeyPair, roundCount, true);

            var history = await ElectionContractStub.GetCandidateHistory.CallAsync(new StringInput
            {
                Value = minerKeyPair.PublicKey.ToHex()
            });

            history.PublicKey.ShouldBe(minerKeyPair.PublicKey.ToHex());
        }
    }
}