using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;

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

            ByteString input;
            if (totalResourceTokensMaps != null && totalResourceTokensMaps.BlockHeight == preBlockHeight &&
                totalResourceTokensMaps.BlockHash == preBlockHash)
            {
                // If totalResourceTokensMaps match current block.
                input = totalResourceTokensMaps.ToByteString();
            }
            else
            {
                input = new TotalResourceTokensMaps
                {
                    BlockHash = preBlockHash,
                    BlockHeight = preBlockHeight
                }.ToByteString();
            }

            generatedTransactions.AddRange(new List<Transaction>
            {
                new Transaction
                {
                    From = from,
                    MethodName = nameof(TokenContractImplContainer.TokenContractImplStub.DonateResourceToken),
                    To = tokenContractAddress,
                    RefBlockNumber = preBlockHeight,
                    RefBlockPrefix = BlockHelper.GetRefBlockPrefix(preBlockHash),
                    Params = input
                }
            });

            Logger.LogInformation("Tx DonateResourceToken generated.");
            return generatedTransactions;
        }
    }
}