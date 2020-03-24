using AElf.Contracts.MultiToken;
using AElf.Kernel.FeeCalculation.Application;
using AElf.Kernel.SmartContract.Application;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee
{
    internal class TxHubEntryPermissionValidationProvider : TokenContractTransactionValidationProviderBase
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