using System.Collections.Generic;
using System.Linq.Expressions;

namespace AElf.Contracts.Economic
{
    public class EconomicContractConstants
    {
        // Token Converter Contract related.
        public const string TokenConverterTokenSymbol = "AETC";
        public const long TokenConverterTokenTotalSupply = 1_000_000_000_000000000;
        public const int TokenConverterTokenDecimals = 8;
        public const int TokenConverterTokenConnectorInitialVirtualBalance = 100_000;
        
        // Resource token related.
        public static readonly List<string> ResourceTokenSymbols = new List<string> {"RAM", "CPU", "NET"};
        public const long ResourceTokenTotalSupply = 1_000_000_000_000000000;
        public const int ResourceTokenDecimals = 8;
        public const string ResourceTokenConnectorWeight = "0.2";
        public const long ResourceTokenConnectorInitialVirtualBalance = 100_000;

        // Consensus related.
        public const string MiningTokenSymbol = "MINE";
        public const long MiningTokenTotalSupply = long.MaxValue;
        
        
    }
}