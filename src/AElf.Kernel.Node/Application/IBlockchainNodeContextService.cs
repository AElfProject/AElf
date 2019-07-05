using System;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.ChainController.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Node.Domain;
using AElf.Kernel.Node.Events;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Types;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.Node.Application
{
    public class BlockchainNodeContextStartDto
    {
        public int ChainId { get; set; }

        public Transaction[] Transactions { get; set; }

        public Type ZeroSmartContractType { get; set; }
    }

    public interface IBlockchainNodeContextService
    {
        Task<BlockchainNodeContext> StartAsync(BlockchainNodeContextStartDto dto);

        Task StopAsync(BlockchainNodeContext blockchainNodeContext);

        Task FinishInitialSyncAsync();
    }


    //Maybe we should call it CSharpBlockchainNodeContextService, or we should spilt the logic depended on CSharp
    public class BlockchainNodeContextService : IBlockchainNodeContextService
    {
        private readonly ITxHub _txHub;
        private readonly IDefaultContractZeroCodeProvider _defaultContractZeroCodeProvider;
        private readonly IBlockchainNodeContextProxyService _blockchainNodeContextProxyService;

        public ILocalEventBus EventBus { get; set; }

        public BlockchainNodeContextService(ITxHub txHub,
            IDefaultContractZeroCodeProvider defaultContractZeroCodeProvider,
            IBlockchainNodeContextProxyService blockchainNodeContextProxyService)
        {
            _txHub = txHub;
            _defaultContractZeroCodeProvider = defaultContractZeroCodeProvider;
            _blockchainNodeContextProxyService = blockchainNodeContextProxyService;

            EventBus = NullLocalEventBus.Instance;
        }

        public async Task<BlockchainNodeContext> StartAsync(BlockchainNodeContextStartDto dto)
        {
            _defaultContractZeroCodeProvider.SetDefaultContractZeroRegistrationByType(dto.ZeroSmartContractType);

            var context = new BlockchainNodeContext
            {
                ChainId = dto.ChainId,
                TxHub = _txHub,
            };
            var chain = await _blockchainNodeContextProxyService.BlockchainService.GetChainAsync() ??
                        await _blockchainNodeContextProxyService.ChainCreationService.CreateNewChainAsync(
                            dto.Transactions);

            await _blockchainNodeContextProxyService.SmartContractAddressUpdateService
                .UpdateSmartContractAddressesAsync(
                    await _blockchainNodeContextProxyService.BlockchainService.GetBlockHeaderByHashAsync(
                        chain.BestChainHash));

            await _blockchainNodeContextProxyService.ConsensusService.TriggerConsensusAsync(new ChainContext
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            });

            return context;
        }

        public async Task FinishInitialSyncAsync()
        {
            await EventBus.PublishAsync(new InitialSyncFinishedEvent());
        }

        public async Task StopAsync(BlockchainNodeContext blockchainNodeContext)
        {
        }
    }
}