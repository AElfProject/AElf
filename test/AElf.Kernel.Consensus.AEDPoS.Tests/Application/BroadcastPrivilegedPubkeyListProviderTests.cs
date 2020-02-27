using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Consensus.DPoS.Tests.Application
{
    public class BroadcastPrivilegedPubkeyListProviderTests : AEDPoSTestBase
    {
        private readonly IBroadcastPrivilegedPubkeyListProvider _broadcastPrivilegedPubkeyListProvider;
        private readonly IBlockchainService _blockchainService;
        private readonly KernelTestHelper _kernelTestHelper;

        public BroadcastPrivilegedPubkeyListProviderTests()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
            _broadcastPrivilegedPubkeyListProvider = GetRequiredService<IBroadcastPrivilegedPubkeyListProvider>();
        }

        [Fact]
        public async Task GetPubkeyList_Test()
        {
            //without extra data
            var chain = await _blockchainService.GetChainAsync();
            var block = _kernelTestHelper.GenerateBlock(chain.BestChainHeight, chain.BestChainHash);
            var result = await _broadcastPrivilegedPubkeyListProvider.GetPubkeyList(null);
            result.Count.ShouldBe(0);
            
            //with extra data
            var extraData = new AElfConsensusHeaderInformation
            {
                SenderPubkey = block.Header.SignerPubkey,
                Behaviour = AElfConsensusBehaviour.NextRound,
            }.ToByteString();
            block = _kernelTestHelper.GenerateBlock(chain.BestChainHeight, chain.BestChainHash, extraData:extraData);
            result = await _broadcastPrivilegedPubkeyListProvider.GetPubkeyList(block.Header);
            result.Count.ShouldBe(3);
        }
    }
}