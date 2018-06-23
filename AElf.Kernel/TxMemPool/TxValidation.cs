using System;
using System.Linq;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Extensions;
using AElf.Kernel.Types;

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
            if (tx.From == Hash.Zero || tx.MethodName == "" || tx.IncrementId < 0)
            {
                // TODO: log errors
                return ValidationError.InvalidTxFormat;
            }
            
            // size validation
            var size = GetTxSize(tx);
            if (size > pool.TxLimitSize)
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
        public static bool VerifySignature(this ITransaction tx)
        {
            if (tx.P == null)
            {
                return false;
            }
            byte[] uncompressedPrivKey = tx.P.ToByteArray();
            Hash addr = uncompressedPrivKey.Take(ECKeyPair.AddressLength).ToArray();

            if (!addr.Equals(tx.From))
                return false;
            ECKeyPair recipientKeyPair = ECKeyPair.FromPublicKey(uncompressedPrivKey);
            ECVerifier verifier = new ECVerifier(recipientKeyPair);
            return verifier.Verify(tx.GetSignature(), tx.GetHash().GetHashBytes());

        }

        
        /// <summary>
        /// verify address
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        public static bool CheckAccountAddress(this ITransaction tx)
        {
            // TODO: more verifications
            return tx.From.Value.Length == ECKeyPair.AddressLength && (tx.To == null || tx.To.Value.Length == ECKeyPair.AddressLength);
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
