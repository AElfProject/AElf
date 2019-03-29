using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.WebApp.Application.Chain.Dto;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.Application.Services;

namespace AElf.WebApp.Application.Chain
{
    public interface IChainAppService : IApplicationService
    {
        Task<WebAppOutput<GetChainInformationResult>> GetChainInformation();

        Task<WebAppOutput<string>> Call(string rawTransaction);
    }
    
    public class ChainAppService : IChainAppService
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        public ILogger<ChainAppService> Logger { get; set; }

        public ChainAppService(IBlockchainService blockchainService,
            ISmartContractAddressService smartContractAddressService,
            ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService
            )
        {
            _blockchainService = blockchainService;
            _smartContractAddressService = smartContractAddressService;
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
            
            Logger = NullLogger<ChainAppService>.Instance;
        }
        
        public Task<WebAppOutput<GetChainInformationResult>> GetChainInformation()
        {
            var basicContractZero = _smartContractAddressService.GetZeroSmartContractAddress();

            return Task.FromResult(new WebAppOutput<GetChainInformationResult>
            {
                Result = new GetChainInformationResult
                {
                    GenesisContractAddress = basicContractZero?.GetFormatted(),
                    ChainId = ChainHelpers.ConvertChainIdToBase58(_blockchainService.GetChainId())
                }
            });
        }

        public async Task<WebAppOutput<string>> Call(string rawTransaction)
        {
            try
            {
                var hexString = ByteArrayHelpers.FromHexString(rawTransaction);
                var transaction = Transaction.Parser.ParseFrom(hexString);
                var response = await CallReadOnly(transaction);
                return new WebAppOutput<string>
                {
                    Result = response?.ToHex()
                };
            }
            catch
            {
                return new WebAppOutput<string>
                {
                    Code = Error.InvalidTransaction,
                    Message = Error.Message[Error.InvalidTransaction]
                };
            }
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