using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.SmartContract.Application;
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

        Task<GetTransactionResultOutput> GetTransactionResult(string transactionId);
    }
    
    public class ChainAppService : IChainAppService
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        private readonly ITransactionManager _transactionManager;
        private readonly ITransactionResultQueryService _transactionResultQueryService;
        private readonly ITxHub _txHub;
        public ILogger<ChainAppService> Logger { get; set; }
        
        public ILocalEventBus LocalEventBus { get; set; }

        public ChainAppService(IBlockchainService blockchainService,
            ISmartContractAddressService smartContractAddressService,
            ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService,
            ITransactionManager transactionManager,
            ITransactionResultQueryService transactionResultQueryService,
            ITxHub txHub
            )
        {
            _blockchainService = blockchainService;
            _smartContractAddressService = smartContractAddressService;
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
            _transactionManager = transactionManager;
            _transactionResultQueryService = transactionResultQueryService;
            _txHub = txHub;
            
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
        
        public async Task<GetTransactionResultOutput> GetTransactionResult(string transactionId)
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

            var output = JsonConvert.DeserializeObject<GetTransactionResultOutput>(transactionResult.ToString());
            if (transactionResult.Status == TransactionResultStatus.Mined)
            {
                var block = await GetBlockAtHeight(transactionResult.BlockNumber);
                output.BlockHash = block.GetHash().ToHex();
            }

            if (transactionResult.Status == TransactionResultStatus.Failed)
                output.Error = transactionResult.Error;

            output.Transaction = JsonConvert.DeserializeObject<TransactionDto>(transaction.ToString());
            var p = await GetTransactionParameters(transaction);
            try
            {
                output.Transaction.Params = ((JObject) JsonConvert.DeserializeObject(p)).ToString();
            }
            catch
            {
                // Params is not structured but plain string
                output.Transaction.Params = p;
            }
            
            return output;
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