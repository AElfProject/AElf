using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.WebApp.Application.Chain.Dto;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
    }
    
    public class ChainAppService : IChainAppService
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        public ILogger<ChainAppService> Logger { get; set; }
        
        public ILocalEventBus LocalEventBus { get; set; }

        public ChainAppService(IBlockchainService blockchainService,
            ISmartContractAddressService smartContractAddressService,
            ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService
            )
        {
            _blockchainService = blockchainService;
            _smartContractAddressService = smartContractAddressService;
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
            
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