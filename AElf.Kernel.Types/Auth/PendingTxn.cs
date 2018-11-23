namespace AElf.Kernel.Types.Auth
{
    public partial class PendingTxn
    {
        public Kernel.Transaction GetTransaction()
        {
            return Kernel.Transaction.Parser.ParseFrom(TxnData);
        }
    }
}