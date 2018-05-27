﻿using System;
using AElf.Kernel.Crypto.ECDSA;

namespace AElf.Kernel.TxMemPool
{
    public static class TxValidation
    {
        
        /// <summary>
        /// validate a tx size, signature, account format
        /// </summary>
        /// <param name="pool"></param>
        /// <param name="tx"></param>
        /// <returns></returns>
        public static bool ValidateTx(this ITxPool pool, ITransaction tx)
        {
            if (tx.From == null || tx.MethodName == "" || Parameters.Parser.ParseFrom(tx.Params).Params.Count == 0 
                || tx.IncrementId < 0)
            {
                // TODO: log errors
                return false;
            }
            
            // size validation
            if (GetTxSize(tx) > pool.TxLimitSize)
            {
                // TODO: log errors, wrong size
                return false;
            }
            
            // TODO: signature validation
            if (!tx.VerifySignature())
            {
                // TODO: log errors, invalid tx signature
                return false;
            }
            
            if(!tx.CheckAccountAddress())
            {
                // TODO: log errors, address error 
                return false;
            }
            
            // fee validation
            if (tx.Fee < pool.MinimalFee)
            {
                // TODO: log errors, not enough Fee error 
                return false;
            }
            
            // TODO : more validations
            return true;
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
