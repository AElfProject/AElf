using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Kernel.TransactionPool.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs1
{
    public class ClaimFeeTransactionGenerator : ISystemTransactionGenerator
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ITransactionInclusivenessProvider _transactionInclusivenessProvider;
        public ILogger<ClaimFeeTransactionGenerator> Logger { get; set; }

        public ClaimFeeTransactionGenerator(ISmartContractAddressService smartContractAddressService,
            ITransactionInclusivenessProvider transactionInclusivenessProvider)
        {
            _smartContractAddressService = smartContractAddressService;
            _transactionInclusivenessProvider = transactionInclusivenessProvider;
        }

        public void GenerateTransactions(Address @from, long preBlockHeight, Hash preBlockHash,
            ref List<Transaction> generatedTransactions)
        {
            if (!_transactionInclusivenessProvider.IsTransactionPackable)
                return;

            if (preBlockHeight < Constants.GenesisBlockHeight)
                return;

            var tokenContractAddress = _smartContractAddressService.GetAddressByContractName(
                TokenSmartContractAddressNameProvider.Name);

            if (tokenContractAddress == null)
                return;

            generatedTransactions.AddRange(new List<Transaction>
            {
                new Transaction
                {
                    From = from,
                    MethodName = nameof(TokenContractContainer.TokenContractStub.ClaimTransactionFees),
                    To = tokenContractAddress,
                    RefBlockNumber = preBlockHeight,
                    RefBlockPrefix = ByteString.CopyFrom(preBlockHash.Value.Take(4).ToArray()),
                    Params = new Empty().ToByteString()
                }
            });
            
            Logger.LogTrace("FeeClaim transaction generated.");
        }
    }
}