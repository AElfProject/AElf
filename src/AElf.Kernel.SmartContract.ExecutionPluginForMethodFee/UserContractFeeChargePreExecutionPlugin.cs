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
    
    public override bool IsTargetTransaction(IReadOnlyList<ServiceDescriptor> descriptors, Transaction transaction, Address tokenContractAddress)
    {
        return IsTargetAcsSymbol(descriptors);
    }

    public override Transaction GetPreTransaction(TokenContractImplContainer.TokenContractImplStub tokenStub,
        ChargeTransactionFeesInput chargeTransactionFeesInput)
    {
        return tokenStub.ChargeUserContractTransactionFees.GetTransaction(chargeTransactionFeesInput);
    }
    
}