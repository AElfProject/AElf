using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Node.Application;
using AElf.Kernel.Node.Events;
using AElf.Types;
using Moq;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Xunit;

namespace AElf.Kernel.Node.Application
{
    public class BlockchainNodeContextServiceTests : NodeTestBase
    {
        private readonly IBlockchainNodeContextService _blockchainNodeContextService;
        private readonly KernelTestHelper _kernelTestHelper;
        private readonly IBlockchainService _blockchainService;
        private readonly KernelNodeTestContext _kernelNodeTestContext;
        private readonly ILocalEventBus _localEventBus;

        public BlockchainNodeContextServiceTests()
        {
            _blockchainNodeContextService = GetRequiredService<IBlockchainNodeContextService>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _kernelNodeTestContext = GetRequiredService<KernelNodeTestContext>();
            _localEventBus = GetRequiredService<ILocalEventBus>();
        }

        [Fact]
        public async Task Start_Test()
        {
            var transactions = new List<Transaction>
            {
                _kernelTestHelper.GenerateTransaction(),
                _kernelTestHelper.GenerateTransaction()
            };
            
            var dto = new BlockchainNodeContextStartDto
            {
                ChainId = 1234,
                ZeroSmartContractType = typeof(IBlockchainNodeContextService),
                Transactions = transactions.ToArray()
            };
            
            _kernelNodeTestContext.MockConsensusService.Verify(
                s => s.TriggerConsensusAsync(It.IsAny<ChainContext>()), Times.Never);

            var context = await _blockchainNodeContextService.StartAsync(dto);
            _kernelNodeTestContext.MockConsensusService.Verify(
                s => s.TriggerConsensusAsync(It.IsAny<ChainContext>()), Times.Once);

            context.ChainId.ShouldBe(dto.ChainId);
            var chain = await _blockchainService.GetChainAsync();
            chain.Id.ShouldBe(dto.ChainId);
            chain.BestChainHeight.ShouldBe(1);
            var block = await _blockchainService.GetBlockByHashAsync(chain.BestChainHash);
            block.Body.TransactionIds.Count.ShouldBe(2);
            block.Body.TransactionIds.ShouldContain(transactions[0].GetHash());
            block.Body.TransactionIds.ShouldContain(transactions[1].GetHash());

            var block2 = await _kernelTestHelper.AttachBlockToBestChain();
            var block3 = await _kernelTestHelper.AttachBlockToBestChain();
            
            chain = await _blockchainService.GetChainAsync();
            chain.BestChainHeight.ShouldBe(3);
            chain.BestChainHash.ShouldBe(block3.GetHash());

            await _blockchainService.SetIrreversibleBlockAsync(chain, block2.Height, block2.GetHash());
            chain = await _blockchainService.GetChainAsync();
            chain.LastIrreversibleBlockHeight.ShouldBe(2);
            
            context = await _blockchainNodeContextService.StartAsync(dto);
            chain = await _blockchainService.GetChainAsync();
            context.ChainId.ShouldBe(dto.ChainId);
            chain.BestChainHeight.ShouldBe(2);
            chain.BestChainHash.ShouldBe(block2.GetHash());
            
            _kernelNodeTestContext.MockConsensusService.Verify(
                s => s.TriggerConsensusAsync(It.IsAny<ChainContext>()), Times.Exactly(2));
        }

        [Fact]
        public async Task FinishInitialSync_Test()
        {
            InitialSyncFinishedEvent eventData = null;
            _localEventBus.Subscribe<InitialSyncFinishedEvent>(d =>
            {
                eventData = d;
                return Task.CompletedTask;
            });

            await _blockchainNodeContextService.FinishInitialSyncAsync();
            eventData.ShouldNotBeNull();
        }
    }
}