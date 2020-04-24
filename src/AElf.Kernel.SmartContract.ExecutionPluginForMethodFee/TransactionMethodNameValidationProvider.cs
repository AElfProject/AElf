using AElf.Contracts.MultiToken;
using AElf.Kernel.FeeCalculation.Application;
using AElf.Kernel.SmartContract.Application;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee
{
    internal class TransactionMethodNameValidationProvider : TokenContractTransactionValidationProviderBase
    {
        //TODO Check whether block can contain ChargeTransactionFees transaction
        public override bool ValidateWhileSyncing => false;
        protected override string[] InvolvedSmartContractMethods { get; }
        
        public TransactionMethodNameValidationProvider(ISmartContractAddressService smartContractAddressService) : base(smartContractAddressService)
        {
            InvolvedSmartContractMethods = new[]
                {nameof(TokenContractImplContainer.TokenContractImplStub.ChargeTransactionFees)};
        }
    }
}