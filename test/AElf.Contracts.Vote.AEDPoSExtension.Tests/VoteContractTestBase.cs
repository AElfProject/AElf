using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.TestKet.AEDPoSExtension;
using AElf.Contracts.TestKit;
using AElf.Kernel.Consensus;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Xunit;

namespace AElf.Contract.Vote
{
    public class VoteContractTestBase : AEDPoSExtensionTestBase
    {
        internal AEDPoSContractImplContainer.AEDPoSContractImplStub ConsensusStub =>
            GetTester<AEDPoSContractImplContainer.AEDPoSContractImplStub>(
                ContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider.Name),
                SampleECKeyPairs.KeyPairs[0]);

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