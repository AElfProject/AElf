namespace AElf.Contracts.Consensus.DPoS.SideChain
{
    // ReSharper disable InconsistentNaming
    public static class DPoSContractConsts
    {
        #region Consensus Settings

        public const int BootMinerWaitingMilliseconds = 8000;
        public const long TotalSupply = 100_000_000_000;
        public const int ForkDetectionRoundNumber = 3;
        public const long MaxMissedTimeSlots = 1024;
        public const int AliasLimit = 20;

        public const string InitialMinersAliases = "YQ,SM,WK,ZY,SC,ZX,RP,ZZ,MH,YS,GL,LN,ZA,MM,GG,MC,WS,KL";
        
        #endregion
        
        #region Dividends Settings

        public const long ElfTokenPerBlock = 10000;
        public const double MinersBasicRatio = 0.4;
        public const double MinersVotesRatio = 0.1;
        public const double MinersReappointmentRatio = 0.1;
        public const double BackupNodesRatio = 0.2;
        public const double VotersRatio = 0.2;

        #endregion

        #region Error Messages

        public const string TicketsNotFound = "Tickets not found.";
        public const string CandidateNotFound = "Candidate not found.";
        public const string TermNumberNotFound = "Term number not found.";
        public const string TermSnapshotNotFound = "Term snapshot not found.";
        public const string TermNumberLookupNotFound = "Term number lookup not found.";
        public const string RoundNumberNotFound = "Round information not found.";
        public const string TargetNotAnnounceElection = "Target didn't announce election.";
        public const string CandidateCannotVote = "Candidate can't vote.";
        public const string LockDayIllegal = "Lock days is illegal.";
        public const string RoundIdNotMatched = "Round Id not matched.";
        public const string InValueNotMatchToOutValue = "In Value not match to Out Value.";
        public const string OutValueIsNull = "Out Value is null.";
        public const string SignatureIsNull = "Signature is null.";
        public const string VoterCannotAnnounceElection = "Voter can't announce election.";

        #endregion
    }
}