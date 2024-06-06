using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;

namespace AElf.Kernel.SmartContract.ExecutionPluginForDelayedMethodFee;

internal class ClaimDelayedFeeTransactionGenerator : ISystemTransactionGenerator
{
    private readonly ISmartContractAddressService _smartContractAddressService;
    private readonly ITotalDelayedTransactionFeesMapProvider _totalDelayedTransactionFeesMapProvider;

    public ClaimDelayedFeeTransactionGenerator(ISmartContractAddressService smartContractAddressService,
        ITotalDelayedTransactionFeesMapProvider totalDelayedTransactionFeesMapProvider)
    {
        _smartContractAddressService = smartContractAddressService;
        _totalDelayedTransactionFeesMapProvider = totalDelayedTransactionFeesMapProvider;
    }

    public ILogger<ClaimDelayedFeeTransactionGenerator> Logger { get; set; }

    public async Task<List<Transaction>> GenerateTransactionsAsync(Address from, long preBlockHeight,
        Hash preBlockHash)
    {
        var generatedTransactions = new List<Transaction>();

        if (preBlockHeight < AElfConstants.GenesisBlockHeight)
            return generatedTransactions;

        if (preBlockHeight % 200 != 0)
        {
            return generatedTransactions;
        }

        var chainContext = new ChainContext
        {
            BlockHash = preBlockHash,
            BlockHeight = preBlockHeight
        };

        var tokenContractAddress = await _smartContractAddressService.GetAddressByContractNameAsync(chainContext,
            TokenSmartContractAddressNameProvider.StringName);

        if (tokenContractAddress == null)
            return generatedTransactions;

        var totalDelayedTxFeesMap =
            await _totalDelayedTransactionFeesMapProvider.GetTotalDelayedTransactionFeesMapAsync(chainContext);
        if (totalDelayedTxFeesMap == null || !totalDelayedTxFeesMap.Fees.Any())
        {
            return generatedTransactions;
        }

        await _totalDelayedTransactionFeesMapProvider.SetTotalDelayedTransactionFeesMapAsync(new BlockIndex
        {
            BlockHash = preBlockHash,
            BlockHeight = preBlockHeight
        }, new TotalDelayedTransactionFeesMap());

        generatedTransactions.AddRange(new List<Transaction>
        {
            new()
            {
                From = from,
                MethodName = nameof(TokenContractImplContainer.TokenContractImplStub.ClaimDelayedTransactionFees),
                To = tokenContractAddress,
                RefBlockNumber = preBlockHeight,
                RefBlockPrefix = BlockHelper.GetRefBlockPrefix(preBlockHash),
                Params = totalDelayedTxFeesMap.ToByteString()
            }
        });

        return generatedTransactions;
    }
}