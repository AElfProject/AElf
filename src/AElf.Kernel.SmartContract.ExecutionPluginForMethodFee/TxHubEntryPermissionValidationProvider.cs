using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.ExecutionPlugin.Abstract;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee
{
    internal class TxHubEntryPermissionValidationProvider : TokenContractPluginTransactionValidationProviderBase
    {
        public TxHubEntryPermissionValidationProvider(ISmartContractAddressService smartContractAddressService) : base(
            smartContractAddressService)
        {
            InvolvedSmartContractMethods = new[]
                {nameof(TokenContractContainer.TokenContractStub.ClaimTransactionFees)};
        }

        public override bool ValidateWhileSyncing => false;
        protected override string[] InvolvedSmartContractMethods { get; }
    }
}