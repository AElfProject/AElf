using System.Collections.Generic;
using AElf.Contracts.MultiToken;
using AElf.Kernel.FeeCalculation.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee;

internal class FeeChargePreExecutionPlugin : MethodFeeChargedPreExecutionPluginBase
{
    public FeeChargePreExecutionPlugin(ISmartContractAddressService smartContractAddressService,
        IPrimaryTokenFeeService txFeeService, ITransactionSizeFeeSymbolsProvider transactionSizeFeeSymbolsProvider,
        IContractReaderFactory<TokenContractImplContainer.TokenContractImplStub> contractReaderFactory) : base(
        smartContractAddressService, txFeeService, transactionSizeFeeSymbolsProvider,
        contractReaderFactory, "acs1")
    {
        Logger = NullLogger<FeeChargePreExecutionPlugin>.Instance;
    }

    protected override bool IsTargetTransaction(IReadOnlyList<ServiceDescriptor> descriptors, Transaction transaction,
        Address tokenContractAddress)
    {
        return IsTargetAcsSymbol(descriptors) || transaction.To == tokenContractAddress;
    }

    protected override bool IsChargeTransactionFee(Transaction transaction, Address tokenContractAddress,
        TokenContractImplContainer.TokenContractImplStub tokenStub)
    {
        return transaction.To != tokenContractAddress ||
               (transaction.MethodName != nameof(tokenStub.ChargeTransactionFees) && transaction.MethodName !=
                   nameof(tokenStub.ChargeUserContractTransactionFees));
    }

    protected override Transaction GetTransaction(TokenContractImplContainer.TokenContractImplStub tokenStub,
        ChargeTransactionFeesInput chargeTransactionFeesInput)
    {
        return tokenStub.ChargeTransactionFees.GetTransaction(chargeTransactionFeesInput);
    }
}