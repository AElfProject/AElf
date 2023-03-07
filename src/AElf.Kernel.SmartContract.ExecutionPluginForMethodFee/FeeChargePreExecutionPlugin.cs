using System.Collections.Generic;
using AElf.Contracts.MultiToken;
using AElf.Kernel.FeeCalculation.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf.Reflection;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee;

internal class FeeChargePreExecutionPlugin : MethodFeeChargedPreExecutionPluginBase
{
    public FeeChargePreExecutionPlugin(ISmartContractAddressService smartContractAddressService,
        IPrimaryTokenFeeService txFeeService, ITransactionSizeFeeSymbolsProvider transactionSizeFeeSymbolsProvider,
        IContractReaderFactory<TokenContractImplContainer.TokenContractImplStub> contractReaderFactory) : base(
        smartContractAddressService, txFeeService, transactionSizeFeeSymbolsProvider,
        contractReaderFactory, "acs1")
    {
    }
    
    public override bool IsTargetTransaction(IReadOnlyList<ServiceDescriptor> descriptors, Transaction transaction,
        Address tokenContractAddress)
    {
        return IsTargetAcsSymbol(descriptors) || transaction.To == tokenContractAddress;
    }

    public override bool IsTransactionShouldSkip(Transaction transaction, Address tokenContractAddress,
        TokenContractImplContainer.TokenContractImplStub tokenStub)
    {
        return transaction.To == tokenContractAddress &&
               transaction.MethodName is nameof(tokenStub.ChargeTransactionFees)
                   or nameof(tokenStub.ChargeUserContractTransactionFees);
    }

    public override Transaction GetPreTransaction(TokenContractImplContainer.TokenContractImplStub tokenStub,
        ChargeTransactionFeesInput chargeTransactionFeesInput)
    {
        return tokenStub.ChargeTransactionFees.GetTransaction(chargeTransactionFeesInput);
    }
    
}