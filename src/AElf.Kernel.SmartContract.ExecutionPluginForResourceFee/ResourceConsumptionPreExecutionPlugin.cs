using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Standards.ACS8;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.ExecutionPluginForResourceFee;

internal class ResourceConsumptionPreExecutionPlugin : SmartContractExecutionPluginBase, IPreExecutionPlugin,
    ISingletonDependency
{
    private readonly IContractReaderFactory<TokenContractImplContainer.TokenContractImplStub>
        _contractReaderFactory;

    private readonly ISmartContractAddressService _smartContractAddressService;

    public ResourceConsumptionPreExecutionPlugin(ISmartContractAddressService smartContractAddressService,
        IContractReaderFactory<TokenContractImplContainer.TokenContractImplStub> contractReaderFactory) :
        base("acs8")
    {
        _smartContractAddressService = smartContractAddressService;
        _contractReaderFactory = contractReaderFactory;
    }

    public async Task<IEnumerable<Transaction>> GetPreTransactionsAsync(
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

        var checkResourceTokenTransaction = tokenStub.CheckResourceToken.GetTransaction(new Empty());

        return new List<Transaction>
        {
            checkResourceTokenTransaction
        };
    }

    public bool IsStopExecuting(ByteString txReturnValue, out string preExecutionInformation)
    {
        preExecutionInformation = string.Empty;
        return false;
    }
}