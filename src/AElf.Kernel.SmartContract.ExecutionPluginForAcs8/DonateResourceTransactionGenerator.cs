using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken.Messages;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs8
{
    public class DonateResourceTransactionGenerator : ISystemTransactionGenerator
    {
        private readonly ISmartContractAddressService _smartContractAddressService;

        public DonateResourceTransactionGenerator(ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
        }

        public void GenerateTransactions(Address @from, long preBlockHeight, Hash preBlockHash,
            ref List<Transaction> generatedTransactions)
        {
            if (preBlockHeight < Constants.GenesisBlockHeight)
            {
                return;
            }

            var tokenContractAddress = _smartContractAddressService.GetAddressByContractName(
                TokenSmartContractAddressNameProvider.Name);

            if (tokenContractAddress == null)
            {
                return;
            }

            generatedTransactions.AddRange(new List<Transaction>
            {
                new Transaction
                {
                    From = from,
                    MethodName = nameof(TokenContractContainer.TokenContractStub.DonateResourceToken),
                    To = tokenContractAddress,
                    RefBlockNumber = preBlockHeight,
                    RefBlockPrefix = ByteString.CopyFrom(preBlockHash.Value.Take(4).ToArray()),
                    Params = new Empty().ToByteString()
                }
            });
        }
    }
}