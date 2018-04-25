using System;

namespace AElf.Kernel.TxMemPool
{
    public static class TxValidation
    {
        
        public static bool ValidateTx(this Transaction tx)
        {
            // fee check
            
            
            // size check
            /*if (GetTxSize(tx) > _config.TxLimitSize)
            {
                // TODO: log errors 
                return false;
            }*/
            
            // tx data validation
            /*if (tx.IncrementId < 0 || tx.MethodName == null || tx.From == null)
            {                
                // TODO: log errors 
                return false;
            }*/
            
            // TODO: signature validation
            
            
            // account address validation
            /* if (tx.From == null || !CheckAddress(tx.From) || !CheckAddress(tx.To))
             {
                 // TODO: log errors 
                 return false;
             }*/

            // TODO : more validations
            return true;
        }
        
        /// <summary>
        /// check validity of address
        /// </summary>
        /// <param name="accountHash"></param>
        /// <returns></returns>    
        /// <exception cref="NotImplementedException"></exception>
        private static bool CheckAddress(Hash accountHash)
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// return size of given tx
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private static int GetTxSize(Transaction tx)
        {
            throw new System.NotImplementedException();
        }
    }
}