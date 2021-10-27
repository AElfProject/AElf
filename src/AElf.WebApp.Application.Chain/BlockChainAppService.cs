using System.Collections.Concurrent;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Types;
using AElf.WebApp.Application.Chain.Dto;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.TransactionPool.Application;
using Google.Protobuf;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.EventBus.Local;
using Volo.Abp.ObjectMapping;

namespace AElf.WebApp.Application.Chain
{
    public interface IBlockChainAppService : IApplicationService
    {
        Task<long> GetBlockHeightAsync();

        Task<BlockDto> GetBlockAsync(string blockHash, bool includeTransactions = false);

        Task<BlockDto> GetBlockByHeightAsync(long blockHeight, bool includeTransactions = false);

        Task<GetTransactionPoolStatusOutput> GetTransactionPoolStatusAsync();

        Task<BlockStateDto> GetBlockStateAsync(string blockHash);
    }

    public class BlockChainAppService : IBlockChainAppService
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ITransactionPoolService _transactionPoolService;
        private readonly IBlockStateSetManger _blockStateSetManger;
        private readonly IObjectMapper<ChainApplicationWebAppAElfModule> _objectMapper;

        public ILogger<BlockChainAppService> Logger { get; set; }

        public ILocalEventBus LocalEventBus { get; set; }

        public BlockChainAppService(IBlockchainService blockchainService,
            IBlockStateSetManger blockStateSetManger,
            ITransactionPoolService transactionPoolService,
            IObjectMapper<ChainApplicationWebAppAElfModule> objectMapper)
        {
            _blockchainService = blockchainService;
            _blockStateSetManger = blockStateSetManger;
            _transactionPoolService = transactionPoolService;
            _objectMapper = objectMapper;

            Logger = NullLogger<BlockChainAppService>.Instance;
            LocalEventBus = NullLocalEventBus.Instance;
        }

        /// <summary>
        /// Get the height of the current chain.
        /// </summary>
        /// <returns></returns>
        public async Task<long> GetBlockHeightAsync()
        {
            var chainContext = await _blockchainService.GetChainAsync();
            return chainContext.BestChainHeight;
        }

        /// <summary>
        /// Get information about a given block by block hash. Otionally with the list of its transactions.
        /// </summary>
        /// <param name="blockHash">block hash</param>
        /// <param name="includeTransactions">include transactions or not</param>
        /// <returns></returns>
        public async Task<BlockDto> GetBlockAsync(string blockHash, bool includeTransactions = false)
        {
            Hash realBlockHash;
            try
            {
                realBlockHash = Hash.LoadFromHex(blockHash);
            }
            catch
            {
                throw new UserFriendlyException(Error.Message[Error.InvalidBlockHash],
                    Error.InvalidBlockHash.ToString());
            }

            var block = await GetBlockAsync(realBlockHash);

            var blockDto = CreateBlockDto(block, includeTransactions);

            return blockDto;
        }

        /// <summary>
        /// Get information about a given block by block height. Optionally with the list of its transactions.
        /// </summary>
        /// <param name="blockHeight">block height</param>
        /// <param name="includeTransactions">include transactions or not</param>
        /// <returns></returns>
        public async Task<BlockDto> GetBlockByHeightAsync(long blockHeight, bool includeTransactions = false)
        {
            if (blockHeight == 0)
                throw new UserFriendlyException(Error.Message[Error.NotFound], Error.NotFound.ToString());
            var blockInfo = await GetBlockAtHeightAsync(blockHeight);

            var blockDto = CreateBlockDto(blockInfo, includeTransactions);
            return blockDto;
        }

        /// <summary>
        /// Get the transaction pool status.
        /// </summary>
        /// <returns></returns>
        public async Task<GetTransactionPoolStatusOutput> GetTransactionPoolStatusAsync()
        {
            var transactionPoolStatus = await _transactionPoolService.GetTransactionPoolStatusAsync();
            return _objectMapper.Map<TransactionPoolStatus, GetTransactionPoolStatusOutput>(transactionPoolStatus);
        }

        /// <summary>
        /// Get the current state about a given block
        /// </summary>
        /// <param name="blockHash">block hash</param>
        /// <returns></returns>
        public async Task<BlockStateDto> GetBlockStateAsync(string blockHash)
        {
            var blockState = await _blockStateSetManger.GetBlockStateSetAsync(Hash.LoadFromHex(blockHash));
            if (blockState == null)
                throw new UserFriendlyException(Error.Message[Error.NotFound], Error.NotFound.ToString());

            return _objectMapper.Map<BlockStateSet, BlockStateDto>(blockState);
        }

        private async Task<Block> GetBlockAsync(Hash blockHash)
        {
            return await _blockchainService.GetBlockByHashAsync(blockHash);
        }

        private async Task<Block> GetBlockAtHeightAsync(long height)
        {
            return await _blockchainService.GetBlockByHeightInBestChainBranchAsync(height);
        }

        private BlockDto CreateBlockDto(Block block, bool includeTransactions)
        {
            if (block == null)
            {
                throw new UserFriendlyException(Error.Message[Error.NotFound], Error.NotFound.ToString());
            }

            return _objectMapper.GetMapper().Map<Block, BlockDto>(block,
                opt => opt.Items[BlockProfile.IncludeTransactions] = includeTransactions);
        }
    }
}