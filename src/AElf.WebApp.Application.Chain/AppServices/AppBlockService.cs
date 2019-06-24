using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.WebApp.Application.Chain.Dto;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Hash = AElf.Types.Hash;

namespace AElf.WebApp.Application.Chain.AppServices
{
    public interface IAppBlockService
    {
        /// <summary>
        /// Get the height of the current chain.
        /// </summary>
        /// <returns></returns>
        Task<long> GetBlockHeightAsync();

        /// <summary>
        /// Get information about a given block by block hash. Otionally with the list of its transactions.
        /// </summary>
        /// <param name="blockHash">block hash</param>
        /// <param name="includeTransactions">include transactions or not</param>
        /// <returns></returns>
        Task<BlockDto> GetBlockAsync(string blockHash, bool includeTransactions = false);

        /// <summary>
        /// Gets the block asynchronous.
        /// </summary>
        /// <param name="blockHash">The block hash.</param>
        /// <returns></returns>
        Task<Block> GetBlockAsync(Hash blockHash);

        /// <summary>
        /// Get information about a given block by block height. Otionally with the list of its transactions.
        /// </summary>
        /// <param name="blockHeight">block height</param>
        /// <param name="includeTransactions">include transactions or not</param>
        /// <returns></returns>
        Task<BlockDto> GetBlockByHeightAsync(long blockHeight, bool includeTransactions = false);

        /// <summary>
        /// Get the current status of the block chain.
        /// </summary>
        /// <returns></returns>
        Task<ChainStatusDto> GetChainStatusAsync();

        /// <summary>
        /// Get the current state about a given block
        /// </summary>
        /// <param name="blockHash">block hash</param>
        /// <returns></returns>
        Task<BlockStateDto> GetBlockStateAsync(string blockHash);

        /// <summary>
        /// Get AEDPoS latest round information from last block header's consensus extra data of best chain.
        /// </summary>
        /// <returns></returns>
        Task<RoundDto> GetCurrentRoundInformationAsync();

        /// <summary>
        /// Gets the block at height asynchronous.
        /// </summary>
        /// <param name="height">The height.</param>
        /// <returns></returns>
        Task<Block> GetBlockAtHeightAsync(long height);

        /// <summary>
        /// Gets the chain context asynchronous.
        /// </summary>
        /// <returns></returns>
        Task<ChainContext> GetChainContextAsync();
    }

    /// <summary>
    /// app block service
    /// </summary>
    /// <seealso cref="AElf.WebApp.Application.Chain.AppServices.IAppBlockService" />
    public class AppBlockService : IAppBlockService,ITransientDependency
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly IBlockchainStateManager _blockchainStateManager;
        private readonly IBlockExtraDataService _blockExtraDataService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppBlockService"/> class.
        /// </summary>
        /// <param name="blockchainService">The blockchain service.</param>
        /// <param name="smartContractAddressService">The smart contract address service.</param>
        /// <param name="blockchainStateManager">The blockchain state manager.</param>
        /// <param name="blockExtraDataService">The block extra data service.</param>
        public AppBlockService(IBlockchainService blockchainService,
            ISmartContractAddressService smartContractAddressService,
            IBlockchainStateManager blockchainStateManager,
            IBlockExtraDataService blockExtraDataService)
        {
            _blockchainService = blockchainService;
            _smartContractAddressService = smartContractAddressService;
            _blockExtraDataService = blockExtraDataService;
            _blockchainStateManager = blockchainStateManager;
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
                realBlockHash = Hash.LoadHex(blockHash);
            }
            catch
            {
                throw new UserFriendlyException(Error.Message[Error.InvalidBlockHash],
                    Error.InvalidBlockHash.ToString());
            }

            var block = await GetBlockAsync(realBlockHash);

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
                    Extra = block.Header.BlockExtraDatas.ToString(),
                    Height = block.Header.Height,
                    Time = block.Header.Time.ToDateTime(),
                    ChainId = ChainHelpers.ConvertChainIdToBase58(block.Header.ChainId),
                    Bloom = block.Header.Bloom.ToByteArray().ToHex(),
                    SignerPubkey = block.Header.SignerPubkey.ToByteArray().ToHex()
                },
                Body = new BlockBodyDto()
                {
                    TransactionsCount = block.Body.TransactionsCount,
                    Transactions = new List<string>()
                }
            };

            if (includeTransactions)
            {
                var transactions = block.Body.Transactions;
                var txs = new List<string>();
                foreach (var txHash in transactions)
                {
                    txs.Add(txHash.ToHex());
                }

                blockDto.Body.Transactions = txs;
            }

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

            if (blockInfo == null)
            {
                throw new UserFriendlyException(Error.Message[Error.NotFound], Error.NotFound.ToString());
            }

            var blockDto = new BlockDto
            {
                BlockHash = blockInfo.GetHash().ToHex(),
                Header = new BlockHeaderDto
                {
                    PreviousBlockHash = blockInfo.Header.PreviousBlockHash.ToHex(),
                    MerkleTreeRootOfTransactions = blockInfo.Header.MerkleTreeRootOfTransactions.ToHex(),
                    MerkleTreeRootOfWorldState = blockInfo.Header.MerkleTreeRootOfWorldState.ToHex(),
                    Extra = blockInfo.Header.BlockExtraDatas.ToString(),
                    Height = blockInfo.Header.Height,
                    Time = blockInfo.Header.Time.ToDateTime(),
                    ChainId = ChainHelpers.ConvertChainIdToBase58(blockInfo.Header.ChainId),
                    Bloom = blockInfo.Header.Bloom.ToByteArray().ToHex(),
                    SignerPubkey = blockInfo.Header.SignerPubkey.ToByteArray().ToHex()
                },
                Body = new BlockBodyDto()
                {
                    TransactionsCount = blockInfo.Body.TransactionsCount,
                    Transactions = new List<string>()
                }
            };

            if (includeTransactions)
            {
                var transactions = blockInfo.Body.Transactions;
                var txs = new List<string>();
                foreach (var txHash in transactions)
                {
                    txs.Add(txHash.ToHex());
                }

                blockDto.Body.Transactions = txs;
            }

            return blockDto;
        }

        /// <summary>
        /// Get the current status of the block chain.
        /// </summary>
        /// <returns></returns>
        public async Task<ChainStatusDto> GetChainStatusAsync()
        {
            var basicContractZero = _smartContractAddressService.GetZeroSmartContractAddress();

            var chain = await _blockchainService.GetChainAsync();
            var branches = JsonConvert.DeserializeObject<Dictionary<string, long>>(chain.Branches.ToString());
            var formattedNotLinkedBlocks = new List<NotLinkedBlockDto>();

            foreach (var notLinkedBlock in chain.NotLinkedBlocks)
            {
                var block = await GetBlockAsync(Hash.LoadBase64(notLinkedBlock.Value));
                formattedNotLinkedBlocks.Add(new NotLinkedBlockDto
                {
                    BlockHash = block.GetHash().ToHex(),
                    Height = block.Height,
                    PreviousBlockHash = block.Header.PreviousBlockHash.ToHex()
                }
                );
            }

            return new ChainStatusDto()
            {
                ChainId = ChainHelpers.ConvertChainIdToBase58(chain.Id),
                GenesisContractAddress = basicContractZero?.GetFormatted(),
                Branches = branches,
                NotLinkedBlocks = formattedNotLinkedBlocks,
                LongestChainHeight = chain.LongestChainHeight,
                LongestChainHash = chain.LongestChainHash?.ToHex(),
                GenesisBlockHash = chain.GenesisBlockHash.ToHex(),
                LastIrreversibleBlockHash = chain.LastIrreversibleBlockHash?.ToHex(),
                LastIrreversibleBlockHeight = chain.LastIrreversibleBlockHeight,
                BestChainHash = chain.BestChainHash?.ToHex(),
                BestChainHeight = chain.BestChainHeight
            };
        }

        /// <summary>
        /// Get the current state about a given block
        /// </summary>
        /// <param name="blockHash">block hash</param>
        /// <returns></returns>
        public async Task<BlockStateDto> GetBlockStateAsync(string blockHash)
        {
            var blockState = await _blockchainStateManager.GetBlockStateSetAsync(Hash.LoadHex(blockHash));
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
                        ActualMiningTimes = i.Value.ActualMiningTimes.Select(t => t.ToDateTime()).ToList(),
                        ProducedTinyBlocks = i.Value.ProducedTinyBlocks,
                        ProducedBlocks = i.Value.ProducedBlocks,
                        MissedBlocks = i.Value.MissedTimeSlots,
                        InValue = i.Value.InValue?.ToHex(),
                        OutValue = i.Value.OutValue?.ToHex(),
                        PreviousInValue = i.Value.PreviousInValue?.ToHex()
                    }),
                RoundNumber = round.RoundNumber,
                TermNumber = round.TermNumber,
                RoundId = round.RealTimeMinersInformation.Values.Select(bpInfo => bpInfo.ExpectedMiningTime.Seconds)
                    .Sum()
            };
        }

        /// <summary>
        /// Gets the block at height asynchronous.
        /// </summary>
        /// <param name="height">The height.</param>
        /// <returns></returns>
        public async Task<Block> GetBlockAtHeightAsync(long height)
        {
            return await _blockchainService.GetBlockByHeightInBestChainBranchAsync(height);
        }

        /// <summary>
        /// Gets the chain context asynchronous.
        /// </summary>
        /// <returns></returns>
        public async Task<ChainContext> GetChainContextAsync()
        {
            var chain = await _blockchainService.GetChainAsync();
            var chainContext = new ChainContext()
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };
            return chainContext;
        }

        /// <summary>
        /// Gets the block asynchronous.
        /// </summary>
        /// <param name="blockHash">The block hash.</param>
        /// <returns></returns>
        public async Task<Block> GetBlockAsync(Hash blockHash)
        {
            return await _blockchainService.GetBlockByHashAsync(blockHash);
        }
    }
}