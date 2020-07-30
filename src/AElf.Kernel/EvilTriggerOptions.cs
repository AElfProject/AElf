using AElf.Types;

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
        public bool NotMatchTransaction { get; set; }
    }
}