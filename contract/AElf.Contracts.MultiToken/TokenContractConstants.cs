using System.Collections.Generic;

namespace AElf.Contracts.MultiToken
{
    public class TokenContractConstants
    {
        public static readonly List<string> ResourceTokenSymbols = new List<string>{"RAM", "CPU", "NET", "STO"};
        public const int WhiteListCountLimit = 50;
        public const int TokenNameLength = 500;
        public const int InstructionLength = 500;
        public const int MaxDecimals = 8;
        public const int SymbolCountLimit = 50;
    }
}