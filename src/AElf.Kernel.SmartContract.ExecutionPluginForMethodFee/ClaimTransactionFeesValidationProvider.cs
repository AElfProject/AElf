using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee
{
    public class ClaimTransactionFeesValidationProvider : IBlockValidationProvider
    {
        private readonly ITotalTransactionFeesMapProvider _totalTransactionFeesMapProvider;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly IAccountService _accountService;

        public ClaimTransactionFeesValidationProvider(ITotalTransactionFeesMapProvider totalTransactionFeesMapProvider,
            ISmartContractAddressService smartContractAddressService, IAccountService accountService)
        {
            _totalTransactionFeesMapProvider = totalTransactionFeesMapProvider;
            _smartContractAddressService = smartContractAddressService;
            _accountService = accountService;
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

            var tokenStub = GetTokenContractStub(Address.FromPublicKey((await _accountService.GetPublicKeyAsync())),
                tokenContractAddress);
            var hashFromState =
                await tokenStub.GetLatestTotalTransactionFeesMapHash.CallAsync(new Empty());
            var totalTransactionFeesMapFromProvider =
                await _totalTransactionFeesMapProvider.GetTotalTransactionFeesMapAsync(new ChainContext
                {
                    BlockHash = block.GetHash(),
                    BlockHeight = block.Header.Height
                });
            var hashFromProvider = Hash.FromMessage(totalTransactionFeesMapFromProvider);
            return hashFromProvider == hashFromState;
        }

        private static TokenContractImplContainer.TokenContractImplStub GetTokenContractStub(Address sender,
            Address contractAddress)
        {
            return new TokenContractImplContainer.TokenContractImplStub
            {
                __factory = new TransactionGeneratingOnlyMethodStubFactory
                {
                    Sender = sender,
                    ContractAddress = contractAddress
                }
            };
        }
    }
}