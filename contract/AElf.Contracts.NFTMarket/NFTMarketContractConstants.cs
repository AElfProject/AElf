namespace AElf.Contracts.NFTMarket
{
    public partial class NFTMarketContract
    {
        private const int FeeDenominator = 10000;
        private const int DefaultExpireDays = 100000;
        private const int DefaultServiceFeeRate = 10;
        private const int DefaultServiceFeeAmount = 1_00000000;
        private const int DefaultDepositConfirmRate = FeeDenominator / 2;
        private const string BadgeMintWhitelistIdMetadataKey = "aelf_badge_whitelist";
    }
}