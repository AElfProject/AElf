
namespace AElf.Kernel
{
    public class EvilTriggerOptions
    {
        public int EvilTriggerNumber { get; set; } = 32;
        public bool RepackagedTransaction { get; set; }
        public bool OverBlockTransactionLimit { get; set; }
        public bool ErrorTransactionCountInBody { get; set; }
        public bool ReverseTransactionList { get; set; }
        public bool RemoveOneTransaction { get; set; }
        public bool ErrorSignatureInBlock { get; set; }
        public bool ErrorSignatureInSystemTransaction { get; set; }
        public bool ChangeBlockHeaderSignPubKey { get; set; }
        public bool ChangeBlockHeader{ get; set; }
        public bool InvalidMethod { get; set; }
        public bool InvalidContracts { get; set; }
        public bool InvalidSignature { get; set; }
        public bool ChangeTransactionList { get; set; }
        public bool ConflictTransaction { get; set; }
        public bool ErrorConsensusExtraDate { get; set; }
        public bool ErrorCrossChainExtraDate { get; set; }
        public bool ErrorSystemTransactionCount { get; set; }
    }
}