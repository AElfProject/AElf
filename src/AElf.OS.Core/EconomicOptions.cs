namespace AElf.OS
{
    public class EconomicOptions
    {
        // Native Token Related
        public string Symbol { get; set; } = "ELF";
        public string TokenName { get; set; } = "Native Token";
        public long TotalSupply { get; set; } = 1_000_000_000_00000000;
        public int Decimals { get; set; } = 8;
        public bool IsBurnable { get; set; } = true;
        public double DividendPoolRatio { get; set; } = 0.12;

        public long TransactionSizeFeeUnitPrice { get; set; } = 1000;

        public long MaximumLockTime { get; set; } = 1080 * 86400;
        public long MinimumLockTime { get; set; } = 90 * 86400;
    }
}