using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Events;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Xunit;

namespace AElf.Kernel.Blockchain.Application
{
    public class FullBlockchainServiceTests: AElfKernelTestBase
    {
        private readonly FullBlockchainService _fullBlockchainService;
        private readonly ILocalEventBus _localEventBus;
        
        public FullBlockchainServiceTests()
        {
            _fullBlockchainService = GetRequiredService<FullBlockchainService>();
            _localEventBus = GetRequiredService<ILocalEventBus>();
        }

        [Fact]
        public async Task Should_Create_Chain_Success()
        {
            var chainId = 1;

            var args = new BestChainFoundEvent();
            _localEventBus.Subscribe<BestChainFoundEvent>(message =>
            {
                args = message;
                return Task.CompletedTask;
            });
            
            var block = new Block();
            block.Header = new BlockHeader
            {
                Height = GlobalConfig.GenesisBlockHeight,
                PreviousBlockHash = Hash.Genesis,
                ChainId = chainId,
                Time = Timestamp.FromDateTime(DateTime.UtcNow)
            };
            block.Body = new BlockBody();
            await _fullBlockchainService.CreateChainAsync(chainId, block);

            var chain = await _fullBlockchainService.GetChainAsync(chainId);
            chain.Id.ShouldBe(chainId);

            args.ChainId.ShouldBe(chainId);
        }
    }
}