using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace AElf.Contracts.MultiToken
{
    public static class TokenContractConstants
    {
        public const int TokenNameLength = 80;
        public const int MaxDecimals = 18;
        public const int SymbolMaxLength = 10;
        public const int MemoMaxLength = 64;

        public const string PayTxFeeSymbolListName = "SymbolListToPayTxFee";
        public const string PayRentalSymbolListName = "SymbolListToPayRental";

        public const string IsProfitable = "IsProfitable";

        public static readonly ReadOnlyCollection<string> TokenMetaDataKeys = new ReadOnlyCollection<string>(
            new List<string>
            {
                TokenContractConstants.IsProfitable
            });
    }
}