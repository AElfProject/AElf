using System.Collections.Generic;
using AElf.Contracts.MultiToken;
using AElf.Kernel.FeeCalculation.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf.Reflection;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee;

internal class UserContractFeeChargePreExecutionPlugin : MethodFeeChargedPreExecutionPluginBase
{
    public UserContractFeeChargePreExecutionPlugin(ISmartContractAddressService smartContractAddressService,
        IPrimaryTokenFeeService txFeeService, ITransactionSizeFeeSymbolsProvider transactionSizeFeeSymbolsProvider,
        IContractReaderFactory<TokenContractImplContainer.TokenContractImplStub> contractReaderFactory) : base(
        smartContractAddressService, txFeeService, transactionSizeFeeSymbolsProvider,
        contractReaderFactory, "acs12")
    {
    }
    
    protected override bool IsApplicableToTransaction(IReadOnlyList<ServiceDescriptor> descriptors, Transaction transaction, Address tokenContractAddress)
    {
        return HasApplicableAcs(descriptors);
    }
    protected override bool IsExemptedTransaction(Transaction transaction, Address tokenContractAddress,
        TokenContractImplContainer.TokenContractImplStub tokenStub)
    {
        return transaction.To == tokenContractAddress &&
               (transaction.MethodName == nameof(tokenStub.ChargeTransactionFees) || transaction.MethodName ==
                   nameof(tokenStub.ChargeUserContractTransactionFees));
    }
    protected override Transaction GetTransaction(TokenContractImplContainer.TokenContractImplStub tokenStub,
        ChargeTransactionFeesInput chargeTransactionFeesInput)
    {
        return tokenStub.ChargeUserContractTransactionFees.GetTransaction(chargeTransactionFeesInput);
    }
    
}