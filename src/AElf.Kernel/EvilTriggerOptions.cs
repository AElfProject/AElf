using AElf.Types;

namespace AElf.Kernel
{
    public abstract class EvilTriggerOptions
    {
        public int EvilTriggerNumber { get; set; } = 32;
        public bool RepackagedTransaction { get; set; }
        public bool OverBlockTransactionLimit { get; set; }
        public bool ErrorTransactionCountInBody { get; set; }
        public bool ReverseTransactionList { get; set; }
        public bool ErrorSignatureInBlock { get; set; }
        public bool ErrorSignatureInSystemTransaction { get; set; }
        public bool ChangeBlockHeaderSignPubKey { get; set; }
        public bool ChangeBlockHeader{ get; set; }
    }
}