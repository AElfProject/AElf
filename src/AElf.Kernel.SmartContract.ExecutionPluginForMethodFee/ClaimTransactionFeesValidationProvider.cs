using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee
{
    internal class ClaimTransactionFeesValidationProvider : IBlockValidationProvider
    {
        private readonly ITotalTransactionFeesMapProvider _totalTransactionFeesMapProvider;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ITokenReaderFactory _tokenReaderFactory;

        public ILogger<ClaimTransactionFeesValidationProvider> Logger { get; set; }

        public ClaimTransactionFeesValidationProvider(ITotalTransactionFeesMapProvider totalTransactionFeesMapProvider,
            ISmartContractAddressService smartContractAddressService, ITokenReaderFactory tokenReaderFactory)
        {
            _totalTransactionFeesMapProvider = totalTransactionFeesMapProvider;
            _smartContractAddressService = smartContractAddressService;
            _tokenReaderFactory = tokenReaderFactory;

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
        public async Task<bool> ValidateBlockAfterExecuteAsync(IBlock block)
        {
            var tokenContractAddress =
                _smartContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider.Name);
            if (tokenContractAddress == null)
            {
                return true;
            }

            var hashFromState = await _tokenReaderFactory.Create(block.GetHash(), block.Header.Height)
                .GetLatestTotalTransactionFeesMapHash.CallAsync(new Empty());
            var totalTransactionFeesMapFromProvider =
                await _totalTransactionFeesMapProvider.GetTotalTransactionFeesMapAsync(new ChainContext
                {
                    BlockHash = block.GetHash(),
                    BlockHeight = block.Header.Height
                });
            if (totalTransactionFeesMapFromProvider == null)
            {
                Logger.LogInformation("totalTransactionFeesMapFromProvider == null");
                return hashFromState.Value.IsEmpty;
            }

            var hashFromProvider = Hash.FromMessage(totalTransactionFeesMapFromProvider);
            var result = hashFromProvider.Value.Equals(hashFromState.Value);
            if (!result)
            {
                Logger.LogError($"Hash from provider: {hashFromProvider}\nHash from state: {hashFromState}");
            }

            return result;
        }
    }
}