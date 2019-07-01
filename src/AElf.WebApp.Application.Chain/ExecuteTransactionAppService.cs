using System;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using AElf.WebApp.Application.Chain.Dto;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace AElf.WebApp.Application.Chain
{

    public interface IExecuteTransactionAppService : IApplicationService
    {
        Task<string> ExecuteTransactionAsync(ExecuteTransactionDto input);
        
        Task<string> ExecuteRawTransactionAsync(ExecuteRawTransactionDto input);

    }

    public class ExecuteTransactionAppService :IExecuteTransactionAppService
    {
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        private readonly IBlockchainService _blockchainService;
        
        public ExecuteTransactionAppService(
            ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService,
            IBlockchainService blockchainService)
        {
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
            _blockchainService = blockchainService;
        }
        
        /// <summary>
        /// Call a read-only method on a contract.
        /// </summary>
        /// <returns></returns>
        public async Task<string> ExecuteTransactionAsync(ExecuteTransactionDto input)
        {
            try
            {
                var byteArray = ByteArrayHelpers.FromHexString(input.RawTransaction);
                var transaction = Transaction.Parser.ParseFrom(byteArray);
                if (!transaction.VerifySignature())
                {
                    throw new UserFriendlyException(Error.Message[Error.InvalidTransaction],
                        Error.InvalidTransaction.ToString());
                }

                var response = await CallReadOnlyAsync(transaction);
                return response?.ToHex();
            }
            catch
            {
                throw new UserFriendlyException(Error.Message[Error.InvalidTransaction],
                    Error.InvalidTransaction.ToString());
            }
        }

        public async Task<string> ExecuteRawTransactionAsync(ExecuteRawTransactionDto input)
        {
            try
            {
                var byteArray = ByteArrayHelpers.FromHexString(input.RawTransaction);
                var transaction = Transaction.Parser.ParseFrom(byteArray);
                transaction.Signature = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(input.Signature));
                if (!transaction.VerifySignature())
                {
                    throw new UserFriendlyException(Error.Message[Error.InvalidTransaction],
                        Error.InvalidTransaction.ToString());
                }

                var response = await CallReadOnlyReturnReadableValueAsync(transaction);
                return response;
            }
            catch
            {
                throw new UserFriendlyException(Error.Message[Error.InvalidTransaction],
                    Error.InvalidTransaction.ToString());
            }
        }
        
        
        
        private async Task<byte[]> CallReadOnlyAsync(Transaction tx)
        {
            var chainContext = await GetChainContextAsync();

            var trace = await _transactionReadOnlyExecutionService.ExecuteAsync(chainContext, tx, DateTime.UtcNow.ToTimestamp());

            if (!string.IsNullOrEmpty(trace.Error))
                throw new Exception(trace.Error);

            return trace.ReturnValue.ToByteArray();
        }
        
        private async Task<string> CallReadOnlyReturnReadableValueAsync(Transaction tx)
        {
            var chainContext = await GetChainContextAsync();

            var trace = await _transactionReadOnlyExecutionService.ExecuteAsync(chainContext, tx, DateTime.UtcNow.ToTimestamp());

            if (!string.IsNullOrEmpty(trace.Error))
                throw new Exception(trace.Error);

            return trace.ReadableReturnValue;
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