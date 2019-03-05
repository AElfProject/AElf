using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.OS.Jobs;
using AElf.OS.Network;
using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Tests.Network;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace AElf.OS.Tests
{
    public class ForkDownloadJobTest : NetTestBase
    {
        private IBlockchainService _blockChainService;
        private IPeerPool _peerPool;
        private IBlockchainExecutingService _blockExecService;
        private INetworkService _netService;

        private ForkDownloadJob _job;

        public ForkDownloadJobTest()
        {
            _blockChainService = GetRequiredService<IBlockchainService>();
            _peerPool = GetRequiredService<IPeerPool>();
            _blockExecService = GetRequiredService<IBlockchainExecutingService>();
            _netService = GetRequiredService<INetworkService>();

            _job = GetRequiredService<ForkDownloadJob>();
        }

        [Fact]
        public async Task ExecSyncJob_ShouldSyncChain()
        {
            var initialState = await _blockChainService.GetChainAsync();
            var genHash = initialState.LongestChainHash;
            
            _job.Execute(new ForkDownloadJobArgs { BlockHash = genHash.DumpByteArray(), BlockHeight = 3 });
            
            var chain = await _blockChainService.GetChainAsync();
            chain.LongestChainHeight.ShouldBe<long>(6);
        }
    }
}