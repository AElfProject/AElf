using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly ITransactionPackingService _transactionPackingService;
        public ILogger<ClaimFeeTransactionGenerator> Logger { get; set; }

        public ClaimFeeTransactionGenerator(ISmartContractAddressService smartContractAddressService,
            ITransactionPackingService transactionPackingService)
        {
            _smartContractAddressService = smartContractAddressService;
            _transactionPackingService = transactionPackingService;
        }

        public Task<List<Transaction>> GenerateTransactionsAsync(Address @from, long preBlockHeight, Hash preBlockHash)
        {
            var generatedTransactions = new List<Transaction>();
            if (!_transactionPackingService.IsTransactionPackingEnabled())
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
                    RefBlockPrefix = ByteString.CopyFrom(preBlockHash.Value.Take(4).ToArray()),
                    Params = new Empty().ToByteString()
                }
            });
            
            Logger.LogTrace("FeeClaim transaction generated.");
            return Task.FromResult(generatedTransactions);
        }
    }
}