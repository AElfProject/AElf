using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.SmartContract.ExecutionPluginForResourceFee
{
    internal class DonateResourceTokenValidationProvider : IBlockValidationProvider
    {
        private readonly ITotalResourceTokensMapsProvider _totalResourceTokensMapsProvider;
        private readonly ISmartContractAddressService _smartContractAddressService;

        private readonly IContractReaderFactory<TokenContractImplContainer.TokenContractImplStub>
            _contractReaderFactory;

        public ILogger<DonateResourceTokenValidationProvider> Logger { get; set; }

        public DonateResourceTokenValidationProvider(ITotalResourceTokensMapsProvider totalResourceTokensMapsProvider,
            ISmartContractAddressService smartContractAddressService,
            IContractReaderFactory<TokenContractImplContainer.TokenContractImplStub> contractReaderFactory)
        {
            _totalResourceTokensMapsProvider = totalResourceTokensMapsProvider;
            _smartContractAddressService = smartContractAddressService;
            _contractReaderFactory = contractReaderFactory;

            Logger = NullLogger<DonateResourceTokenValidationProvider>.Instance;
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