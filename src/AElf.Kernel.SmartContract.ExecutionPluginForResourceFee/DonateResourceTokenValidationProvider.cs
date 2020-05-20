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
            if (block.Header.Height == AElfConstants.GenesisBlockHeight) return true;

            var tokenContractAddress =
                await _smartContractAddressService.GetAddressByContractNameAsync(new ChainContext
                {
                    BlockHash = block.GetHash(),
                    BlockHeight = block.Header.Height
                }, TokenSmartContractAddressNameProvider.StringName);
            if (tokenContractAddress == null)
            {
                return true;
            }

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

            if (hashFromState.Value.IsEmpty)
            {
                // If hash from state is empty, data from provider must be null.
                return totalResourceTokensMapsFromProvider == null;
            }

            if (hashFromState == HashHelper.ComputeFrom(new TotalResourceTokensMaps
            {
                BlockHash = block.Header.PreviousBlockHash,
                BlockHeight = block.Header.Height - 1
            }))
            {
                if (totalResourceTokensMapsFromProvider == null) return true;
                return totalResourceTokensMapsFromProvider.BlockHeight != block.Header.Height - 1;
            }

            if (totalResourceTokensMapsFromProvider == null) return false;
            var hashFromProvider = HashHelper.ComputeFrom(totalResourceTokensMapsFromProvider);
            return hashFromState == hashFromProvider;
        }
    }
}