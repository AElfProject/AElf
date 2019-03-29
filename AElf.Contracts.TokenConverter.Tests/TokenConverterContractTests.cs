using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Resource.FeeReceiver;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Token;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Contracts.TokenConverter
{
    public class TokenConverterContractTests : TokenConverterTestBase
    {
        private ECKeyPair FeeKeyPair;
        private ECKeyPair ManagerKeyPair;
        private ECKeyPair FoundationKeyPair;
        
        private Address BasicZeroContractAddress;
        private Address TokenContractAddress;
        private Address FeeReceiverContractAddress;
        private Address TokenConverterContractAddress;

        public TokenConverterContractTests()
        {
            AsyncHelper.RunSync(()=>Tester.InitialChainAndTokenAsync());
            BasicZeroContractAddress = Tester.GetZeroContractAddress();
            TokenContractAddress = Tester.GetContractAddress(TokenSmartContractAddressNameProvider.Name);
            FeeReceiverContractAddress = Tester.GetContractAddress(ResourceFeeReceiverSmartContractAddressNameProvider.Name);
            TokenConverterContractAddress = Tester.GetContractAddress(TokenConverterSmartContractAddressNameProvider.Name);
            
            FeeKeyPair = CryptoHelpers.GenerateKeyPair();
            FoundationKeyPair = CryptoHelpers.GenerateKeyPair();
            ManagerKeyPair = CryptoHelpers.GenerateKeyPair();
        }

        private async Task<TransactionResult> InitializeTokenConverterContract()
        {
            //init token converter
            var input = new InitializeInput
            {
                BaseTokenSymbol = "ELF",
                FeeRateNumerator = 5,
                FeeRateDenominator = 5,
                Manager = Address.FromPublicKey(ManagerKeyPair.PublicKey),
                MaxWeight = 1000_000,
                TokenContractAddress = TokenContractAddress,
                FeeReceiverAddress = FeeReceiverContractAddress,
            };
            
            return await Tester.ExecuteTokenConverterMethodAsync(nameof(TokenConverterContract.Initialize), input);
        }
    }
}