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

namespace AElf.Kernel.SmartContract.ExecutionPluginForResourceFee
{
    public class DonateResourceTransactionGenerator : ISystemTransactionGenerator
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ITotalResourceTokensMapsProvider _totalResourceTokensMapsProvider;

        public ILogger<DonateResourceTransactionGenerator> Logger { get; set; }


        public DonateResourceTransactionGenerator(ISmartContractAddressService smartContractAddressService,
            ITotalResourceTokensMapsProvider totalResourceTokensMapsProvider)
        {
            _smartContractAddressService = smartContractAddressService;
            _totalResourceTokensMapsProvider = totalResourceTokensMapsProvider;
        }

        public async Task<List<Transaction>> GenerateTransactionsAsync(Address @from, long preBlockHeight,
            Hash preBlockHash)
        {
            var generatedTransactions = new List<Transaction>();
            if (preBlockHeight == 1) return generatedTransactions;

            if (preBlockHeight < AElfConstants.GenesisBlockHeight)
                return generatedTransactions;
            
            var chainContext = new ChainContext
            {
                BlockHash = preBlockHash,
                BlockHeight = preBlockHeight
            };

            var tokenContractAddress =
                await _smartContractAddressService.GetAddressByContractNameAsync(chainContext,
                    TokenSmartContractAddressNameProvider.StringName);

            if (tokenContractAddress == null)
            {
                return generatedTransactions;
            }
            
            var totalResourceTokensMaps = await _totalResourceTokensMapsProvider.GetTotalResourceTokensMapsAsync(
                chainContext);

            var input = ByteString.Empty;
            if (totalResourceTokensMaps != null && totalResourceTokensMaps.BlockHeight == preBlockHeight &&
                totalResourceTokensMaps.BlockHash == preBlockHash)
            {
                input = totalResourceTokensMaps.ToByteString();
            }
            else
            {
                await _totalResourceTokensMapsProvider.SetTotalResourceTokensMapsAsync(new BlockIndex
                {
                    BlockHash = preBlockHash,
                    BlockHeight = preBlockHeight
                }, TotalResourceTokensMaps.Parser.ParseFrom(ByteString.Empty));
            }

            generatedTransactions.AddRange(new List<Transaction>
            {
                new Transaction
                {
                    From = from,
                    MethodName = nameof(TokenContractImplContainer.TokenContractImplStub.DonateResourceToken),
                    To = tokenContractAddress,
                    RefBlockNumber = preBlockHeight,
                    RefBlockPrefix = ByteString.CopyFrom(preBlockHash.Value.Take(4).ToArray()),
                    Params = input
                }
            });

            Logger.LogInformation("Tx DonateResourceToken generated.");
            return generatedTransactions;
        }
    }
}