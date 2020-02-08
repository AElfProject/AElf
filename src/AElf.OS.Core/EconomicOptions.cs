namespace AElf.OS
{
    public class EconomicOptions
    {
        public string Symbol { get; set; } = "ELF";
        public string TokenName { get; set; } = "Native Token";
        public long TotalSupply { get; set; } = 1_000_000_000_00000000;
        public int Decimals { get; set; } = 8;
        public bool IsBurnable { get; set; } = true;
        public double DividendPoolRatio { get; set; } = 0.12;
        public long MaximumLockTime { get; set; } = 1080 * 86400;
        public long MinimumLockTime { get; set; } = 1 * 3600;
        public string SymbolListToPayTxFee { get; set; } = "WRITE,READ,STORAGE,TRAFFIC";
        public string SymbolListToPayRental { get; set; } = "CPU,RAM,DISK,NET";
        public long TransactionSizeFeeUnitPrice { get; set; } = 1000;
        public int Cpu { get; set; } = 0;
        public int Ram { get; set; } = 0;
        public int Disk { get; set; } = 0;
    }
}