using System;
using AElf.Cryptography.ECDSA;

namespace AElf.Kernel.TxMemPool
{
    public static class TxValidation
    {
        public enum ValidationError
        {
            Success,
            InvalidTxFormat,
            NotEnoughGas,
            TooBigSize,
            WrongAddress,
            InvalidSignature
        }
        /// <summary>
        /// validate a tx size, signature, account format
        /// </summary>
        /// <param name="pool"></param>
        /// <param name="tx"></param>
        /// <returns></returns>
        public static ValidationError ValidateTx(this ITxPool pool, ITransaction tx)
        {
            if (tx.From == Hash.Zero || tx.MethodName == "" || Parameters.Parser.ParseFrom(tx.Params).Params.Count == 0 
                || tx.IncrementId < 0)
            {
                // TODO: log errors
                return ValidationError.InvalidTxFormat;
            }
            
            // size validation
            if (GetTxSize(tx) > pool.TxLimitSize)
            {
                // TODO: log errors, wrong size
                return ValidationError.TooBigSize;
            }
            
            // TODO: signature validation
            if (!tx.VerifySignature())
            {
                // TODO: log errors, invalid tx signature
                return ValidationError.InvalidSignature;
            }
            
            if(!tx.CheckAccountAddress())
            {
                // TODO: log errors, address error 
                return ValidationError.WrongAddress;
            }
            
            /*// fee validation
            if (tx.Fee < pool.MinimalFee)
            {
                // TODO: log errors, not enough Fee error 
                return false;
            }*/
            
            // TODO : more validations
            return ValidationError.Success;
        }


        /// <summary>
        /// verify signature in tx
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        public static bool VerifySignature(this ITransaction txo)
        {
            var tx = ((Transaction) txo).Clone();
            if (tx.P == null)
            {
                return false;
            }
            try
            {
                byte[] uncompressedPrivKey = tx.P.ToByteArray();
                ECKeyPair recipientKeyPair = ECKeyPair.FromPublicKey(uncompressedPrivKey);
                ECVerifier verifier = new ECVerifier(recipientKeyPair);
                return verifier.Verify(tx.GetSignature(), tx.GetHash().GetHashBytes());

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
        }

        
        /// <summary>
        /// verify address
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        public static bool CheckAccountAddress(this ITransaction tx)
        {
            // TODO: more verifications
            return tx.From.Value.Length == 32 && (tx.To == null || tx.To.Value.Length == 32);
        }
        
       
        
        /// <summary>
        /// return size of given tx
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        private static int GetTxSize(ITransaction tx)
        {
            return tx.Size();
        }
    }
}
