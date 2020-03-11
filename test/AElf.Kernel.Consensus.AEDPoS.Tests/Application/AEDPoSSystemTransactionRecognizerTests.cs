using System.Linq;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Consensus.DPoS.Tests.Application
{
    // ReSharper disable once InconsistentNaming
    public class AEDPoSSystemTransactionRecognizerTests : AEDPoSTestBase
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ISystemTransactionRecognizer _systemTransactionRecognizer;

        public AEDPoSSystemTransactionRecognizerTests()
        {
            _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
            _systemTransactionRecognizer = GetRequiredService<ISystemTransactionRecognizer>();
        }

        [Theory]
        [InlineData(nameof(AEDPoSContractImplContainer.AEDPoSContractImplBase.InitialAElfConsensusContract))]
        [InlineData(nameof(AEDPoSContractImplContainer.AEDPoSContractImplBase.FirstRound))]
        [InlineData(nameof(AEDPoSContractImplContainer.AEDPoSContractImplBase.UpdateValue))]
        [InlineData(nameof(AEDPoSContractImplContainer.AEDPoSContractImplBase.UpdateTinyBlockInformation))]
        [InlineData(nameof(AEDPoSContractImplContainer.AEDPoSContractImplBase.NextRound))]
        [InlineData(nameof(AEDPoSContractImplContainer.AEDPoSContractImplBase.NextTerm))]
        public void RecognizeTransaction_Success(string methodName)
        {
            var contractAddress =
                _smartContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider.Name);
            var transaction = CreateTransaction(contractAddress, methodName);
            var isSystemTransaction = _systemTransactionRecognizer.IsSystemTransaction(transaction);
            isSystemTransaction.ShouldBeTrue();
        }

        [Fact]
        public void RecognizeTransaction_Failed()
        {
            {
                // wrong contract address
                var transaction = CreateTransaction(SampleAddress.AddressList.Last(),
                    nameof(AEDPoSContractImplContainer.AEDPoSContractImplBase.InitialAElfConsensusContract));

                var isSystemTransaction = _systemTransactionRecognizer.IsSystemTransaction(transaction);
                isSystemTransaction.ShouldBeFalse();
            }
            {
                // wrong method
                var contractAddress =
                    _smartContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider
                        .Name);
                var transaction = CreateTransaction(contractAddress,
                    nameof(AEDPoSContractImplContainer.AEDPoSContractImplBase.SetMaximumMinersCount));

                var isSystemTransaction = _systemTransactionRecognizer.IsSystemTransaction(transaction);
                isSystemTransaction.ShouldBeFalse();
            }
        }

        private Transaction CreateTransaction(Address contractAddress, string methodName)
        {
            return new Transaction
            {
                From = SampleAddress.AddressList.Last(),
                To = contractAddress,
                MethodName = methodName,
                Params = ByteString.CopyFromUtf8("Params")
            };
        }
    }
}