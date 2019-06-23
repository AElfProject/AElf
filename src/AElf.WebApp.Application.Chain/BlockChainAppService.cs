using AElf.WebApp.Application.Chain.AppServices;
using AElf.WebApp.Application.Chain.AppServices.AppTransactionResultService;
using AElf.WebApp.Application.Chain.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace AElf.WebApp.Application.Chain
{
    public interface IBlockChainAppService : IApplicationService
    {
        Task<string> ExecuteTransactionAsync(ExecuteTransactionDto input);

        Task<string> ExecuteRawTransactionAsync(ExecuteRawTransactionDto input);

        Task<byte[]> GetContractFileDescriptorSetAsync(string address);

        Task<CreateRawTransactionOutput> CreateRawTransactionAsync(CreateRawTransactionInput input);

        Task<SendRawTransactionOutput> SendRawTransactionAsync(SendRawTransactionInput input);

        Task<SendTransactionOutput> SendTransactionAsync(SendTransactionInput input);

        Task<string[]> SendTransactionsAsync(SendTransactionsInput input);

        Task<TransactionResultDto> GetTransactionResultAsync(string transactionId);

        Task<List<TransactionResultDto>> GetTransactionResultsAsync(string blockHash, int offset = 0, int limit = 10);

        Task<long> GetBlockHeightAsync();

        Task<BlockDto> GetBlockAsync(string blockHash, bool includeTransactions = false);

        Task<BlockDto> GetBlockByHeightAsync(long blockHeight, bool includeTransactions = false);

        Task<GetTransactionPoolStatusOutput> GetTransactionPoolStatusAsync();

        Task<ChainStatusDto> GetChainStatusAsync();

        Task<BlockStateDto> GetBlockStateAsync(string blockHash);

        List<TaskQueueInfoDto> GetTaskQueueStatusAsync();

        Task<RoundDto> GetCurrentRoundInformationAsync();
    }

    public class BlockChainAppService : IBlockChainAppService
    {
        private readonly IAppTransactionService _appTransactionService;

        private readonly IAppBlockService _appBlockService;

        private readonly IAppTaskQueueService _appTaskQueueService;

        private readonly IAppTransactionResultService _appTransactionResultService;

        public BlockChainAppService(IAppTransactionService appTransactionService,
        IAppBlockService appBlockService,
        IAppTaskQueueService appTaskQueueService,
        IAppTransactionResultService appTransactionResultService)
        {
            _appTransactionService = appTransactionService;
            _appBlockService = appBlockService;
            _appTaskQueueService = appTaskQueueService;
            _appTransactionResultService = appTransactionResultService;
        }

        /// <summary>
        /// Call a read-only method on a contract.
        /// </summary>
        /// <returns></returns>
        public async Task<string> ExecuteTransactionAsync(ExecuteTransactionDto input)
        {
            return await _appTransactionService.ExecuteTransactionAsync(input);
        }

        public async Task<string> ExecuteRawTransactionAsync(ExecuteRawTransactionDto input)
        {
            return await _appTransactionService.ExecuteRawTransactionAsync(input);
        }

        /// <summary>
        /// Get the protobuf definitions related to a contract
        /// </summary>
        /// <param name="address">contract address</param>
        /// <returns></returns>
        public async Task<byte[]> GetContractFileDescriptorSetAsync(string address)
        {
            return await _appTransactionService.GetContractFileDescriptorSetAsync(address);
        }

        /// <summary>
        /// Creates an unsigned serialized transaction
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<CreateRawTransactionOutput> CreateRawTransactionAsync(CreateRawTransactionInput input)
        {
            return await _appTransactionService.CreateRawTransactionAsync(input);
        }

        /// <summary>
        /// send a transaction
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<SendRawTransactionOutput> SendRawTransactionAsync(SendRawTransactionInput input)
        {
            return await _appTransactionService.SendRawTransactionAsync(input);
        }

        /// <summary>
        /// Broadcast a transaction
        /// </summary>
        /// <returns></returns>
        public async Task<SendTransactionOutput> SendTransactionAsync(SendTransactionInput input)
        {
            return await _appTransactionService.SendTransactionAsync(input);
        }

        /// <summary>
        /// Broadcast multiple transactions
        /// </summary>
        /// <returns></returns>
        public async Task<string[]> SendTransactionsAsync(SendTransactionsInput input)
        {
            return await _appTransactionService.SendTransactionsAsync(input);
        }

        /// <summary>
        /// Get the current status of a transaction
        /// </summary>
        /// <param name="transactionId">transaction id</param>
        /// <returns></returns>
        public async Task<TransactionResultDto> GetTransactionResultAsync(string transactionId)
        {
            return await _appTransactionResultService.GetTransactionResultAsync(transactionId);
        }

        /// <summary>
        /// Get multiple transaction results.
        /// </summary>
        /// <param name="blockHash">block hash</param>
        /// <param name="offset">offset</param>
        /// <param name="limit">limit</param>
        /// <returns></returns>
        /// <exception cref="UserFriendlyException"></exception>
        public async Task<List<TransactionResultDto>> GetTransactionResultsAsync(string blockHash, int offset = 0,
            int limit = 10)
        {
            return await _appTransactionResultService.GetTransactionResultsAsync(blockHash, offset,limit);
        }

        /// <summary>
        /// Get the height of the current chain.
        /// </summary>
        /// <returns></returns>
        public async Task<long> GetBlockHeightAsync()
        {
            return await _appBlockService.GetBlockHeightAsync();
        }

        /// <summary>
        /// Get information about a given block by block hash. Otionally with the list of its transactions.
        /// </summary>
        /// <param name="blockHash">block hash</param>
        /// <param name="includeTransactions">include transactions or not</param>
        /// <returns></returns>
        public async Task<BlockDto> GetBlockAsync(string blockHash, bool includeTransactions = false)
        {
            return await _appBlockService.GetBlockAsync(blockHash, includeTransactions);
        }

        /// <summary>
        /// Get the transaction pool status.
        /// </summary>
        /// <returns></returns>
        public async Task<GetTransactionPoolStatusOutput> GetTransactionPoolStatusAsync()
        {
            return await _appTransactionService.GetTransactionPoolStatusAsync();
        }

        /// <summary>
        /// Get the current status of the block chain.
        /// </summary>
        /// <returns></returns>
        public async Task<ChainStatusDto> GetChainStatusAsync()
        {
            return await _appBlockService.GetChainStatusAsync();
        }

        /// <summary>
        /// Get the current state about a given block
        /// </summary>
        /// <param name="blockHash">block hash</param>
        /// <returns></returns>
        public async Task<BlockStateDto> GetBlockStateAsync(string blockHash)
        {
            return await _appBlockService.GetBlockStateAsync(blockHash);
        }

        public List<TaskQueueInfoDto> GetTaskQueueStatusAsync()
        {
            return _appTaskQueueService.GetTaskQueueStatusAsync();
        }

        /// <summary>
        /// Get AEDPoS latest round information from last block header's consensus extra data of best chain.
        /// </summary>
        /// <returns></returns>
        public async Task<RoundDto> GetCurrentRoundInformationAsync()
        {
            return await _appBlockService.GetCurrentRoundInformationAsync();
        }

        /// <summary>
        /// Get information about a given block by block height. Otionally with the list of its transactions.
        /// </summary>
        /// <param name="blockHeight">block height</param>
        /// <param name="includeTransactions">include transactions or not</param>
        /// <returns></returns>
        public async Task<BlockDto> GetBlockByHeightAsync(long blockHeight, bool includeTransactions = false)
        {
            return await _appBlockService.GetBlockByHeightAsync(blockHeight, includeTransactions);
        }
    }
}