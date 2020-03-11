using AElf.Contracts.Parliament;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Proposal.Tests.Application
{
    public class ProposalTransactionRecognizerTests : ProposalTestBase
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ISystemTransactionRecognizer _systemTransactionRecognizer;

        public ProposalTransactionRecognizerTests()
        {
            _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
            _systemTransactionRecognizer = GetRequiredService<ISystemTransactionRecognizer>();
        }

        [Fact]
        public void RecognizeTransaction_Success()
        {
            var contractAddress =
                _smartContractAddressService.GetAddressByContractName(ParliamentSmartContractAddressNameProvider.Name);
            var transaction = CreateTransaction(contractAddress,
                nameof(ParliamentContractContainer.ParliamentContractStub.ApproveMultiProposals));
            var isSystemTransaction = _systemTransactionRecognizer.IsSystemTransaction(transaction);
            isSystemTransaction.ShouldBeTrue();
        }

        [Fact]
        public void RecognizeTransaction_Failed()
        {
            {
                // wrong contract address
                var transaction = CreateTransaction(NormalAddress,
                    nameof(ParliamentContractContainer.ParliamentContractStub.ApproveMultiProposals));

                var isSystemTransaction = _systemTransactionRecognizer.IsSystemTransaction(transaction);
                isSystemTransaction.ShouldBeFalse();
            }
            {
                // wrong method
                var contractAddress =
                    _smartContractAddressService.GetAddressByContractName(ParliamentSmartContractAddressNameProvider
                        .Name);
                var transaction = CreateTransaction(contractAddress,
                    nameof(ParliamentContractContainer.ParliamentContractStub.Approve));

                var isSystemTransaction = _systemTransactionRecognizer.IsSystemTransaction(transaction);
                isSystemTransaction.ShouldBeFalse();
            }
        }

        private Transaction CreateTransaction(Address contractAddress, string methodName)
        {
            return new Transaction
            {
                From = NormalAddress,
                To = contractAddress,
                MethodName = methodName,
                Params = ByteString.CopyFromUtf8("Params")
            };
        }
    }
}