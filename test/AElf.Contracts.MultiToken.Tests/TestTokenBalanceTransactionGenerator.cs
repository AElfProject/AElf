using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken.Messages;
using AElf.Kernel;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using Google.Protobuf;

namespace AElf.Contracts.MultiToken
{
    public class TestTokenBalanceTransactionGenerator : ISystemTransactionGenerator
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        public TestTokenBalanceTransactionGenerator(ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
        }
        
        public void GenerateTransactions(Address @from, long preBlockHeight, Hash preBlockHash, ref List<Transaction> generatedTransactions)
        {
            generatedTransactions.Add(new Transaction
            {
                From = from,
                To = _smartContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider.Name),
                MethodName = nameof(TokenContract.Transfer),
                Params = new TransferInput
                {
                    Amount = 1000L,
                    Memo = "transfer test",
                    Symbol = "ELFTEST",
                    To = Address.Generate()
                }.ToByteString(),
                RefBlockNumber = preBlockHeight,
                RefBlockPrefix = ByteString.CopyFrom(preBlockHash.Value.Take(4).ToArray())
            });
        }
    }
}