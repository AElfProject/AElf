namespace AElf.Kernel.Types.Proposal
{
    public partial class PendingTxn
    {
        public Kernel.Transaction GetTransaction()
        {
            return Kernel.Transaction.Parser.ParseFrom(TxnData);
        }
    }
}