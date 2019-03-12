using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.ChainController.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Node.Domain;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Kernel.Types;
using Google.Protobuf;

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
    }


    //Maybe we should call it CSharpBlockchainNodeContextService, or we should spilt the logic depended on CSharp
    public class BlockchainNodeContextService : IBlockchainNodeContextService
    {
        private readonly ITxHub _txHub;
        private readonly IBlockchainService _blockchainService;
        private readonly IChainCreationService _chainCreationService;
        private readonly ISmartContractAddressUpdateService _smartContractAddressUpdateService;
        private readonly IDefaultContractZeroCodeProvider _defaultContractZeroCodeProvider;
        private readonly IConsensusService _consensusService;

        public BlockchainNodeContextService(
            IBlockchainService blockchainService, IChainCreationService chainCreationService, ITxHub txHub,
            ISmartContractAddressUpdateService smartContractAddressUpdateService,
            IDefaultContractZeroCodeProvider defaultContractZeroCodeProvider, IConsensusService consensusService)
        {
            _blockchainService = blockchainService;
            _chainCreationService = chainCreationService;
            _txHub = txHub;
            _smartContractAddressUpdateService = smartContractAddressUpdateService;
            _defaultContractZeroCodeProvider = defaultContractZeroCodeProvider;
            _consensusService = consensusService;
        }

        public async Task<BlockchainNodeContext> StartAsync(BlockchainNodeContextStartDto dto)
        {
            _defaultContractZeroCodeProvider.SetDefaultContractZeroRegistrationByType(dto.ZeroSmartContractType);

            var context = new BlockchainNodeContext
            {
                ChainId = dto.ChainId,
                TxHub = _txHub,
            };
            
            var chain = await _blockchainService.GetChainAsync();
            var restartNode = true;
            if (chain == null)
            {
                chain = await _chainCreationService.CreateNewChainAsync(dto.Transactions);
                restartNode = false;
            }

            await _smartContractAddressUpdateService.UpdateSmartContractAddressesAsync(
                await _blockchainService.GetBlockHeaderByHashAsync(chain.BestChainHash));

            if (restartNode)
            {
                await _consensusService.TriggerConsensusAsync();
            }

            return context;
        }

        public async Task StopAsync(BlockchainNodeContext blockchainNodeContext)
        {
        }
    }
}