using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Types;
using AElf.WebApp.Application.Chain.Dto;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.EventBus.Local;

namespace AElf.WebApp.Application.Chain
{
    public interface IBlockChainAppService : IApplicationService
    {
        Task<long> GetBlockHeightAsync();

        Task<BlockDto> GetBlockAsync(string blockHash, bool includeTransactions = false);

        Task<BlockDto> GetBlockByHeightAsync(long blockHeight, bool includeTransactions = false);

        Task<GetTransactionPoolStatusOutput> GetTransactionPoolStatusAsync();

        Task<BlockStateDto> GetBlockStateAsync(string blockHash);

        Task<RoundDto> GetCurrentRoundInformationAsync();
    }

    public class BlockChainAppService : IBlockChainAppService
    {
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockExtraDataService _blockExtraDataService;
        private readonly ITxHub _txHub;
        private readonly IBlockchainStateManager _blockchainStateManager;

        public ILogger<BlockChainAppService> Logger { get; set; }

        public ILocalEventBus LocalEventBus { get; set; }

        public BlockChainAppService(IBlockchainService blockchainService,
            IBlockExtraDataService blockExtraDataService,
            ITxHub txHub,
            IBlockchainStateManager blockchainStateManager)
        {
            _blockchainService = blockchainService;
            _blockExtraDataService = blockExtraDataService;
            _txHub = txHub;
            _blockchainStateManager = blockchainStateManager;

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
                realBlockHash = HashHelper.HexStringToHash(blockHash);
            }
            catch
            {
                throw new UserFriendlyException(Error.Message[Error.InvalidBlockHash],
                    Error.InvalidBlockHash.ToString());
            }

            var block = await GetBlockAsync(realBlockHash);
            var blockDto = CreateNewBlockDto(block, includeTransactions);

            return blockDto;
        }

        /// <summary>
        /// Get information about a given block by block height. Otionally with the list of its transactions.
        /// </summary>
        /// <param name="blockHeight">block height</param>
        /// <param name="includeTransactions">include transactions or not</param>
        /// <returns></returns>
        public async Task<BlockDto> GetBlockByHeightAsync(long blockHeight, bool includeTransactions = false)
        {
            if (blockHeight == 0)
                throw new UserFriendlyException(Error.Message[Error.NotFound], Error.NotFound.ToString());
            var blockInfo = await GetBlockAtHeightAsync(blockHeight);
            var blockDto = CreateNewBlockDto(blockInfo, includeTransactions);
            
            return blockDto;
        }

        /// <summary>
        /// Get the transaction pool status.
        /// </summary>
        /// <returns></returns>
        public async Task<GetTransactionPoolStatusOutput> GetTransactionPoolStatusAsync()
        {
            return new GetTransactionPoolStatusOutput
            {
                Queued = await _txHub.GetAllTransactionCountAsync(),
                Validated = await _txHub.GetValidatedTransactionCountAsync()
            };
        }

        /// <summary>
        /// Get the current state about a given block
        /// </summary>
        /// <param name="blockHash">block hash</param>
        /// <returns></returns>
        public async Task<BlockStateDto> GetBlockStateAsync(string blockHash)
        {
            var blockState = await _blockchainStateManager.GetBlockStateSetAsync(HashHelper.HexStringToHash(blockHash));
            if (blockState == null)
                throw new UserFriendlyException(Error.Message[Error.NotFound], Error.NotFound.ToString());
            
            return JsonConvert.DeserializeObject<BlockStateDto>(blockState.ToString());
        }

        /// <summary>
        /// Get AEDPoS latest round information from last block header's consensus extra data of best chain.
        /// </summary>
        /// <returns></returns>
        public async Task<RoundDto> GetCurrentRoundInformationAsync()
        {
            var blockHeader = await _blockchainService.GetBestChainLastBlockHeaderAsync();
            var consensusExtraData = _blockExtraDataService.GetExtraDataFromBlockHeader("Consensus", blockHeader);
            var information = AElfConsensusHeaderInformation.Parser.ParseFrom(consensusExtraData);
            var round = information.Round;
            return new RoundDto
            {
                ExtraBlockProducerOfPreviousRound = round.ExtraBlockProducerOfPreviousRound,
                RealTimeMinerInformation = round.RealTimeMinersInformation.ToDictionary(i => i.Key, i =>
                    new MinerInRoundDto
                    {
                        Order = i.Value.Order,
                        ExpectedMiningTime = i.Value.ExpectedMiningTime.ToDateTime(),
                        ActualMiningTimes = i.Value.ActualMiningTimes?.Select(t => t.ToDateTime()).ToList(),
                        ProducedTinyBlocks = i.Value.ProducedTinyBlocks,
                        ProducedBlocks = i.Value.ProducedBlocks,
                        MissedBlocks = i.Value.MissedTimeSlots,
                        InValue = i.Value.InValue?.ToHex(),
                        OutValue = i.Value.OutValue?.ToHex(),
                        PreviousInValue = i.Value.PreviousInValue?.ToHex(),
                        ImpliedIrreversibleBlockHeight = i.Value.ImpliedIrreversibleBlockHeight
                    }),
                RoundNumber = round.RoundNumber,
                TermNumber = round.TermNumber,
                ConfirmedIrreversibleBlockHeight = round.ConfirmedIrreversibleBlockHeight,
                ConfirmedIrreversibleBlockRoundNumber = round.ConfirmedIrreversibleBlockRoundNumber,
                IsMinerListJustChanged = round.IsMinerListJustChanged,
                RoundId = round.RealTimeMinersInformation.Values.Select(bpInfo => bpInfo.ExpectedMiningTime.Seconds)
                    .Sum()
            };
        }

        private async Task<Block> GetBlockAsync(Hash blockHash)
        {
            return await _blockchainService.GetBlockByHashAsync(blockHash);
        }

        private async Task<Block> GetBlockAtHeightAsync(long height)
        {
            return await _blockchainService.GetBlockByHeightInBestChainBranchAsync(height);
        }

        private BlockDto CreateNewBlockDto(Block block, bool includeTransactions)
        {
            if (block == null)
            {
                throw new UserFriendlyException(Error.Message[Error.NotFound], Error.NotFound.ToString());
            }
            var blockDto = new BlockDto
            {
                BlockHash = block.GetHash().ToHex(),
                Header = new BlockHeaderDto
                {
                    PreviousBlockHash = block.Header.PreviousBlockHash.ToHex(),
                    MerkleTreeRootOfTransactions = block.Header.MerkleTreeRootOfTransactions.ToHex(),
                    MerkleTreeRootOfWorldState = block.Header.MerkleTreeRootOfWorldState.ToHex(),
                    Extra = block.Header.ExtraData.ToString(),
                    Height = block.Header.Height,
                    Time = block.Header.Time.ToDateTime(),
                    ChainId = ChainHelper.ConvertChainIdToBase58(block.Header.ChainId),
                    Bloom = block.Header.Bloom.ToByteArray().ToHex(),
                    SignerPubkey = block.Header.SignerPubkey.ToByteArray().ToHex()
                },
                Body = new BlockBodyDto()
                {
                    TransactionsCount = block.Body.TransactionsCount,
                    Transactions = new List<string>()
                }
            };

            if (!includeTransactions) return blockDto;
            var transactions = block.Body.TransactionIds;
            var txs = new List<string>();
            foreach (var transactionId in transactions)
            {
                txs.Add(transactionId.ToHex());
            }
            blockDto.Body.Transactions = txs;

            return blockDto;
        }
    }
}