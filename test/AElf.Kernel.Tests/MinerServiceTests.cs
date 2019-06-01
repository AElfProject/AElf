using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Miner.Application;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Kernel
{
    public class MinerServiceTests : KernelWithChainTestBase
    {

        private IMinerService _minerService;
        private IBlockchainService _chainService;

        public MinerServiceTests()
        {
            _chainService = GetRequiredService<IBlockchainService>();
            _minerService = GetRequiredService<IMinerService>();
        }

        [Fact]
        public async Task MinAsync_Success()
        {
            var chain = await _chainService.GetChainAsync();
            var hash = chain.BestChainHash;
            var height = chain.BestChainHeight;

            var block = await _minerService.MineAsync(hash, height, TimestampHelper.GetUtcNow(), TimestampHelper.DurationFromMinutes(1));
            block.ShouldNotBeNull();
            block.Header.Signature.ShouldNotBeNull();
        }
    }
}