namespace AElf.Kernel.Txn.Application
{
    
    public class TransactionPackingService : ITransactionPackingService
    {
        //TODO: a service should not mange status. what's this class for?
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