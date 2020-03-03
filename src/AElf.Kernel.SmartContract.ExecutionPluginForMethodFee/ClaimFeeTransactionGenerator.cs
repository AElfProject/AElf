using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Kernel.Txn.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee
{
    public class ClaimFeeTransactionGenerator : ISystemTransactionGenerator
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly TransactionPackingOptions _transactionPackingOptions;
        public ILogger<ClaimFeeTransactionGenerator> Logger { get; set; }

        public ClaimFeeTransactionGenerator(ISmartContractAddressService smartContractAddressService,
            IOptionsMonitor<TransactionPackingOptions> transactionPackingOptions)
        {
            _smartContractAddressService = smartContractAddressService;
            _transactionPackingOptions = transactionPackingOptions.CurrentValue;
        }

        public Task<List<Transaction>> GenerateTransactionsAsync(Address @from, long preBlockHeight, Hash preBlockHash)
        {
            var generatedTransactions = new List<Transaction>();
            if (!_transactionPackingOptions.IsTransactionPackable)
                return Task.FromResult(generatedTransactions);

            if (preBlockHeight < Constants.GenesisBlockHeight)
                return Task.FromResult(generatedTransactions);

            var tokenContractAddress = _smartContractAddressService.GetAddressByContractName(
                TokenSmartContractAddressNameProvider.Name);

            if (tokenContractAddress == null)
                return Task.FromResult(generatedTransactions);

            generatedTransactions.AddRange(new List<Transaction>
            {
                new Transaction
                {
                    From = from,
                    MethodName = nameof(TokenContractContainer.TokenContractStub.ClaimTransactionFees),
                    To = tokenContractAddress,
                    RefBlockNumber = preBlockHeight,
                    //TODO: make a Util to get Block Prefix
                    RefBlockPrefix = ByteString.CopyFrom(preBlockHash.Value.Take(4).ToArray()),
                    //TODO: new Empty() => Empty.Instance, or a constant
                    Params = new Empty().ToByteString()
                }
            });
            
            Logger.LogInformation("FeeClaim transaction generated.");
            return Task.FromResult(generatedTransactions);
        }
    }
}