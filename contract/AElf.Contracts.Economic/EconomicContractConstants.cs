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
        public const long ResourceTokenTotalSupply = 100_000_000_00000000;
        public const int ResourceTokenDecimals = 8;
        public const string ResourceTokenConnectorWeight = "0.2";
        public const long ResourceTokenConnectorInitialVirtualBalance = 100_000_00000000;
        public const long CpuUnitPrice = 100;
        public const long StoUnitPrice = 100;
        public const long NetUnitPrice = 100;

        public const string CpuConnectorSymbol = "CPU";
        public const string RamConnectorSymbol = "RAM";
        public const string NetConnectorSymbol = "NET";
        public const string StoConnectorSymbol = "STO";

//        //resource to sell
        public const long CpuInitialVirtualBalance = 100_00000000;
        public const long StoInitialVirtualBalance = 100_00000000;
        public const long NetInitialVirtualBalance = 100_00000000;
        public const long RamInitialVirtualBalance = 100_00000000;
        
        public const string NativeTokenToCpuSymbol = "NTCPU"; //NativeTokenToCPU
        public const string NativeTokenToRamSymbol = "NTRAM";
        public const string NativeTokenToNetSymbol = "NTNET";
        public const string NativeTokenToStoSymbol = "NTSTO";

        public const long NativeTokenToCpuBalance = 1_000_000_00000000;
        public const long NativeTokenToNetBalance = 1_000_000_00000000;
        public const long NativeTokenToRamBalance = 1_000_000_00000000;
        public const long NativeTokenToStoBalance = 1_000_000_00000000;

        // Election related.
        public const string ElectionTokenSymbol = "VOTE";
        public const long ElectionTokenTotalSupply = long.MaxValue;
    }
}