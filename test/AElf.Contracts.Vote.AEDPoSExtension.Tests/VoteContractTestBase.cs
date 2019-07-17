using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.TestKet.AEDPoSExtension;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Xunit;

namespace AElf.Contract.Vote
{
    public class VoteContractTestBase : AEDPoSExtensionTestBase
    {
        public VoteContractTestBase()
        {
            BlockMiningService.DeploySystemContracts(new Dictionary<Hash, byte[]>
            {
                {VoteSmartContractAddressNameProvider.Name, Codes.Single(c => c.Key.Contains("Vote")).Value}
            });
        }

        [Fact]
        public async Task Test()
        {
            var round = await ConsensusStub.GetCurrentRoundInformation.CallAsync(new Empty());
        }
    }
}