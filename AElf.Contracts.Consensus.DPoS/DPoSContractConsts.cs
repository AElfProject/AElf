namespace AElf.Contracts.Consensus.DPoS
{
    // ReSharper disable InconsistentNaming
    public static class DPoSContractConsts
    {
        #region Consensus Settings

        public const int BootMinerWaitingMilliseconds = 8000;
        public const ulong TotalSupply = 100_000_000_000;
        public const ulong LockTokenForElection = 100_000;
        public const int AliasLimit = 20;

        public const string InitialMinersAliases = "YQ,SM,WK,ZY,SC,ZX,RP,ZZ,MH,YS,GL,LN,ZA,MM,GG,MC,WS,KL";
        
        #endregion
        
        #region Dividends Settings

        public const ulong ElfTokenPerBlock = 10000;
        public const double MinersBasicRatio = 0.4;
        public const double MinersVotesRatio = 0.1;
        public const double MinersReappointmentRatio = 0.1;
        public const double BackupNodesRatio = 0.2;
        public const double VotersRatio = 0.2;

        #endregion
    }
}