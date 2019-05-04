using System.Threading.Tasks;
using AElf.Kernel;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Election
{
    public partial class ElectionContractTests : ElectionContractTestBase
    {
        [Fact]
        public async Task ElectionContract_GetCandidateInformation()
        {
            const int roundCount = 5;

            var minerKeyPair = FullNodesKeyPairs[0];

            await ElectionContract_GetVictories_ValidCandidatesEnough();

            await NextTerm(BootMinerKeyPair);

            await ProduceBlocks(minerKeyPair, roundCount, true);

            var information = await ElectionContractStub.GetCandidateInformation.CallAsync(new StringInput
            {
                Value = minerKeyPair.PublicKey.ToHex()
            });

            information.PublicKey.ShouldBe(minerKeyPair.PublicKey.ToHex());
        }
    }
}