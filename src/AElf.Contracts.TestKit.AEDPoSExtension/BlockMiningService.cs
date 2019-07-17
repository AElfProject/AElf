namespace AElf.Contracts.TestKet.AEDPoSExtension
{
    public class BlockMiningService
    {
        private readonly ITransactionListProvider _transactionListProvider;

        public BlockMiningService(ITransactionListProvider transactionListProvider)
        {
            _transactionListProvider = transactionListProvider;
        }
    }
}