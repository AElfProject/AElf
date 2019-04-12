using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.WebApp.Application.Chain.Dto;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.EventBus.Local;

namespace AElf.WebApp.Application.Chain
{
    public interface IChainAppService : IApplicationService
    {
        Task<GetChainInformationOutput> GetChainInformation();

        Task<string> Call(string rawTransaction);

        Task<byte[]> GetFileDescriptorSet(string address);

        Task<BroadcastTransactionOutput> BroadcastTransaction(string rawTransaction);

        Task<string[]> BroadcastTransactions(string rawTransactions);

        Task<TransactionResultDto> GetTransactionResult(string transactionId);

        Task<List<TransactionResultDto>> GetTransactionsResult(string blockHash, int offset = 0, int limit = 10);

        Task<long> GetBlockHeight();

        Task<BlockDto> GetBlockInfo(long blockHeight, bool includeTransactions = false);

        Task<GetTransactionPoolStatusOutput> GetTransactionPoolStatus();

        Task<ChainStatusDto> GetChainStatus();

        Task<BlockStateDto> GetBlockState(string blockHash);
    }
    
    public class ChainAppService : IChainAppService
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        private readonly ITransactionManager _transactionManager;
        private readonly ITransactionResultQueryService _transactionResultQueryService;
        private readonly ITxHub _txHub;
        public IStateStore<BlockStateSet> _blockStateSets;
        public ILogger<ChainAppService> Logger { get; set; }
        
        public ILocalEventBus LocalEventBus { get; set; }

        public ChainAppService(IBlockchainService blockchainService,
            ISmartContractAddressService smartContractAddressService,
            ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService,
            ITransactionManager transactionManager,
            ITransactionResultQueryService transactionResultQueryService,
            ITxHub txHub,
            IStateStore<BlockStateSet> blockStateSets
            )
        {
            _blockchainService = blockchainService;
            _smartContractAddressService = smartContractAddressService;
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
            _transactionManager = transactionManager;
            _transactionResultQueryService = transactionResultQueryService;
            _txHub = txHub;
            _blockStateSets = blockStateSets;
            
            Logger = NullLogger<ChainAppService>.Instance;
            LocalEventBus = NullLocalEventBus.Instance;
        }
        
        public Task<GetChainInformationOutput> GetChainInformation()
        {
            var basicContractZero = _smartContractAddressService.GetZeroSmartContractAddress();

            return Task.FromResult(new GetChainInformationOutput
            {
                GenesisContractAddress = basicContractZero?.GetFormatted(),
                ChainId = ChainHelpers.ConvertChainIdToBase58(_blockchainService.GetChainId())
            });
        }

        public async Task<string> Call(string rawTransaction)
        {
            try
            {
                var hexString = ByteArrayHelpers.FromHexString(rawTransaction);
                var transaction = Transaction.Parser.ParseFrom(hexString);
                var response = await CallReadOnly(transaction);
                return response?.ToHex();
            }
            catch
            {
                throw new UserFriendlyException(Error.Message[Error.InvalidTransaction],Error.InvalidTransaction.ToString());
            }
        }
        
        public async Task<byte[]> GetFileDescriptorSet(string address)
        {
            try
            {
                var result = await GetFileDescriptorSetAsync(Address.Parse(address));
                return result;
            }
            catch
            {
                throw new UserFriendlyException(Error.Message[Error.NotFound], Error.NotFound.ToString());
            }
        }
        
        public async Task<BroadcastTransactionOutput> BroadcastTransaction(string rawTransaction)
        {
            var txIds = await PublishTransactionsAsync(new []{rawTransaction});
            return new BroadcastTransactionOutput
            {
                TransactionId = txIds[0]
            };
        }
        
        public async Task<string[]> BroadcastTransactions(string rawTransactions)
        {
            var txIds = await PublishTransactionsAsync(rawTransactions.Split(","));
            
            return txIds;
        }
        
        public async Task<TransactionResultDto> GetTransactionResult(string transactionId)
        {
            Hash transactionHash;
            try
            {
                transactionHash = Hash.LoadHex(transactionId);
            }
            catch
            {
                throw new UserFriendlyException(Error.Message[Error.InvalidTransactionId],Error.InvalidTransactionId.ToString());
            }

            var transactionResult = await GetTransactionResult(transactionHash);
            var transaction = await _transactionManager.GetTransaction(transactionResult.TransactionId);

            var output = JsonConvert.DeserializeObject<TransactionResultDto>(transactionResult.ToString());
            if (transactionResult.Status == TransactionResultStatus.Mined)
            {
                var block = await GetBlockAtHeight(transactionResult.BlockNumber);
                output.BlockHash = block.GetHash().ToHex();
            }

            if (transactionResult.Status == TransactionResultStatus.Failed)
                output.Error = transactionResult.Error;

            if (transactionResult.Status == TransactionResultStatus.NotExisted)
            {
                output.Status = transactionResult.Status.ToString();
                return output;
            }

            output.Transaction = JsonConvert.DeserializeObject<TransactionDto>(transaction.ToString());
            return output;
        }

        public async Task<List<TransactionResultDto>> GetTransactionsResult(string blockHash, int offset = 0, int limit = 10)
        {
            if (offset < 0)
            {
                throw new UserFriendlyException(Error.Message[Error.InvalidOffset],Error.InvalidOffset.ToString());
            }

            if (limit <= 0 || limit > 100)
            {
                throw new UserFriendlyException(Error.Message[Error.InvalidLimit],Error.InvalidLimit.ToString());
            }

            Hash realBlockHash;
            try
            {
                realBlockHash = Hash.LoadHex(blockHash);
            }
            catch
            {
                throw new UserFriendlyException(Error.Message[Error.InvalidBlockHash],Error.InvalidBlockHash.ToString());
            }

            var block = await GetBlock(realBlockHash);
            if (block == null)
            {
                throw new UserFriendlyException(Error.Message[Error.NotFound],Error.NotFound.ToString());
            }

            var output = new List<TransactionResultDto>();
            if (offset <= block.Body.Transactions.Count - 1)
            {
                limit = Math.Min(limit, block.Body.Transactions.Count - offset);
                var transactionHashes = block.Body.Transactions.ToList().GetRange(offset, limit);
                foreach (var hash in transactionHashes)
                {
                    var transactionResult = await GetTransactionResult(hash);
                    var transactionResultDto = JsonConvert.DeserializeObject<TransactionResultDto>(transactionResult.ToString());
                    var transaction = await _transactionManager.GetTransaction(transactionResult.TransactionId);
                    transactionResultDto.BlockHash = block.GetHash().ToHex();

                    if (transactionResult.Status == TransactionResultStatus.Failed)
                        transactionResultDto.Error = transactionResult.Error;

                    transactionResultDto.Transaction = JsonConvert.DeserializeObject<TransactionDto>(transaction.ToString());

                    transactionResultDto.Status = transactionResult.Status.ToString();
                    output.Add(transactionResultDto);
                }
            }

            return output;
        }
        
        public async Task<long> GetBlockHeight()
        {
            var chainContext = await _blockchainService.GetChainAsync();
            return chainContext.BestChainHeight;
        }
        
        public async Task<BlockDto> GetBlockInfo(long blockHeight, bool includeTransactions = false)
        {
            var blockInfo = await GetBlockAtHeight(blockHeight);
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
                    Bloom = blockInfo.Header.Bloom.ToByteArray().ToHex()
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
        
        public async Task<GetTransactionPoolStatusOutput> GetTransactionPoolStatus()
        {
            var queued= await _txHub.GetTransactionPoolSizeAsync();
            return new GetTransactionPoolStatusOutput
            {
                Queued = queued
            };
        }
        
        public async Task<ChainStatusDto> GetChainStatus()
        {
            var chain = await _blockchainService.GetChainAsync();
            var branches = JsonConvert.DeserializeObject<Dictionary<string,long>>(chain.Branches.ToString());
            var formattedNotLinkedBlocks = new List<NotLinkedBlockDto>();

            foreach (var notLinkedBlock in chain.NotLinkedBlocks)
            {
                var block = await this.GetBlock(Hash.LoadBase64(notLinkedBlock.Value));
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
        
        public async Task<BlockStateDto> GetBlockState(string blockHash)
        {
            var stateStorageKey = Hash.LoadHex(blockHash).ToStorageKey();
            var blockState = await _blockStateSets.GetAsync(stateStorageKey);
            if (blockState == null)
                throw new UserFriendlyException(Error.Message[Error.NotFound],Error.NotFound.ToString());
            return JsonConvert.DeserializeObject<BlockStateDto>(blockState.ToString());
        }
        
        private async Task<Block> GetBlock(Hash blockHash)
        {
            return await _blockchainService.GetBlockByHashAsync(blockHash);
        }
        
        private async Task<string> GetTransactionParameters(Transaction tx)
        {
            string output = null;
            try
            {
                var chainContext = await GetChainContextAsync();

                output = await _transactionReadOnlyExecutionService.GetTransactionParametersAsync(
                    chainContext, tx);
            }
            catch (InvalidCastException ex)
            {
                Logger.LogWarning($"Unsupported type conversion error： {ex}");
            }

            return output;
        }
        
        private async Task<Block> GetBlockAtHeight(long height)
        {
            return await _blockchainService.GetBlockByHeightInBestChainBranchAsync(height);
        }
        
        private async Task<TransactionResult> GetTransactionResult(Hash txHash)
        {
            // in storage
            var res = await _transactionResultQueryService.GetTransactionResultAsync(txHash);
            if (res != null)
            {
                return res;
            }

            // in tx pool
            var receipt = await _txHub.GetTransactionReceiptAsync(txHash);
            if (receipt != null)
            {
                return new TransactionResult
                {
                    TransactionId = receipt.TransactionId,
                    Status = TransactionResultStatus.Pending
                };
            }

            // not existed
            return new TransactionResult
            {
                TransactionId = txHash,
                Status = TransactionResultStatus.NotExisted
            };
        }
        
        private async Task<string[]> PublishTransactionsAsync(string[] rawTransactions)
        {
            var txIds = new string[rawTransactions.Length];
            var transactions = new List<Transaction>();
            for (var i = 0; i < rawTransactions.Length; i++)
            {
                Transaction transaction;
                try
                {
                    var hexString = ByteArrayHelpers.FromHexString(rawTransactions[i]);
                    transaction = Transaction.Parser.ParseFrom(hexString);
                }
                catch
                {
                    throw new UserFriendlyException(Error.Message[Error.InvalidTransaction],Error.InvalidTransaction.ToString());
                }

                if (!transaction.VerifySignature())
                {
                    throw new UserFriendlyException(Error.Message[Error.InvalidTransaction],Error.InvalidTransaction.ToString());
                }

                transactions.Add(transaction);
                txIds[i] = transaction.GetHash().ToHex();
            }

            await LocalEventBus.PublishAsync(new TransactionsReceivedEvent()
            {
                Transactions = transactions
            });
            return txIds;
        }
        
        private async Task<byte[]> GetFileDescriptorSetAsync(Address address)
        {
            var chain = await _blockchainService.GetChainAsync();
            var chainContext = new ChainContext()
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };

            return await _transactionReadOnlyExecutionService.GetFileDescriptorSetAsync(chainContext, address);
        }
        
        private async Task<byte[]> CallReadOnly(Transaction tx)
        {
            var chainContext = await GetChainContextAsync();

            var trace = await _transactionReadOnlyExecutionService.ExecuteAsync(chainContext, tx, DateTime.Now);

            if (!string.IsNullOrEmpty(trace.StdErr))
                throw new Exception(trace.StdErr);

            return trace.ReturnValue.ToByteArray();
        }
        
        private async Task<ChainContext> GetChainContextAsync()
        {
            var chain = await _blockchainService.GetChainAsync();
            var chainContext = new ChainContext()
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };
            return chainContext;
        }
    }
}