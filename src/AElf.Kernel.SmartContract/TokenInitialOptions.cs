namespace AElf.Kernel.SmartContract
{
    public class TokenInitialOptions
    {
        public string Symbol { get; set; }
        public string Name { get; set; }
        public int TotalSupply { get; set; }
        public int Decimals { get; set; }
        public bool IsBurnable { get; set; }
        public double DividendPoolRatio { get; set; }
        public long LockForElection { get; set; }
    }
}