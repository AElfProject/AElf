using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee
{
    internal class ClaimTransactionFeesValidationProvider : IBlockValidationProvider
    {
        private readonly ITotalTransactionFeesMapProvider _totalTransactionFeesMapProvider;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly IContractReaderFactory<TokenContractImplContainer.TokenContractImplStub>
            _contractReaderFactory;

        public ILogger<ClaimTransactionFeesValidationProvider> Logger { get; set; }

        public ClaimTransactionFeesValidationProvider(ITotalTransactionFeesMapProvider totalTransactionFeesMapProvider,
            ISmartContractAddressService smartContractAddressService,
            IContractReaderFactory<TokenContractImplContainer.TokenContractImplStub> contractReaderFactory)
        {
            _totalTransactionFeesMapProvider = totalTransactionFeesMapProvider;
            _smartContractAddressService = smartContractAddressService;
            _contractReaderFactory = contractReaderFactory;

            Logger = NullLogger<ClaimTransactionFeesValidationProvider>.Instance;
        }

        /// <summary>
        /// No need to validate before attaching.
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public Task<bool> ValidateBeforeAttachAsync(IBlock block)
        {
            return Task.FromResult(true);
        }

        /// <summary>
        /// No need to validate before executing.
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public Task<bool> ValidateBlockBeforeExecuteAsync(IBlock block)
        {
            return Task.FromResult(true);
        }

        /// <summary>
        /// Compare 
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public Task<bool> ValidateBlockAfterExecuteAsync(IBlock block)
        {
            return Task.FromResult(true);
        }
    }
}