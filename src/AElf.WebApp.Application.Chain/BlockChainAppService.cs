using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.WebApp.Application.Chain.Dto;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.EventBus.Local;

namespace AElf.WebApp.Application.Chain
{
    public interface IBlockChainAppService : IApplicationService
    {
        Task<string> Call(CallInput input);

        Task<byte[]> GetContractFileDescriptorSet(string address);

        Task<CreateRawTransactionOutput> CreateRawTransaction(CreateRawTransactionInput input);
        
        Task<SendRawTransactionOutput> SendRawTransaction(SendRawTransactionInput input);

        Task<BroadcastTransactionOutput> BroadcastTransaction(BroadcastTransactionInput input);

        Task<string[]> BroadcastTransactions(BroadcastTransactionsInput input);

        Task<TransactionResultDto> GetTransactionResult(string transactionId);

        Task<List<TransactionResultDto>> GetTransactionResults(string blockHash, int offset = 0, int limit = 10);

        Task<long> GetBlockHeight();

        Task<BlockDto> GetBlock(string blockHash, bool includeTransactions = false);

        Task<BlockDto> GetBlockByHeight(long blockHeight, bool includeTransactions = false);
        
        Task<GetTransactionPoolStatusOutput> GetTransactionPoolStatus();

        Task<ChainStatusDto> GetChainStatus();

        Task<BlockStateDto> GetBlockState(string blockHash);
    }
    
    public class BlockChainAppService : IBlockChainAppService
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        private readonly ITransactionManager _transactionManager;
        private readonly ITransactionResultQueryService _transactionResultQueryService;
        private readonly ITxHub _txHub;
        private readonly IBlockchainStateManager _blockchainStateManager;
        public ILogger<BlockChainAppService> Logger { get; set; }
        
        public ILocalEventBus LocalEventBus { get; set; }

        public BlockChainAppService(IBlockchainService blockchainService,
            ISmartContractAddressService smartContractAddressService,
            ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService,
            ITransactionManager transactionManager,
            ITransactionResultQueryService transactionResultQueryService,
            ITxHub txHub,
            IBlockchainStateManager blockchainStateManager
            )
        {
            _blockchainService = blockchainService;
            _smartContractAddressService = smartContractAddressService;
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
            _transactionManager = transactionManager;
            _transactionResultQueryService = transactionResultQueryService;
            _txHub = txHub;
            _blockchainStateManager = blockchainStateManager;
            
            Logger = NullLogger<BlockChainAppService>.Instance;
            LocalEventBus = NullLocalEventBus.Instance;
        }

        /// <summary>
        /// Call a read-only method on a contract.
        /// </summary>
        /// <returns></returns>
        public async Task<string> Call(CallInput input)
        {
            try
            {
                var hexString = ByteArrayHelpers.FromHexString(input.RawTransaction);
                var transaction = Transaction.Parser.ParseFrom(hexString);
                if (!transaction.VerifySignature())
                {
                    throw new UserFriendlyException(Error.Message[Error.InvalidTransaction],Error.InvalidTransaction.ToString());
                }
                var response = await CallReadOnly(transaction);
                return response?.ToHex();
            }
            catch
            {
                throw new UserFriendlyException(Error.Message[Error.InvalidTransaction],Error.InvalidTransaction.ToString());
            }
        }
        

        /// <summary>
        /// Get the protobuf definitions related to a contract
        /// </summary>
        /// <param name="address">contract address</param>
        /// <returns></returns>
        public async Task<byte[]> GetContractFileDescriptorSet(string address)
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
        
        /// <summary>
        /// Creates an unsigned serialized transaction 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<CreateRawTransactionOutput> CreateRawTransaction(CreateRawTransactionInput input)
        {
            var transaction = new Transaction
            {
                From = Address.Parse(input.From),
                To = Address.Parse(input.To),
                RefBlockNumber = input.RefBlockNumber,
                RefBlockPrefix = ByteString.CopyFrom(Hash.LoadHex(input.RefBlockHash).Value.Take(4).ToArray()),
                MethodName = input.MethodName
            };
            var methodDescriptor = await GetContractMethodDescriptorAsync(Address.Parse(input.To), input.MethodName);
            if (methodDescriptor == null)
                throw new UserFriendlyException(Error.Message[Error.NoMatchMethodInContractAddress],
                    Error.NoMatchMethodInContractAddress.ToString());
            try
            {
                transaction.Params = methodDescriptor.InputType.Parser.ParseJson(input.Params).ToByteString();
            }
            catch
            {
                throw new UserFriendlyException(Error.Message[Error.InvalidParams],Error.InvalidParams.ToString());
            }
            return new CreateRawTransactionOutput
            {
                RawTransaction = transaction.ToByteArray().ToHex()
            };
        }

        /// <summary>
        /// send a transaction
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<SendRawTransactionOutput> SendRawTransaction(SendRawTransactionInput input)
        {
            var transaction = Transaction.Parser.ParseFrom(ByteArrayHelpers.FromHexString(input.Transaction));
            transaction.Signature = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(input.Signature));
            var txIds = await PublishTransactionsAsync(new[] {transaction.ToByteArray().ToHex()});

            var output = new SendRawTransactionOutput
            {
                TransactionId = txIds[0]
            };

            if (!input.ReturnTransaction) return output;
            
            var transactionDto = JsonConvert.DeserializeObject<TransactionDto>(transaction.ToString());
            var contractMethodDescriptor = await GetContractMethodDescriptorAsync(transaction.To, transaction.MethodName);
            if (contractMethodDescriptor == null)
                throw new UserFriendlyException(Error.Message[Error.NoMatchMethodInContractAddress],
                    Error.NoMatchMethodInContractAddress.ToString());
            try
            {
                transactionDto.Params =
                    JsonFormatter.ToDiagnosticString(
                        contractMethodDescriptor.InputType.Parser.ParseFrom(transaction.Params));
            }
            catch
            {
                throw new UserFriendlyException(Error.Message[Error.InvalidParams],Error.InvalidParams.ToString());
            }
            output.Transaction = transactionDto;

            return output;
        }

        /// <summary>
        /// Broadcast a transaction
        /// </summary>
        /// <returns></returns>
        public async Task<BroadcastTransactionOutput> BroadcastTransaction(BroadcastTransactionInput input)
        {
            var txIds = await PublishTransactionsAsync(new[] {input.RawTransaction});
            return new BroadcastTransactionOutput
            {
                TransactionId = txIds[0]
            };
        }
        

        /// <summary>
        /// Broadcast multiple transactions
        /// </summary>
        /// <returns></returns>
        public async Task<string[]> BroadcastTransactions(BroadcastTransactionsInput input)
        {
            var txIds = await PublishTransactionsAsync(input.RawTransactions.Split(","));
            
            return txIds;
        }
        
        /// <summary>
        /// Get the current status of a transaction
        /// </summary>
        /// <param name="transactionId">transaction id</param>
        /// <returns></returns>
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
            
            var methodDescriptor = await GetContractMethodDescriptorAsync(transaction.To, transaction.MethodName);
            output.Transaction.Params = JsonFormatter.ToDiagnosticString(
                methodDescriptor.InputType.Parser.ParseFrom(transaction.Params));
            
            return output;
        }

        /// <summary>
        /// Get multiple transaction results.
        /// </summary>
        /// <param name="blockHash">block hash</param>
        /// <param name="offset">offset</param>
        /// <param name="limit">limit</param>
        /// <returns></returns>
        /// <exception cref="UserFriendlyException"></exception>
        public async Task<List<TransactionResultDto>> GetTransactionResults(string blockHash, int offset = 0, int limit = 10)
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

                    var methodDescriptor = await GetContractMethodDescriptorAsync(transaction.To, transaction.MethodName);
                    transactionResultDto.Transaction.Params = JsonFormatter.ToDiagnosticString(
                        methodDescriptor.InputType.Parser.ParseFrom(transaction.Params));
                        
                    transactionResultDto.Status = transactionResult.Status.ToString();
                    output.Add(transactionResultDto);
                }
            }

            return output;
        }
        
        /// <summary>
        /// Get the height of the current chain.
        /// </summary>
        /// <returns></returns>
        public async Task<long> GetBlockHeight()
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
        public async Task<BlockDto> GetBlock(string blockHash, bool includeTransactions = false)
        {
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
                    Bloom = block.Header.Bloom.ToByteArray().ToHex()
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
        public async Task<BlockDto> GetBlockByHeight(long blockHeight, bool includeTransactions = false)
        {
            if (blockHeight == 0)
                throw new UserFriendlyException(Error.Message[Error.NotFound], Error.NotFound.ToString());
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
        
        /// <summary>
        /// Get the transaction pool status.
        /// </summary>
        /// <returns></returns>
        public async Task<GetTransactionPoolStatusOutput> GetTransactionPoolStatus()
        {
            var queued= await _txHub.GetTransactionPoolSizeAsync();
            return new GetTransactionPoolStatusOutput
            {
                Queued = queued
            };
        }
        
        /// <summary>
        /// Get the current status of the block chain.
        /// </summary>
        /// <returns></returns>
        public async Task<ChainStatusDto> GetChainStatus()
        {
            var basicContractZero = _smartContractAddressService.GetZeroSmartContractAddress();
     
            var chain = await _blockchainService.GetChainAsync();
            var branches = JsonConvert.DeserializeObject<Dictionary<string,long>>(chain.Branches.ToString());
            var formattedNotLinkedBlocks = new List<NotLinkedBlockDto>();

            foreach (var notLinkedBlock in chain.NotLinkedBlocks)
            {
                var block = await GetBlock(Hash.LoadBase64(notLinkedBlock.Value));
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
        public async Task<BlockStateDto> GetBlockState(string blockHash)
        {
            var blockState = await _blockchainStateManager.GetBlockStateSetAsync(Hash.LoadHex(blockHash));
            if (blockState == null)
                throw new UserFriendlyException(Error.Message[Error.NotFound],Error.NotFound.ToString());
            return JsonConvert.DeserializeObject<BlockStateDto>(blockState.ToString());
        }
        
        private async Task<Block> GetBlock(Hash blockHash)
        {
            return await _blockchainService.GetBlockByHashAsync(blockHash);
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
        
        private async Task<IEnumerable<FileDescriptor>> GetFileDescriptorsAsync(Address address)
        {
            var chain = await _blockchainService.GetChainAsync();
            var chainContext = new ChainContext()
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };

            return await _transactionReadOnlyExecutionService.GetFileDescriptorsAsync(chainContext, address);
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
        
        private async Task<MethodDescriptor> GetContractMethodDescriptorAsync(Address contractAddress, string methodName)
        {
            IEnumerable<FileDescriptor> fileDescriptors;
            try
            {
                fileDescriptors = await GetFileDescriptorsAsync(contractAddress);
            }
            catch
            {
                throw new UserFriendlyException(Error.Message[Error.InvalidContractAddress],Error.InvalidContractAddress.ToString());
            }
            
            foreach (var fileDescriptor in fileDescriptors)
            {
                var method = fileDescriptor.Services.Select(s => s.FindMethodByName(methodName)).FirstOrDefault();
                if (method == null) continue;
                return method;
            }

            return null;
        }
    }
}