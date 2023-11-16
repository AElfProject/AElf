using AElf.Contracts.MultiToken;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Volo.Abp.DependencyInjection;

namespace AElf.Runtime.WebAssembly.TransactionPayment;

internal class ChargeGasFeePostExecutionPlugin : IPostExecutionPlugin, ISingletonDependency
{
    private readonly IContractReaderFactory<TokenContractImplContainer.TokenContractImplStub>
        _contractReaderFactory;

    private readonly ISmartContractAddressService _smartContractAddressService;
    private readonly IFeeService _feeService;

    public ChargeGasFeePostExecutionPlugin(
        IContractReaderFactory<TokenContractImplContainer.TokenContractImplStub> contractReaderFactory,
        ISmartContractAddressService smartContractAddressService, IFeeService feeService)
    {
        _contractReaderFactory = contractReaderFactory;
        _smartContractAddressService = smartContractAddressService;
        _feeService = feeService;
    }

    public async Task<IEnumerable<Transaction>> GetPostTransactionsAsync(IReadOnlyList<ServiceDescriptor> descriptors,
        ITransactionContext transactionContext)
    {
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
            Sender = transactionContext.Transaction.From
        });

        var logEvent =
            transactionContext.Trace.Logs.Last(l => l.Name == WebAssemblyTransactionPaymentConstants.LogEventName);
        var weight = new Weight();
        weight.MergeFrom(logEvent.NonIndexed);
        var fee = _feeService.CalculateFees(weight);
        return new[] { tokenStub.Transfer.GetTransaction(new TransferInput
        {
            // TODO: Transfer to another address like profit pool
            To = tokenContractAddress,
            Amount = fee,
            Symbol = "ELF"
        }) };
    }
}