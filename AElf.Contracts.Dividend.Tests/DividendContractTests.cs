using System.Threading.Tasks;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.SmartContract;
using AElf.Kernel.Token;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Contracts.Dividend
{
    public class DividendContractTests : DividendContractTestBase
    {
        private readonly ECKeyPair _spenderKeyPair;
        private Address BasicZeroContractAddress { get; set; }
        private Address ConsensusContractAddress { get; set; }
        private Address TokenContractAddress { get; set; }
        private Address DividendContractAddress { get; set; }
        
        private static long _totalSupply;
        private static long _balanceOfStarter;
        
        public DividendContractTests()
        {
            AsyncHelper.RunSync(() =>
                Tester.InitialChainAsync(Tester.GetDefaultContractTypes(Tester.GetCallOwnerAddress(), out _totalSupply,
                    out _, out _balanceOfStarter)));
            BasicZeroContractAddress = Tester.GetZeroContractAddress();
            ConsensusContractAddress = Tester.GetContractAddress(ConsensusSmartContractAddressNameProvider.Name);
            TokenContractAddress = Tester.GetContractAddress(TokenSmartContractAddressNameProvider.Name);
            DividendContractAddress = Tester.GetContractAddress(DividendsSmartContractAddressNameProvider.Name);
        }

        [Fact]
        public async Task Initialize_Test()
        {
            var input = new InitialDividendContractInput
            {
                ConsensusContractSystemName = ConsensusSmartContractAddressNameProvider.Name,
                TokenContractSystemName = TokenSmartContractAddressNameProvider.Name
            };
            var transactionResult = await Tester.ExecuteContractWithMiningAsync(DividendContractAddress,
                nameof(DividendContract.InitializeDividendContract), input);
            
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            transactionResult = await Tester.ExecuteContractWithMiningAsync(DividendContractAddress,
                nameof(DividendContract.InitializeDividendContract), input);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Already initialized.").ShouldBeTrue();
        }

        [Fact]
        public async Task CheckDividendsOfPreviousTermToFriendlyString()
        {
            var bytes = await Tester.CallContractMethodAsync(DividendContractAddress,
                nameof(DividendContract.CheckDividendsOfPreviousTermToFriendlyString),
                new Empty());
            var stringValue = FriendlyString.Parser.ParseFrom(bytes);
            stringValue.ShouldNotBeNull();
        }
    }
}