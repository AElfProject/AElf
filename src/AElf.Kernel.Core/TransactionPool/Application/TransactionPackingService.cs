namespace AElf.Kernel.TransactionPool.Application
{
    public class TransactionPackingService : ITransactionPackingService
    {
        private bool _isTransactionPackable = true;
        
        public void EnableTransactionPacking()
        {
            _isTransactionPackable = true;
        }

        public void DisableTransactionPacking()
        {
            _isTransactionPackable = false;
        }

        public bool IsTransactionPackingEnabled()
        {
            return _isTransactionPackable;
        }
    }
}