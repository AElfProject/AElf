using System.Linq;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;

namespace AElf.ChainController.TxMemPool
{
    public static class TxValidation
    {
        public enum TxInsertionAndBroadcastingError
        {
            Success = 0,
            AlreadyInserted,
            Valid,
            WrongTransactionType,
            InvalidTxFormat,
            NotEnoughGas,
            TooBigSize,
            WrongAddress,
            InvalidSignature,
            PoolClosed,
            BroadCastFailed,
            Failed,
            AlreadyExecuted,
            InvalidReferenceBlock,
            ExpiredReferenceBlock
        }
        /// <summary>
        /// validate a tx size, signature, account format
        /// </summary>
        /// <param name="pool"></param>
        /// <param name="tx"></param>
        /// <returns></returns>
        public static TxInsertionAndBroadcastingError ValidateTx(this IPool pool, ITransaction tx)
        {
            if (tx.Type != pool.Type)
            {
                // TODO: verifiy corrrectness of dpos 
                return TxInsertionAndBroadcastingError.WrongTransactionType;
            }
            
            if (tx.From == Hash.Zero || tx.MethodName == "" || tx.IncrementId < 0)
            {
                return TxInsertionAndBroadcastingError.InvalidTxFormat;
            }
            
            // size validation
            var size = GetTxSize(tx);
            if (size > pool.TxLimitSize)
            {
                return TxInsertionAndBroadcastingError.TooBigSize;
            }
            
            // TODO: signature validation
            if (!tx.VerifySignature())
            {
                return TxInsertionAndBroadcastingError.InvalidSignature;
            }
            
            if(!tx.CheckAccountAddress())
            {
                return TxInsertionAndBroadcastingError.WrongAddress;
            }
            
            /*// fee validation
            if (tx.Fee < pool.MinimalFee)
            {
                // TODO: log errors, not enough Fee error 
                return false;
            }*/
            
            // TODO : more validations
            return TxInsertionAndBroadcastingError.Valid;
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
