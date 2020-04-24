using AElf.Contracts.MultiToken;
using AElf.Kernel.FeeCalculation.Application;
using AElf.Kernel.SmartContract.Application;

namespace AElf.Kernel.SmartContract.ExecutionPluginForResourceFee
{
    internal class TransactionMethodNameValidationProvider : TokenContractTransactionValidationProviderBase
    {
        //TODO Check whether block can contain ChargeResourceToken transaction and CheckResourceToken transaction
        public override bool ValidateWhileSyncing => false;
        protected override string[] InvolvedSmartContractMethods { get; }

        public TransactionMethodNameValidationProvider(ISmartContractAddressService smartContractAddressService) : base(
            smartContractAddressService)
        {
            InvolvedSmartContractMethods = new[]
            {
                nameof(TokenContractImplContainer.TokenContractImplStub.ChargeResourceToken),
                nameof(TokenContractImplContainer.TokenContractImplStub.CheckResourceToken)
            };
        }
    }
}