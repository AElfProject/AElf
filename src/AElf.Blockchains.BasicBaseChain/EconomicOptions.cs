namespace AElf.Blockchains.BasicBaseChain
{
    public class EconomicOptions
    {
        public string Symbol { get; set; } = "ELF";
        public string TokenName { get; set; } = "Native Token";
        public long TotalSupply { get; set; } = 1_000_000_000_00000000;
        public int Decimals { get; set; } = 8;
        public bool IsBurnable { get; set; } = true;
        public double DividendPoolRatio { get; set; } = 0.2;
        public long LockForElection { get; set; } = 100_000_00000000;
        public long MaximumLockTime { get; set; } = 1080 * 86400;
        public long MinimumLockTime { get; set; } = 90 * 86400;

    }
}