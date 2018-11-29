using AElf.Common;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using GlobalConfig = AElf.Common.GlobalConfig;

namespace AElf.Miner.TxMemPool
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
            ExpiredReferenceBlock,
            KnownTx
        }
        
        
        /// <summary>
        /// verify signature in tx
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        public static bool VerifySignature(this Transaction tx)
        {
            if (tx.Sig == null)
            {
                return false;
            }
            
            // todo warning verification (maybe duplicated in TxSignatureVerifier ??)
//            var addr = Address.FromRawBytes(uncompressedPrivKey);
//            if (!addr.Equals(tx.From))
//                return false;
            
            ECVerifier verifier = new ECVerifier();
            return verifier.Verify(tx.GetSignature(), tx.GetHash().DumpByteArray());
        }

        
        /// <summary>
        /// verify address
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        public static bool CheckAccountAddress(this Transaction tx)
        {
            // TODO: more verifications
            //return tx.From.Value.Length == GlobalConfig.AddressLength && (tx.To == null || tx.To.Value.Length == GlobalConfig.AddressLength);
            return true;
        }
        
       
        
        /// <summary>
        /// return size of given tx
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        private static int GetTxSize(Transaction tx)
        {
            return tx.Size();
        }
    }
}
