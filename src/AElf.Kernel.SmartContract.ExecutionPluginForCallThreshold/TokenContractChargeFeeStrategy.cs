using System.Collections.Generic;
using AElf.Contracts.MultiToken;
using AElf.Kernel.FeeCalculation.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;
using Microsoft.Extensions.Logging;

namespace AElf.Kernel.SmartContract.ExecutionPluginForCallThreshold
{
    public class TokenContractChargeFeeStrategy : ChargeFeeStrategyBase
    {
        // public ILogger<TokenContractChargeFeeStrategy> Logger { get; set; }
        
        public TokenContractChargeFeeStrategy(ISmartContractAddressService smartContractAddressService) : base(
            smartContractAddressService)
        {
            // Logger.LogDebug($"Hi, I am TokenContractChargeFeeStrategy from ExecutionPluginForCallThreshold");
        }

        public override string MethodName => string.Empty;

        protected override Hash SystemContractHashName => TokenSmartContractAddressNameProvider.Name;

        protected override List<string> GetInvolvedSmartContractMethods()
        {
            return new List<string>
            {
                // Pre-plugin tx
                nameof(TokenContractContainer.TokenContractStub.CheckThreshold),
            };
        }
    }
}