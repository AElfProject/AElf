using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.FeeCalculation.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Standards.ACS8;
using AElf.Types;
using Google.Protobuf.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.ExecutionPluginForResourceFee;

internal class ResourceConsumptionPostExecutionPlugin : SmartContractExecutionPluginBase, IPostExecutionPlugin,
    ISingletonDependency
{
    private readonly IContractReaderFactory<TokenContractImplContainer.TokenContractImplStub>
        _contractReaderFactory;

    private readonly IResourceTokenFeeService _resourceTokenFeeService;
    private readonly ISmartContractAddressService _smartContractAddressService;

    public ResourceConsumptionPostExecutionPlugin(ISmartContractAddressService smartContractAddressService,
        IResourceTokenFeeService resourceTokenFeeService,
        IContractReaderFactory<TokenContractImplContainer.TokenContractImplStub> contractReaderFactory) :
        base("acs8")
    {
        _smartContractAddressService = smartContractAddressService;
        _resourceTokenFeeService = resourceTokenFeeService;
        _contractReaderFactory = contractReaderFactory;

        Logger = NullLogger<ResourceConsumptionPostExecutionPlugin>.Instance;
    }

    public ILogger<ResourceConsumptionPostExecutionPlugin> Logger { get; set; }

    public async Task<IEnumerable<Transaction>> GetPostTransactionsAsync(
        IReadOnlyList<ServiceDescriptor> descriptors, ITransactionContext transactionContext)
    {
        if (!HasApplicableAcs(descriptors)) return new List<Transaction>();

        var chainContext = new ChainContext
        {
            BlockHash = transactionContext.PreviousBlockHash,
            BlockHeight = transactionContext.BlockHeight - 1
        };

        // Generate token contract stub.
        var tokenContractAddress =
            await _smartContractAddressService.GetAddressByContractNameAsync(chainContext,
                TokenSmartContractAddressNameProvider.StringName);
        if (tokenContractAddress == null) return new List<Transaction>();

        var tokenStub = _contractReaderFactory.Create(new ContractReaderContext
        {
            ContractAddress = tokenContractAddress,
            Sender = transactionContext.Transaction.To
        });

        if (transactionContext.Transaction.To == tokenContractAddress &&
            transactionContext.Transaction.MethodName == nameof(tokenStub.ChargeResourceToken))
            return new List<Transaction>();

        if (transactionContext.Transaction.MethodName ==
            nameof(ResourceConsumptionContractContainer.ResourceConsumptionContractStub.BuyResourceToken))
            return new List<Transaction>();

        var chargeResourceTokenInput = new ChargeResourceTokenInput
        {
            Caller = transactionContext.Transaction.From
        };

        var feeCalculationResult =
            await _resourceTokenFeeService.CalculateFeeAsync(transactionContext, chainContext);
        chargeResourceTokenInput.CostDic.Add(feeCalculationResult);

        var chargeResourceTokenTransaction = tokenStub.ChargeResourceToken.GetTransaction(chargeResourceTokenInput);
        return new List<Transaction>
        {
            chargeResourceTokenTransaction
        };
    }
}