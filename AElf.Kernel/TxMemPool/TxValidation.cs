using System;

namespace AElf.Kernel.TxMemPool
{
    public static class TxValidation
    {
        
        public static bool ValidateTx(this ITxPoolConfig config, Transaction tx)
        {
            // size validation
            if (GetTxSize(tx) > config.TxLimitSize)
            {
                // TODO: log errors 
                return false;
            }
            
            // TODO: signature validation
            
            // account address validation
            if (!tx.CheckAddress(out var addr) || !addr.Equals(tx.From))
            {
                // TODO: log errors, address error 
                return false;
            }
            // fee validation
            if (tx.Fee < config.FeeThreshold)
            {
                // TODO: log errors, not enough Fee error 
                return false;
            }
            
            // TODO : more validations
            return true;
        }

        /// <summary>
        /// check validity of address
        /// </summary>
        /// <param name="tx"></param>
        /// <param name="accAddress"></param>
        /// <returns></returns>    
        /// <exception cref="NotImplementedException"></exception>
        private static bool CheckAddress(this Transaction tx, out Hash accAddress)
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// return size of given tx
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        private static int GetTxSize(Transaction tx)
        {
            return tx.CalculateSize();
        }
    }
}
