using System.Linq;
using AElf.Contracts.CrossChain;
using AElf.Kernel;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.CrossChain
{
    public class CrossChainTransactionRecognizerTests : CrossChainTestBase
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ISystemTransactionRecognizer _systemTransactionRecognizer;
        private readonly Address _sampleAddress = SampleAddress.AddressList.Last();

        public CrossChainTransactionRecognizerTests()
        {
            _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
            _systemTransactionRecognizer = GetRequiredService<ISystemTransactionRecognizer>();
        }

        [Theory]
        [InlineData(nameof(CrossChainContractContainer.CrossChainContractStub.ProposeCrossChainIndexing))]
        [InlineData(nameof(CrossChainContractContainer.CrossChainContractStub.ReleaseCrossChainIndexing))]
        public void RecognizeTransaction_Success(string methodName)
        {
            var contractAddress =
                _smartContractAddressService.GetAddressByContractName(CrossChainSmartContractAddressNameProvider
                    .Name);
            var transaction = CreateTransaction(contractAddress,methodName);

            var isSystemTransaction = _systemTransactionRecognizer.IsSystemTransaction(transaction);
            isSystemTransaction.ShouldBeTrue();
        }
        
        [Fact]
        public void RecognizeTransaction_WrongContractAddress()
        {
            var transaction = CreateTransaction(_sampleAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.ProposeCrossChainIndexing));

            var isSystemTransaction = _systemTransactionRecognizer.IsSystemTransaction(transaction);
            isSystemTransaction.ShouldBeFalse();
        }
        
        [Fact]
        public void RecognizeTransaction_WrongMethod()
        {
            var contractAddress =
                _smartContractAddressService.GetAddressByContractName(CrossChainSmartContractAddressNameProvider
                    .Name);
            var transaction = CreateTransaction(contractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.ReleaseSideChainCreation));

            var isSystemTransaction = _systemTransactionRecognizer.IsSystemTransaction(transaction);
            isSystemTransaction.ShouldBeFalse();
        }

        private Transaction CreateTransaction(Address contractAddress, string methodName)
        {
            return new Transaction
            {
                From = _sampleAddress,
                To = contractAddress,
                MethodName = methodName,
                Params = ByteString.CopyFromUtf8("Params")
            };
        }
    }
}