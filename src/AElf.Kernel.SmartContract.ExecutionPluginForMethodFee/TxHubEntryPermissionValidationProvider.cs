using AElf.Contracts.MultiToken;
using AElf.Kernel.FeeCalculation.Application;
using AElf.Kernel.SmartContract.Application;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee
{
    internal class TxHubEntryPermissionValidationProvider : TokenContractTransactionValidationProviderBase
    {
        //TODO Check only one ClaimTransactionFees can be in block.
        public TxHubEntryPermissionValidationProvider(ISmartContractAddressService smartContractAddressService) : base(
            smartContractAddressService)
        {
            InvolvedSmartContractMethods = new[]
                {nameof(TokenContractImplContainer.TokenContractImplStub.ClaimTransactionFees)};
        }

        public override bool ValidateWhileSyncing => false;
        protected override string[] InvolvedSmartContractMethods { get; }
    }
}