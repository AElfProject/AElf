using System.Collections.Generic;
using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.ExecutionPlugin.Abstract;
using AElf.Kernel.Token;
using AElf.Types;

namespace AElf.Kernel.SmartContract.ExecutionPluginForResourceFee
{
    internal class TxHubEntryPermissionValidationProvider : PluginTransactionValidationProviderBase
    {
        public override bool ValidateWhileSyncing => false;
        
        public TxHubEntryPermissionValidationProvider(ISmartContractAddressService smartContractAddressService) : base(
            smartContractAddressService)
        {
        }
        
        protected override Hash GetInvolvedSystemContractHashName()
        {
            return TokenSmartContractAddressNameProvider.Name;
        }

        protected override List<string> GetInvolvedSmartContractMethods()
        {
            return new List<string> {nameof(TokenContractContainer.TokenContractStub.DonateResourceToken)};
        }
    }
}