using System.Collections.Generic;

namespace AElf.Contracts.Economic
{
    public static class EconomicContractConstants
    {
        public const long NativeTokenConnectorInitialVirtualBalance = 100_000_00000000;

        // Token Converter Contract related.
        public const string TokenConverterFeeRate = "0.005";
        public const string TokenConverterTokenSymbol = "AETC";
        public const long TokenConverterTokenTotalSupply = 1_000_000_000_00000000;
        public const int TokenConverterTokenDecimals = 8;
        public const long TokenConverterTokenConnectorInitialVirtualBalance = 100_000_00000000;

        public const int ConnectorSettingProposalReleaseThreshold = 6666;

        // Resource token related.
        public static readonly List<string> ResourceTokenSymbols = new List<string> {"RAM", "CPU", "NET", "STO"};
        public const long ResourceTokenTotalSupply = 1_000_000_000_00000000;
        public const int ResourceTokenDecimals = 8;
        public const string ResourceTokenConnectorWeight = "0.0005";
        public const long ResourceTokenConnectorInitialVirtualBalance = 100_000;
        public const long CpuUnitPrice = 100;
        public const long StoUnitPrice = 100;
        public const long NetUnitPrice = 100;
        
        //like Provision
        public const long CpuConnector = 100_000_000;    
        public const long StoConnector = 100_000_000;
        public const long NetConnector = 100_000_000;
        public const long RamConnector = 100_000_000;
        
        // resource to sell 
        public const long CpuRelayConnectorInitialVirtualBalance = 1_000_000;        
        public const long StoRelayConnectorInitialVirtualBalance = 1_000_000;
        public const long NetRelayConnectorInitialVirtualBalance = 1_000_000;
        public const long RamRelayConnectorInitialVirtualBalance = 1_000_000;

        // Election related.
        public const string ElectionTokenSymbol = "VOTE";
        public const long ElectionTokenTotalSupply = long.MaxValue;
    }
}