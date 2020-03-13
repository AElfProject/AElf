using AElf.Contracts.MultiToken;
using AElf.Kernel.FeeCalculation.Application;
using AElf.Kernel.SmartContract.Application;

namespace AElf.Kernel.SmartContract.ExecutionPluginForResourceFee
{
    internal class TxHubEntryPermissionValidationProvider : TokenContractTransactionValidationProviderBase
    {
        public override bool ValidateWhileSyncing => false;
        protected override string[] InvolvedSmartContractMethods { get; }

        public TxHubEntryPermissionValidationProvider(ISmartContractAddressService smartContractAddressService) : base(
            smartContractAddressService)
        {
            InvolvedSmartContractMethods = new[] {nameof(TokenContractContainer.TokenContractStub.DonateResourceToken)};
        }
    }
}