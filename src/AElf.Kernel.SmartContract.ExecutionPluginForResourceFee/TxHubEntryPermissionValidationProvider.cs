using AElf.Contracts.MultiToken;
using AElf.Kernel.FeeCalculation.Application;
using AElf.Kernel.SmartContract.Application;

namespace AElf.Kernel.SmartContract.ExecutionPluginForResourceFee
{
    internal class TxHubEntryPermissionValidationProvider : TokenContractTransactionValidationProviderBase
    {
        //TODO Check only one DonateResourceToken can be in block.
        public override bool ValidateWhileSyncing => false;
        protected override string[] InvolvedSmartContractMethods { get; }

        public TxHubEntryPermissionValidationProvider(ISmartContractAddressService smartContractAddressService) : base(
            smartContractAddressService)
        {
            InvolvedSmartContractMethods = new[] {nameof(TokenContractImplContainer.TokenContractImplStub.DonateResourceToken)};
        }
    }
}