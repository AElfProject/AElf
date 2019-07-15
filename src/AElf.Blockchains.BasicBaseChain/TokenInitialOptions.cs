namespace AElf.Blockchains
{
    public class TokenInitialOptions
    {
        public string Symbol { get; set; } = "ELF";
        public string Name { get; set; }
        public long TotalSupply { get; set; }
        public int Decimals { get; set; }
        public bool IsBurnable { get; set; }
        public double DividendPoolRatio { get; set; }
        public long LockForElection { get; set; }
    }
}