using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.ContractTestKit;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.Tests
{
    public class MockTransactionGenerator : ISystemTransactionGenerator
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly Address _addressWithoutToken;

        public MockTransactionGenerator(ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
            _addressWithoutToken = SampleAccount.Accounts[5].Address;
        }
        
        public async Task<List<Transaction>> GenerateTransactionsAsync(Address @from, long preBlockHeight, Hash preBlockHash)
        {
            var transactions = new List<Transaction>();
            var transaction = new Transaction
            {
                From = _addressWithoutToken,
                To = await _smartContractAddressService.GetAddressByContractNameAsync(new ChainContext
                {
                    BlockHash = preBlockHash,
                    BlockHeight = preBlockHeight
                }, TokenSmartContractAddressNameProvider.StringName),
                MethodName = nameof(TokenContractImplContainer.TokenContractImplStub.Transfer),
                Params = new TransferInput
                {
                    Amount = 1000,
                    Memo = "transfer test",
                    Symbol = "ELF",
                    To = SampleAddress.AddressList[0]
                }.ToByteString(),
                RefBlockNumber = preBlockHeight,
                RefBlockPrefix = ByteString.CopyFrom(preBlockHash.Value.Take(4).ToArray())
            };

            transactions.Add(transaction);
            return transactions;
        }
    }
}