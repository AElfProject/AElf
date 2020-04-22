using System.Linq;
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
        public async Task<bool> ValidateBlockAfterExecuteAsync(IBlock block)
        {
            var tokenContractAddress =
                _smartContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider.Name);
            if (tokenContractAddress == null)
            {
                return true;
            }

            if (block.Header.Height <= 2) return true;

            var hashFromState = await _contractReaderFactory.Create(new ContractReaderContext
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Header.Height,
                ContractAddress = tokenContractAddress
            }).GetLatestTotalResourceTokensMapsHash.CallAsync(new Empty());

            var totalResourceTokensMapsFromProvider =
                await _totalResourceTokensMapsProvider.GetTotalResourceTokensMapsAsync(new ChainContext
                {
                    BlockHash = block.Header.PreviousBlockHash,
                    BlockHeight = block.Header.Height - 1
                });

            bool result;
            if (!hashFromState.Value.Any())
            {
                // Didn't donate resource tokens of previews block.
                result = !totalResourceTokensMapsFromProvider.Value.Any();
                return result;
            }

            // Normal case if donated resource tokens in preview block.
            var hashFromProvider = HashHelper.ComputeFromMessage(totalResourceTokensMapsFromProvider);
            result = hashFromProvider == hashFromState;
            if (result)
            {
                return true;
            }

            Logger.LogError($"Hash from provider: {hashFromProvider}\nHash from state: {hashFromState}");

            if (hashFromState == HashHelper.ComputeFromMessage(new TotalResourceTokensMaps
            {
                BlockHash = block.Header.PreviousBlockHash,
                BlockHeight = block.Header.Height - 1
            }))
            {
                // Didn't pass log event processor.
                result = totalResourceTokensMapsFromProvider.BlockHeight != block.Header.Height - 1;
            }

            return result;
        }
    }
}