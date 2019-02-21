using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.ChainController.Application;
using AElf.Kernel.Node.Domain;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Kernel.Types;
using Google.Protobuf;

namespace AElf.Kernel.Node.Application
{
    public class BlockchainNodeContextStartDto
    {
        public int ChainId { get; set; }

        public Transaction[] Transactions { get; set; }
    }

    public interface IBlockchainNodeContextService
    {
        Task<BlockchainNodeContext> StartAsync(BlockchainNodeContextStartDto dto);

        Task StopAsync(BlockchainNodeContext blockchainNodeContext);
    }

    
    //Maybe we should call it CSharpBlockchainNodeContextService, or we should spilt the logic depended on CSharp
    public class BlockchainNodeContextService : IBlockchainNodeContextService
    {
        private IChainRelatedComponentManager<ITxHub> _txHubs;
        private IBlockchainService _blockchainService;
        private IChainCreationService _chainCreationService;

        public BlockchainNodeContextService(IChainRelatedComponentManager<ITxHub> txHubs,
            IBlockchainService blockchainService, IChainCreationService chainCreationService)
        {
            _txHubs = txHubs;
            _blockchainService = blockchainService;
            _chainCreationService = chainCreationService;
        }

        public async Task<BlockchainNodeContext> StartAsync(BlockchainNodeContextStartDto dto)
        {
            var context = new BlockchainNodeContext
            {
                ChainId = dto.ChainId,
                TxHub = await _txHubs.CreateAsync(dto.ChainId)
            };
            var chain = await _blockchainService.GetChainAsync(dto.ChainId);

            if (chain == null)
            {
                await _chainCreationService.CreateNewChainAsync(dto.ChainId, dto.Transactions);
            }

            return context;
        }

        public async Task StopAsync(BlockchainNodeContext blockchainNodeContext)
        {
            await _txHubs.RemoveAsync(blockchainNodeContext.ChainId);
        }

        private byte[] ReadContractCode(string path)
        {
            return File.ReadAllBytes(Path.GetFullPath(path));
        }
        
    }
}