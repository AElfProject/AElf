using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.SmartContract.ExecutionPluginForResourceFee
{
    public class DonateResourceTokenValidationProvider : IBlockValidationProvider
    {
        private readonly ITotalResourceTokensMapsProvider _totalResourceTokensMapsProvider;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly IAccountService _accountService;

        public DonateResourceTokenValidationProvider(ITotalResourceTokensMapsProvider totalResourceTokensMapsProvider,
            ISmartContractAddressService smartContractAddressService, IAccountService accountService)
        {
            _totalResourceTokensMapsProvider = totalResourceTokensMapsProvider;
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
                (await tokenStub.GetLatestTotalResourceTokensMapsHash.SendAsync(new Empty())).Output;
            var totalResourceTokensMapsFromProvider =
                await _totalResourceTokensMapsProvider.GetTotalResourceTokensMapsAsync(new ChainContext
                {
                    BlockHash = block.GetHash(),
                    BlockHeight = block.Header.Height
                });
            if (totalResourceTokensMapsFromProvider == null)
            {
                return hashFromState == null;
            }

            var hashFromProvider = Hash.FromMessage(totalResourceTokensMapsFromProvider);
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